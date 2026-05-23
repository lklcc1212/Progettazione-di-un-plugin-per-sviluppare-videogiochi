namespace AnimLink
{
    using UnityEngine;

    using static UtilityExtension;

    /// <summary>
    /// Extended animation base class that adds tween-like capabilities
    /// such as easing, looping, playback direction control, and speed adjustment.
    /// <br/>
    /// This layer is built on top of <see cref="AnimationCore{TSelf, TValue}"/> and
    /// is intended for animations that support interpolation-based behavior.
    /// </summary>
    /// <typeparam name="TSelf">
    /// The type that inherits from this base class.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of value being animated (e.g. Vector3, float, Color).
    /// </typeparam>
    public abstract class AnimationTweenBase<TSelf, TValue> : AnimationCore<TSelf, TValue>, IReversibleAnimation where TSelf : AnimationTweenBase<TSelf, TValue>
    {
        #region Playback Control

        /// <summary>
        /// Plays the animation in reverse.
        /// <br></br><br>Requirements:</br>
        /// <br>- <see cref="Play()"/> has been called at least once.</br>
        /// <br>- The animation has been stopped via <see cref="Stop()"/> or has naturally completed.</br>
        /// <br>- No parameter-modifying methods (such as <c>ConfigXXX</c> / <c>SetXXX</c>) 
        /// have been called after stopping, regardless of whether they succeeded,
        /// <b>except</b> for:
        /// <br><see cref="SetDuration(float)"/></br>
        /// <br><see cref="DoPath.SetPathRotation(bool, Vector3?, bool)"/></br>
        /// <br><see cref="DoPath.SetRotDimension(Dimension)"/></br></br>
        /// </summary>
        public virtual void PlayBackward()
        {
            if (!VerifySetup() || (_ID == ushort.MinValue && !_finishedPlayingBackward) || _isLocking)
                return;

            StartAnimation(playBackward: true);
        }

        #endregion

        #region Setting

        /// <summary>
        /// Slows down the animation speed. <strong>Factor range: [0,1]</strong>.
        /// </summary>
        public virtual TSelf SlowDownBy(float factor)
        {
            factor = Mathf.Clamp01(factor) + 1f;
            RescaleDuration(Duration * factor);
            return (TSelf)this;
        }

        /// <summary>
        /// Speeds up the animation. <strong>Factor range: [0,1]</strong>.
        /// </summary>
        public virtual TSelf SpeedUpBy(float factor)
        {
            factor = Mathf.Clamp01(factor);
            RescaleDuration(Duration * (1f - factor));
            return (TSelf)this;
        }

        /// <summary>
        /// Sets the easing type for the animation.
        /// </summary>
        public virtual TSelf SetEase(Ease ease)
        {
            if (CheckPlayingAndResetID()) return (TSelf)this;

            if (ease.HasMultipleFlags())
            {
                LogWarning($"[{GetType().Name}] The ease parameter has multiple flags or is 0, so the previous value will be kept({_ease}).");
                return (TSelf)this;
            }

            _ease = ease;
            return (TSelf)this;
        }

        /// <summary>
        /// Sets the number of loops. 
        /// <para>Use ≤ -1 for infinite looping.</para>
        /// </summary>
        /// <param name="loops">Number of repetitions. ≤ -1 means infinite loops.</param>
        public virtual TSelf SetLoops(int loops)
        {
            SetLoopsInternal(loops);
            return (TSelf)this;
        }

        /// <summary>
        /// 内部方法，用于设置动画的循环次数。
        /// <para>如果动画正在播放，会返回 false 并不修改循环次数；否则设置 _loops 和 Loops 并返回 true。</para>
        /// </summary>
        /// <param name="loops">循环次数，≤ -1 表示无限循环。</param>
        /// <returns>设置成功返回 true，动画正在播放时返回 false。</returns>
        protected bool SetLoopsInternal(int loops)
        {
            if (CheckPlayingAndResetID()) return false;

            _isInfLoops = loops < 0;

            Loops = loops;
            _lastLoopIndex = _isInfLoops ? int.MaxValue/*代表没有_lastLoopIndex*/ : loops - 1;
            return true;
        }

        #endregion
    }
}