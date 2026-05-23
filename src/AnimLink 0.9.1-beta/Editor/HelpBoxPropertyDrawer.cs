using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HelpBoxAttribute))]
public class HelpBoxDrawer : PropertyDrawer
{
    private static readonly int _initialHeight = 10;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        HelpBoxAttribute helpBoxAttribute = (HelpBoxAttribute)attribute;

        if (helpBoxAttribute == null)
            return;

        EditorGUI.BeginProperty(position, label, property);

        // 获取当前缩进级别
        float indentation = EditorGUI.indentLevel * 15f;

        // 计算帮助框高度
        float helpBoxHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(helpBoxAttribute.Text),
              (EditorGUIUtility.currentViewWidth - indentation) / 1.1f) + _initialHeight;

        // 绘制帮助框
        EditorGUI.HelpBox(new Rect(position.x + indentation, position.y, position.width - indentation, helpBoxHeight),
                         helpBoxAttribute.Text, MessageType.Info);


        if (helpBoxAttribute.NoProperty)
        {
            GUILayout.Space(helpBoxHeight - 18);
        }
        else
        {
            EditorGUILayout.Space(helpBoxHeight);
            position.y += helpBoxHeight;
            // 在帮助框下方绘制属性
            EditorGUI.PropertyField(position, property);
        }
        EditorGUI.EndProperty();
    }
}