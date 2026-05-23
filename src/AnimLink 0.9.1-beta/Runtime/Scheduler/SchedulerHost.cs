namespace AnimLink
{
    using UnityEngine;

    internal class SchedulerHost : MonoBehaviour
    {
        private void OnDisable()
        {
            Invoke(nameof(Reactivate), 0f);
        }

        private void Reactivate()
        {
            gameObject.SetActive(true);
        }

        private void Update()
        {
            FrameScheduler.Tick();
            CoroutineScheduler.Tick();
        }

        private void FixedUpdate() => FixedScheduler.Tick();
    }
}