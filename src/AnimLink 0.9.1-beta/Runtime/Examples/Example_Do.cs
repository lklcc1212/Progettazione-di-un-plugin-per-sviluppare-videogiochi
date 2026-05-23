using System.Collections.Generic;
using AnimLink;
using UnityEngine;

public class Example_Do : MonoBehaviour
{
    [HelpBox("During playback: press A, B, C, D, or E to use.", true)]
    public int Null = 0;

    [Header("DoPath:")]
    public List<Vector3> Path = new();

    private bool _isProcessing = false;

    void Update()
    {
        // DoPath
        if (Input.GetKeyDown(KeyCode.A) && !_isProcessing)
        {
            var DoPath = transform.DoPath(Path.ToArray(), 5f)
                 .SetLoops(1, PathLoop.PingPong)
                 .SetPathType(PathType.CatmullRom)
                 .SetEase(Ease.Linear)
                 .AddOnComplete(() =>
                 {
                     _isProcessing = !_isProcessing;
                     Debug.Log("DoPath completed.");
                 })
                 .Play();
            if (DoPath.IsPlaying)
                _isProcessing = !_isProcessing;
        }

        // DoPosition
        if (Input.GetKeyDown(KeyCode.B) && !_isProcessing)
        {
            _isProcessing = !_isProcessing;
            transform.DoPosition(Vector2.one * 3, 2)
                .SetLoops(2, BaseLoop.PingPong)
                .SetEase(Ease.OutBounce)
                .AddOnComplete(() =>
                {
                    _isProcessing = !_isProcessing;
                    Debug.Log("DoPosition completed.");
                })
                .Play();
        }

        // DoScale (single axis)
        if (Input.GetKeyDown(KeyCode.C) && !_isProcessing)
        {
            _isProcessing = !_isProcessing;
            transform.DoScale(5, 1, Axis.X)
                .AddOnComplete(() =>
                {
                    _isProcessing = !_isProcessing;
                    Debug.Log("DoScale completed.");
                })
                .Play();
        }

        // DoScale (vector)
        if (Input.GetKeyDown(KeyCode.D) && !_isProcessing)
        {
            _isProcessing = !_isProcessing;
            transform.DoScale(Vector3.one * 5, 2)
                .AddOnComplete(() =>
                {
                    _isProcessing = !_isProcessing;
                    Debug.Log("DoScale completed.");
                })
                .Play();
        }

        // DoColor
        if (Input.GetKeyDown(KeyCode.E) && !_isProcessing)
        {
            _isProcessing = !_isProcessing;
            GetComponent<SpriteRenderer>().DoColor(Color.red, 2.5f)
                .SetEase(Ease.OutSine)
                .SetLoops(2, ValueLoop.PingPong)
                .AddOnComplete(() =>
                {
                    _isProcessing = !_isProcessing;
                    Debug.Log("DoColor completed.");
                })
                .Play();
        }

        // Others: DoAlpha and DoRotation
    }
}
