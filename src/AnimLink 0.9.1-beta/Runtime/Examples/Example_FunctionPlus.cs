using AnimLink;
using System.Collections;
using UnityEngine;

public class Example_FunctionPlus : MonoBehaviour
{
#if UNITY_6000_0_OR_NEWER
    [HelpBox("During playback: press Space to use.")]
#else
    [HelpBox("During playback: press Space to use.", true)]
    public string Null;
#endif

    public FunctionPlus FunctionPlus = new();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // When the player presses the Space key
        {
            if (FunctionPlus._isIEnumerator)
            {
                StartCoroutine(FunctionPlus.GetIEnumerator()); // Call coroutine
            }
            else
            {
                FunctionPlus.InvokeMethod(); // Call method
            }
        }
    }

    public void VoidMethod()
    {
        Debug.Log("Hello world!");
    }

    public IEnumerator MethodWithCoroutine(string yourName)
    {
        Debug.Log("Hello world! " + yourName + " -> start");
        yield return new WaitForSeconds(1f);
        Debug.Log("Hello world! " + yourName + " -> 1s later");
    }
}
