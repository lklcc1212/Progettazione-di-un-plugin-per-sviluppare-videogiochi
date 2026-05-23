using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

using AnimLink;
using static AnimLink.AnimSequenceConfig;

using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(AnimSequenceConfig))]
public class AnimSequenceConfigPropertyDrawer : PropertyDrawer
{
    static bool _isUnity6;
    static bool _notInitialized = true;

    private static readonly GUIStyle _header = new(EditorStyles.label)
    {
        richText = true,
        alignment = TextAnchor.MiddleCenter
    };
    private static readonly GUIStyle _info_style = new(EditorStyles.helpBox)
    {
        alignment = TextAnchor.MiddleCenter,
        fontSize = 12,
        wordWrap = false
    };

    /// <summary>
    /// 用于存储 AnimationGroup 编辑器绘制所需的所有信息，
    /// — 比如属性、折叠状态、索引和目标对象，
    /// 便于在拆分的 GUI 函数中传递和访问。
    /// </summary>
    public struct AnimSequenceConfigGUIContext
    {
        public SerializedProperty Property;
        public SerializedProperty StepConfigs;
        public AnimSequenceConfig SequenceConfig;
        public SerializedProperty Foldout1;
        public SerializedProperty Foldout2;
        public SerializedProperty SwitchIndex1;
        public SerializedProperty SwitchIndex2;
        public SerializedProperty AddIndex;
        public SerializedProperty RemoveIndex;
        public int ArraySize;
        public Object TargetObject;

        // 构造函数
        public AnimSequenceConfigGUIContext(SerializedProperty property, SerializedProperty stepConfigs,
                                          AnimSequenceConfig sequenceConfig, SerializedProperty foldout1,
                                          SerializedProperty foldout2, SerializedProperty switchIndex1,
                                          SerializedProperty switchIndex2, SerializedProperty addIndex,
                                          SerializedProperty removeIndex, int arraySize, Object targetObject)
        {
            Property = property;
            StepConfigs = stepConfigs;
            SequenceConfig = sequenceConfig;
            Foldout1 = foldout1;
            Foldout2 = foldout2;
            SwitchIndex1 = switchIndex1;
            SwitchIndex2 = switchIndex2;
            AddIndex = addIndex;
            RemoveIndex = removeIndex;
            ArraySize = arraySize;
            TargetObject = targetObject;
        }

        public static AnimSequenceConfigGUIContext Create(SerializedProperty property, Object targetObject)
        {
            var StepConfigs = property.FindPropertyRelative("StepConfigs");
            return new AnimSequenceConfigGUIContext(
                property,
                StepConfigs,
                // 获取 animSequenceConfig 实例
                (AnimSequenceConfig)UtilityExtension.GetObjectInstanceByReflection(property.propertyPath, targetObject),
                property.FindPropertyRelative("_foldout1"),
                property.FindPropertyRelative("_foldout2"),
                property.FindPropertyRelative("_switchIndex1"),
                property.FindPropertyRelative("_switchIndex2"),
                property.FindPropertyRelative("_addIndex"),
                property.FindPropertyRelative("_removeIndex"),
                StepConfigs.arraySize,
                targetObject
            );
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 获取目标对象
        Object targetObject = property.serializedObject.targetObject;

        // 检查是否是 Unity6
        if (_notInitialized)
        {
            _isUnity6 = Application.unityVersion.StartsWith("6");
            _notInitialized = false;
        }

        var ascGuiCtx = AnimSequenceConfigGUIContext.Create(property, targetObject);

        DrawMainFoldout(ref ascGuiCtx, position, label);

        EditorGUILayout.EndVertical();
        EditorGUI.EndProperty();

        // 应用属性修改
        property.serializedObject.ApplyModifiedProperties();
    }

