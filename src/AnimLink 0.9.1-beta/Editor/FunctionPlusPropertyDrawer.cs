using AnimLink;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(FunctionPlus))]
public class FunctionPlusPropertyDrawer : PropertyDrawer
{

    /// <summary>
    /// 用于在 FunctionPlus 的编辑器绘制过程中传递上下文信息，
    /// 包含当前 FunctionPlus 实例、目标对象、所引用对象以及方法菜单。
    /// </summary>
    struct FunctionPlusContext
    {
        /// <summary>当前编辑的 FunctionPlus 实例。</summary>
        public FunctionPlus functionPlus;
        /// <summary>目标GameObject。</summary>
        public Object target;
        /// <summary>正在被编辑的目标对象（通常是 MonoBehaviour）。</summary>
        public Object objectReferenceValue;
        /// <summary>用于选择方法的菜单。</summary>
        public GenericMenu menu;
    }

    private readonly static GUIStyle _popupStyle = new(EditorStyles.popup);
    private readonly static GUIStyle _titleStyle = new(EditorStyles.label)
    {
        fontSize = 20,
    };
    private readonly static GUIStyle _textStyle = new(EditorStyles.label);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 绘制折叠面板并更新展开状态
        string titlePrefix = property.FindPropertyRelative("_isInListOrArray").boolValue ? "" : $"{property.propertyPath.Split(".")[^1]}: ";
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, new GUIContent($"{titlePrefix}Function Plus"), true);

        // 如果该属性不处于展开状态
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }
        EditorGUI.indentLevel++;
        // 最终需要预留的垂直空间
        int finalSpace = 22;

        // 获取目标对象
        Object targetObject = property.serializedObject.targetObject;

        // 获取 FunctionPlus 实例
        FunctionPlus functionPlus = (FunctionPlus)UtilityExtension.GetObjectInstanceByReflection(property.propertyPath, targetObject);

        SerializedProperty objectProp = property.FindPropertyRelative("_object");

        Rect orig_rect = EditorGUILayout.GetControlRect();
        Rect rect = new(orig_rect.x, orig_rect.y, orig_rect.width / 1.5f, orig_rect.height);
        EditorGUI.PropertyField(rect, objectProp, GUIContent.none);

        if (functionPlus == null)
        {
            EditorGUI.EndProperty();
            return;
        }

        FunctionPlusContext fpContext = new()
        {
            functionPlus = functionPlus,
            target = targetObject,
            objectReferenceValue = objectProp.objectReferenceValue,
            // 菜单
            menu = new GenericMenu()
        };

        DrawMainBlock(fpContext);

        DrawButtons(ref rect, fpContext);

        finalSpace += DrawParametersBlock(fpContext, ref rect, orig_rect);

        GUILayout.Space(finalSpace);
        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();

        property.serializedObject.ApplyModifiedProperties();
    }

    #region Draw main logic block

    void DrawMainBlock(FunctionPlusContext fpContext)
    {

        if (fpContext.objectReferenceValue == null)
        {
            DisableFunctionPlus(fpContext);
            GUI.enabled = false;
            return;
        }

        if (fpContext.functionPlus._previousObject != fpContext.objectReferenceValue)
            RefreshMethodInfosIfObjectChanged(fpContext);
        else
            PopulateMenu(fpContext);
    }

    void DisableFunctionPlus(FunctionPlusContext fpContext)
    {
        FunctionPlus fp = fpContext.functionPlus;

        // 如果对象为 null，将 functionPlus 设置为无方法。
        if (fp._currentEnableMethodIndex != -1)
        {
            OnSelect(fpContext, -1, null);
            fp._previous_SMName = "";
        }
        GUI.enabled = false;
    }

    void RefreshMethodInfosIfObjectChanged(FunctionPlusContext fpContext)
    {
        FunctionPlus fp = fpContext.functionPlus;

        Object objectReferenceValue = fpContext.objectReferenceValue;
        Object target = fpContext.target;

        // 获取有效方法。
        List<MethodInfo> validMethod = GetValidMethods(objectReferenceValue);

        // 保存
        Undo.RecordObject(target, "Changed MonoBehaviour.");

        fp._sMethodInfos = validMethod.Select(t => new SerializableMethodInfo(t)).ToList();

        // 重置方法选择状态
        if (fp._currentEnableMethodIndex != -1)
        {
            fp._currentEnableMethodIndex = -1;
            SetFunctionPlusBaseVars(fp, "No Function", true, false);
        }

        fp._previousObject = (MonoBehaviour)objectReferenceValue;
        EditorUtility.SetDirty(target);
    }

    void PopulateMenu(FunctionPlusContext fpContext)
    {
        FunctionPlus fp = fpContext.functionPlus;
        Object target = fpContext.target;
        GenericMenu menu = fpContext.menu;

        // 填充菜单
        menu.AddItem(new GUIContent("No Function"), fp._currentEnableMethodIndex == -1, () => { OnSelect(fpContext, -1, null); });
        menu.AddSeparator("");
        int Count = 0;
        foreach (SerializableMethodInfo serializableMethodInfo in fp._sMethodInfos)
        {
            MethodInfo methodInfo = serializableMethodInfo.MethodInfo;
            int index = Count;
            if (methodInfo != null)
            {
                // 处理已有方法项
                bool isCurrent = fp._currentEnableMethodIndex == Count;
                string name = methodInfo.ToString();

                // 如果当前方法为“缺失”并且找到了它，则将其设置为当前方法。
                if (isCurrent && fp._isMissing)
                {
                    SetFunctionPlusBaseVars(fp, methodInfo.Name, false, methodInfo.ReturnType == typeof(IEnumerator));
                    EditorUtility.SetDirty(target);
                }

                menu.AddItem(new GUIContent(name), isCurrent, () => { OnSelect(fpContext, index, serializableMethodInfo); });
            }
            else
            {
                HandleMissingMethod(fpContext, index, serializableMethodInfo);
            }
            Count++;
        }
    }

    void HandleMissingMethod(FunctionPlusContext fpContext, int index, SerializableMethodInfo serializableMethodInfo)
    {

        FunctionPlus fp = fpContext.functionPlus;
        Object target = fpContext.target;

        // 如果当前方法为“缺失”，则将其设置为“缺失”+保存。
        fpContext.menu.AddItem(new GUIContent($"{index}. Method missing."), fp._currentEnableMethodIndex == index, () => { OnSelect(fpContext, index, serializableMethodInfo, true); });
        if (fp._currentEnableMethodIndex == index && !fp._isMissing)
        {
            SetFunctionPlusBaseVars(fp, $"{index}. Method missing.", true, false);
            EditorUtility.SetDirty(target);
        }
    }

    #endregion
    #region Draw buttons (Refresh & Menu)

    void DrawButtons(ref Rect rect, FunctionPlusContext fpContext)
    {
        FunctionPlus fp = fpContext.functionPlus;

        //显示刷新按钮
        if (GUI.Button(new Rect(rect.x + rect.width, rect.y, rect.width / 2, rect.height), new GUIContent("Refresh")))
        {
            RefreshMethodInfos(fpContext);
        }
        rect.y += 20;
        //显示菜单按钮
        if (GUI.Button(new Rect(rect.x + 15 * EditorGUI.indentLevel, rect.y, rect.width - 15 * EditorGUI.indentLevel, rect.height), new GUIContent(fp._methodName), _popupStyle))
        {
            fpContext.menu.ShowAsContext();
        }
    }

    #endregion
    #region Draw parameters

    struct ParameterSetupContext
    {
        /// <summary>
        /// 自定义储存值
        /// </summary>
        public CustomValue customValue;
        /// <summary>
        /// 参数信息
        /// </summary>
        public ParameterInfo parameterInfo;
        /// <summary>
        /// 当前的方法信息
        /// </summary>
        public MethodInfo method;
        /// <summary>
        /// 当前参数的索引
        /// </summary>
        public int paramIndex;
        /// <summary>
        /// 总参数数量
        /// </summary>
        public int parametersTotal;
    }

    int DrawParametersBlock(FunctionPlusContext fpContext, ref Rect rect, Rect origrect)
    {
        FunctionPlus fp = fpContext.functionPlus;
        Object target = fpContext.target;

        int space = 0;

        //检查是否有参数
        if (!fp.ContainsParameters())
            return space;

        MethodInfo currentMethodInfo = fp.Serializable_MethodInfo.MethodInfo;
        ParameterInfo[] parameterInfos = currentMethodInfo.GetParameters();
        int parameterLength = parameterInfos.Length;

        rect.y += 20;
        rect.width = origrect.width / 1.25f;
        EditorGUI.LabelField(rect, "Parameters:", _titleStyle);
        space += 20 * parameterLength + 20;

        // 验证并重置参数数组
        CheckAndResetParameterArray(fpContext, currentMethodInfo, parameterLength);

        Rect labelRect = new(rect);
        Rect inputRect = new(rect) { x = rect.x + rect.width / 5 };

        //显示参数
        for (int i = 0; i < parameterLength; i++)
        {
            CustomValue customValue = fp._parameters[i];
            ParameterInfo parameterInfo = parameterInfos[i];

            if (customValue == null)
                continue;

            // 如果 customValue 类型为 None，设置类型
            if (customValue.Type == CustomValue.TypeOfValue.None)
            {
                ParameterSetupContext parameterSetupContext = new()
                {
                    customValue = customValue,
                    method = currentMethodInfo,
                    parameterInfo = parameterInfo,
                    parametersTotal = parameterLength,
                    paramIndex = i
                };

                InitializeCustomValueType(fp, parameterSetupContext);
            }

            labelRect.y = inputRect.y += 20;
            DrawSingleParameter(target, parameterInfo, customValue, ref labelRect, ref inputRect, ref space);
        }
        return space;
    }

    void CheckAndResetParameterArray(FunctionPlusContext fpContext, MethodInfo method, int parametersTotal)
    {
        FunctionPlus fp = fpContext.functionPlus;
        Object target = fpContext.target;

        //验证前一个函数与当前函数的一致性
        string currentMethodInfoName = method.ToString();
        bool notTheSameMethod = fp._previous_SMName != currentMethodInfoName;
        // 更新参数设置 + 保存
        if (notTheSameMethod)
        {
            Undo.RecordObject(target, "Changed a size of parameters & Previous_SM value & Method");
            fp._parameters = new CustomValue[parametersTotal];
            fp._previous_SMName = currentMethodInfoName;
            EditorUtility.SetDirty(target);
            //Undo.CollapseUndoOperations(Undo.GetCurrentGroup()); "不确定"
        }
    }

    void InitializeCustomValueType(FunctionPlus fp, ParameterSetupContext parameterContext)
    {
        MethodInfo method = parameterContext.method;
        ParameterInfo param = parameterContext.parameterInfo;
        CustomValue cv = parameterContext.customValue;
        int paramIndex = parameterContext.paramIndex;
        int lastIndex = parameterContext.parametersTotal - 1;

        bool isFlag = false;
        // 检查是否包含 IsFlag 特性
        if (fp._isFlagAttribute == null && !fp._noAttribute)
        {
            fp._isFlagAttribute = method.GetCustomAttribute<IsFlagAttribute>(false) ?? null;
            fp._noAttribute = fp._isFlagAttribute == null;
        }
        // 设置参数类型和标志位
        if (!fp._noAttribute)
        {
            if (fp._isFlagAttribute.ParameterIndices.Contains(paramIndex))
            {
                isFlag = true;
            }
        }
        cv.SetType(param.ParameterType, isFlag);
        // 设置默认值
        if (param.HasDefaultValue)
        {
            cv.SaveValue(param.DefaultValue);
        }
        // 重置变量以供下次使用
        if (paramIndex == lastIndex)
        {
            fp._noAttribute = false;
            fp._isFlagAttribute = null;
        }
    }

    void DrawSingleParameter(Object target, ParameterInfo parameterInfo, CustomValue cv, ref Rect labelRect, ref Rect inputRect, ref int space)
    {
        // 根据参数类型绘制不同的 GUI 控件
        /*
         * 基本格式：
         * 1. 获取数据
         * 2-3. 显示位置
         * 4. 显示标题
         * 5. 显示输入界面
         * 6-9. 如果发生更改则保存
         */

        Type parameterType = parameterInfo.ParameterType;
        string parameterName = parameterInfo.Name;

        switch (cv.Type)
        {
            case CustomValue.TypeOfValue.Object:
                DrawObject(target, cv, parameterName, parameterType, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.Int:
                DrawBasicParameter<int>(target, cv, parameterName, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.Float:
                DrawBasicParameter<float>(target, cv, parameterName, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.Bool:
                DrawBasicParameter<bool>(target, cv, parameterName, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.String:
                DrawBasicParameter<string>(target, cv, parameterName, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.Enum:
                DrawEnum(target, cv, parameterName, parameterType, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.Vector2:
                DrawBasicParameter<Vector2>(target, cv, parameterName, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.Vector3:
                DrawBasicParameter<Vector3>(target, cv, parameterName, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.Quaternion:
                DrawQuaternion(target, cv, parameterName, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.Color:
                DrawBasicParameter<Color>(target, cv, parameterName, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.LayerMask:
                DrawBasicParameter<LayerMask>(target, cv, parameterName, labelRect, inputRect);
                break;
            case CustomValue.TypeOfValue.ArrayOrList:
                DrawArrayOrList(cv, target, parameterName, ref labelRect, ref inputRect, ref space);
                break;
            default:
                EditorGUI.LabelField(labelRect, $"Unsupported type: {cv.Type}", _textStyle);
                break;
        }
    }

    static class BasicParameterDrawerCache
    {
        // 字典缓存委托
        public static readonly Dictionary<CustomValue.TypeOfValue, Delegate> _drawers = new()
        {
            { CustomValue.TypeOfValue.Int, (Func<int, Rect, int>)((v, r) => EditorGUI.DelayedIntField(r, v)) },
            { CustomValue.TypeOfValue.Float, (Func<float, Rect, float>)((v, r) => EditorGUI.DelayedFloatField(r, v)) },
            { CustomValue.TypeOfValue.Bool, (Func<bool, Rect, bool>)((v, r) => EditorGUI.Toggle(r, v)) },
            { CustomValue.TypeOfValue.String, (Func<string, Rect, string>)((v, r) => EditorGUI.TextArea(r, v)) },
            { CustomValue.TypeOfValue.Vector2, (Func<Vector2, Rect, Vector2>)((v, r) => EditorGUI.Vector2Field(r,GUIContent.none, v)) },
            { CustomValue.TypeOfValue.Vector3, (Func<Vector3, Rect, Vector3>)((v, r) => EditorGUI.Vector3Field(r,GUIContent.none, v)) },
            { CustomValue.TypeOfValue.Color, (Func<Color, Rect, Color>)((v, r) => EditorGUI.ColorField(r, v)) },
            { CustomValue.TypeOfValue.LayerMask, (Func<LayerMask, Rect, LayerMask>)((v, r) => EditorGUI.MaskField(r, v, InternalEditorUtility.layers)) }
        };

        public static bool TryGetDrawer<T>(out Func<T, Rect, T> drawer, CustomValue.TypeOfValue type)
        {
            if (_drawers.TryGetValue(type, out Delegate @delegate) && @delegate is Func<T, Rect, T> func)
            {
                drawer = func;
                return true;
            }
            drawer = null;
            return false;
        }
    }

    void DrawBasicParameter<T>(Object target, CustomValue customValue, string name, Rect labelRect, Rect inputRect)
    {
        // 获取对应类型的绘制委托
        if (!BasicParameterDrawerCache.TryGetDrawer(out Func<T, Rect, T> drawFunc, customValue.Type))
        {
            EditorGUI.LabelField(labelRect, $"Unsupported type: {customValue.Type}", _textStyle);
            return;
        }

        // 显示标题
        EditorGUI.LabelField(labelRect, name + ":", _textStyle);

        // 读取当前值
        T currentValue = (T)customValue.ReadValue;

        // 绘制控件并获取新值
        T newValue = drawFunc(currentValue, inputRect);

        // 保存更改
        if (!EqualityComparer<T>.Default.Equals(currentValue, newValue))
            SaveValue(customValue, newValue, target);
    }

    void DrawEnum(Object target, CustomValue customValue, string name, Type parameterType, Rect labelRect, Rect inputRect)
    {

        Enum value = (Enum)customValue.ReadValue;

        // 如果值为 null，则设置为默认值
        if (value == null)
        {
            customValue.SaveValue(Enum.GetValues(parameterType).GetValue(0));
            return;
        }

        EditorGUI.LabelField(labelRect, name + ":", _textStyle);

        // 根据 IsFlag 特性显示 EnumPopup 或 EnumFlagsField 控件
        Enum newValue = customValue.IsFlag
            ? EditorGUI.EnumFlagsField(inputRect, value)
            : EditorGUI.EnumPopup(inputRect, value);

        if (!Equals(value, newValue))
            SaveValue(customValue, newValue, target);
    }

    void DrawObject(Object target, CustomValue customValue, string name, Type parameterType, Rect labelRect, Rect inputRect)
    {
        Object value = (Object)customValue.ReadValue;
        EditorGUI.LabelField(labelRect, name + ":", _textStyle);

        Object newValue = EditorGUI.ObjectField(inputRect, value, parameterType, true);
        if (newValue != value)
            SaveValue(customValue, newValue, target);
    }

    void DrawQuaternion(Object target, CustomValue customValue, string name, Rect labelRect, Rect inputRect)
    {
        Vector3 value = customValue.GetQuaternionInVector3();
        EditorGUI.LabelField(labelRect, name + ":", _textStyle);
        Vector3 newValue = EditorGUI.Vector3Field(inputRect, GUIContent.none, value);
        if (newValue != value)
            SaveValue(customValue, newValue, target);
    }

    void DrawArrayOrList(CustomValue customValue, Object target, string name, ref Rect labelRect, ref Rect inputRect, ref int space)
    {
        int elementHeight = 0;

        //获取 reorderableList
        ReorderableList reorderableList = customValue.CustomList.CurrentReorderableList;

        // 如果 reorderableList 不为 null，绘制它
        if (reorderableList != null)
        {
            int indentLevelSpace = EditorGUI.indentLevel * 15;
            // 绘制标签
            EditorGUI.LabelField(labelRect, name + ":", _textStyle);
            // 绘制 reorderableList
            reorderableList.DoList(new Rect(inputRect.x + indentLevelSpace, inputRect.y,
                inputRect.width - indentLevelSpace, inputRect.height));

            int count = reorderableList.list.Count;
            elementHeight = count == 0 ? 50 : count * 23 + 29;
            labelRect.y = inputRect.y += elementHeight;
        }
        else
        {
            // 如果 reorderableList 为 null，则创建一个新的
            customValue.CustomList.CurrentReorderableList = customValue.CustomList.GetNewReorderableList(target);
        }

        space += elementHeight;
    }

    #endregion
    #region Save Value

    /// <summary>
    /// 保存值并将撤销操作记录到 CustomValue 实例
    /// </summary>
    void SaveValue(CustomValue customValue, object newValue, Object targetObject)
    {
        Undo.RecordObject(targetObject, $"{targetObject}: Saved a value.");
        customValue.SaveValue(newValue);
        EditorUtility.SetDirty(targetObject);
    }

    #endregion
    #region OnMethodSelected
    void OnSelect(FunctionPlusContext fpContext, int index, SerializableMethodInfo serializableMethodInfo, bool isMissing = false)
    {
        FunctionPlus fp = fpContext.functionPlus;
        Object target = fpContext.target;

        // 如果选中的方法索引与当前启用的方法索引相同，则不做任何处理
        if (index == fp._currentEnableMethodIndex)
            return;

        MethodInfo method = serializableMethodInfo?.MethodInfo;
        bool methodIsNull = method == null;

        bool serializableMethodInfoIsNull = serializableMethodInfo == null;
        bool previousMethodIsMissing = fp._isMissing && fp._currentEnableMethodIndex != -1;
        // 处理之前缺失的方法...
        if (previousMethodIsMissing && (methodIsNull || serializableMethodInfoIsNull))
            HandleMissingMethodToOtherMethod(fp);
        // 记录撤销操作
        Undo.RecordObject(target, $"Changed method to {(methodIsNull ? "Null" : method)}.");

        fp._currentEnableMethodIndex = index;

        // 处理方法选择
        if (methodIsNull || serializableMethodInfo == null)
        {
            if (serializableMethodInfo == null)
                fp._previous_SMName = "";
            fp._parameters = new CustomValue[0];
            SetFunctionPlusBaseVars(fp, isMissing ? $"{index}. Method missing." : "No Function", true, false);
        }
        else
        {
            SetFunctionPlusBaseVars(fp, method.Name, false, method.ReturnType == typeof(IEnumerator));
        }
        EditorUtility.SetDirty(target);
    }
    #endregion
    #region  RefreshMethodInfos

    /// <summary>
    /// 刷新可用的方法信息列表
    /// </summary>
    void RefreshMethodInfos(FunctionPlusContext fpContext)
    {
        FunctionPlus fp = fpContext.functionPlus;
        Object target = fpContext.target;

        // 获取当前有效的方法列表
        List<MethodInfo> validMethod = GetValidMethods(fp._object);

        // 修复 CurrentMethodIndex / 如果当前方法为“缺失”，则将 CurrentMethod 设置为 null
        if (!fp._isMissing)
        {
            int Count = 0;
            MethodInfo methodInfo = fpContext.functionPlus.Serializable_MethodInfo.MethodInfo;
            foreach (MethodInfo M_info in validMethod)
            {
                if (M_info == methodInfo)
                {
                    Undo.RecordObject(target, "Refresh MethodInfos.");
                    fp._currentEnableMethodIndex = Count;
                    break;
                }
                Count++;
            }
        }
        else
        {
            // 处理当前方法缺失的情况
            if (fp._currentEnableMethodIndex != -1)
            {
                HandleMissingMethodToOtherMethod(fp);
                Undo.RecordObject(target, "Refresh MethodInfos.");

                // 重置为非功能状态
                fp._previous_SMName = "";
                fp._currentEnableMethodIndex = -1;
                SetFunctionPlusBaseVars(fp, "No Function", true, false);
                fp._parameters = new CustomValue[0];
            }
        }

        // 更新方法信息列表
        Undo.RecordObject(target, "Refresh MethodInfos.");
        fp._sMethodInfos = validMethod.Select(M_info => new SerializableMethodInfo(M_info)).ToList();
        EditorUtility.SetDirty(target);

        // 合并撤销操作
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }

    #endregion
    #region  Handle "Missing" method to other method

    /* 
     * english version:
     * Convert the variable that has been converted to the "missing" state to the variable before it is missing to avoid the undo being unable to change correctly.
     * E.g.
     * 1. Before missing: IsIEnumerator = true; MethodName = "Test"; IsMissing = false
     * 2. After missing: IsIEnumerator = false; MethodName = "X. Missing"; IsMissing = trueX
     * 3. Refresh method list
     * 4. (This function) IsIEnumerator = true; MethodName = "Test"; IsMissing = false
     * 5. Start recording Undo
     * 6. IsIEnumerator = false; MethodName = "No function";
     * 7. End recording Undo
     * 8. (Undo) if the the previous method is available, it will be restored to the previous method.
     * 9. IsIEnumerator = true; MethodName = "Test"; IsMissing = false
     * X = Will not be recorded by Undo, thus avoiding errors.
     * 
     * 中文版:
     * 将已转换为“缺失”状态的变量恢复为缺失之前的状态，以避免撤销操作无法正确更改。
     * 示例：
     * 1. 缺失前：IsIEnumerator = true; MethodName = "Test"; IsMissing = false
     * 2. 缺失后：IsIEnumerator = false; MethodName = "X. Missing"; IsMissing = true
     * 3. 刷新方法列表
     * 4. （本函数）IsIEnumerator = true; MethodName = "Test"; IsMissing = false
     * 5. 开始记录撤销操作
     * 6. IsIEnumerator = false; MethodName = "No function";
     * 7. 结束记录撤销操作
     * 8. （撤销）如果之前的方法可用，将恢复为之前的方法。
     * 9. IsIEnumerator = true; MethodName = "Test"; IsMissing = false
     * X = 将不会被撤销记录，从而避免错误。
     */

    void HandleMissingMethodToOtherMethod(FunctionPlus fp) =>
    SetFunctionPlusBaseVars(fp,
        fp.Serializable_MethodInfo.MethodName,
        isMissing: false,
        isIEnumerator: fp.Serializable_MethodInfo.ReturnType.Type == typeof(IEnumerator));

    #endregion
    #region Set function plus base variables

    void SetFunctionPlusBaseVars(FunctionPlus functionPlus, string methodName, bool isMissing, bool isIEnumerator)
    {
        functionPlus._methodName = methodName;
        functionPlus._isMissing = isMissing;
        functionPlus._isIEnumerator = isIEnumerator;
        // 重置参数计数缓存
        functionPlus._parameterCount = -1;
    }

    #endregion
    #region Get valid methods

    List<MethodInfo> GetValidMethods(object obj)
    {
        // 获取方法信息
        Type type = obj.GetType();
        List<MethodInfo> AllMethods = new();
        // 获取基类函数，直到MonoBehaviour类型为止
        while (type != typeof(MonoBehaviour)) // 循环条件：类型不是MonoBehaviour时继续
        {
            // 获取当前类型中显式声明的公共实例方法
            AllMethods.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
            // 更新类型为其父类类型
            type = type.BaseType;
        }
        List<MethodInfo> validMethod = new();
        // 过滤有效方法
        foreach (MethodInfo M_info in AllMethods)
        {
            // 验证返回类型（仅支持：void和IEnumerator）
            if (M_info.ReturnType != typeof(void) && M_info.ReturnType != typeof(IEnumerator))
            {
                continue;
            }
            ParameterInfo[] parameterInfos = M_info.GetParameters();
            bool isValid = true;

            // 验证参数类型
            foreach (ParameterInfo p in parameterInfos)
            {
                Type pType = p.ParameterType;

                // 支持数组或 List<T>
                if (pType.IsArray)
                {
                    pType = pType.GetElementType();
                }
                else if (pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(List<>)
                         && pType.GetGenericArguments() is { Length: 1 } args)
                {
                    pType = args[0];
                }

                // 检查类型是否合法
                if (!IsValidParameter(pType))
                {
                    isValid = false;
                    break;
                }
            }
            if (isValid)
                validMethod.Add(M_info);
        }

        return validMethod;
    }

    /// <summary>
    /// 允许的参数类型列表
    /// </summary>
    readonly Type[] ValidParameterTypes =
    {
    typeof(int), typeof(float), typeof(bool),
    typeof(string), typeof(Vector2), typeof(Vector3),
    typeof(Quaternion), typeof(Color), typeof(LayerMask),
    typeof(Object)
    };

    /// <summary>
    /// 验证参数类型是否合法
    /// </summary>
    /// <param name="p_Type">待验证的类型</param>
    /// <returns>是否属于允许的类型</returns>
    bool IsValidParameter(Type p_Type)
    {
        // 检查基础类型、Object派生类和枚举类型
        if (ValidParameterTypes.Contains(p_Type) || p_Type.IsSubclassOf(typeof(Object)) || p_Type.IsSubclassOf(typeof(Enum)))
        {
            return true;
        }
        return false;
    }


    #endregion
}