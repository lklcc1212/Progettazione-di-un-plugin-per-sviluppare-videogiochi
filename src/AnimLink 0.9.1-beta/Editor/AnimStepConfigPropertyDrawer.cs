using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using AnimLink;
using static AnimLink.AnimSequenceConfig;
using static AnimLink.AnimSequenceConfig.AnimStepConfig;

using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(AnimStepConfig))]
public class AnimStepConfigPropertyDrawer : PropertyDrawer
{

    // Enum -> Type
    static readonly Dictionary<AlphaCompType, Type> DoAlphaObjectTypeToComponentTypeMap = new()
        {
            { AlphaCompType.Sprite, typeof(SpriteRenderer)},
            { AlphaCompType.Graphic, typeof(Graphic) },
            { AlphaCompType.CanvasGroup, typeof(CanvasGroup) },
            { AlphaCompType.Material, typeof(Material) },
            { AlphaCompType.Mesh, typeof(MeshRenderer) }
        };
    // Enum -> Type
    static readonly Dictionary<ColorCompType, Type> DoColorObjectTypeToComponentTypeMap = new()
        {
            { ColorCompType.Sprite, typeof(SpriteRenderer)},
            { ColorCompType.Graphic, typeof(Graphic) },
            { ColorCompType.Material, typeof(Material) },
            { ColorCompType.Mesh, typeof(MeshRenderer) }
        };


    private static readonly GUIStyle _header = new(EditorStyles.label)
    {
        richText = true,
        alignment = TextAnchor.MiddleCenter
    };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty index = property.FindPropertyRelative("_index");
        SerializedProperty foldout = property.FindPropertyRelative("_foldout");
        SerializedProperty type = property.FindPropertyRelative("Type");

        EditorGUI.BeginProperty(position, label, property);

        //设置当前索引
        if (!property.propertyPath.EndsWithArrayDataIndex())
            index.intValue = -2;
        else
        {
            index.intValue = property.propertyPath.GetLastArrayDataIndex();
        }

        bool isInList = index.intValue != -2;
        StepType currentType = (StepType)type.enumValueFlag;

        // 构建标题前缀
        string titlePrefix = isInList
            ? $"{index.intValue}."
            : $"{property.PropertyPathParts()[^1]}: ";

        // 构建标题后缀
        string titleSuffix;
        if (currentType == StepType.Do)
        {
            var doType = property.FindPropertyRelative("DoType");
            titleSuffix = doType.enumDisplayNames[doType.enumValueIndex];
        }
        else
        {
            titleSuffix = type.enumDisplayNames[type.enumValueIndex];
        }

        // 渲染折叠面板
        foldout.boolValue = EditorGUI.Foldout(
            position,
            foldout.boolValue,
            new GUIContent(titlePrefix + titleSuffix),
            true
        );

        if (foldout.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            int previousCommandType = type.enumValueFlag;
            EditorGUILayout.PropertyField(type);
            if (EditorGUI.EndChangeCheck())
            {
                if (type.enumValueFlag > (int)StepType.AdvancedShake && index.intValue == -2)
                {
                    Debug.LogWarning("[AnimLink] Delay and Function cannot be used with a separate AnimStepConfig.");
                    type.enumValueFlag = previousCommandType;
                }
                else
                {
                    HandleOnTypeChange(property);
                }
            }

            switch (currentType)
            {
                #region Do
                case StepType.Do:
                    HandleDoProperty(property, isInList);
                    break;
                #endregion
                #region  Shake & Advanced Shake
                case StepType.Shake or StepType.AdvancedShake:
                    HandleShakeProperty(property, currentType == StepType.AdvancedShake, isInList);
                    break;
                #endregion
                #region  Delay
                case StepType.Delay:
                    HandleDelayProperty(property);
                    break;
                #endregion
                #region Function
                case StepType.Function:
                    HandleFunctionProperty(property);
                    break;
                    #endregion
            }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();

        property.serializedObject.ApplyModifiedProperties();
    }