    // ---- 主折叠面板 ----
    void DrawMainFoldout(ref AnimSequenceConfigGUIContext ascGuiCtx, Rect position, GUIContent label)
    {
        SerializedProperty property = ascGuiCtx.Property;
        SerializedProperty foldout1 = ascGuiCtx.Foldout1;

        EditorGUI.BeginProperty(position, label, property);
        EditorGUILayout.Space(-20);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Rect rect = EditorGUILayout.BeginVertical();
        EditorGUILayout.Space(20);
        EditorGUILayout.EndVertical();
        if (_isUnity6)
            rect.x = 40;
        // 绘制主折叠面板
        foldout1.boolValue = EditorGUI.BeginFoldoutHeaderGroup(rect, foldout1.boolValue, $"{property.PropertyPathParts()[^1]}: AnimSequenceConfig", EditorStyles.foldout);
        EditorGUI.EndFoldoutHeaderGroup();

        // 主折叠面板内容
        if (foldout1.boolValue)
        {
            // 绘制信息标签
            EditorGUILayout.LabelField("If it is not set correctly it will not be added to the queue.", _info_style, GUILayout.Height(20));

            rect = DrawButtons(ascGuiCtx, rect, position);

            DrawSecondaryFoldout(ascGuiCtx, rect, position);

            ShowEditorPreviewButton(property, ascGuiCtx.SequenceConfig, ascGuiCtx.TargetObject);
        }
    }

    // ---- 按钮绘制 ----
    private Rect DrawButtons(AnimSequenceConfigGUIContext ascGuiCtx, Rect rect, Rect position)
    {
        Object targetObject = ascGuiCtx.TargetObject;
        AnimSequenceConfig animSequenceConfig = ascGuiCtx.SequenceConfig;
        SerializedProperty stepConfigs = ascGuiCtx.StepConfigs;
        SerializedProperty addIndex = ascGuiCtx.AddIndex;
        SerializedProperty removeIndex = ascGuiCtx.RemoveIndex;
        SerializedProperty switchIndex1 = ascGuiCtx.SwitchIndex1;
        SerializedProperty switchIndex2 = ascGuiCtx.SwitchIndex2;

        //---- 获取变量值 ----
        // Switch 1 的索引
        int swIndex1 = switchIndex1.intValue;
        // Switch 2 的索引
        int swIndex2 = switchIndex2.intValue;
        // 数组长度
        int arraySize = ascGuiCtx.ArraySize;

        //检测索引是否合法
        bool IsValidIndex(int index)
        {
            return index >= 0 && index < arraySize;
        }

        rect.width /= 4;
        rect.height = 18;
        rect.x = position.xMax / 2 - rect.width;
        rect.y += 45;

        #region Buttons
        // 绘制“添加到最后”按钮
        if (GUI.Button(rect, "Add to last"))
        {
            Undo.RecordObject(targetObject, null);

            string groupName;
            if (arraySize > 0)
            {
                List<AnimStepConfig> stepList = new(animSequenceConfig.StepConfigs) { new() };
                animSequenceConfig.StepConfigs = stepList.ToArray();
                groupName = $"{targetObject}: Added new element.";
            }
            else
            {
                animSequenceConfig.StepConfigs = new AnimStepConfig[1] { new() };
                groupName = $"{targetObject}: The first element has been initialized.";
            }

            Undo.SetCurrentGroupName(groupName);
            EditorUtility.SetDirty(targetObject);
        }
        rect.y += 20;

        // 绘制“按索引添加”按钮
        if (GUI.Button(rect, "Add by index"))
        {
            int addIndexValue = Mathf.Clamp(ascGuiCtx.AddIndex.intValue, 0, arraySize);
            Undo.SetCurrentGroupName(targetObject + ": Added new element.");
            Undo.RecordObject(targetObject, null);
            List<AnimStepConfig> settingList = new(animSequenceConfig.StepConfigs);
            settingList.Insert(addIndexValue, new());
            animSequenceConfig.StepConfigs = settingList.ToArray();
            EditorUtility.SetDirty(targetObject);
        }
        rect.y += 20;

        // 绘制“按索引移除”按钮
        if (GUI.Button(rect, "Remove by index"))
        {
            int removeIndexValue = ascGuiCtx.RemoveIndex.intValue;
            // 检查索引是否有效
            if (IsValidIndex(removeIndexValue))
            {
                RemoveElement(stepConfigs, targetObject, animSequenceConfig, removeIndexValue);
            }
            else
            {
                Debug.LogWarning($"[Animation Sequence Config] Invalid index ({removeIndexValue}) — valid range: 0 to {arraySize - 1}.");
            }
        }
        rect.y += 20;

        // 绘制“交换元素”按钮
        if (GUI.Button(rect, "Switch element"))
        {
            /* 交换算法解释：
             * 使用元组解构语法 (a, b) = (b, a) 来交换两个元素的位置。
            */
            if (swIndex1 != swIndex2 && IsValidIndex(swIndex1) && IsValidIndex(swIndex2))
            {
                Undo.RecordObject(targetObject, null);
                (animSequenceConfig.StepConfigs[swIndex1], animSequenceConfig.StepConfigs[swIndex2]) = (animSequenceConfig.StepConfigs[swIndex2], animSequenceConfig.StepConfigs[swIndex1]);
                EditorUtility.SetDirty(targetObject);
                Undo.SetCurrentGroupName(targetObject + ": Switched element.");
            }
            else
                Debug.LogWarning($"[Animation Sequence Config] Failed to switch elements: swIndex1={swIndex2}, swIndex2={swIndex2}, arraySize={arraySize}. Ensure indices are valid and not equal.");
        }
        rect.position -= new Vector2(-rect.width, 60);

        // 绘制“移除最后”按钮
        if (GUI.Button(rect, "Remove the last"))
        {
            if (arraySize > 0)
            {
                RemoveElement(stepConfigs, targetObject, animSequenceConfig, arraySize - 1);
            }
        }
        #endregion

        #region Index Fields
        rect.y += 20;
        // 绘制“添加索引”输入框
        addIndex.intValue = EditorGUI.IntField(rect, addIndex.intValue);
        rect.y += 20;
        // 绘制“移除索引”输入框
        removeIndex.intValue = EditorGUI.IntField(rect, removeIndex.intValue);
        rect.y += 20;
        rect.width /= 2;
        // 绘制“索引1”输入框
        switchIndex1.intValue = EditorGUI.IntField(rect, switchIndex1.intValue);
        rect.x += rect.width;
        // 绘制“索引2”输入框
        switchIndex2.intValue = EditorGUI.IntField(rect, switchIndex2.intValue);
        #endregion

        EditorGUILayout.Space(100);

        return rect;
    }

