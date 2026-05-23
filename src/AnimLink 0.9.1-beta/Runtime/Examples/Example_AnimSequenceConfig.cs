using AnimLink;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example_AnimSequenceConfig : MonoBehaviour
{
#if UNITY_6000_0_OR_NEWER 
    [HelpBox("During playback: press W to start / press E to stop playing (AnimationSequence)")]
#else
    [HelpBox("During playback: press W (AnimationSequence)", true)]
    public int Null;
#endif

    [SerializeField]
    private AnimSequenceConfig _animSequenceConfig;
    private AnimationSequence _animationSequence;

    [HelpBox("Not recommended to use.\n↓", true)]
    public int Null;

    [SerializeField]
    private List<AnimSequenceConfig.AnimStepConfig> tools;

    void Start()
    {
        _animationSequence = _animSequenceConfig.ExportAnimationSequence(); // Export Animation Sequence
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) // When the player presses W
        {
            _animationSequence.Play(); // Play
        }
        if (Input.GetKeyDown(KeyCode.E)) // When the player presses E
        {
            _animationSequence.Stop(); // Stop playback
        }
    }

    #region FunctionPlus
    public void VoidMethod()
    {
        Debug.Log("Hello world!");
    }

    public enum MyEnum
    {
        A = 1,
        B = 2,
        C = 4
    }

    // The third parameter is Flags (make enum values 1, 2, 4, 8, 16... for bitwise operations)
    [IsFlag(ParameterIndices = new int[] { 2 })]
    public void MethodWithParameter(int i, MyEnum myEnum, MyEnum myEnum1) // Method with parameters (max parameter count: unknown)
    {
        Debug.Log("Hello world! " + i);
        Debug.Log(myEnum + "|" + myEnum1);
        foreach (var item in myEnum1.GetFlags())
        {
            Debug.Log(item);
        }
    }

    public IEnumerator MethodWithCoroutine(string yourName, int[] ints)
    {
        Debug.Log("Hello world! " + yourName + " -> start");
        yield return new WaitForSeconds(1f);
        Debug.Log("Hello world! " + yourName + " -> 1s later");
        Debug.Log("ints length: " + ints.Length + " | first element: " + ints[0]);
    }
    #endregion
}