    void HandleDoProperty(SerializedProperty property, bool isInAnimSequenceConfig)
    {
        SerializedProperty dotype = property.FindPropertyRelative("DoType");
        Object targetObject = property.serializedObject.targetObject;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(dotype);

        if (EditorGUI.EndChangeCheck())
        {
            HandleOnTypeChange(property);
        }

        if (isInAnimSequenceConfig)
            EditorGUILayout.PropertyField(property.FindPropertyRelative("ExecuteTheNext"));

        switch ((DOType)dotype.enumValueFlag)
        {
            case DOType.DoPath:
                DrawDoPath(property, targetObject);
                break;
            case DOType.DoPosition:
                DrawDoPosition(property);
                break;
            case DOType.DoScale:
                DrawDoScale(property);
                break;
            case DOType.DoRotation:
                DrawDoRotation(property);
                break;
            case DOType.DoAlpha:
                DrawDoAlpha(property);
                break;
            case DOType.DoColor:
                DrawDoColor(property);
                break;
            case DOType.DoJolt:
                DrawDoJolt(property);
                break;
        }
        if (dotype.enumValueFlag != 0)
        {
            ShowEditorPreviewAnimationButton(property);
        }
    }

    void DrawDoPath(SerializedProperty property, Object targetObject)
    {
        SerializedProperty loopType2 = property.FindPropertyRelative("LoopType2");
        SerializedProperty transforms_tovectors = property.FindPropertyRelative("TransformsToVectors");
        SerializedProperty pathType = property.FindPropertyRelative("PathType");
        SerializedProperty showPath = property.FindPropertyRelative("_showPath");
        SerializedProperty followPathRotation = property.FindPropertyRelative("FollowPathRotation");

        EditorGUILayout.PropertyField(property.FindPropertyRelative("Transform"));
        DrawDoCommonFields(property);
        EditorGUILayout.HelpBox("Back and Elastic easing (InBack/OutBack, InElastic/OutElastic, InOutBack/Elastic) is not recommended for Catmull-Rom or Cubic Bezier animations.", MessageType.Warning);
        EditorGUILayout.PropertyField(pathType);

        PathType path_type = (PathType)pathType.enumValueFlag;
        bool isCubicBezier = path_type == PathType.CubicBezier;

        EditorGUILayout.PropertyField(loopType2, new GUIContent("Loop Type"));
        PathLoop loopType = (PathLoop)loopType2.enumValueFlag;
        switch (loopType)
        {
            case PathLoop.PingPong or PathLoop.FromStart:
                if (path_type == PathType.CatmullRom)
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("EnableCatmullRomCorrection"));
                break;
            case PathLoop.Loop:
                if (isCubicBezier)
                {
                    loopType2.enumValueFlag = 0;// FromStart
                }
                break;
        }

        EditorGUILayout.PropertyField(followPathRotation);
        if (followPathRotation.boolValue)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("IgnoreDecreasingEasedT"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("FacingAxis"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("RotationDimension"));
        }

        EditorGUILayout.PropertyField(property.FindPropertyRelative("UseFixedDeltaTime"));
        EditorGUILayout.PropertyField(transforms_tovectors);

        if (isCubicBezier)
            EditorGUILayout.HelpBox("Cubic Bezier curves don't support loops.\nThe number of control points must be 4 + 3n!", MessageType.Info);
        else
            EditorGUILayout.HelpBox("For Linear and Catmull-Rom, the number of control points must be at least 2!", MessageType.Info);

