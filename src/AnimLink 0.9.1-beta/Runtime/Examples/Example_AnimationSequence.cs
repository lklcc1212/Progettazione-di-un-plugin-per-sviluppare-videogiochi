using UnityEngine;
using AnimLink;
using static AnimLink.AnimLinkExtension;

public class Example_AnimationSequence : MonoBehaviour
{
    [HelpBox("During playback: press Space to play / press S to stop.", true)]
    public string Description;

    private readonly AnimationSequence _animationSequence = new();

    void Start()
    {
        // Configure the AnimationSequence
        _animationSequence
            .Append(transform.AdvancedShake(0.2f, 0.5f, Axis.X | Axis.Y)
                .SetFrequency(20)
                .SetReturnDuration(0.1f)
                .AddOnComplete(() => Debug.Log("Task 1 completed.")) /* You can use ConfigureCurve here, but we'll skip it for now */)
            .Append(transform.DoPosition(new Vector3(0, 1), 1)
            .AddOnComplete(() => Debug.Log("Task 2 completed.")))
            .Append(1) // Delay of 1 second
            .Append(() => Debug.Log("Hello world!")); // You can continue adding more...
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // When the player presses Space
        {
            _animationSequence.Play(); // Play
        }

        if (Input.GetKeyDown(KeyCode.S)) // When the player presses S
        {
            _animationSequence.Stop(); // Stop
        }
    }
}