    // ---- 次级折叠面板 ----
    private void DrawSecondaryFoldout(AnimSequenceConfigGUIContext ascGuiCtx, Rect rect, Rect position)
    {
        SerializedProperty foldout2 = ascGuiCtx.Foldout2;
        SerializedProperty stepConfigs = ascGuiCtx.StepConfigs;
        // 数组长度
        int arraySize = ascGuiCtx.ArraySize;

        rect = new(new Vector2(_isUnity6 ? 40 : 20, rect.y + 20), position.size);

        // 绘制次级折叠面板
        foldout2.boolValue = EditorGUI.BeginFoldoutHeaderGroup(rect, foldout2.boolValue, "All", EditorStyles.foldout);
        EditorGUI.EndFoldoutHeaderGroup();

        // 次级折叠面板内容
        if (foldout2.boolValue)
        {
            if (arraySize > 0)
            {
                // 增加缩进级别
                EditorGUI.indentLevel += 3;
                int arrayLength = stepConfigs.arraySize;
                // 遍历数组元素（设置）
                for (int i = 0; i < arrayLength; i++)
                {
                    // 获取数组元素
                    SerializedProperty element = stepConfigs.GetArrayElementAtIndex(i);
                    rect = EditorGUILayout.BeginVertical();
                    // 为每个元素创建一个带边框的面板
                    GUI.Box(new Rect(rect.x + 30, rect.y, rect.width - 28, rect.height + 2), GUIContent.none, EditorStyles.helpBox);
                    // 绘制元素
                    EditorGUILayout.PropertyField(element, GUIContent.none);
                    EditorGUILayout.EndVertical();
                }
                // 恢复缩进级别
                EditorGUI.indentLevel -= 3;
            }
            else
            {
                // 增加缩进级别
                EditorGUI.indentLevel += 2;
                EditorGUILayout.LabelField("Is empty.");
                // 恢复缩进级别
                EditorGUI.indentLevel -= 2;
            }
        }
    }

    /// <summary>
    /// 在移除元素之前，将索引设置为 -1。
    /// </summary>
    void RemoveElement(SerializedProperty stepConfigs, Object targetObject, AnimSequenceConfig animSequenceConfig, int index)
    {
        stepConfigs = new SerializedObject(targetObject).FindProperty(stepConfigs.propertyPath);
        SerializedProperty element = stepConfigs.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("_index").intValue = -1;
        stepConfigs.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        Undo.SetCurrentGroupName(targetObject + ": Removed element.");
        Undo.RecordObject(targetObject, null);
        var list = animSequenceConfig.StepConfigs.ToList();
        list.RemoveAt(index);
        animSequenceConfig.StepConfigs = list.ToArray();
        EditorUtility.SetDirty(targetObject);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }

