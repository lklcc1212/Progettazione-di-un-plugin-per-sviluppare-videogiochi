namespace AnimLink
{
    using System;

    using static UtilityExtension;

    /// <summary>
    /// Base class for animations that support value-based looped animations, such as alpha or color changes.
    /// <para>
    /// Provides core loop-handling logic for derived animation types, including PingPong, Increment, and Decrement loops.
    /// </para>
    /// </summary>
    /// <typeparam name="TSelf">The derived animation type.</typeparam>
    /// <typeparam name="TValue">The type of data managed by this animation.</typeparam>
    public abstract class DoValueLoop<TSelf, TValue> : AnimationTweenBase<TSelf, TValue> where TSelf : DoValueLoop<TSelf, TValue>
    {
        protected ValueLoop _loopType = ValueLoop.FromStart;

        /// <summary>
        /// Sets the number of loops and the loop type for the animation.
        /// </summary>
        /// <param name="loops">
        /// The number of times the animation should repeat. 
        /// A value of ≤ -1 indicates infinite looping.
        /// </param>
        /// <param name="loopType">The type of loop to apply (e.g., PingPong, Increment, Decrement).</param>
        public TSelf SetLoops(int loops, ValueLoop loopType)
        {
            if (!SetLoopsInternal(loops)) return (TSelf)this;

            if (loopType.HasMultipleFlags())
                LogWarning($"[{GetType().Name}] The loopType parameter has multiple flags, so the previous value ( {_loopType} ) will be kept.");
            else
                _loopType = loopType;
            return (TSelf)this;
        }

        protected bool LoopCore(Action reverse, Action updateAlphaIncrement)
        {
            // 判断是否到达边界
            if (_playBackward ? _elapsedTime > 0f : _elapsedTime < _duration)
                return true;

            // 更新循环状态
            if (!UpdateLoopState(_loopType != ValueLoop.PingPong))
                return false;

            HandleLoopReset(reverse, updateAlphaIncrement);
            return true;
        }

        private void HandleLoopReset(Action reverse, Action updateAlphaIncrement)
        {
            // 处理 overshoot
            _elapsedTime = _playBackward
                ? _duration + _elapsedTime
                : _elapsedTime - _duration;

            switch (_loopType)
            {
                case ValueLoop.PingPong:
                    reverse();
                    break;

                case ValueLoop.Increment:
                case ValueLoop.Decrement:
                    updateAlphaIncrement();
                    break;
            }
        }
    }

}