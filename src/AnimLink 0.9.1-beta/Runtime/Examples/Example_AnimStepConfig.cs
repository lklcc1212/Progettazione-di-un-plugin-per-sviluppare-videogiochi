using AnimLink;
using UnityEngine;

public class Example_AnimStepConfig : MonoBehaviour
{
#if UNITY_6000_0_OR_NEWER
    [HelpBox("During playback: press Space to play, S to pause/stop, R to resume.")]
#else
    [HelpBox("During playback: press Space to play.", true)]
    public int Null;
#endif

    [SerializeField]
    private AnimSequenceConfig.AnimStepConfig _animStepConfig;

    private bool isPlaying;
    public IAnimation command;

    void Update()
    {
        if (!isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Export a single animation
                IAnimation animation = command = _animStepConfig.ExportAnimation();
                if (animation != null)
                {
                    isPlaying = true;
                    animation.AddOnComplete(() => isPlaying = false).Play();
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                // Stop the animation
                command.Stop();
                isPlaying = false;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                // Resume the animation
                command.Resume();
                isPlaying = true;
            }
        }
    }
}
