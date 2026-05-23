using UnityEngine;

///// < summary >
///// 如果你使用的是 Unity 6 以下的版本，可能会出现不按预期显示的情况，请谨慎使用。
///// </summary>
/// <summary>
/// Displays a help box in the Unity Inspector.  
/// <para>Note: If you are using a Unity version earlier than 6, the help box may not appear as expected.  
/// Use this attribute with caution on older Unity versions.</para>
/// </summary>
public class HelpBoxAttribute : PropertyAttribute
{
    public readonly string Text;
    public readonly bool NoProperty;
    public HelpBoxAttribute(string text, bool noProperty = false)
    {
        Text = text;
        NoProperty = noProperty;
    }
}