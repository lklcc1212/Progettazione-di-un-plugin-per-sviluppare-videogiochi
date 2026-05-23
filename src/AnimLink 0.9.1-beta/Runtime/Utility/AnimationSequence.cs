namespace AnimLink
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
#endif

    using UnityEngine;
    using UnityEngine.SceneManagement;

    using static UtilityExtension;

    /// <summary>
    /// Animation sequence: manages and executes a series of animations, delays, and actions in order.
    /// </summary>
    public sealed class AnimationSequence
    {
        #region 字段与属性
        // 表示当前正在运行的协程实例，用于启动和停止执行
        private CoroutineScheduler.CoroutineFrame _coroutine = null;

        // 当前的动画项列表
        private readonly List<AnimationItem> _items = new(8);
        /// <summary>
        /// Gets the list of animation items.
        /// This is a read-only list, so external code cannot add, remove, or modify the items directly.
        /// To modify the list, use the methods provided by this class.
        /// </summary>
        public IReadOnlyList<AnimationItem> Items => _items.AsReadOnly();
        // 执行时使用的列表副本，避免修改原列表
        private readonly List<AnimationItem> _copyItems = new(8);
        // 正在执行的项（命令或子协程）集合，用于停止时清理
        private readonly List<object> _executingItems = new(8);
        private int _currentIndex = 0;
        private bool _isPaused = false;
        private bool _isChanged = false;

        private readonly Wait_While _waitWhile = new(null);
        private readonly Wait_ForSeconds _waitForSeconds = new(0);
        private readonly Wait_ForSecondsRealtime _waitForSecondsRealTime = new(0);

        /// <summary>
        /// Indicates whether this animation sequence is currently playing.
        /// </summary>
        public bool IsPlaying => _coroutine != null && !_isPaused;

        /// <summary>
        /// Indicates whether this animation sequence is currently paused.
        /// </summary>
        public bool IsPaused => _isPaused;
        #endregion

        #region 嵌套类型：AnimationItem
        /// <summary>
        /// A single animation queue item, which can be an animation, FunctionPlus call, delay, or regular Action.
        /// </summary>
        public readonly struct AnimationItem
        {
            public enum ItemType
            {
                Animation,  // IAnimation 类型的动画命令
                FunctionPlus,      // FunctionPlus 类型的函数或协程
                Delay,             // 延迟，float 值（秒）
                Action             // 普通 Action 回调
            }

            // 当前项的类型
            public ItemType Type { get; }
            // 是否等待完成或使用实时时间
            public bool WFC_RT { get; }
            // 不同类型的字段，仅对应类型有效
            public readonly IAnimation Animation;
            public readonly FunctionPlus FunctionPlus;
            public readonly float Delay;
            public readonly Action Action;

            // 构造：动画命令
            public AnimationItem(IAnimation command, bool wait)
            {
                Type = ItemType.Animation;
                Animation = command;
                WFC_RT = wait;
                FunctionPlus = null;
                Delay = 0;
                Action = null;
            }
            // 构造：FunctionPlus
            public AnimationItem(FunctionPlus function, bool wait)
            {
                Type = ItemType.FunctionPlus;
                FunctionPlus = function;
                WFC_RT = wait;
                Animation = null;
                Delay = 0;
                Action = null;
            }
            // 构造：延迟
            public AnimationItem(float delay, bool wait)
            {
                Type = ItemType.Delay;
                Delay = delay;
                WFC_RT = wait;
                Animation = null;
                FunctionPlus = null;
                Action = null;
            }
            // 构造：普通 Action
            public AnimationItem(Action action)
            {
                Type = ItemType.Action;
                Action = action;
                WFC_RT = false;
                Animation = null;
                FunctionPlus = null;
                Delay = 0;
            }
        }
        #endregion

        #region 添加项方法 Append
        /// <summary>
        /// Appends an animation to the end of the sequence.
        /// </summary>
        /// <param name="command">The animation to add.</param>
        /// <param name="wait">
        /// If true, the sequence will wait for this command to complete before continuing.
        /// </param>
        public AnimationSequence Append(IAnimation command, bool wait = true)
        {
            if (command != null)
                _items.Add(new(command, wait));
            _isChanged = true;
            return this;
        }

        /// <summary>
        /// Appends a callback <see cref="Action"/> at the end of the sequence.
        /// </summary>
        /// <param name="action">The action to invoke during the sequence.</param>
        public AnimationSequence Append(Action action)
        {
            if (action != null)
                _items.Add(new(action));
            _isChanged = true;
            return this;
        }

        /// <summary>
        /// Appends a delay to the end of the animation sequence.
        /// </summary>
        /// <param name="delay">The duration of the delay in seconds.</param>
        /// <param name="GameTime">
        /// If true, the delay uses game time (affected by Time.timeScale); 
        /// if false, it uses real time (unaffected by time scale).
        /// </param>
        public AnimationSequence Append(float delay, bool GameTime = true)
        {
            _items.Add(new(delay, GameTime));
            _isChanged = true;
            return this;
        }

        /// <summary>
        /// Appends a <see cref="FunctionPlus"/> call to the end of the animation sequence, with the option to wait for completion.
        /// </summary>
        /// <param name="functionPlus">The <see cref="FunctionPlus"/> instance to append.</param>
        /// <param name="wait">
        /// If true, the sequence will wait for the <see cref="FunctionPlus"/> execution to complete before continuing; 
        /// if false, it will continue immediately.
        /// </param>
        public AnimationSequence Append(FunctionPlus functionPlus, bool wait = false)
        {
            if (functionPlus != null)
                _items.Add(new(functionPlus, wait));
            _isChanged = true;
            return this;
        }
        #endregion

        #region 插入项方法 Insert
        /// <summary>
        /// Inserts an animation at the specified index in the sequence.
        /// </summary>
        /// <param name="index">The index at which to insert the command.</param>
        /// <param name="command">The animation to insert.</param>
        /// <param name="wait">
        /// If true, the sequence will wait for the command to complete before continuing; 
        /// if false, it will continue immediately.
        /// </param>
        public AnimationSequence Insert(int index, IAnimation command, bool wait = true)
        {
            if (command != null)
                _items.Insert(index, new AnimationItem(command, wait));
            _isChanged = true;
            return this;
        }

        /// <summary>
        /// Inserts a <see cref="FunctionPlus"/> call at the specified index in the sequence.
        /// </summary>
        /// <param name="index">The index at which to insert the <see cref="FunctionPlus"/> call.</param>
        /// <param name="functionPlus">The <see cref="FunctionPlus"/> instance to insert.</param>
        /// <param name="wait">
        /// If true, the sequence will wait for the <see cref="FunctionPlus"/> execution to complete; 
        /// if false, it will continue immediately.
        /// </param>
        public AnimationSequence Insert(int index, FunctionPlus functionPlus, bool wait = false)
        {
            if (functionPlus != null)
                _items.Insert(index, new AnimationItem(functionPlus, wait));
            _isChanged = true;
            return this;
        }

        /// <summary>
        /// Inserts a delay at the specified index in the sequence.
        /// </summary>
        /// <param name="index">The index at which to insert the delay.</param>
        /// <param name="delay">The duration of the delay in seconds.</param>
        /// <param name="gameTime">
        /// If true, the delay uses game time (affected by Time.timeScale); if false, it uses real time.
        /// </param>
        public AnimationSequence Insert(int index, float delay, bool gameTime = true)
        {
            _items.Insert(index, new AnimationItem(delay, gameTime));
            _isChanged = true;
            return this;
        }

        /// <summary>
        /// Inserts an <see cref="Action"/> at the specified index in the sequence.
        /// </summary>
        /// <param name="index">The index at which to insert the action.</param>
        /// <param name="action">The action to invoke during the sequence.</param>
        public AnimationSequence Insert(int index, Action action)
        {
            if (action != null)
                _items.Insert(index, new AnimationItem(action));
            _isChanged = true;
            return this;
        }
        #endregion

        #region 移除项方法 Remove
        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        public AnimationSequence RemoveAt(int index)
        {
            if (index >= 0 && index < _items.Count)
                _items.RemoveAt(index);
            return this;
        }

        /// <summary>
        /// Removes all items corresponding to the specified animation.
        /// </summary>
        public AnimationSequence Remove(IAnimation command)
        {
            _isChanged = true;
            _items.RemoveAll(item => item.Type == AnimationItem.ItemType.Animation && item.Animation == command);
            return this;
        }

        /// <summary>
        /// Removes all items corresponding to the specified FunctionPlus instance.
        /// </summary>
        public AnimationSequence Remove(FunctionPlus functionPlus)
        {
            _isChanged = true;
            _items.RemoveAll(item => item.Type == AnimationItem.ItemType.FunctionPlus && item.FunctionPlus == functionPlus);
            return this;
        }

        /// <summary>
        /// Removes all items corresponding to the specified Action.
        /// </summary>
        public AnimationSequence Remove(Action action)
        {
            _isChanged = true;
            _items.RemoveAll(item => item.Type == AnimationItem.ItemType.Action && item.Action == action);
            return this;
        }
        #endregion

        #region 清空序列 Clear
        /// <summary>
        /// Clears all items in the animation sequence.
        /// </summary>
        public AnimationSequence Clear()
        {
            _isChanged = true;
            _items.Clear();
            return this;
        }
        #endregion

        #region 动画序列的执行与控制逻辑
        /// <summary>
        /// 内部执行协程：按顺序执行各项并根据类型处理等待。
        /// </summary>
        private IEnumerator ExecuteAnimationSequence()
        {
            yield return null; // 确保在下一帧开始执行，允许外部调用者获取到协程信息

            _copyItems.Clear();
            _copyItems.AddRange(_items);

            for (int i = _currentIndex; i < _copyItems.Count; i++)
            {
                if (_coroutine == null)
                    yield break;

                AnimationItem item = _copyItems[i];

                switch (item.Type)
                {
                    case AnimationItem.ItemType.Animation:
                        // 执行动画命令并根据 WFC_RT 决定是否等待
                        IAnimation animation;
                        _executingItems.Add(animation = item.Animation);
                        animation
                            .AddOnComplete(() => _executingItems.Remove(animation))
                            .Play();

                        // 如果没有成功执行，则去除。
                        if (!animation.IsPlaying) _executingItems.Remove(animation);

                        if (item.WFC_RT)
                        {
                            //yield return new WaitWhile(() => animation.IsPlaying);
                            _waitWhile.Predicate = () => animation.IsPlaying;
                            yield return _waitWhile;
                        }

                        break;
                    case AnimationItem.ItemType.FunctionPlus:
                        // 执行 FunctionPlus，可为协程或普通方法
                        FunctionPlus functionPlus = item.FunctionPlus;
                        if (functionPlus._isIEnumerator)
                        {
                            if (item.WFC_RT)
                                yield return functionPlus.GetIEnumerator();
                            else
                            {
                                _executingItems.Add(CoroutineScheduler.StartCoroutine(functionPlus.GetIEnumerator(), (frame) => _executingItems.Remove(frame)));
                            }
                        }
                        else
                            functionPlus.InvokeMethod();
                        break;
                    case AnimationItem.ItemType.Delay:
                        // 延迟：游戏时间或真实时间
                        if (item.WFC_RT)
                        {
                            _waitForSeconds.Seconds = item.Delay;
                            yield return _waitForSeconds;
                        }
                        else
                        {
                            _waitForSecondsRealTime.Seconds = item.Delay;
                            yield return _waitForSecondsRealTime;
                        }
                        //yield return item.WFC_RT
                        //    ? new WaitForSeconds(item.Delay)
                        //    : new WaitForSecondsRealtime(item.Delay);
                        break;
                    case AnimationItem.ItemType.Action:
                        // 调用回调
                        item.Action?.Invoke();
                        break;
                }
                _currentIndex++;
            }

            yield return new Wait_While(() => _executingItems.Count > 0);

            _coroutine = null;
        }

        /// <summary>
        /// Starts executing the animation sequence. 
        /// If the sequence is already running or empty, this call will be ignored.
        /// </summary>
        public void Play()
        {
            bool flag = _coroutine != null && _coroutine.IsRunning && !_coroutine.Paused;
            if (flag || _items.Count == 0)
                return;

            if (!flag)
                Stop(); // 确保之前的执行完全停止

            _isChanged = false;
            _isPaused = false;
            _currentIndex = 0;
            _executingItems.Clear();
            _coroutine = CoroutineScheduler.StartCoroutine(ExecuteAnimationSequence());
        }

        /// <summary>
        /// Stops the currently executing sequence and all running functions.
        /// </summary>
        public void Stop()
        {
            if (_coroutine == null) return;

            CoroutineScheduler.StopCoroutine(_coroutine);
            foreach (var item in _executingItems)
            {
                if (item is IAnimation animation)
                {
                    animation.Stop();
                }
                else if (item is CoroutineScheduler.CoroutineFrame coroutine)
                {
                    CoroutineScheduler.StopCoroutine(coroutine);
                }
            }
            _executingItems.Clear();
            _coroutine = null;
        }

        /// <summary>
        /// Pauses the currently executing sequence. 
        /// </summary>
        public void Pause()
        {
            if (_coroutine == null) return;

            CoroutineScheduler.PauseCoroutine(_coroutine);
            foreach (var item in _executingItems)
            {
                if (item is IAnimation animation)
                {
                    animation.Stop();
                }
                else if (item is CoroutineScheduler.CoroutineFrame coroutine)
                {
                    CoroutineScheduler.PauseCoroutine(coroutine);
                }
            }
            _isPaused = true;
        }

        /// <summary>
        /// Resumes execution of the sequence from the paused state. 
        /// If the sequence has been modified after pausing, a warning will be logged.
        /// </summary>
        public void Resume()
        {
            if (_coroutine == null || !_isPaused) return;

            if (_isChanged)
            {
                LogWarning("[AnimationSequence] The sequence has been changed after pausing.");
                return;
            }

            CoroutineScheduler.ResumeCoroutine(_coroutine);
            foreach (var item in _executingItems)
            {
                if (item is IAnimation animation)
                {
                    animation.Resume();
                }
                else if (item is CoroutineScheduler.CoroutineFrame coroutine)
                {
                    CoroutineScheduler.ResumeCoroutine(coroutine);
                }
            }
            _isPaused = false;
        }
        #endregion

        #region Editor Preview
#if UNITY_EDITOR
        private ushort _editorID = ushort.MinValue;
        private readonly ManualDeltaTime _manualDeltaTime = new();
        private CancellationTokenSource _animSequenceTokenSource;
        private readonly List<CancellationTokenSource> _animsPreviewTokenSource = new();
        private int _progress = 0;
        private string _executorName = string.Empty;
        private float _delayElapsedTime = 0;
        private IAnimation _currentAnimCommand = null;

        private void InitializeEditorPreview()
        {
            _progress = 0;
            _copyItems.Clear();
            _copyItems.AddRange(_items);
            _manualDeltaTime.Reset();
            _currentAnimCommand = null;
            _delayElapsedTime = 0;
            _animsPreviewTokenSource.Clear();
        }

        internal bool UpdateEditorPreview(ushort id)
        {
            if (_animSequenceTokenSource.IsCancellationRequested)
                return false;

            float deltaTime = _manualDeltaTime.GetManualDeltaTime();

            // 1. 执行未完成的 AnimationItem
            if (_progress < _copyItems.Count)
            {
                var item = _copyItems[_progress];

                switch (item.Type)
                {
                    case AnimationItem.ItemType.Animation:
                        {
                            // 如果当前没有动画，则启动这一项
                            if (_currentAnimCommand == null)
                            {
                                var cmd = item.Animation;

                                _executingItems.Add(cmd);

                                var token = new CancellationTokenSource();
                                _animsPreviewTokenSource.Add(token);

                                _currentAnimCommand = cmd;

                                cmd.AddOnComplete(() =>
                                {
                                    _executingItems.Remove(cmd);
                                }).PlayEditorPreview(token, false, false);

                                // 如果没有成功执行，则去除。
                                if (!cmd.IsPlaying)
                                    _executingItems.Remove(cmd);
                            }

                            // 如果需要等待当前动画执行完（WFC_RT）
                            if (item.WFC_RT && _currentAnimCommand.IsPlaying)
                                return true;

                            // 当前动画执行完了，则清空引用
                            _currentAnimCommand = null;
                            break;
                        }

                    case AnimationItem.ItemType.Delay:
                        {
                            _delayElapsedTime += deltaTime;

                            if (_delayElapsedTime < item.Delay)
                                return true;

                            _delayElapsedTime = 0f;
                            break;
                        }
                }

                _progress++;
                return true;
            }

            // 2. 所有进度已跑完，但仍有未完成动画
            if (_executingItems.Count > 0)
                return true;

            // 3. 全部执行完毕
            _animSequenceTokenSource.Cancel();
            return false;
        }


        private Func<ushort, bool> PrepareEditorPreview()
        {
            InitializeEditorPreview();

            return UpdateEditorPreview;
        }

        internal void StartEditorPreview(CancellationTokenSource animSequenceTokenSource, bool resetAfterPreview = false, string executorName = "")
        {
            if (_editorID != ushort.MinValue || _items.Count == 0)
            {
                animSequenceTokenSource.Cancel();
                return;
            }

            if (_items.Any(e => e.Type is AnimationItem.ItemType.FunctionPlus or AnimationItem.ItemType.Action))
            {
                Debug.LogWarning("[EditorPreview] AnimationSequence contains FunctionPlus or Action items which are not supported in editor preview.");
                animSequenceTokenSource.Cancel();
                return;
            }

            _executorName = executorName;
            _animSequenceTokenSource = animSequenceTokenSource;
            _animSequenceTokenSource.Token.Register(() =>
            {
                if (_editorID != 0)
                    ResetEditorPreview(resetAfterPreview);
            });
            _editorID = FrameScheduler.StartEditor(PrepareEditorPreview());

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.wantsToQuit += EditorWantQuit;
            BuildProcessor.BuildAction += OnBuild;
            EditorSceneManager.sceneClosing += OnSceneClosing;
        }

        private void OnSceneClosing(Scene scene, bool removingScene)
        {
            Debug.LogWarning("[EditorPreview] Please stop the Editor Preview before switching scenes.");
            ResetEditorPreview();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            ResetEditorPreview();
        }

        private bool EditorWantQuit()
        {
            string msg = "[EditorPreview] Cannot exit the editor while animation preview is active. Please stop the preview first.";

            if (!string.IsNullOrEmpty(_executorName))
                msg += $" Executor name: {_executorName}";

            Debug.LogWarning(msg);
            return false;
        }

        private void OnBuild()
        {
            ResetEditorPreview();
        }

        private void ResetEditorPreview(bool resetAfterPreview = true)
        {
            if (_editorID == 0)
                return;

            FrameScheduler.StopEditor(_editorID);
            _editorID = 0;

            _animSequenceTokenSource?.Cancel();
            foreach (var tokenSource in _animsPreviewTokenSource)
            {
                tokenSource.Cancel();
            }
            _animsPreviewTokenSource.Clear();

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.wantsToQuit -= EditorWantQuit;
            BuildProcessor.BuildAction -= OnBuild;
            EditorSceneManager.sceneClosing -= OnSceneClosing;

            //重置至最初状态
            if (resetAfterPreview)
            {
                // 收集动画命令
                IAnimation[] animations = _copyItems
                    //只获取执行过的
                    .Take(_progress + 1)
                    .Where(e => e.Type == AnimationItem.ItemType.Animation)
                    .Select(e => e.Animation)
                    .ToArray();

                //安全重置
                for (int i = animations.Length - 1; i >= 0; i--)
                {
                    animations[i].Reset();
                }
            }

            UnityEngine.Object firstTObject = _items.FirstOrDefault((a) => a.Animation != null && a.Animation.TargetObject != null).Animation?.TargetObject;
            if (firstTObject != null)
                SceneUtils.MarkDirtyAndSaveScene(firstTObject);
        }
#endif
        #endregion
    }
}