    void ShowEditorPreviewButton(SerializedProperty property, AnimSequenceConfig animSequenceConfig, Object targetObject)
    {
        if (EditorApplication.isPlaying)
            return;
        EditorGUILayout.LabelField("<b>Main Animations Sequence Preview:</b>", _header);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("_resetAfterMainPreview"));


        bool isPreviewRunning = animSequenceConfig?.MainAnimPreviewTokenSource?.IsCancellationRequested ?? true;
        if (isPreviewRunning)
        {
            //启动预览
            if (GUILayout.Button("Play Preview"))
            {
                PlayPreview(animSequenceConfig, property.propertyPath, (MonoBehaviour)targetObject);
            }
        }
        else
        {
            if (GUILayout.Button("Stop preview"))
            {
                //停止预览
                animSequenceConfig.MainAnimPreviewTokenSource.Cancel();
            }
        }
    }

    private void PlayPreview(AnimSequenceConfig animSequenceConfig, string propertyPath, MonoBehaviour mono)
    {
        // 检查是否有活动的子动画预览
        if (animSequenceConfig.ActiveSubAnimCount > 0)
        {
            Debug.LogWarning("[EditorPreview] Cannot start main animation preview while sub-animation previews are active.");
            return;
        }

        if (animSequenceConfig.StepConfigs.Length == 0)
        {
            Debug.LogWarning("[EditorPreview] The sequence is empty. Cannot preview.");
            return;
        }

        AnimationSequence animationSequence = animSequenceConfig.ExportAnimationSequence();

        if (animationSequence.Items.Count == 0)
        {
            Debug.LogWarning("[EditorPreview] There are no elements in the sequence; they may not be set up correctly.");
            return;
        }

        animSequenceConfig.MainAnimPreviewTokenSource = new();
        animationSequence.StartEditorPreview(animSequenceConfig.MainAnimPreviewTokenSource, animSequenceConfig.ResetAfterMainPreview, mono.name);
        StopPreviewOnChangeAsync(animSequenceConfig, propertyPath, mono);
    }

    /// <summary>
    /// 当设置项在编辑器中发生 Reset、Remove时，停止预览动画。
    /// </summary>
    static async void StopPreviewOnChangeAsync(AnimSequenceConfig animSequenceConfig, string animSequenceConfigPath, MonoBehaviour mono)
    {
        int initiaSubAnimCount = animSequenceConfig.StepConfigs.Length;
        List<AnimStepConfig> orderOfsteps = new(animSequenceConfig.StepConfigs);
        bool isGroupStillValid = true;
        bool isSameSteps = true;

        bool IsGroupStillValid()
        {
            return UtilityExtension.GetObjectInstanceByReflection(animSequenceConfigPath, mono) == animSequenceConfig;
        }

        bool shouldContinue() =>
        !animSequenceConfig.MainAnimPreviewTokenSource.IsCancellationRequested
        && (isGroupStillValid = IsGroupStillValid())
        && (isSameSteps = orderOfsteps.SequenceEqual(animSequenceConfig.StepConfigs)
            && animSequenceConfig.StepConfigs.Length == initiaSubAnimCount)
        && !EditorApplication.isCompiling
        && mono;

        while (shouldContinue()) // 检测是否被 Reset/Remove/...
        {
            await Task.Delay(33);
        }

        if (!animSequenceConfig.MainAnimPreviewTokenSource.IsCancellationRequested)
        {
            // 检查 MonoBehaviour 是否有效
            if (!mono)
            {
                Undo.RevertAllInCurrentGroup();
                Debug.LogWarning("[EditorPreview] Cannot remove MonoBehaviour during active animation preview.");
            }
            else if (isGroupStillValid || !isSameSteps)
            {
                Undo.RevertAllInCurrentGroup();
                Debug.LogWarning("[EditorPreview] Cannot reset the MonoBehaviour or modify the Anim sequence config while the preview is running. Please stop the preview first.");
            }
        }

        animSequenceConfig.MainAnimPreviewTokenSource.Cancel();

        WindowViewUtils.RepaintInspectorWindow();
    }
}
