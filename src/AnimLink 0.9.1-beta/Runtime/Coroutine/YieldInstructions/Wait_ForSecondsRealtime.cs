namespace AnimLink
{
    using System.Collections;

    using UnityEngine;

    public sealed class Wait_ForSecondsRealtime : IYieldInstruction
    {
        internal float Seconds;
        private float _targetTime;

        public bool KeepWaiting => Time.realtimeSinceStartup < _targetTime;

        public Wait_ForSecondsRealtime(float seconds)
        {
            Seconds = seconds;
            _targetTime = 0f;
        }

        public void Reset()
        {
            _targetTime = Time.realtimeSinceStartup + Seconds;
        }
    }
}
