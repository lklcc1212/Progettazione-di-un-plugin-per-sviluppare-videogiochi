namespace AnimLink
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using static AnimLink.UtilityExtension;

    using Object = UnityEngine.Object;

    [Serializable]
    internal class CustomValue
    {
        public enum TypeOfValue
        {
            None,
            Object,
            Int,
            Float,
            Bool,
            String,
            Enum,
            Vector2,
            Vector3,
            Quaternion,
            Color,
            LayerMask,
            ArrayOrList,
        }

        [SerializeField]
        private TypeOfValue _type = TypeOfValue.None;
        public TypeOfValue Type { get { return _type; } }
        [SerializeField]
        private SerializableType _serializableType;
        public SerializableType SerializableType { get { return _serializableType; } }


        [SerializeField]
        private Object _object; //1
        [SerializeField]
        private int _intValue;
        [SerializeField]
        private float _floatValue;
        [SerializeField]
        private string _stringValue;
        [SerializeField]
        private bool _boolValue;
        [SerializeField]
        private SerializableEnum _enumValue;
        [SerializeField]
        private SerializableType _enumType;
        public SerializableType EnumType { get { return _enumType; } }
        [SerializeField]
        private bool _isFlag;
        public bool IsFlag { get { return _isFlag; } }
        [SerializeField]
        private Vector2 _vector2Value;
        [SerializeField]
        private Vector3 _vector3Value;
        [SerializeField]
        private Vector3 _quaternionInVector3;
        [SerializeField]
        private Quaternion _quaternionValue;
        [SerializeField]
        private Color _colorValue;
        [SerializeField]
        private LayerMask _layer; //11

        [SerializeField]
        private CustomList _customList;
        public CustomList CustomList { get { if (_type == TypeOfValue.ArrayOrList) return _customList; else { Debug.Log("You do not have permission to use it. (Only when Type_ is ArrayOrList)"); return null; } } }

        public CustomValue() { }

        /// <param name="isFlag">仅适用于枚举类型。</param>
        public CustomValue(object value, bool isFlag = false)
        {
            SetType(value.GetType(), isFlag);
            if (_type == TypeOfValue.Quaternion)
                value = ((Quaternion)value).eulerAngles;
            SaveValue(value);
        }

        /// <summary>
        /// 注意：如果类型是四元数，请传入 Vector3。
        /// </summary>
        /// <param name="value">输入支持的类型：Int、Float、String、Bool、Enum、Vector2、Vector3（也适用于Quaternion）、Color、LayerMask、EditorEngine.Object。</param>
        public void SaveValue(object value)
        {
            try
            {
                switch (_type)
                {
                    case TypeOfValue.Object:
                        _object = (Object)value;
                        break;
                    case TypeOfValue.String:
                        _stringValue = (string)value;
                        break;
                    case TypeOfValue.Int:
                        _intValue = (int)value;
                        break;
                    case TypeOfValue.Float:
                        _floatValue = (float)value;
                        break;
                    case TypeOfValue.Bool:
                        _boolValue = (bool)value;
                        break;
                    case TypeOfValue.Enum:
                        _enumValue.Value = (Enum)value;
                        break;
                    case TypeOfValue.Vector2:
                        _vector2Value = (Vector2)value;
                        break;
                    case TypeOfValue.Vector3:
                        _vector3Value = (Vector3)value;
                        break;
                    case TypeOfValue.Quaternion:
                        _quaternionInVector3 = (Vector3)value;
                        _quaternionValue = Quaternion.Euler(_quaternionInVector3);
                        break;
                    case TypeOfValue.Color:
                        _colorValue = (Color)value;
                        break;
                    case TypeOfValue.LayerMask:
                        _layer = (LayerMask)value;
                        break;
                    case TypeOfValue.ArrayOrList:
                        _customList.SaveValues(value);
                        break;
                    default:
                        LogWarning("Type: ValueType.None.");
                        break;
                }
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Cannot convert from {value.GetType()} to {_type.ToString().Split(".")[^1]}.");
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("value cannot be null.");
            }
        }

        /// <returns>返回你输入的类型的值。</returns>   
        public object ReadValue => _type switch
        {
            TypeOfValue.Object => _object == null ? null : _object,
            TypeOfValue.String => _stringValue,
            TypeOfValue.Int => _intValue,
            TypeOfValue.Float => _floatValue,
            TypeOfValue.Bool => _boolValue,
            TypeOfValue.Enum => _enumValue.Value,
            TypeOfValue.Vector2 => _vector2Value,
            TypeOfValue.Vector3 => _vector3Value,
            TypeOfValue.Quaternion => _quaternionValue,
            TypeOfValue.Color => _colorValue,
            TypeOfValue.LayerMask => _layer,
            TypeOfValue.ArrayOrList => _customList.ReadValue(_customList.IsArray),
            _ => null,
        };

        public Vector3 GetQuaternionInVector3() => _quaternionInVector3;

        /// <param name="isFlag">仅适用于枚举类型。</param>
        public void SetType(Type valueType, bool isFlag = false)
        {
            if (valueType.IsSubclassOf(typeof(Object)) || valueType == typeof(Object))
            {
                _type = TypeOfValue.Object;
            }
            else if (valueType == typeof(int))
            {
                _type = TypeOfValue.Int;
            }
            else if (valueType == typeof(float))
            {
                _type = TypeOfValue.Float;
            }
            else if (valueType == typeof(string))
            {
                _type = TypeOfValue.String;
            }
            else if (valueType == typeof(bool))
            {
                _type = TypeOfValue.Bool;
            }
            else if (valueType == typeof(Enum) || valueType.IsSubclassOf(typeof(Enum)))
            {
                _type = TypeOfValue.Enum;
                _enumType = new(valueType);
                _isFlag = isFlag;
            }
            else if (valueType == typeof(Vector2))
            {
                _type = TypeOfValue.Vector2;
            }
            else if (valueType == typeof(Vector3))
            {
                _type = TypeOfValue.Vector3;
            }
            else if (valueType == typeof(Quaternion))
            {
                _type = TypeOfValue.Quaternion;
            }
            else if (valueType == typeof(Color))
            {
                _type = TypeOfValue.Color;
            }
            else if (valueType == typeof(LayerMask))
            {
                _type = TypeOfValue.LayerMask;
            }
            else if (valueType.IsArray || valueType.GetGenericTypeDefinition() == typeof(List<>))
            {
                _type = TypeOfValue.ArrayOrList;
                _customList = new(valueType, isFlag);
            }
            else
            {
                throw new Exception("This type is not supported: " + valueType);
            }
            _serializableType = new(valueType);
        }
    }
}