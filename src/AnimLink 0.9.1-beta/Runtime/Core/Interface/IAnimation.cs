namespace AnimLink
{
    using System;
    using System.Threading;

    using Object = UnityEngine.Object;

    /// <summary>
    /// Defines the basic interface for all animations in AnimLink.
    /// <para>
    /// Implementations, such as <see cref="AnimationCore{TSelf, TValue}"/>, provide full lifecycle control,
    /// pausing, resuming and callbacks
    /// when the animation completes. This interface allows different animation types
    /// (e.g., position, scale, rotation, color, alpha, etc.) to be treated uniformly
    /// by the animation system.
    /// </para>
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// The user-defined duration of the animation.
        /// </summary>
        float Duration { get; }

        /// <summary>
        /// Current loop time of the animation.
        /// <br></br>
        /// The value is always clamped within [0, <see langword="Duration"/>] for each loop.
        /// </summary>
        public float CurrentLoopTime { get; }

        /// <summary>
        /// Total elapsed time since the animation started (always increasing).
        /// </summary>
        public float ElapsedTime { get; }

        /// <summary>
        /// Number of loops the animation will perform.
        /// </summary>
        int Loops { get; }

        /// <summary>
        /// Indicates whether the animation is currently playing.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Indicates whether the animation is currently playing in reverse.
        /// </summary>
        bool IsPlayingBackward { get; }

        /// <summary>
        /// The target Unity object that the animation affects.
        /// </summary>
        Object TargetObject { get; }

        /// <summary>
        /// Gets the normalized progress of the animation in the range <b>[0, 1]</b>.
        /// <br/><br/>
        /// For <b>finite loops</b>, the progress increases linearly <b>from 0 to 1</b> across all loops.
        /// If <b>Loops ≤ -1</b> (infinite looping), this property <b>returns -1</b>
        /// because a normalized progress cannot be defined.
        /// </summary>
        public float Progress { get; }


#if UNITY_EDITOR
        /// <summary>
        /// 注意：除非你正在修改可视化面板，否则不建议轻易使用此方法。此方法仅能在 Editor 中使用。
        /// </summary>
        internal void PlayEditorPreview(CancellationTokenSource tokenSource, bool resetAfterPreview = false, bool shouldResetOnEvent = true, string gameObjName = "");
#endif

        /// <summary>
        /// Starts animation playback from the beginning or after completion/stop.
        /// <br>Can be executed again only after completion or stop.</br>
        /// <br>It is recommended to call this method only after configuring all necessary settings.</br>
        /// </summary>
        public IAnimation Play();

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
        public void Resume();

        /// <summary>
        /// Stops or pauses the animation.
        /// </summary>
        public void Stop();

        /// <summary>
        /// Resets the animation to its initial state.
        /// </summary>
        public void Reset();

        /// <summary>
        /// Sets the duration of the animation in seconds. <strong>Valid range: (0, +∞]</strong>.
        /// </summary>
        public void SetDuration(float duration);

        /// <summary>
        /// Sets the target object for this animation.
        /// </summary>
        /// <param name="target">The target object to assign.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the target was successfully assigned; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool SetTarget(Object target);

        /// <summary>
        /// Registers a callback to be invoked when the animation completes.
        /// </summary>
        public IAnimation AddOnComplete(Action action);

        /// <summary>
        /// Unregisters a callback, so it will no longer be called when the animation completes.
        /// </summary>
        public IAnimation RemoveOnComplete(Action action);

        /// <summary>
        /// Clears all registered completion callbacks.
        /// </summary>
        public IAnimation ClearOnComplete();

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
        public IAnimation AddStopWhen(Func<bool> condition);

        /// <summary>
        /// Removes a previously registered stop condition.
        /// </summary>
        /// <param name="condition">The condition to remove.</param>
        public IAnimation RemoveStopWhen(Func<bool> condition);

        /// <summary>
        /// Clears all registered stop conditions.
        /// </summary>
        public IAnimation ClearStopWhen();

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
        public IAnimation AddCallWhenOne(Func<bool> condition, Action callback, out ushort id);

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
        public IAnimation AddCallWhenOne(Func<bool> condition, Action<ushort> callback, out ushort id);

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
        public IAnimation RemoveCallWhenOnce(ushort id);

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
        public IAnimation AddCallEveryFrame(Action action, out ushort id);

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
        public IAnimation AddCallEveryFrame(Action<ushort> action, out ushort id);

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
        public IAnimation RemoveCallEveryFrame(ushort id);

        /// <summary>
        /// Clears all registered callbacks that are executed every frame.
        /// </summary>
        public IAnimation ClearCallEveryFrame();

        /// <summary>
        /// Clears all registered one-time callbacks.
        /// </summary>
        public IAnimation ClearCallWhenOnce();
    }
}
