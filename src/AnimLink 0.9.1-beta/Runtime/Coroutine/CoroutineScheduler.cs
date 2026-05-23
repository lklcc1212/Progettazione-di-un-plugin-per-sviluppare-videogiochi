namespace AnimLink
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using static UtilityExtension;

    public static class CoroutineScheduler
    {
        #region === CoroutineFrame ===

        public class CoroutineFrame
        {
            internal IEnumerator Enumerator;
            internal object Current;
            internal readonly Stack<IEnumerator> Stack = new();
            internal IYieldInstruction WaitInstruction;
            internal bool Paused;
            internal bool IsRunning;

            internal Action<CoroutineFrame> OnComplete;

            internal void Reset()
            {
                Enumerator = null;
                Current = null;
                Stack.Clear();
                WaitInstruction = null;
                Paused = false;
                IsRunning = false;
                OnComplete = null;
            }
        }

        #endregion ===================

        #region === Internal Fields ===

        private readonly static List<CoroutineFrame> _coroutineFrames = new(4);

        //保证遍历时可加入/删除元素
        private static readonly List<CoroutineFrame> _pendingCoroutineRemovals = new(8);
        private static readonly List<CoroutineFrame> _pendingCoroutineAdds = new(8);

        private static bool _processingCoroutine = false;

        internal static int ActiveCoroutineCount => _coroutineFrames.Count;

        #endregion

        internal static void Tick()
        {
            _processingCoroutine = true;
            Update();
            _processingCoroutine = false;

            // 再删除 pending（可能是在遍历期间通过 Stop 添加的）
            if (_pendingCoroutineRemovals.Count > 0)
            {
                foreach (var v in _pendingCoroutineRemovals)
                {
                    _coroutineFrames.Remove(v);
                    CoroutineFramePool.Release(v);
                }
                _pendingCoroutineRemovals.Clear();
            }

            // 最后添加 pending（可能是在遍历期间通过 Start 添加的）
            if (_pendingCoroutineAdds.Count > 0)
            {
                foreach (var v in _pendingCoroutineAdds)
                    _coroutineFrames.Add(v);

                _pendingCoroutineAdds.Clear();
            }
        }

        public static CoroutineFrame StartCoroutine(IEnumerator enumerator, Action<CoroutineFrame> onComplete = null)
        {
            if (enumerator == null)
            {
                LogWarning("Cannot start coroutine with null enumerator.");
                return null;
            }

            CoroutineFrame frame = CoroutineFramePool.Get();

            frame.Enumerator = enumerator;
            frame.OnComplete = onComplete;
            frame.IsRunning = true;

            if (_processingCoroutine)
            {
                _pendingCoroutineAdds.Add(frame);
            }
            else
            {
                _coroutineFrames.Add(frame);
            }

            UpdateCoroutineFrame(frame);

            return frame;
        }

        public static bool StopCoroutine(CoroutineFrame coroutineFrame)
        {
            if (!CheckCoroutineFrameValid(coroutineFrame)) return false;

            if (_processingCoroutine)
            {
                _pendingCoroutineRemovals.Add(coroutineFrame);
                return _coroutineFrames.Contains(coroutineFrame);
            }
            else
            {
                CoroutineFramePool.Release(coroutineFrame);
                return _coroutineFrames.Remove(coroutineFrame);
            }
        }

        public static bool PauseCoroutine(CoroutineFrame coroutineFrame)
        {
            if (!CheckCoroutineFrameValid(coroutineFrame)) return false;

            if (coroutineFrame.Paused)
                return false;
            coroutineFrame.Paused = true;
            return true;
        }

        public static bool ResumeCoroutine(CoroutineFrame coroutineFrame)
        {
            if (!CheckCoroutineFrameValid(coroutineFrame)) return false;

            if (!coroutineFrame.Paused)
                return false;
            coroutineFrame.Paused = false;
            return true;
        }

        public static bool IsCoroutineRunning(CoroutineFrame coroutineFrame)
        {
            if (!CheckCoroutineFrameValid(coroutineFrame)) return false;

            return coroutineFrame.IsRunning && !coroutineFrame.Paused;
        }

        private static bool CheckCoroutineFrameValid(CoroutineFrame coroutineFrame)
        {
            if (coroutineFrame == null)
            {
                LogWarning("CoroutineFrame is null.");
                return false;
            }
            return true;
        }

        private static void Update()
        {
            for (int i = _coroutineFrames.Count - 1; i >= 0; i--)
            {
                CoroutineFrame node = _coroutineFrames[i];
                if (!UpdateCoroutineFrame(node))
                {
                    node.OnComplete?.Invoke(node);
                    _coroutineFrames.RemoveAt(i);
                    CoroutineFramePool.Release(node);
                }
            }
        }

        private static bool UpdateCoroutineFrame(CoroutineFrame node)
        {
            if (node.Paused) return true;

            if (node.WaitInstruction != null)
            {
                if (node.WaitInstruction.KeepWaiting)
                {
                    return true;
                }

                node.WaitInstruction = null;
            }

            IEnumerator cur = node.Stack.Count > 0 ? node.Stack.Peek() : node.Enumerator;

            if (!cur.MoveNext())
            {
                if (node.Stack.Count > 0)
                {
                    node.Stack.Pop();
                    return true;
                }

                return false;
            }

            object yielded = cur.Current;
            if (yielded is IYieldInstruction instr)
            {
                node.WaitInstruction = instr;
                instr.Reset();
            }
            else if (yielded is IEnumerator enumerator)
            {
                node.Stack.Push(enumerator);
            }
            else if (yielded != null)
            {
                throw new NotSupportedException($"Unsupported yield type: {yielded.GetType().FullName}. " +
                                                 "Supported types: IEnumerator, IYieldInstruction, CustomYieldInstruction and null.");
            }

            return true;
        }
    }
}
