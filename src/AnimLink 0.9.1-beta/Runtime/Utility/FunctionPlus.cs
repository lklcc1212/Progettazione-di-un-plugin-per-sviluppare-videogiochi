namespace AnimLink
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using UnityEngine;

    using Object = UnityEngine.Object;

    [Serializable]
    public class FunctionPlus
    {
        [SerializeField]
        internal MonoBehaviour _object;
        [SerializeField]
        internal MonoBehaviour _previousObject;
        [SerializeField]
        internal int _currentEnableMethodIndex = -1;
        [SerializeField]
        private bool _isInListOrArray;
        internal SerializableMethodInfo Serializable_MethodInfo { get { if (_currentEnableMethodIndex != -1) { return _sMethodInfos[_currentEnableMethodIndex]; } return null; } }
        [SerializeField]
        internal string _previous_SMName = "";
        [SerializeField]
        internal string _methodName = "No function";
        [SerializeField]
        internal List<SerializableMethodInfo> _sMethodInfos = new();
        internal bool ContainsParameters()
        {
            if (Serializable_MethodInfo?.MethodInfo == null)
                return false;
            if (_parameterCount == -1)
                _parameterCount = Serializable_MethodInfo.MethodInfo.GetParameters().Length;
            return _parameterCount > 0;
        }
        [SerializeField]
        internal int _parameterCount = -1;
        [SerializeField]
        internal CustomValue[] _parameters = null;
        [SerializeField]
        internal bool _isIEnumerator = false;
        [SerializeField]
        internal bool _isMissing = true;
        [SerializeField]
        internal IsFlagAttribute _isFlagAttribute = null;
        [SerializeField]
        internal bool _noAttribute = false;

        /// <summary>
        /// Returns <c>true</c> if the selected method's return type is <see cref="IEnumerator"/>.
        /// </summary>
        public bool IsIEnumerator => Serializable_MethodInfo?.MethodInfo?.ReturnType == typeof(IEnumerator);
        /// <summary>
        /// Returns <c>true</c> if the selected method is missing or cannot be resolved.
        /// </summary>
        public bool IsMissing => Serializable_MethodInfo?.MethodInfo == null;

        /// <summary>
        /// Invokes the selected method on the assigned object using reflection.  
        /// Automatically handles parameter conversion for lists and arrays of Unity objects or enums.
        /// </summary>
        /// <remarks>
        /// This method does not support IEnumerator methods (coroutines).  
        /// Use <see cref="GetIEnumerator"/> instead to obtain an IEnumerator for coroutine execution.  
        /// </remarks>
        public void InvokeMethod()
        {
            if (!_object)
            {
                Debug.LogError("[FunctionPlus] The object is null");
                return;
            }
            MethodInfo method = Serializable_MethodInfo?.MethodInfo;
            if (IsMissing || IsIEnumerator)
            {
                Debug.LogError("[FunctionPlus] The method is IEnumerator or is missing");
                return;
            }
            method.Invoke(_object, ContainsParameters() ? _parameters.Select(p =>
            {
                object value = p.ReadValue;
                if (value is List<Object> or List<Enum> or Object[] or Enum[])
                    return ConvertArr_ListType(value, p.SerializableType.Type);
                return value;
            }).ToArray() : null);
        }

        /// <summary>
        /// Retrieves the IEnumerator from a coroutine method (a method returning IEnumerator).  
        /// This can be passed to <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/> for execution.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerator"/> returned by the target method, or <c>null</c> if  
        /// the method is missing or not an IEnumerator.
        /// </returns>
        /// <remarks>
        /// Only use this method when the target method’s return type is <see cref="IEnumerator"/>.  
        /// If the target is not a coroutine, use <see cref="InvokeMethod"/> instead.
        /// </remarks>
        public IEnumerator GetIEnumerator()
        {
            if (!_object)
            {
                Debug.LogError("[FunctionPlus] The object is null");
                return null;
            }
            MethodInfo method = Serializable_MethodInfo?.MethodInfo;
            if (IsMissing || !IsIEnumerator)
            {
                Debug.LogError("[FunctionPlus] The method isn't IEnumerator or is missing");
                return null;
            }
            return (IEnumerator)method.Invoke(_object, ContainsParameters() ? _parameters.Select(p =>
            {
                object value = p.ReadValue;
                if (value is List<Object> or List<Enum> or Object[] or Enum[])
                    return ConvertArr_ListType(value, p.SerializableType.Type);
                return value;
            }).ToArray() : null);
        }

        object ConvertArr_ListType(object value, Type targetType)
        {
            if (value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(List<>))
            {
                return ((IList)value).ConvertListType(targetType);
            }
            else if (value.GetType().IsArray)
            {
                return ((Array)value).ConvertArrayType(targetType);
            }
            return null;
        }
    }
}