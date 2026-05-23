namespace AnimLink
{
    using System;

    using static UtilityExtension;

    /// <summary>
    /// Base class for animations that support looped playback.
    /// <para>
    /// Provides common loop-handling logic for derived animation types, including PingPong and Increment loops.
    /// </para>
    /// </summary>
    /// <typeparam name="TSelf">The derived animation type.</typeparam>
    /// <typeparam name="TValue">The type of data managed by this animation.</typeparam>
    public abstract class DoBaseLoop<TSelf, TValue> : AnimationTweenBase<TSelf, TValue> where TSelf : DoBaseLoop<TSelf, TValue>
    {
        protected BaseLoop _loopType;

        /// <summary>
        /// Sets the number of loops and the loop type for the animation.
        /// </summary>
        /// <param name="loops">The number of repetitions. Use ≤ -1 for infinite looping.</param>
        /// <param name="loopType">The type of looping to apply.</param>
        public TSelf SetLoops(int loops, BaseLoop loopType)
        {
            if (!SetLoopsInternal(loops)) return (TSelf)this;

            if (loopType.HasMultipleFlags())
                LogWarning($"[DoPosition] The loopType parameter has multiple flags, so the previous value ( {_loopType} ) will be kept.");
            else
                _loopType = loopType;

            return (TSelf)this;
        }

        protected bool LoopCore(Action reverse, Action increment)
        {
            if (_playBackward ? _elapsedTime > 0f : _elapsedTime < _duration)
                return true;

            if (!UpdateLoopState(_loopType != BaseLoop.PingPong))
                return false;

            HandleLoopReset(reverse, increment);

            return true;
        }

        private void HandleLoopReset(Action reverse, Action increment)
        {
            // Overshoot(多余时间)
            _elapsedTime = _playBackward ? _duration + _elapsedTime : _elapsedTime - _duration;

            switch (_loopType)
            {
                case BaseLoop.PingPong:
                    reverse();
                    break;
                case BaseLoop.Increment:
                    increment();
                    break;
            }
        }
    }

}