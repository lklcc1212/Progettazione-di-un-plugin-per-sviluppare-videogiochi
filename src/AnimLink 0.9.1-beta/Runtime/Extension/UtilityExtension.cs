namespace AnimLink
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.UI;

    using static AnimSequenceConfig;

    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;

    /// <summary>
    /// General utility extensions: provides tools for regex parsing (internal), exporting animation sequences,
    /// reflection-based access (internal), list/array conversions (internal), and other helper methods.
    /// </summary>
    public static class UtilityExtension
    {
        /*
          * 说明：
          * \[ 匹配字符 "[" 
          * (\d+) 匹配一个或多个数字
          * \] 匹配字符 "]"
          * 最终输出：(\d+) 中的数字
          * 例如：[1233] -> 1233
          *       HALLO[1233] -> 1233
          *       ASD[abs] -> 不匹配
         */

        static readonly Regex _arrayIndexPattern = new(@"\[(\d+)\]", RegexOptions.Compiled);


        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(string msg) => Debug.LogWarning(msg);


        #region Export AnimationSequence & single Animation

        /// <summary>
        /// Exports an <see cref="AnimationSequence"/> from an array of <see cref="AnimStepConfig"/>.
        /// </summary>
        /// <param name="animSequenceConfig">The array of <see cref="AnimStepConfig"/> to export.</param>
        /// <returns>An <see cref="AnimationSequence"/> containing all exported commands, or null if the input is null, empty or not set correctly.</returns>
        public static AnimationSequence ExportAnimationSequence(this AnimStepConfig[] animSequenceConfig)
        {
            if (animSequenceConfig == null || animSequenceConfig.Length == 0) return null;
            AnimSequenceConfig tool = new(animSequenceConfig);
            return tool.ExportAnimationSequence();
        }

        /// <summary>
        /// Exports an <see cref="AnimationSequence"/> from a list of <see cref="AnimStepConfig"/>.
        /// </summary>
        /// <param name="configs">The list of <see cref="AnimStepConfig"/> to export.</param>
        /// <returns>An <see cref="AnimationSequence"/> containing all exported commands, or null if the input is null, empty or not set correctly.</returns>
        public static AnimationSequence ExportAnimationSequence(this List<AnimStepConfig> configs)
        {
            if (configs == null || configs.Count == 0) return null;
            AnimSequenceConfig tool = new(configs.ToArray());
            return tool.ExportAnimationSequence();
        }

        /// <summary>
        /// Converts an <see cref="AnimSequenceConfig"/> into an <see cref="AnimationSequence"/>.
        /// <para>Handles Do, Shake, AdvancedShake, Delay, and Function commands.</para>
        /// </summary>
        /// <param name="animSequenceConfig">The anim sequence config to export.</param>
        /// <returns>An <see cref="AnimationSequence"/> representing all commands in the group.</returns>
        static public AnimationSequence ExportAnimationSequence(this AnimSequenceConfig animSequenceConfig)
        {
            AnimationSequence animSequense = new();
            foreach (AnimStepConfig setting in animSequenceConfig.StepConfigs)
            {
                switch (setting.Type)
                {
                    case StepType.Do or StepType.Shake or StepType.AdvancedShake:
                        IAnimation animation = setting.ExportAnimation();
                        if (animation != null)
                            animSequense.Append(animation, !setting.ExecuteTheNext);
                        break;
                    case StepType.Delay:
                        animSequense.Append(setting.DelayTime, !setting.RealTime);
                        break;
                    case StepType.Function:
                        if (setting.UseFunctionPlus)
                        {
                            FunctionPlus functionPlus = setting.FunctionPlus;
                            if (!functionPlus._isMissing && functionPlus._object)
                            {
                                animSequense.Append(functionPlus, setting.WaitingForMethodExecutionComplete);
                            }
                        }
                        else
                        {
                            if (setting.Function.GetPersistentEventCount() != 0)
                                animSequense.Append(() => setting.Function.Invoke());
                        }
                        break;
                    default: // None
                        break;
                }
            }
            return animSequense;
        }

        /// <summary>
        /// Exports a single <see cref="AnimStepConfig"/> to an <see cref="IAnimation"/> instance.
        /// <para>Supports Do, Shake, and AdvancedShake types.</para>
        /// </summary>
        /// <param name="animStepConfig">The anim step config to export.</param>
        /// <returns>An <see cref="IAnimation"/> representing the command, or null if it is not set up correctly.</returns>
        public static IAnimation ExportAnimation(this AnimStepConfig animStepConfig)
        {
            return animStepConfig.Type switch
            {
                StepType.Do => animStepConfig.DoType switch
                {
                    AnimStepConfig.DOType.DoPath => ExportDoPath(animStepConfig),
                    AnimStepConfig.DOType.DoPosition => ExportDoPosition(animStepConfig),
                    AnimStepConfig.DOType.DoScale => ExportDoScale(animStepConfig),
                    AnimStepConfig.DOType.DoRotation => ExportDoRotation(animStepConfig),
                    AnimStepConfig.DOType.DoAlpha => ExportDoAlpha(animStepConfig),
                    AnimStepConfig.DOType.DoColor => ExportDoColor(animStepConfig),
                    AnimStepConfig.DOType.DoJolt => ExportDoJolt(animStepConfig),
                    _ => null
                },
                StepType.Shake => ExportShake(animStepConfig),
                StepType.AdvancedShake => ExportAdvancedShake(animStepConfig),
                _ => null
            };
        }

        private static IAnimation ExportDoPath(AnimStepConfig animStepConfig)
        {
            var t = animStepConfig.Transform;

            if (!t) return null;

            return t.DoPath(
                animStepConfig.TransformsToVectors ?
                animStepConfig.Transforms.Select(t => t.position).ToArray() :
                animStepConfig.Vectors,
                animStepConfig.Duration
                )
                .SetEase(animStepConfig.Ease)
                .SetPathType(animStepConfig.PathType)
                .SetLoops(animStepConfig.Loops, animStepConfig.LoopType2)
                .SetSamplesPerSegment(animStepConfig.SamplesPerSegment)
                .SetCREndCorrection(animStepConfig.EnableCatmullRomCorrection)
                .SetPathRotation(animStepConfig.FollowPathRotation, animStepConfig.FollowPathRotation ? animStepConfig.FacingAxis : null, animStepConfig.IgnoreDecreasingEasedT)
                .SetRotDimension(animStepConfig.RotationDimension)
                .UseFixedDeltaTime(animStepConfig.UseFixedDeltaTime);
        }

        private static IAnimation ExportDoPosition(AnimStepConfig animStepConfig)
        {
            var t = animStepConfig.Transform;

            if (!t) return null;

            DoPosition doPosition;
            if (animStepConfig.IsModifyingAxis)
            {
                if (animStepConfig.Axis == Axis.None)
                    return null;
                doPosition = t.DoPosition(animStepConfig.Value, animStepConfig.Duration, animStepConfig.Axis);
            }
            else
                doPosition = t.DoPosition(animStepConfig.TrToVec3 && animStepConfig.Space == Space.World ? animStepConfig.TargetTransform.position : animStepConfig.Position, animStepConfig.Duration);

            return doPosition.SetLoops(animStepConfig.Loops, animStepConfig.LoopType1).SetEase(animStepConfig.Ease).SetSpace(animStepConfig.Space).UseFixedDeltaTime(animStepConfig.UseFixedDeltaTime);
        }

        private static IAnimation ExportDoScale(AnimStepConfig animStepConfig)
        {
            var t = animStepConfig.Transform;

            if (!t) return null;

            DoScale doScale;
            if (animStepConfig.IsModifyingAxis)
            {
                if (animStepConfig.Axis == Axis.None)
                    return null;
                doScale = t.DoScale(animStepConfig.Value, animStepConfig.Duration, animStepConfig.Axis);
            }
            else
                doScale = t.DoScale(animStepConfig.Scale, animStepConfig.Duration);

            return doScale
                .SetEase(animStepConfig.Ease)
                .SetLoops(animStepConfig.Loops, animStepConfig.LoopType1)
                .UseFixedDeltaTime(animStepConfig.UseFixedDeltaTime);
        }

        private static IAnimation ExportDoRotation(AnimStepConfig animStepConfig)
        {
            var t = animStepConfig.Transform;

            if (!t) return null;

            return t.DoRotation(animStepConfig.Angle, animStepConfig.Duration)
                .SetLoops(animStepConfig.Loops, animStepConfig.LoopType1)
                .SetRotateMode(animStepConfig.RotateMode)
                .SetSpace(animStepConfig.Space)
                .SetEase(animStepConfig.Ease)
                .UseFixedDeltaTime(animStepConfig.UseFixedDeltaTime);


        }

        private static IAnimation ExportDoAlpha(AnimStepConfig animStepConfig)
        {
            var component = animStepConfig.Component;

            if (!component) return null;

            DoAlpha doAlpha;
            switch (animStepConfig.AlphaCompType)
            {
                case AlphaCompType.Sprite:
                    doAlpha = ((SpriteRenderer)component).DoAlpha(animStepConfig.Alpha, animStepConfig.Duration);
                    break;
                case AlphaCompType.Mesh:
                    doAlpha = ((MeshRenderer)component).DoAlpha(animStepConfig.Alpha, animStepConfig.Duration).SetMaterialIndex(animStepConfig.MaterialIndex);
                    break;
                case AlphaCompType.Graphic:
                    doAlpha = ((Graphic)component).DoAlpha(animStepConfig.Alpha, animStepConfig.Duration);
                    break;
                case AlphaCompType.CanvasGroup:
                    doAlpha = ((CanvasGroup)component).DoAlpha(animStepConfig.Alpha, animStepConfig.Duration);
                    break;
                case AlphaCompType.Material:
                    doAlpha = ((Material)component).DoAlpha(animStepConfig.Alpha, animStepConfig.Duration);
                    break;
                default: return null;
            }

            return doAlpha.SetLoops(animStepConfig.Loops, animStepConfig.LoopType3).SetEase(animStepConfig.Ease)
                .UseFixedDeltaTime(animStepConfig.UseFixedDeltaTime);
        }

        private static IAnimation ExportDoColor(AnimStepConfig animStepConfig)
        {
            var component = animStepConfig.Component;

            if (!component) return null;

            DoColor doColor;
            switch (animStepConfig.ColorCompType)
            {
                case ColorCompType.Sprite:
                    doColor = ((SpriteRenderer)component).DoColor(animStepConfig.Color, animStepConfig.Duration);
                    break;
                case ColorCompType.Mesh:
                    doColor = ((MeshRenderer)component).DoColor(animStepConfig.Color, animStepConfig.Duration).SetMaterialIndex(animStepConfig.MaterialIndex);
                    break;
                case ColorCompType.Graphic:
                    doColor = ((Graphic)component).DoColor(animStepConfig.Color, animStepConfig.Duration);
                    break;
                case ColorCompType.Material:
                    doColor = ((Material)component).DoColor(animStepConfig.Color, animStepConfig.Duration);
                    break;
                default: return null;
            }
            return doColor.SetLoops(animStepConfig.Loops, animStepConfig.LoopType3).SetEase(animStepConfig.Ease)
                .WithAlpha(animStepConfig.WithAlpha).UseFixedDeltaTime(animStepConfig.UseFixedDeltaTime);
        }

        private static IAnimation ExportDoJolt(AnimStepConfig animStepConfig)
        {
            var t = animStepConfig.Transform;

            if (!t) return null;

            return t.DoJolt(animStepConfig.Direction, animStepConfig.Duration, animStepConfig.Vibratio, animStepConfig.Elasticity)
                .SetJoltType(animStepConfig.JoltType).UseFixedDeltaTime(animStepConfig.UseFixedDeltaTime);
        }

        private static IAnimation ExportShake(AnimStepConfig animStepConfig)
        {
            var t = animStepConfig.Transform;

            if (!t) return null;

            if (animStepConfig.ActiveAxes == Axis.None) return null;

            return t.Shake(animStepConfig.Magnitude, animStepConfig.Duration, animStepConfig.ActiveAxes)
                .SetShakeType(animStepConfig.ShakeType).UseFixedDeltaTime(animStepConfig.UseFixedDeltaTime);
        }

        private static IAnimation ExportAdvancedShake(AnimStepConfig animStepConfig)
        {
            var t = animStepConfig.Transform;

            if (!t) return null;

            if (animStepConfig.ActiveAxes == Axis.None) return null;

            return t.AdvancedShake(animStepConfig.Magnitude, animStepConfig.Duration, animStepConfig.ActiveAxes)
                .SetReturnDuration(animStepConfig.ReturnDuration)
                .SetShakeType(animStepConfig.AdvancedShakeType)
                .SetFrequency(animStepConfig.Frequency)
                .SetCurves(animStepConfig.MagnitudeCurve, animStepConfig.ReturnCurve)
                .UseFixedDeltaTime(animStepConfig.UseFixedDeltaTime);
        }
        #endregion
        #region Enum Extension

        /// <summary>
        /// Checks whether an enum value contains multiple flags.
        /// </summary>
        /// <param name="allowZero">
        /// If true, zero is considered as having a valid flag; otherwise zero is not considered a flag.
        /// </param>
        static public bool HasMultipleFlags(this Enum @enum, bool allowZero = false)
        {
            // 无符号 64 位整数 
            ulong bits = Convert.ToUInt64(@enum);

            if (allowZero && bits == 0) return true;
            /*
            解释原理(示例):
            0. 假设 x = A | B | C -> A = 1, B = 2, C = 4, D = 8, E = 16, ...
            1. 所以 x = 7 -> 0111
            2. bit = x - 1 = 6 -> 0110
            3. x & bit = 0111 & 0110 = 0110(6) != 0 -> true(has flags)
            "4." 如果 x = E = 16 -> 10000 -> 15 -> 01111 -> 01111 & 10000 = 00000(0) == 0 -> false(no flags)
             */
            return (bits & (bits - 1)) != 0;
        }

        /// <summary>
        /// Returns all individual flags set in the enum value.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="enumValue">The enum value to extract flags from.</param>
        /// <returns>An enumerable of individual flags contained in the value.</returns>
        public static IEnumerable<T> GetFlags<T>(this T enumValue) where T : Enum
        {
            System.Type enumType = enumValue.GetType();

            foreach (T value in Enum.GetValues(enumType).Cast<T>())
            {
                if (Convert.ToInt32(value) != 0 && enumValue.HasFlag(value))
                {
                    yield return value;
                }
            }
        }

        #endregion
        #region ConvertListType

        internal static object ConvertListType(this IList originalList, Type targetListType)
        {
            //获取列表元素的类型
            Type targetElementType = targetListType.GetGenericArguments()[0];

            // 创建新的List<T>实例
            IList newList = (IList)Activator.CreateInstance(targetListType);

            // 遍历原始列表，筛选并添加符合类型的元素 
            foreach (object item in originalList)
            {
                bool isUnityNull = item.IsUnityNull();
                if (isUnityNull || targetElementType.IsAssignableFrom(item.GetType()))
                {
                    newList.Add(isUnityNull ? null : item);
                }
            }

            return newList;
        }

        internal static object ConvertArrayType(this Array originalArray, Type targetArrayType)
        {
            //获取列表元素的类型
            Type targetElementType = targetArrayType.GetElementType();
            int length = originalArray.Length;

            // 创建目标数组
            Array newArray = Array.CreateInstance(targetElementType, length);
            int newIndex = 0;

            // 遍历原始列表，筛选并添加符合类型的元素 
            foreach (object item in originalArray)
            {
                bool isUnityNull = item.IsUnityNull();
                if (isUnityNull || targetElementType.IsAssignableFrom(item.GetType()))
                {
                    newArray.SetValue(isUnityNull ? null : item, newIndex++);
                }
            }

            return newArray;
        }

        #endregion
#if UNITY_EDITOR
        #region Get Object instance by reflection

        /// <summary>
        /// Uses reflection to traverse a serialized property path(<see cref="UnityEditor.SerializedProperty.propertyPath"/>) and retrieve the corresponding object instance.
        /// Supports nested fields, arrays, and lists.
        /// Useful in Editor scripts for accessing the target object of a <see cref="UnityEditor.SerializedProperty"/>.
        /// </summary>
        /// <param name="path">The property path, e.g., "someList.Array.data[0].fieldName".</param>
        /// <param name="target">The root object (usually the serialized object target).</param>
        /// <returns>The object instance at the end of the path, or null if not found or an error occurs.</returns>
        static internal object GetObjectInstanceByReflection(string path, Object target)
        {
            object obj = target;
            try
            {
                //使用放射性遍历属性路径查找目标实例
                foreach (string pathPart in path.Split('.'))
                {
                    if (pathPart == "Array")
                        continue;
                    Match match = _arrayIndexPattern.Match(pathPart);
                    if (match.Success) // 处理数组索引
                    {
                        int index = int.Parse(match.Groups[1].Value);
                        if (obj is Array array) // 如果是Array
                        {
                            obj = array.GetValue(index);
                        }
                        else if (obj is IList list) // 如果是 List<T>
                        {
                            obj = list[index];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else // 处理属性
                    {
                        FieldInfo field = obj.GetType().GetField(pathPart, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ?? throw new InvalidOperationException($"Field '{pathPart}' not found on type '{obj.GetType().Name}'.");
                        obj = field.GetValue(obj);
                    }
                }

                return obj;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
        #region Editor: Path string handling

        /// <summary>
        /// string path = "something.Array.data[3].more.Array.data[15]";
        /// <br>Debug.Log(path.ReplaceLastArrayDataOccurrence());</br>
        /// <br>输出: something.Array.data[3].more</br>
        /// </summary>
        internal static string ReplaceLastArrayDataOccurrence(this string path)
        {
            var matches = Regex.Matches(path, @"\.Array\.data\[\d+\]");

            if (matches.Count == 0)
                return path;

            var lastMatch = matches[^1];
            return path.Remove(lastMatch.Index, lastMatch.Length);
        }

        /// <summary>
        /// string path1 = "something.Array.data[3].more.Array.data[15]";
        /// <br>string path2 = "something.Array.data[3]";</br>
        /// <br>Debug.Log(EndsWithArrayDataIndex(path1)); // true</br>
        /// <br>Debug.Log(EndsWithArrayDataIndex(path2)); // true</br>
        /// <br>Debug.Log(EndsWithArrayDataIndex("other.thing")); // false </br>
        /// </summary>
        internal static bool EndsWithArrayDataIndex(this string path)
        {
            return Regex.IsMatch(path, @"\.Array\.data\[\d+\]$");
        }

        /// <summary>
        /// string path1 = "something.Array.data[3].more.Array.data[15]";
        /// <br>Debug.Log(EndsWithArrayDataIndex(path1)); // 15</br>
        /// </summary>
        /// <returns>Fail: int.MinValue</returns>
        internal static int GetLastArrayDataIndex(this string path)
        {
            Match match = _arrayIndexPattern.Match(path);
            if (match.Success)
            {
                if (int.TryParse(match.Groups[^1].Value, out int i))
                {
                    return i;
                }
            }
            return int.MinValue;
        }

        /// <summary>
        /// string path = "something.Array.data[3].more.Array.data[15]";
        /// <br>string modified = path.ReplaceLastArrayDataIndex(12);</br>
        /// <br>输出: something.Array.data[3].more.Array.data[12]</br>
        /// </summary>
        internal static string ReplaceLastArrayDataIndex(this string path, int newIndex)
        {
            return Regex.Replace(path, @"\.Array\.data\[\d+\](?!.*\.Array\.data\[\d+\])", $".Array.data[{newIndex}]");
        }

        #endregion
#endif
    }
}