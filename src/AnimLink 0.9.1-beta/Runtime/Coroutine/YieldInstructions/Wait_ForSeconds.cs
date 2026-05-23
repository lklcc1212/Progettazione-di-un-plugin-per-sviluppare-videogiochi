namespace AnimLink
{
    using System.Collections;

    using UnityEngine;

    public sealed class Wait_ForSeconds : IYieldInstruction
    {
        internal float Seconds;
        private float _elapsedTime;

        public bool KeepWaiting
        {
            get
            {
                _elapsedTime += Time.deltaTime;
                return _elapsedTime < Seconds;
            }
        }

        public Wait_ForSeconds(float seconds)
        {
            Seconds = seconds;
            _elapsedTime = 0f;
        }

        public void Reset()
        {
            _elapsedTime = 0f;
        }
    }
}