        if (transforms_tovectors.boolValue)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Transforms"));
        }
        else
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Vectors"));
        }

        if (path_type != PathType.LinearPath)
        {
            EditorGUILayout.IntSlider(property.FindPropertyRelative("SamplesPerSegment"), DoPath.MIN_SAMPLE_COUNT, DoPath.MAX_SAMPLE_COUNT, "Samples Per Segment");
        }
        EditorGUILayout.LabelField("<b>Show Path Options</b>", _header);
        EditorGUILayout.PropertyField(showPath);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("_lineColor"));
        if (showPath.boolValue)
        {
            //处理显示路径
            AnimStepConfig setting = (AnimStepConfig)UtilityExtension.GetObjectInstanceByReflection(property.propertyPath, targetObject);
            if (setting != null)
                //如果没有显示Path了....
                if (setting.ShowPathCancellationTokenSource?.IsCancellationRequested ?? true)
                {
                    setting.ShowPathCancellationTokenSource = new();
                    setting.StartShowingPathAsync(setting.ShowPathCancellationTokenSource.Token, (MonoBehaviour)targetObject, property.propertyPath);
                }
        }
    }

    void DrawDoPosition(SerializedProperty property)
    {
        SerializedProperty loopType1 = property.FindPropertyRelative("LoopType1");
        SerializedProperty position_field = property.FindPropertyRelative("Position");
        SerializedProperty tr_to_vec3 = property.FindPropertyRelative("TrToVec3");
        SerializedProperty target_transform = property.FindPropertyRelative("TargetTransform");
        SerializedProperty space = property.FindPropertyRelative("Space");
        SerializedProperty isModifyingAxis = property.FindPropertyRelative("IsModifyingAxis");

        EditorGUILayout.PropertyField(property.FindPropertyRelative("Transform"));
        DrawDoCommonFields(property);
        EditorGUILayout.PropertyField(loopType1, new GUIContent("Loop Type"));
        EditorGUILayout.PropertyField(space);
        EditorGUILayout.PropertyField(isModifyingAxis);

        if (isModifyingAxis.boolValue)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Axis"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Value"));
        }
        else
        {

            if ((BaseLoop)loopType1.enumValueFlag != BaseLoop.Increment)
            {
                if (!space.boolValue)
                    EditorGUILayout.PropertyField(tr_to_vec3, new GUIContent("Transform To Vector3"));

                if (tr_to_vec3.boolValue && !space.boolValue)
                    EditorGUILayout.PropertyField(target_transform);
                else
                    EditorGUILayout.PropertyField(position_field);
            }
            else
            {
                EditorGUILayout.PropertyField(position_field);
            }
        }

        EditorGUILayout.PropertyField(property.FindPropertyRelative("UseFixedDeltaTime"));
    }

    void DrawDoScale(SerializedProperty property)
    {
        SerializedProperty isModifyingAxis = property.FindPropertyRelative("IsModifyingAxis");

        EditorGUILayout.PropertyField(property.FindPropertyRelative("Transform"));
        DrawDoCommonFields(property);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("LoopType1"), new GUIContent("Loop Type"));
        EditorGUILayout.PropertyField(isModifyingAxis);

        if (isModifyingAxis.boolValue)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Axis"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Value"));
        }
        else
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Scale"));
        }

        EditorGUILayout.PropertyField(property.FindPropertyRelative("UseFixedDeltaTime"));
    }

    void DrawDoRotation(SerializedProperty property)
    {
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Transform"));
        DrawDoCommonFields(property);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Space"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("LoopType1"), new GUIContent("Loop Type"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("RotateMode"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Angle"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("UseFixedDeltaTime"));
    }

    void DrawDoAlpha(SerializedProperty property)
    {
        SerializedProperty alpha = property.FindPropertyRelative("Alpha");
        SerializedProperty material_index = property.FindPropertyRelative("MaterialIndex");
        SerializedProperty alphaCompType = property.FindPropertyRelative("AlphaCompType");
        SerializedProperty component = property.FindPropertyRelative("Component");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(alphaCompType, new GUIContent("Component Type"));
        if (EditorGUI.EndChangeCheck())
        {
            component.objectReferenceValue = null;
        }

        AlphaCompType enum1 = (AlphaCompType)alphaCompType.enumValueFlag;
        if (DoAlphaObjectTypeToComponentTypeMap.TryGetValue(enum1, out Type componentType1))
        {
            EditorGUILayout.ObjectField(component, componentType1);
            if (enum1 == AlphaCompType.Mesh)
            {
                EditorGUILayout.PropertyField(material_index);
                ShowClearMaterialPropertyBlockButton(component, material_index);
            }
        }

        DrawDoCommonFields(property);
        alpha.floatValue = EditorGUILayout.Slider(new GUIContent("Alpha"), alpha.floatValue, 0, 1);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("LoopType3"), new GUIContent("Loop Type"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("UseFixedDeltaTime"));
    }

    void DrawDoColor(SerializedProperty property)
    {
        SerializedProperty colorCompType = property.FindPropertyRelative("ColorCompType");
        SerializedProperty material_index = property.FindPropertyRelative("MaterialIndex");
        SerializedProperty component = property.FindPropertyRelative("Component");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(colorCompType, new GUIContent("Component Type"));
        if (EditorGUI.EndChangeCheck())
        {
            component.objectReferenceValue = null;
        }
        ColorCompType @enum2 = (ColorCompType)colorCompType.enumValueFlag;
        if (DoColorObjectTypeToComponentTypeMap.TryGetValue(@enum2, out Type componentType2))
        {
            EditorGUILayout.ObjectField(component, componentType2);
            if (@enum2 == ColorCompType.Mesh)
            {
                EditorGUILayout.PropertyField(material_index);
                ShowClearMaterialPropertyBlockButton(component, material_index);
            }
        }

        DrawDoCommonFields(property);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Color"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("WithAlpha"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("LoopType3"), new GUIContent("Loop Type"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("UseFixedDeltaTime"));

    }

    void DrawDoJolt(SerializedProperty property)
    {
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Transform"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Duration"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Direction"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Vibratio"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Elasticity"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("JoltType"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("UseFixedDeltaTime"));
    }

    void DrawDoCommonFields(SerializedProperty property)
    {
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Duration"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Loops"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Ease"));
    }

    void HandleShakeProperty(SerializedProperty property, bool isAdvancedShake, bool isInAnimSequenceConfig)
    {
        SerializedProperty ActiveAxes = property.FindPropertyRelative("ActiveAxes");
        SerializedProperty transform = property.FindPropertyRelative("Transform");

        if (isInAnimSequenceConfig)
            EditorGUILayout.PropertyField(property.FindPropertyRelative("ExecuteTheNext"));

        if (isAdvancedShake)
        {
            EditorGUILayout.PropertyField(transform);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Frequency"));
            EditorGUILayout.HelpBox("It shows an error after editing AnimationCurve, please don't worry about it.", MessageType.Info);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("MagnitudeCurve"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("ReturnCurve"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("ReturnDuration"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("AdvancedShakeType"));
        }
        else
        {
            EditorGUILayout.PropertyField(transform);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("ShakeType"));
        }

        ActiveAxes.enumValueFlag = Convert.ToInt16(EditorGUILayout.EnumFlagsField(new GUIContent("Axis"), (Axis)ActiveAxes.enumValueFlag));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Duration"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Magnitude"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("UseFixedDeltaTime"));
        ShowEditorPreviewAnimationButton(property);
    }

    void HandleDelayProperty(SerializedProperty property)
    {
        EditorGUILayout.PropertyField(property.FindPropertyRelative("DelayTime"), new GUIContent("Delay Time(s)"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("RealTime"));
    }

    void HandleFunctionProperty(SerializedProperty property)
    {
        SerializedProperty use_functionplus = property.FindPropertyRelative("UseFunctionPlus");

        EditorGUILayout.PropertyField(use_functionplus);
        if (use_functionplus.boolValue)
        {
            SerializedProperty functionplus = property.FindPropertyRelative("FunctionPlus");
            functionplus.FindPropertyRelative("_isInListOrArray").boolValue = true;
            Rect rect = EditorGUILayout.BeginVertical();
            int indentXOffset = EditorGUI.indentLevel * 15;
            GUI.Box(new(rect.x + indentXOffset, rect.y, rect.width - indentXOffset, rect.height), GUIContent.none, EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(functionplus);
            if (functionplus.isExpanded && !functionplus.FindPropertyRelative("_isMissing").boolValue && functionplus.FindPropertyRelative("_isIEnumerator").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(property.FindPropertyRelative("WaitingForMethodExecutionComplete"));
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        else
        {
            SerializedProperty function = property.FindPropertyRelative("Function");
            Rect rect = EditorGUILayout.BeginHorizontal();
            rect.x += 55;
            rect.width -= 55;
            EditorGUI.PropertyField(rect, function);
            int arraySize = function.FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize;
            EditorGUILayout.Space(arraySize == 0 ? 95 : 48 + arraySize * 49);
            EditorGUILayout.EndHorizontal();
        }
    }

    void HandleOnTypeChange(SerializedProperty property)
    {
        property.FindPropertyRelative("CancelPreviewAnimation").boolValue = true;
        property.serializedObject.ApplyModifiedProperties();
    }

    void ShowClearMaterialPropertyBlockButton(SerializedProperty meshRendererProperty, SerializedProperty indexProperty)
    {
        Rect rect = EditorGUILayout.GetControlRect();
        int indentOffset = EditorGUI.indentLevel * 15;
        rect.x += indentOffset;
        rect.width -= indentOffset;

        if (GUI.Button(rect, new GUIContent("Clear _Color by index(MaterialPropertyBlock)")))
        {
            if (meshRendererProperty.objectReferenceValue is MeshRenderer meshRenderer)
            {
                int index = indexProperty.intValue;
                ClearMaterialPropertyBlock(meshRenderer, index);
            }
        }
    }

    void ClearMaterialPropertyBlock(MeshRenderer meshRenderer, int index)
    {
        int materialsLength = meshRenderer.sharedMaterials.Length;
        if (index < 0 || index >= materialsLength)
        {
            Debug.LogError($"Invalid material index: {index}. MeshRenderer has {materialsLength} material(s). Index must be in range [0, {materialsLength - 1}].");
            return;
        }

        //清除_Color
        MaterialPropertyBlock block = new();
        meshRenderer.GetPropertyBlock(block, index);
        block = block.Clear(MaterialPropertyBLockCleaner.Name_._Color);
        meshRenderer.SetPropertyBlock(block, index);

        //刷新Views
        WindowViewUtils.RepaintGameView();
        SceneView.RepaintAll();
    }

    void ShowEditorPreviewAnimationButton(SerializedProperty property)
    {
        if (EditorApplication.isPlaying)
            return;
        EditorGUILayout.LabelField("<b>Editor Preview</b>", _header);

        SerializedProperty resetAfterPreview = property.FindPropertyRelative("_resetAfterPreview");
        EditorGUILayout.PropertyField(resetAfterPreview);

        Object targetObject = property.serializedObject.targetObject;
        string animationStepConfigPath = property.propertyPath;
        AnimStepConfig animationStepConfig = (AnimStepConfig)UtilityExtension.GetObjectInstanceByReflection(animationStepConfigPath, targetObject);

        Rect buttonRect = EditorGUILayout.GetControlRect();
        float indentationOffset = EditorGUI.indentLevel * 15;
        buttonRect.x += indentationOffset;
        buttonRect.width -= indentationOffset;

        bool isPreviewRunning = animationStepConfig?.PreviewEditorTokenSource?.IsCancellationRequested ?? true;
        if (isPreviewRunning)
        {
            if (GUI.Button(buttonRect, "Play preview"))
            {
                PlayPreview(animationStepConfig, animationStepConfigPath, resetAfterPreview.boolValue, (MonoBehaviour)targetObject);
            }
        }
        else
        {
            if (GUI.Button(buttonRect, "Stop preview"))
            {
                //停止预览
                animationStepConfig.PreviewEditorTokenSource.Cancel();
            }
        }
    }

    private void PlayPreview(AnimStepConfig animStepConfig, string animStepConfigPath, bool resetAfterPreview, MonoBehaviour mono)
    {
        AnimSequenceConfig animSequenceConfig = null;
        IAnimation iAnimation = animStepConfig.ExportAnimation();

        if (iAnimation == null)
        {
            Debug.LogError("[EditorPreview] Configuration not set up correctly.");
            return;
        }

        if (animStepConfig.Index >= 0)
        {
            string[] pathParts = animStepConfigPath.Split(".");
            string animSequenceConfigPath = string.Join(".", pathParts[..^3]);

            if (UtilityExtension.GetObjectInstanceByReflection(animSequenceConfigPath, mono) is AnimSequenceConfig config)
            {
                animSequenceConfig = config;
                bool isMainAnimPreviewRunning = config.MainAnimPreviewTokenSource?.IsCancellationRequested == false;
                if (isMainAnimPreviewRunning)
                {
                    Debug.LogWarning("[EditorPreview] AnimSequenceConfig's main animation preview is already running. Please stop it before starting a new one.");
                    return;
                }
                else
                    ++config.ActiveSubAnimCount;
            }
        }

        animStepConfig.CancelPreviewAnimation = false;
        animStepConfig.PreviewEditorTokenSource = new();
        iAnimation.PlayEditorPreview(animStepConfig.PreviewEditorTokenSource, resetAfterPreview, true, mono.name);
        StopPreviewOnChangeAsync(animStepConfig, animSequenceConfig, animStepConfigPath, mono);
    }

    /// <summary>
    /// 当设置项在编辑器中发生 Reset、Remove 或 Switch（仅适用于数组/List）时，停止预览动画。
    /// </summary>
    static async void StopPreviewOnChangeAsync(AnimStepConfig animStepConfig, AnimSequenceConfig animSequenceConfig, string animStepConfigPath, MonoBehaviour mono)
    {
        string initialId = animStepConfig.GetNewAnimationPreviewTempID();
        CancellationToken previewToken = animStepConfig.PreviewEditorTokenSource.Token;
        string stepConfigsPath = animStepConfigPath.ReplaceLastArrayDataOccurrence();
        bool isAnimationValid = true;

        bool IsAnimationStillValid()
        {
            // 已被移除
            if (animStepConfig.Index == -1)
                return false;

            //持续监测
            // 如果对象已被移除或重设，视为无效
            if (UtilityExtension.GetObjectInstanceByReflection(animStepConfigPath, mono) is AnimStepConfig command && command.PreviewEditorTokenSource != null)
                return true;

            // 如果对象是单独的设置项且已被移除或重设，视为无效
            if (animStepConfig.Index == -2)
                return false;

            if (UtilityExtension.GetObjectInstanceByReflection(stepConfigsPath, mono)
                is not IEnumerable<AnimStepConfig> settings)
                return false;

            // 如果对象是数组或列表中的一项，检查引用是否仍然存在 (更新索引)
            int newIndex = 0;
            foreach (var item in settings)
            {
                if (ReferenceEquals(item, animStepConfig))
                {
                    // 更新路径中的索引
                    animStepConfigPath = animStepConfigPath.ReplaceLastArrayDataIndex(newIndex);
                    return true;
                }
                newIndex++;
            }

            return false;
        }

        bool ShouldContinuePreview() =>
            !animStepConfig.CancelPreviewAnimation
            && !previewToken.IsCancellationRequested
            && animStepConfig.AnimationPreviewTempID == initialId // 检测是否被 Switch。当两个setting被互换时，id会发生改变。（TempID 改变 | 仅List和Array）
            && (isAnimationValid = IsAnimationStillValid())  // 检测是否被 Reset/Remove
            && !EditorApplication.isCompiling
            && mono;

        while (ShouldContinuePreview())  // 检测是否被 Reset/Remove/...
        {
            await Task.Delay(33);
        }

        if (!previewToken.IsCancellationRequested)
        {
            //如果调用AnimationPreview的Mono消失了，Animation的引用发生改变或Animation被移除, 则Redo并警告用户。
            // 检查 MonoBehaviour 是否有效
            if (!mono)
            {
                Undo.RevertAllInCurrentGroup();
                Debug.LogWarning("[EditorPreview] Cannot remove MonoBehaviour during active animation preview.");
            }
            else
            {
                AnimStepConfig animationStepConfig = (AnimStepConfig)UtilityExtension.GetObjectInstanceByReflection(animStepConfigPath, mono);
                // 当 Preview 被 Reset 或 Remove 或 Settings发生改变(仅List和Array)后(id不一致)，才为 true
                if (!isAnimationValid || animationStepConfig.AnimationPreviewTempID == string.Empty && animationStepConfig.Index == -2 || animationStepConfig.AnimationPreviewTempID != initialId)
                {
                    Undo.RevertAllInCurrentGroup();
                    Debug.LogWarning("[EditorPreview] Cannot reset MonoBehaviour or remove Animation during active animation preview. (This may also occur if references have changed)");
                }
            }
        }

        //重置取消标志
        animStepConfig.CancelPreviewAnimation = false;
        //停止预览
        animStepConfig.PreviewEditorTokenSource.Cancel();

        //减少活动子动画的计数
        if (animSequenceConfig != null)
            --animSequenceConfig.ActiveSubAnimCount;

        WindowViewUtils.RepaintInspectorWindow();
    }
}