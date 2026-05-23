namespace AnimLink
{
    /// <summary>
    /// Represents an animation that supports reverse playback.
    /// <para>
    /// This capability allows the animation to be played backwards from its current or completed state,
    /// reversing its progression over time.
    /// </para>
    /// </summary>
    public interface IReversibleAnimation : IAnimation
    {
        /// <summary>
        /// Plays the animation in reverse.
        /// <br></br><br>Requirements:</br>
        /// <br>- <see cref="Play()"/> has been called at least once.</br>
        /// <br>- The animation has been stopped via <see cref="Stop()"/> or has naturally completed.</br>
        /// <br>- No parameter-modifying methods (such as <c>ConfigXXX</c> / <c>SetXXX</c>) 
        /// have been called after stopping, regardless of whether they succeeded,
        /// <b>except</b> for:
        /// <br><see cref="SetDuration(float)"/></br>
        /// <br><see cref="DoPath.SetPathRotation(bool, UnityEngine.Vector3?, bool)"/></br>
        /// <br><see cref="DoPath.SetRotDimension(Dimension)"/></br></br>
        /// </summary>
        public void PlayBackward();
    }
}
