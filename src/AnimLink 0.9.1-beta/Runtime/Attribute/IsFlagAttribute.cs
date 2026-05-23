using System;

/// <summary>
/// Marks a method parameter (by index) as a Flags enum parameter.  
/// This attribute is used by the FunctionPlus system to correctly identify  
/// and draw it in the Inspector using <see cref="UnityEditor.EditorGUI.EnumFlagsField"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class IsFlagAttribute : Attribute
{
    /// <summary>
    /// The indices of the parameters that represent Flags enums.
    /// Example: new int[] { 1 } → 0 = obj, 1 = enum list.
    /// </summary>
    public int[] ParameterIndices;
    public IsFlagAttribute()
    {
    }
}

/*
Usage Example:
    [IsFlag(ParameterIndices = new int[] { 1 })]  // 0 = obj, 1 = enums
    public void Example(object obj, List<Enum> enums)
    {
        // Your code here
    }
*/