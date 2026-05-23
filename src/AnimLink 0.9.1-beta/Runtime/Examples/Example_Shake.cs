using AnimLink;
using UnityEngine;
using static AnimLink.AnimLinkExtension;

public class Example_Shake : MonoBehaviour
{
    [HelpBox("During playback: press A (Shake) or B (AdvancedShake) to use.", true)]
    public string Null = "";

    [Header("AdvancedShake params:")]
    public AnimationCurve MagnitudeCurve = AnimationCurve.Linear(0, 1, 1, 1);
    public AnimationCurve ReturnCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private bool _isShaking = false;

    // Update is called once per frame
    void Update()
    {
        // Simple Shake
        if (Input.GetKeyDown(KeyCode.A))
        {
            _isShaking = !_isShaking;
            // You can also use ShakePosition
            transform.Shake(0.075f, 0.5f, Axis.X | Axis.Y /* specify the axes to shake (X,Y,Z) */).Play()
                .AddOnComplete(() =>
                {
                    _isShaking = !_isShaking;
                    Debug.Log("Task completed.");
                });
        }

        // Advanced Shake
        if (Input.GetKeyDown(KeyCode.B))
        {
            _isShaking = !_isShaking;
            transform.AdvancedShake(0.1f, 0.5f, Axis.X | Axis.Y | Axis.Z)
                .SetReturnDuration(0.25f)
                .SetCurves(MagnitudeCurve, ReturnCurve)
                .SetFrequency(20)
                .AddOnComplete(() =>
                {
                    _isShaking = !_isShaking;
                    Debug.Log("Task completed.");
                })
                .Play();
        }
    }
}
