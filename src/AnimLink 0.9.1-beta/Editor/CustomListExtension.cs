using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using AnimLink;
using static AnimLink.CustomValue;

using Object = UnityEngine.Object;

static public class CustomListExtension
{
    /// <summary>
    /// 根据 CustomList 的值类型返回一个新的 ReorderableList。
    /// </summary>
    static internal ReorderableList GetNewReorderableList(this CustomList customList, Object forUndo_SetDirty)
    {
        return customList.TypeOfValue switch
        {
            TypeOfValue.Object => CreateReorderableList(customList.Objects, typeof(Object), forUndo_SetDirty, "Object list:", (rect, index, isActive, isFocused) =>
                {
                    rect = new(rect.x, rect.y + 3, rect.width, 18);
                    var current = customList.Objects[index];
                    var newObj = EditorGUI.ObjectField(rect, GUIContent.none, current, customList.ObjectType.Type, true);
                    if (current != newObj)
                    {
                        Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                        customList.UpdateListValue(index, newObj);
                        EditorUtility.SetDirty(forUndo_SetDirty);
                    }
                }),
            TypeOfValue.Int => CreateReorderableList(customList.IntValues, typeof(int), forUndo_SetDirty, "Int list:", (rect, index, isActive, isFocused) =>
            {
                rect = new(rect.x, rect.y + 3, rect.width, 18);
                var current = customList.IntValues[index];
                var newObj = EditorGUI.DelayedIntField(rect, GUIContent.none, current);
                if (current != newObj)
                {
                    Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                    customList.UpdateListValue(index, newObj);
                    EditorUtility.SetDirty(forUndo_SetDirty);
                }
            }),
            TypeOfValue.Float => CreateReorderableList(customList.FloatValues, typeof(float), forUndo_SetDirty, "Float list:", (rect, index, isActive, isFocused) =>
                {
                    rect = new(rect.x, rect.y + 3, rect.width, 18);
                    var current = customList.FloatValues[index];
                    var newObj = EditorGUI.DelayedFloatField(rect, GUIContent.none, current);
                    if (current != newObj)
                    {
                        Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                        customList.UpdateListValue(index, newObj);
                        EditorUtility.SetDirty(forUndo_SetDirty);
                    }
                }),
            TypeOfValue.String => CreateReorderableList(customList.StringValues, typeof(string), forUndo_SetDirty, "String list:", (rect, index, isActive, isFocused) =>
                {
                    rect = new(rect.x, rect.y + 3, rect.width, 18);
                    var current = customList.StringValues[index];
                    var newObj = EditorGUI.TextArea(rect, current);
                    if (current != newObj)
                    {
                        Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                        customList.UpdateListValue(index, newObj);
                        EditorUtility.SetDirty(forUndo_SetDirty);
                    }
                }),
            TypeOfValue.Bool => CreateReorderableList(customList.BoolValues, typeof(bool), forUndo_SetDirty, "Bool list:", (rect, index, isActive, isFocused) =>
                {
                    rect = new(rect.x, rect.y + 3, rect.width, 18);
                    var current = customList.BoolValues[index];
                    var newObj = EditorGUI.Toggle(rect, GUIContent.none, current);
                    if (current != newObj)
                    {
                        Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                        customList.UpdateListValue(index, newObj);
                        EditorUtility.SetDirty(forUndo_SetDirty);
                    }
                }),
            TypeOfValue.Enum => CreateReorderableList(customList.EnumValues, typeof(Enum), forUndo_SetDirty, "Enum list:", (rect, index, isActive, isFocused) =>
            {
                rect = new(rect.x, rect.y + 3, rect.width, 18);
                var current = customList.EnumValues[index].Value ??= (Enum)Enum.GetValues(customList.EnumType.Type).GetValue(0);
                var newObj = customList.IsFlag ? EditorGUI.EnumFlagsField(rect, current) : EditorGUI.EnumPopup(rect, current);
                if (Convert.ToInt32(current) != Convert.ToInt32(newObj))
                {
                    Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                    customList.UpdateListValue(index, newObj);
                    EditorUtility.SetDirty(forUndo_SetDirty);
                }
            }),
            TypeOfValue.Vector2 => CreateReorderableList(customList.Vector2Values, typeof(Vector2), forUndo_SetDirty, "Vector2 list:", (rect, index, isActive, isFocused) =>
            {
                rect = new(rect.x, rect.y + 3, rect.width, 18);
                var current = customList.Vector2Values[index];
                var newObj = EditorGUI.Vector2Field(rect, GUIContent.none, current);
                if (current != newObj)
                {
                    Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                    customList.UpdateListValue(index, newObj);
                    EditorUtility.SetDirty(forUndo_SetDirty);
                }
            }),
            TypeOfValue.Vector3 => CreateReorderableList(customList.Vector3Values, typeof(Vector3), forUndo_SetDirty, "Vector3 list:", (rect, index, isActive, isFocused) =>
            {
                rect = new(rect.x, rect.y + 3, rect.width, 18);
                var current = customList.Vector3Values[index];
                var newObj = EditorGUI.Vector3Field(rect, GUIContent.none, current);
                if (current != newObj)
                {
                    Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                    customList.UpdateListValue(index, newObj);
                    EditorUtility.SetDirty(forUndo_SetDirty);
                }
            }),
            TypeOfValue.Quaternion => CreateReorderableList(customList.QuaternionValuesInVector3, typeof(Vector3), forUndo_SetDirty, "Quaternion list:", (rect, index, isActive, isFocused) =>
            {
                rect = new(rect.x, rect.y + 3, rect.width, 18);
                var current = customList.QuaternionValuesInVector3[index];
                var newObj = EditorGUI.Vector3Field(rect, GUIContent.none, current);
                if (current != newObj)
                {
                    Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                    customList.UpdateListValue(index, newObj);
                    EditorUtility.SetDirty(forUndo_SetDirty);
                }
            }),
            TypeOfValue.Color => CreateReorderableList(customList.ColorValues, typeof(Color), forUndo_SetDirty, "Color list:", (rect, index, isActive, isFocused) =>
            {
                rect = new(rect.x, rect.y + 3, rect.width, 18);
                var current = customList.ColorValues[index];
                var newObj = EditorGUI.ColorField(rect, GUIContent.none, current);
                if (current != newObj)
                {
                    Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                    customList.UpdateListValue(index, newObj);
                    EditorUtility.SetDirty(forUndo_SetDirty);
                }
            }),
            TypeOfValue.LayerMask => CreateReorderableList(customList.Layers, typeof(LayerMask), forUndo_SetDirty, "LayerMask list:", (rect, index, isActive, isFocused) =>
            {
                rect = new(rect.x, rect.y + 3, rect.width, 18);
                LayerMask current = customList.Layers[index];
                LayerMask newObj = EditorGUI.MaskField(rect, current, InternalEditorUtility.layers);
                if (current != newObj)
                {
                    Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Saved a value.");
                    customList.UpdateListValue(index, newObj);
                    EditorUtility.SetDirty(forUndo_SetDirty);
                }
            }),
            _ => null,
        };
    }

    /// <summary>
    /// 根据列表类型创建一个新的ReorderableList。
    /// </summary>
    static private ReorderableList CreateReorderableList<T>(List<T> list, Type elementType, Object forUndo_SetDirty, string header, Action<Rect, int, bool, bool> drawElement)
    {
        return new(list, elementType)
        {
            drawHeaderCallback = rect =>
            {
                int indentLevelSpace = EditorGUI.indentLevel * 15;
                EditorGUI.LabelField(new Rect(rect.x - indentLevelSpace, rect.y, rect.width + indentLevelSpace, rect.height), header);
            },
            drawElementCallback = (rect, index, isActive, isFocused) => drawElement(rect, index, isActive, isFocused),
            onAddCallback = list =>
            {
                Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Added a new element to the list.");
                list.index = list.list.Add(default(T));
                EditorUtility.SetDirty(forUndo_SetDirty);
            },
            onRemoveCallback = list =>
            {
                Undo.RecordObject(forUndo_SetDirty, $"{forUndo_SetDirty}: Removed an element from the list.");
                list.list.RemoveAt(list.index);
                list.index = Mathf.Clamp(list.index - 1, 0, list.list.Count);
                EditorUtility.SetDirty(forUndo_SetDirty);
            }
        };
    }
}