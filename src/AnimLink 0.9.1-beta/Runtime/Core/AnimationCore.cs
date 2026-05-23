namespace AnimLink
{
    using System;
    using System.Threading;
    using System.Collections.Generic;

    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
#endif
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    using static UtilityExtension;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Generic base class for animations, used to manage animations
    /// on supported components in Unity. Provides full lifecycle control and editor preview support.
    /// </summary>
    /// <typeparam name="TSelf">The type that inherits from this base class.</typeparam>
    /// <typeparam name="TValue">The type of value being animated (e.g. Vector3, float, Color).</typeparam>
    public abstract class AnimationCore<TSelf, TValue> : IAnimation where TSelf : AnimationCore<TSelf, TValue>
    {
        #region Fields

        private event Action OnComplete;

        protected ushort _ID = ushort.MinValue;

        protected Transform Transform;

        /// <summary>
        /// 缓存的 <see cref="Loops"/> - 1，用于快速判断循环结束。
        /// </summary>
        protected int _lastLoopIndex;
        protected int _loops = 0;
        /// <summary>
        /// 执行次数计数器
        /// </summary>
        protected int _loopCounter = 0;

        protected float _elapsedTime;
        /// <summary>
        /// 如果总时间大于0，则表示动画执行过。
        /// </summary>
        protected float _totalElapsedTime;

        /// <summary>
        /// 当前循环总时间（0 到 Duration）
        /// </summary>
        protected float _currentLoopTime;

        /// <summary>
        /// 用于内部使用
        /// <para>注：当LoopType等于PingPong时，Duration会被除二（往/返）。</para>
        /// </summary>
        protected float _duration;

        /// <summary>
        /// 缓动类型。默认: Ease.Linear (Ease.Linear, Ease.InSine, Ease.OutSine, ...)
        /// </summary>
        protected Ease _ease = Ease.Linear;

        /// <summary>
        /// 是否反向播放
        /// </summary>
        protected bool _isReversed;
        protected bool _playBackward = false;
        protected bool _isInfLoops = false;

        /// <summary>
        /// 指示动画是否已正向播放完成且参数未发生变化。
        /// <para>若为 <see langword="true"/>，表示可以调用 <see cref="PlayBackward"/>；反之亦然。</para>
        /// </summary>
        protected bool _finishedPlayingBackward = false;

        /// <summary>
        /// 表示动画是否已执行过一次（通过 <see cref="Play"/> 或 
        /// <see cref="StartEditorPreview(CancellationTokenSource, bool, bool, string)"/>）。
        /// <para>该状态会在 <see cref="ValidateTargetAndAssign(Object, string)"/> 中被重置。</para>
        /// </summary>
        protected bool _initialized;

        /// <summary>
        /// 动画当前是否正在执行，同时表示参数是否可修改：
        /// <br>true  = 动画正在运行，参数不可修改</br>
        /// <br>false = 动画未运行，参数可修改</br>
        /// </summary>
        protected bool _isLocking;

        private Func<float> _getDeltaTime;

        // 支持的组件
        protected SpriteRenderer _spriteRenderer;
        protected CanvasGroup _canvasGroup;
        protected Graphic _graphic;
        protected bool _isTmpText;
        protected MeshRenderer _meshRenderer;
        protected int _materialIndex = 0;
        protected Material _material;

        //UpdateAnimation
        protected readonly Func<ushort, bool> _updateAnimationFunc;

        /// <summary>
        /// 动画停止条件集合。
        /// 每个条件函数都会在每帧中被检查；
        /// 当任意函数返回 true 时，动画会被立即停止。
        /// </summary>
        private readonly List<Func<bool>> _stopWhenConditions = new();

        /// <summary>
        /// 每帧检测的条件回调
        /// </summary>
        private readonly List<CallEveryFrame> _callEveryFrame = new();

        /// <summary>
        /// 仅触发一次的条件回调
        /// </summary>
        private readonly List<CallWhenCondition> _rawCallWhenOnce = new();
        private readonly List<CallWhenCondition> _tmpCallWhenOnce = new();

#if UNITY_EDITOR
        internal ManualDeltaTime _manualDeltaTime;
        protected bool _isNotEditorPreview;
        private CancellationTokenSource _animPreviewTokenSource;
        private bool _shouldResetOnEvent;
        private string _executorName = string.Empty;
#endif

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether the animation is currently playing.
        /// </summary>
        public bool IsPlaying => _isLocking;

        /// <summary>
        /// Current loop time of the animation.
        /// <br></br>
        /// The value is always clamped within [0, <see langword="Duration"/>] for each loop.
        /// </summary>
        public float CurrentLoopTime => Mathf.Clamp(_currentLoopTime, 0, Duration);

        /// <summary>
        /// The user-defined duration of the animation.
        /// </summary>
        public float Duration { get; protected set; }

        /// <summary>
        /// Total elapsed time since the animation started (always increasing).
        /// </summary>
        public float ElapsedTime => _totalElapsedTime;

        /// <summary>
        /// Indicates whether the animation is updated using fixed delta time (e.g., FixedUpdate).
        /// </summary>
        public bool UseFixedDTime { get; protected set; }

        /// <summary>
        /// Number of loops the animation will perform.
        /// </summary>
        public int Loops { get; protected set; } = 1;

        /// <summary>
        /// The target Unity object that the animation affects.
        /// </summary>
        public Object TargetObject { get; protected set; }

        /// <summary>
        /// Indicates whether the animation is currently playing in reverse.
        /// </summary>
        public bool IsPlayingBackward => _playBackward && _isLocking;

        /// <summary>
        /// Gets the normalized progress of the animation in the range <b>[0, 1]</b>.
        /// <br/><br/>
        /// For <b>finite loops</b>, the progress increases linearly <b>from 0 to 1</b> across all loops.
        /// If <b>Loops ≤ -1</b> (infinite looping), this property <b>returns -1</b>
        /// because a normalized progress cannot be defined.
        /// </summary>
        public virtual float Progress
        {
            get
            {
                if (Loops < 0) return -1f; // 无限循环

                return Mathf.Clamp01((_currentLoopTime + Duration * _loops) / (Duration * Loops));
            }
        }

        #endregion

        #region Constructor

        public AnimationCore()
        {
            _updateAnimationFunc = AnimationWrapper;
        }

        #endregion

        #region ResetFields

        /// <summary>
        /// 重置字段(ElapsedTime, _isReversed 和 Loops)。
        /// </summary>
        protected void ResetFields()
        {
            _isReversed = false;
            _currentLoopTime = 0;
            _loopCounter = 0;
            _elapsedTime = 0;
            _totalElapsedTime = 0;
            _loops = 0;
            _tmpCallWhenOnce.Clear();
            _tmpCallWhenOnce.AddRange(_rawCallWhenOnce);
        }

        #endregion

        #region DeltaTime

        /// <summary>
        /// 把 deltaTime 获取逻辑预先缓存到委托中
        /// </summary>
        private void InitDeltaTimeMode()
        {
#if UNITY_EDITOR
            if (_isNotEditorPreview)
            {
                if (UseFixedDTime)
                    _getDeltaTime = () => Time.fixedDeltaTime;
                else
                    _getDeltaTime = () => Time.deltaTime;
            }
            else
            {
                _manualDeltaTime = new();
                _getDeltaTime = () => _manualDeltaTime.GetManualDeltaTime();
            }
#else
            if (UseFixedDTime)
                _getDeltaTime = () => Time.fixedDeltaTime;
            else
                _getDeltaTime = () => Time.deltaTime;
#endif
        }

        /// <summary>
        /// 更新ElapsedTime
        /// </summary>
        protected void UpdateElapsedTime()
        {
            // 计算本帧的时间增量（自动考虑 TimeScale）
            float deltaTime = _getDeltaTime();

            _totalElapsedTime += deltaTime; // 总时间应始终为正增长

            // 如果倒放，则取反
            if (_playBackward)
                deltaTime = -deltaTime;

            _elapsedTime += deltaTime;

            _currentLoopTime += deltaTime;
        }

        /// <summary>
        /// Configures the animation to update using a fixed delta time (FixedUpdate) or normal delta time (Update).
        /// </summary>
        /// <param name="useFixedDeltaTime">
        /// If true, uses Time.fixedDeltaTime; otherwise, uses Time.deltaTime.
        /// </param>
        public virtual TSelf UseFixedDeltaTime(bool useFixedDeltaTime)
        {
            UseFixedDTime = useFixedDeltaTime;
            return (TSelf)this;
        }

        #endregion

        #region OnComplete Callbacks

        /// <summary>
        /// Registers a callback to be invoked when the animation completes.
        /// </summary>
        public TSelf AddOnComplete(Action action)
        {
            OnComplete += action;
            return (TSelf)this;
        }

        /// <summary>
        /// Unregisters a callback, so it will no longer be called when the animation completes.
        /// </summary>
        public TSelf RemoveOnComplete(Action action)
        {
            OnComplete -= action;
            return (TSelf)this;
        }

        /// <summary>
        /// Clears all registered completion callbacks.
        /// </summary>
        public TSelf ClearOnComplete()
        {
            OnComplete = null;
            return (TSelf)this;
        }

        /// <summary>
        /// 动画完成后调用。
        /// 调用完成回调函数，重置内部 ID，解锁动画，并重置循环计数器。
        /// </summary>
        protected void Completed()
        {
            OnComplete?.Invoke();

            // 如果是反向播，则false，否则true
            _finishedPlayingBackward = !_playBackward;

            _ID = ushort.MinValue;

            _isLocking = false;
        }

        #endregion

        #region Condition Callback & Frame Callback

        /// <summary>
        /// Registers a condition that will stop the animation when it returns <see langword="true"/>.
        /// <para>
        /// Once the animation is stopped by this condition, it can still be resumed later by calling 
        /// <see cref="Resume"/> or <see cref="PlayBackward"/>.
        /// </para>
        /// </summary>
        /// <param name="condition">
        /// A callback that returns <see langword="true"/> to trigger the stop.
        /// </param>
        public TSelf AddStopWhen(Func<bool> condition)
        {
            if (condition != null && !_stopWhenConditions.Contains(condition))
                _stopWhenConditions.Add(condition);
            return (TSelf)this;
        }

        /// <summary>
        /// Removes a previously registered stop condition.
        /// </summary>
        /// <param name="condition">The condition to remove.</param>
        public TSelf RemoveStopWhen(Func<bool> condition)
        {
            _stopWhenConditions.Remove(condition);
            return (TSelf)this;
        }

        /// <summary>
        /// Registers a callback to be executed once when a specified condition is met.
        /// <para>
        /// The callback will be executed only the first time the <paramref name="condition"/> returns true.
        /// Subsequent evaluations of the condition will not trigger the callback again.
        /// </para>
        /// </summary>
        /// <param name="condition">A function returning true when the callback should be triggered.</param>
        /// <param name="callback">The action to execute when the condition is met.</param>
        /// <param name="id">
        /// Outputs a unique identifier for this callback, which can later be used to remove it.
        /// A value of 0 indicates an invalid ID and cannot be used for removal.
        /// </param>
        public TSelf AddCallWhenOne(Func<bool> condition, Action callback, out ushort id)
        {
            id = 0;
            if (condition == null || callback == null)
                return (TSelf)this;

            _rawCallWhenOnce.Add(new CallWhenCondition(condition, callback));

            id = _rawCallWhenOnce[^1].id;
            return (TSelf)this;
        }

        /// <summary>
        /// Registers a callback to be executed once when a specified condition is met.
        /// <para>
        /// The callback will be executed only the first time the <paramref name="condition"/> returns true.
        /// Subsequent evaluations of the condition will not trigger the callback again.
        /// </para>
        /// </summary>
        /// <param name="condition">A function returning true when the callback should be triggered.</param>
        /// <param name="callback">
        /// The action to execute when the condition is met. 
        /// The callback receives the unique ID of this registration as a parameter.
        /// </param>
        /// <param name="id">
        /// Outputs a unique identifier for this callback, which can later be used to remove it.
        /// A value of 0 indicates an invalid ID and cannot be used for removal.
        /// </param>
        public TSelf AddCallWhenOne(Func<bool> condition, Action<ushort> callback, out ushort id)
        {
            id = 0;
            if (condition == null || callback == null)
                return (TSelf)this;

            _rawCallWhenOnce.Add(new CallWhenCondition(condition, callback));

            id = _rawCallWhenOnce[^1].id;
            return (TSelf)this;
        }

        /// <summary>
        /// Removes a previously registered one-time callback by its unique identifier.
        /// <para>
        /// This only affects callbacks registered via <see cref="AddCallWhenOne(Func{bool}, Action, out ushort)"/> 
        /// or <see cref="AddCallWhenOne(Func{bool}, Action{ushort}, out ushort)"/>.
        /// Once removed, the callback will no longer be executed, even if its condition returns true.
        /// </para>
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the callback returned when it was added. 
        /// A value of 0 indicates an invalid ID and will be ignored.
        /// </param>
        public TSelf RemoveCallWhenOnce(ushort id)
        {
            if (id == 0)
                return (TSelf)this;

            for (int i = _rawCallWhenOnce.Count - 1; i >= 0; i--)
            {
                if (_rawCallWhenOnce[i].id == id)
                {
                    _rawCallWhenOnce.RemoveAt(i);
                    break;
                }
            }

            return (TSelf)this;
        }

        /// <summary>
        /// Registers a callback to be executed every frame.
        /// <para>
        /// The callback will be invoked on every frame until it is removed.
        /// </para>
        /// </summary>
        /// <param name="action">The action to execute every frame.</param>
        /// <param name="id">
        /// Outputs a unique identifier for this callback, which can later be used to remove it.
        /// A value of 0 indicates an invalid ID and cannot be used for removal.
        /// </param>
        public TSelf AddCallEveryFrame(Action action, out ushort id)
        {
            id = 0;
            if (action == null)
                return (TSelf)this;

            _callEveryFrame.Add(new(action));

            id = _callEveryFrame[^1].id;
            return (TSelf)this;
        }

        /// <summary>
        /// Registers a callback to be executed every frame, receiving its unique ID.
        /// <para>
        /// The callback will be invoked on every frame until it is removed.
        /// </para>
        /// </summary>
        /// <param name="action">The action to execute every frame. Receives the callback's unique ID as a parameter.</param>
        /// <param name="id">
        /// Outputs a unique identifier for this callback, which can later be used to remove it.
        /// A value of 0 indicates an invalid ID and cannot be used for removal.
        /// </param>
        public TSelf AddCallEveryFrame(Action<ushort> action, out ushort id)
        {
            id = 0;
            if (action == null)
                return (TSelf)this;

            _callEveryFrame.Add(new(action));

            id = _callEveryFrame[^1].id;
            return (TSelf)this;
        }

        /// <summary>
        /// Removes a previously registered callback that executes every frame by its unique identifier.
        /// <para>
        /// This only affects callbacks registered via <see cref="AddCallEveryFrame(Action, out ushort)"/> 
        /// or <see cref="AddCallEveryFrame(Action{ushort}, out ushort)"/>.
        /// Once removed, the callback will no longer be executed on subsequent frames.
        /// </para>
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the callback returned when it was added. 
        /// A value of 0 indicates an invalid ID and will be ignored.
        /// </param>
        public TSelf RemoveCallEveryFrame(ushort id)
        {
            if (id == 0)
                return (TSelf)this;

            for (int i = _callEveryFrame.Count - 1; i >= 0; i--)
            {
                if (_callEveryFrame[i].id == id)
                {
                    _callEveryFrame.RemoveAt(i);
                    break;
                }
            }

            return (TSelf)this;
        }

        /// <summary>
        /// Clears all registered callbacks that are executed every frame.
        /// </summary>
        public TSelf ClearCallEveryFrame()
        {
            _callEveryFrame.Clear();
            return (TSelf)this;
        }

        /// <summary>
        /// Clears all registered one-time callbacks.
        /// </summary>
        public TSelf ClearCallWhenOnce()
        {
            _rawCallWhenOnce.Clear();
            return (TSelf)this;
        }

        /// <summary>
        /// Clears all registered stop conditions.
        /// </summary>
        public TSelf ClearStopWhen()
        {
            _stopWhenConditions.Clear();
            return (TSelf)this;
        }

        private void CheckCall()
        {
            // 每帧检查
            for (int i = _callEveryFrame.Count - 1; i >= 0; i--)
            {
                _callEveryFrame[i].Invoke();
            }

            // 仅执行一次的检查
            for (int i = _tmpCallWhenOnce.Count - 1; i >= 0; i--)
            {
                var callWhen = _tmpCallWhenOnce[i];
                if (callWhen.condition())
                {
                    callWhen.Invoke();
                    _tmpCallWhenOnce.RemoveAt(i);
                }
            }
        }

        private bool CheckStopWhenConditions()
        {
            for (int i = _stopWhenConditions.Count - 1; i >= 0; i--)
            {
                if (_stopWhenConditions[i]())
                {
                    Stop();
                    return false;
                }
            }
            return true;
        }


        #endregion

        #region Playback Control

        /// <summary>
        /// Resumes animation playback.
        /// <br></br><br>Requirements:</br>
        /// <br>- <see cref="Play()"/> has been called at least once.</br>
        /// <br>- The animation has been stopped via <see cref="Stop()"/>.</br>
        /// <br>- The animation has <b>not yet finished</b>.</br>
        /// <br>- No parameter-modifying methods (such as <c>ConfigXXX</c> / <c>SetXXX</c>) 
        /// have been called after stopping, regardless of whether they succeeded,
        /// <b>except</b> for:
        /// <br><see cref="SetDuration(float)"/></br>
        /// <br><see cref="DoPath.SetPathRotation(bool, Vector3?, bool)"/></br>
        /// <br><see cref="DoPath.SetRotDimension(Dimension)"/></br></br>
        /// </summary>
        public void Resume()
        {
            if (!VerifySetup() || _ID == ushort.MinValue || _isLocking)
                return;

            StartAnimation();
        }

        /// <summary>
        /// Starts animation playback from the beginning or after completion/stop.
        /// <br>Can be executed again only after completion or stop.</br>
        /// <br>It is recommended to call this method only after configuring all necessary settings.</br>
        /// </summary>
        public TSelf Play()
        {
            if (!VerifySetup() || _isLocking || Loops == 0)
                return (TSelf)this;

            _initialized = true;

            ResetFields();
            StartAnimation();

            return (TSelf)this;
        }

        /// <summary>
        /// Stops or pauses the animation.
        /// </summary>
        public void Stop()
        {
            if (_ID != ushort.MinValue)
#if UNITY_EDITOR
                if (_isNotEditorPreview)
                {
#endif
                    if (UseFixedDTime)
                        FixedScheduler.StopFixedUpdate(_ID);
                    else
                        FrameScheduler.StopUpdate(_ID);
#if UNITY_EDITOR
                }
                else
                {
                    FrameScheduler.StopEditor(_ID);
                }
#endif
            _isLocking = false;
        }

        #endregion

        #region Validation & Safety

        /// <summary>
        /// 执行 <see cref="Play"/> / <see cref="Resume"/> 之前的基本检查。可重写。
        /// </summary>
        protected virtual bool VerifySetup()
        {
            if (!TargetObject)
            {
                LogWarning($"[{GetType().Name}] TargetObject is null.");
                return false;
            }

            if (Duration <= 0)
            {
                LogWarning($"[{GetType().Name}] Duration is less than or equal to 0.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查动画是否正在播放。
        /// </summary>
        /// <returns>true = 动画正在播放；false = 动画未播放</returns>
        protected bool CheckPlaying()
        {
            if (_isLocking)
            {
                LogWarning($"[{GetType().Name}] Cannot set up while playing.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查动画是否正在播放，并决定是否可以修改参数。
        /// 如果动画未播放，将 <see cref="_ID"/>和<see cref="_finishedPlayingBackward"/> 重置，防止 <see cref="Resume"/>/<see cref="PlayBackward"/> 被调用。
        /// </summary>
        /// <returns>true = 动画正在播放，不可修改；false = 可以修改</returns>
        protected bool CheckPlayingAndResetID()
        {
            if (CheckPlaying())
                return true;

            _ID = ushort.MinValue;
            _finishedPlayingBackward = false;
            return false;
        }

        /// <summary>
        /// 验证目标对象是否有效，并检查是否发生变化。
        /// <para>如果 <paramref name="target"/> 为 <c>null</c>，方法会返回 <c>false</c>，并可记录警告日志。</para>
        /// <para>如果 <paramref name="target"/> 与当前 <see cref="TargetObject"/> 不同，
        /// 会将 <c>_initialized</c> 设为 <c>false</c>，避免后续 <see cref="Reset"/> 对旧对象生效。</para>
        /// <para>通常应在 <c>ConfigXXX(...)</c> 方法中调用，或在切换动画类型（如从 Position 切换到 Scale）时调用。</para>
        /// </summary>
        /// <returns>
        /// 如果目标对象有效，返回 <c>false</c> 并将 <see cref="TargetObject"/> 更新为该对象；
        /// 否则返回 <c>true</c>。
        /// </returns>
        protected bool ValidateTargetAndAssign(Object target, string parameterName = "")
        {
            if (!target)
            {
                LogWarning($"[{GetType().Name}] The {parameterName} parameter is null.");
                return true;
            }

            if (target != TargetObject)
                _initialized = false;

            TargetObject = target;
            return false;
        }

        #endregion

        #region Start Animation

        private bool AnimationWrapper(ushort id)
        {
            // 检查 StopWhen 条件
            if (!CheckStopWhenConditions())
                return false;

            // 检查 Call 条件（每帧或一次性回调）
            CheckCall();

            if (!_isLocking || !TargetObject)
                return false;

            bool shouldContinue = UpdateAnimation(); // 更新动画

            if (!shouldContinue)
                Completed(); // 完成时

            return shouldContinue; // 是否继续动画 （true = 继续； false = 停止）
        }

        /// <summary>
        /// 统一准备并启动动画。
        /// </summary>
        protected void StartAnimation(bool playBackward = false, bool isNotEditorPreview = true)
        {
            _isLocking = true;
            _playBackward = playBackward;

#if UNITY_EDITOR
            _isNotEditorPreview = isNotEditorPreview;
#endif
            InitDeltaTimeMode();
            PrepareAnimation();
            RunAnimation();
        }

        /// <summary>
        /// 启动动画执行。
        /// </summary>
        private void RunAnimation()
        {
            ushort id;
#if  UNITY_EDITOR
            if (_isNotEditorPreview)
            {
#endif
                id = UseFixedDTime ? FixedScheduler.StartFixedUpdate(_updateAnimationFunc)
                                    : FrameScheduler.StartUpdate(_updateAnimationFunc);
#if  UNITY_EDITOR
            }
            else
            {
                id = FrameScheduler.StartEditor(_updateAnimationFunc);
            }
#endif
            _ID = id;
        }

        #endregion

        #region Settings

        /// <summary>
        /// Sets the duration of the animation in seconds. <strong>Valid range: (0, +∞]</strong>.
        /// </summary>
        public virtual TSelf SetDuration(float duration)
        {
            if (duration <= 0 || Mathf.Approximately(duration, Duration))
                return (TSelf)this;

            RescaleDuration(duration);
            return (TSelf)this;
        }

        protected void RescaleDuration(float newDuration)
        {
            bool isPingPong = Mathf.Approximately(Duration, _duration * 2f);
            float newSingleDuration = newDuration * (isPingPong ? 0.5f : 1f);

            if (_isLocking || _ID != ushort.MinValue)
            {
                Duration = newDuration;
                _duration = newSingleDuration;
                return;
            }

            float oldSingleDuration = _duration;
            float progress = _elapsedTime / oldSingleDuration;
            float loopProgress = _currentLoopTime / Duration;

            _elapsedTime = progress * newSingleDuration;
            _currentLoopTime = loopProgress * newDuration;
        }

        /// <summary>
        /// Sets the target object for this animation.
        /// </summary>
        /// <param name="target">The target object to assign.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the target was successfully assigned; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool SetTarget(Object target)
        {
            if (CheckPlayingAndResetID()) return false;

            if (ValidateTargetAndAssign(target, "target")) return false;

            if (!IsTargetValid(target))
            {
                LogWarning($"[{GetType().Name}] Unsupported target object type.");
                return false;
            }
            SetTargetInternal(target);
            return true;
        }

        /// <summary>
        /// 检查目标对象是否有效。
        /// 子类可以重写此方法，定义自己的目标类型判断规则。
        /// </summary>
        /// <param name="target">待验证的目标对象。</param>
        /// <returns>如果目标有效返回 true，否则返回 false。</returns>
        protected virtual bool IsTargetValid(Object target)
        {
            return target is Transform;
        }

        /// <summary>
        /// 设置内部目标对象。
        /// 子类可以重写此方法，保存自己的目标引用或执行额外初始化。
        /// </summary>
        /// <param name="target">已通过校验的目标对象。</param>
        protected virtual void SetTargetInternal(Object target)
        {
            Transform = (Transform)target;
        }

        #endregion

        #region UpdateLoopState

        /// <summary>
        /// 更新循环状态，并判断动画是否应继续播放。
        /// </summary>
        /// <param name="isNotPingPong">是否不是 PingPong 模式。</param>
        /// 
        /// <returns>
        /// 返回 <c>true</c> 表示继续执行动画下一帧，  
        /// 返回 <c>false</c> 表示当前循环已经结束（到达首或尾部）。
        /// </returns>
        protected bool UpdateLoopState(bool isNotPingPong)
        {
            bool backward = _playBackward;

            // 判断是否需要调整循环次数
            if (!(isNotPingPong || (backward != _isReversed)))
                return true; // 不调整，直接继续下一帧

            // 获取delta
            int delta = backward ? -1 : 1;

            // 判断是否到达边界
            if (backward)
            {
                if (_loopCounter <= 0)
                    return false; // 循环结束

                // 未结束：重置当前循环时间（内部逻辑使用，可超过 [0, Duration]）
                _currentLoopTime += Duration;
            }
            else
            {
                if (_loops >= _lastLoopIndex)
                    return false; // 循环结束

                // 未结束：重置当前循环时间（内部逻辑使用，可超过 [0, Duration]）
                _currentLoopTime -= Duration;
            }

            // 更新计数器
            _loopCounter += delta;

            if (!_isInfLoops)
                _loops += delta;

            return true;  // 继续执行下一帧
        }


        #endregion

        #region Abstract Methods

        /// <summary>
        /// Sets the target value (goal) for this animation.
        /// </summary>
        /// <param name="goal">The target value used to drive the animation.</param>
        public abstract TSelf SetGoal(TValue goal);

        /// <summary>
        /// Resets the animation to its initial state.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// 初始化动画准备工作的方法，子类必须实现。
        /// </summary>
        protected abstract void PrepareAnimation();

        /// <summary>
        /// 子类必须实现的动画更新方法。
        /// </summary>
        /// <returns>
        /// 返回 <c>true</c> 表示动画尚未完成，需要继续调用；
        /// 返回 <c>false</c> 表示动画已结束。
        /// </returns>
        protected abstract bool UpdateAnimation();

        #endregion

        #region Editor Preview
#if UNITY_EDITOR

        /// <summary>
        /// 启动编辑器预览协程，以便在 Unity 编辑器中播放动画或行为。此方法确保当进入播放模式或编译时，预览会自动停止。
        /// <para><strong>注意：除非你修改了可视化面板，否则不建议轻易使用此方法。此方法仅能在 Assembly-CSharp-Editor 中使用。</strong></para>
        /// </summary>
        /// <param name="animationTokenSource">
        /// 一个 <see cref="CancellationTokenSource"/>，可用于手动取消预览。
        /// 当预览结束或编辑器状态发生变化时，将触发此令牌。
        /// </param>
        /// <param name="resetAfterPreview">当预览结束时是否重置(<see cref="Reset"/>)。</param>
        /// <param name="shouldResetOnEvent">当编辑器状态变化时(Scene改变，构建和进入PlayMode)，是否重置(<see cref="Reset"/>)。</param>
        internal TSelf StartEditorPreview(CancellationTokenSource animationTokenSource, bool resetAfterPreview = false, bool shouldResetOnEvent = true, string executorName = "")
        {
            if (animationTokenSource == null)
            {
                Debug.LogError("cancellationTokenSource is null");
                return (TSelf)this;
            }

            if (_ID != ushort.MinValue || Loops == 0 || !VerifySetup())
            {
                animationTokenSource.Cancel();
                return (TSelf)this;
            }

            ResetFields();

            _animPreviewTokenSource = animationTokenSource;
            _shouldResetOnEvent = shouldResetOnEvent;
            _executorName = executorName;

            AddOnComplete(() => ResetEditorPreview(resetAfterPreview));

            _animPreviewTokenSource.Token.Register(() =>
            {
                if (_ID != ushort.MinValue)
                    ResetEditorPreview();
            });

            StartAnimation(isNotEditorPreview: false);
            _initialized = true;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.wantsToQuit += EditorWantQuit;
            BuildProcessor.BuildAction += OnBuild;
            EditorSceneManager.sceneClosing += OnSceneClosing;

            return (TSelf)this;
        }

        private void OnSceneClosing(Scene scene, bool removingScene)
        {
            Debug.LogWarning("[EditorPreview] Please stop the Editor Preview before switching scenes.");
            ResetEditorPreview(_shouldResetOnEvent);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state) => ResetEditorPreview(_shouldResetOnEvent);

        private bool EditorWantQuit()
        {
            string msg = "[EditorPreview] Cannot exit the editor while animation preview is active. Please stop the preview first.";

            if (!string.IsNullOrEmpty(_executorName))
                msg += $" Executor name: {_executorName}";

            Debug.LogWarning(msg);
            return false;
        }

        private void OnBuild() => ResetEditorPreview(_shouldResetOnEvent);

        private void ResetEditorPreview(bool resetAfterPreview = true)
        {
            if (_ID == ushort.MinValue) return;

            Stop();
            _ID = ushort.MinValue;

            //重置Target至最初状态
            if (resetAfterPreview) Reset();

            if (_shouldResetOnEvent)
                SceneUtils.MarkDirtyAndSaveScene(TargetObject);

            _animPreviewTokenSource?.Cancel();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.wantsToQuit -= EditorWantQuit;
            BuildProcessor.BuildAction -= OnBuild;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
        }
#endif
        #endregion

        #region Interface Implementation

        #region OnComplete Callbacks

        IAnimation IAnimation.AddOnComplete(Action action)
        {
            AddOnComplete(action);
            return this;
        }

        IAnimation IAnimation.RemoveOnComplete(Action action)
        {
            RemoveOnComplete(action);
            return this;
        }

        IAnimation IAnimation.ClearOnComplete()
        {
            ClearOnComplete();
            return this;
        }

        #endregion

        #region Condition Callback & Frame Callback

        IAnimation IAnimation.AddStopWhen(Func<bool> condition)
        {
            AddStopWhen(condition);
            return this;
        }

        IAnimation IAnimation.RemoveStopWhen(Func<bool> condition)
        {
            RemoveStopWhen(condition);
            return this;
        }

        IAnimation IAnimation.ClearStopWhen()
        {
            ClearStopWhen();
            return this;
        }

        IAnimation IAnimation.AddCallWhenOne(Func<bool> condition, Action callback, out ushort id)
        {
            AddCallWhenOne(condition, callback, out id);
            return this;
        }

        IAnimation IAnimation.AddCallWhenOne(Func<bool> condition, Action<ushort> callback, out ushort id)
        {
            AddCallWhenOne(condition, callback, out id);
            return this;
        }

        IAnimation IAnimation.RemoveCallWhenOnce(ushort id)
        {
            RemoveCallWhenOnce(id);
            return this;
        }

        IAnimation IAnimation.AddCallEveryFrame(Action action, out ushort id)
        {
            AddCallEveryFrame(action, out id);
            return this;
        }

        IAnimation IAnimation.AddCallEveryFrame(Action<ushort> action, out ushort id)
        {
            AddCallEveryFrame(action, out id);
            return this;
        }

        IAnimation IAnimation.RemoveCallEveryFrame(ushort id)
        {
            RemoveCallEveryFrame(id);
            return this;
        }

        IAnimation IAnimation.ClearCallEveryFrame()
        {
            ClearCallEveryFrame();
            return this;
        }

        IAnimation IAnimation.ClearCallWhenOnce()
        {
            ClearCallWhenOnce();
            return this;
        }

        #endregion

        #region Playback Control

        IAnimation IAnimation.Play() { Play(); return this; }

#if UNITY_EDITOR
        void IAnimation.PlayEditorPreview(CancellationTokenSource animationTokenSource, bool resetAfterPreview, bool shouldResetOnEvent, string executorName)
            => StartEditorPreview(animationTokenSource, resetAfterPreview, shouldResetOnEvent, executorName);
#endif

        #endregion

        #region Settings
        void IAnimation.SetDuration(float duration)
        {
            SetDuration(duration);
        }

        bool IAnimation.SetTarget(Object target)
        {
            return SetTarget(target);
        }

        #endregion

        #endregion

        #region Overload Operators

        public static bool operator true(AnimationCore<TSelf, TValue> animationController) => animationController != null;

        public static bool operator false(AnimationCore<TSelf, TValue> animationController) => animationController == null;

        public static implicit operator bool(AnimationCore<TSelf, TValue> animationController) => animationController != null;
        #endregion

        #region CallXXX Struct

        private readonly struct CallWhenCondition
        {
            public readonly Func<bool> condition;
            public readonly Action action;
            public readonly Action<ushort> actionWithId;
            private readonly bool _withId;
            public readonly ushort id;

            public CallWhenCondition(Func<bool> condition, Action action)
            {
                this.condition = condition;
                this.action = action;
                actionWithId = null;
                id = IDGenerator.NextID();
                _withId = false;
            }

            public CallWhenCondition(Func<bool> condition, Action<ushort> actionWithId)
            {
                this.condition = condition;
                action = null;
                this.actionWithId = actionWithId;
                id = IDGenerator.NextID();
                _withId = true;
            }

            public readonly void Invoke()
            {
                if (_withId)
                    actionWithId.Invoke(id);
                else
                    action.Invoke();
            }
        }

        private readonly struct CallEveryFrame
        {
            public readonly ushort id;
            private readonly bool _withId;
            public readonly Action action;
            public readonly Action<ushort> actionWithId;

            public CallEveryFrame(Action action)
            {
                id = IDGenerator.NextID();
                this.action = action;
                actionWithId = null;
                _withId = false;
            }

            public CallEveryFrame(Action<ushort> actionWithId)
            {
                id = IDGenerator.NextID();
                action = null;
                this.actionWithId = actionWithId;
                _withId = true;
            }

            public readonly void Invoke()
            {
                if (_withId)
                    actionWithId.Invoke(id);
                else
                    action.Invoke();
            }
        }
        #endregion
    }
}
