namespace AnimLink
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

#if UNITY_EDITOR
    using UnityEditorInternal;
#endif
    using UnityEngine;

    using static CustomValue;
    using static AnimLink.UtilityExtension;

    using Object = UnityEngine.Object;

    [Serializable]
    internal class CustomList
    {
        [SerializeField]
        private TypeOfValue _typeOfValue = TypeOfValue.None;
        public TypeOfValue TypeOfValue { get { return _typeOfValue; } }

        [SerializeField]
        private SerializableType _objectType;
        public SerializableType ObjectType { get { return _objectType; } }

        [SerializeField]
        private List<Object> _objects = new();

        [SerializeField]
        private List<int> _intValues = new();

        [SerializeField]
        private List<float> _floatValues = new();

        [SerializeField]
        private List<string> _stringValues = new();

        [SerializeField]
        private List<bool> _boolValues = new();

        [SerializeField]
        private List<SerializableEnum> _enumValues = new();

        [SerializeField]
        private SerializableType _enumType;
        public SerializableType EnumType => _enumType;

        [SerializeField]
        private bool _isFlag;
        public bool IsFlag => _isFlag;

        [SerializeField]
        private List<Vector2> _vector2Values = new();

        [SerializeField]
        private List<Vector3> _vector3Values = new();

        [SerializeField]
        private List<Vector3> _quaternionValuesInVector3 = new();

        [SerializeField]
        private List<Color> _colorValues = new();

        [SerializeField]
        private List<LayerMask> _layers = new();

        public List<Object> Objects => _typeOfValue == TypeOfValue.Object ? _objects : new();
        public List<int> IntValues => _typeOfValue == TypeOfValue.Int ? _intValues : new();
        public List<float> FloatValues => _typeOfValue == TypeOfValue.Float ? _floatValues : new();
        public List<string> StringValues => _typeOfValue == TypeOfValue.String ? _stringValues : new();
        public List<bool> BoolValues => _typeOfValue == TypeOfValue.Bool ? _boolValues : new();
        public List<SerializableEnum> EnumValues => _typeOfValue == TypeOfValue.Enum ? _enumValues : new();
        public List<Vector2> Vector2Values => _typeOfValue == TypeOfValue.Vector2 ? _vector2Values : new();
        public List<Vector3> Vector3Values => _typeOfValue == TypeOfValue.Vector3 ? _vector3Values : new();
        public List<Vector3> QuaternionValuesInVector3 => _typeOfValue == TypeOfValue.Quaternion ? _quaternionValuesInVector3 : new();
        public List<Color> ColorValues => _typeOfValue == TypeOfValue.Color ? _colorValues : new();
        public List<LayerMask> Layers => _typeOfValue == TypeOfValue.LayerMask ? _layers : new();

        [SerializeField]
        private bool _isArray;
        public bool IsArray => _isArray;
        //
#if UNITY_EDITOR
        public ReorderableList CurrentReorderableList = null;
#endif

        /// <param name="isFlag">仅当类型为枚举（Enum）时适用。</param>
        public CustomList(object values, bool isFlag = false)
        {
            SetType(values.GetType(), isFlag);
            if (_typeOfValue == TypeOfValue.Quaternion)
            {
                _quaternionValuesInVector3 = values switch
                {
                    Quaternion[] array => array.Select(q => q.eulerAngles).ToList(),
                    List<Quaternion> list => list.Select(q => q.eulerAngles).ToList(),
                    _ => throw new ArgumentException("Unsupported type")
                };
            }
            else
                SaveValues(values);
        }

        /// <param name="valueType">输入：typeof(List<...>) 或 typeof(...[])</param>
        /// <param name="isFlag">仅当类型为枚举（Enum）时适用。</param>
        public CustomList(Type valueType, bool isFlag = false)
        {
            SetType(valueType, isFlag);
        }

        /// <summary>
        /// 注意：如果类型是四元数（Quaternion），请传入 Vector3。
        /// </summary>
        /// <param name="values">输入支持的类型（数组或List<>）：Int、Float、String、Bool、Enum、Vector2、Vector3（也可用于Quaternion）、Color、LayerMask、EditorEngine.Object。</param>
        public void SaveValues(object values)
        {
            if (IsArray)
            {
                values = ((Array)values).Cast<List<object>>().ToList();
            }
            try
            {
#if UNITY_EDITOR
                if (CurrentReorderableList != null)
                    CurrentReorderableList.list = (IList)values;
#endif
                switch (_typeOfValue)
                {
                    case TypeOfValue.Object:
                        _objects = (List<Object>)values;
                        break;
                    case TypeOfValue.String:
                        _stringValues = (List<string>)values;
                        break;
                    case TypeOfValue.Int:
                        _intValues = (List<int>)values;
                        break;
                    case TypeOfValue.Float:
                        _floatValues = (List<float>)values;
                        break;
                    case TypeOfValue.Bool:
                        _boolValues = (List<bool>)values;
                        break;
                    case TypeOfValue.Enum:
                        _enumValues = ((IEnumerable)values).OfType<Enum>().Select(e => new SerializableEnum(e)).ToList();
                        break;
                    case TypeOfValue.Vector2:
                        _vector2Values = (List<Vector2>)values;
                        break;
                    case TypeOfValue.Vector3:
                        _vector3Values = (List<Vector3>)values;
                        break;
                    case TypeOfValue.Quaternion:
                        _quaternionValuesInVector3 = (List<Vector3>)values;
                        break;
                    case TypeOfValue.Color:
                        _colorValues = (List<Color>)values;
                        break;
                    case TypeOfValue.LayerMask:
                        _layers = (List<LayerMask>)values;
                        break;
                    default:
                        LogWarning("Type: ValueType.None.");
                        break;
                }
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Cannot convert from {values.GetType()} to {_typeOfValue.ToString().Split(".")[^1]}.");
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("value cannot be null.");
            }
        }

        public object ReadValue(bool inArrayForm = false) => _typeOfValue switch
        {
            TypeOfValue.Object => Convert(_objects, inArrayForm),
            TypeOfValue.String => Convert(StringValues, inArrayForm),
            TypeOfValue.Int => Convert(IntValues, inArrayForm),
            TypeOfValue.Float => Convert(FloatValues, inArrayForm),
            TypeOfValue.Bool => Convert(BoolValues, inArrayForm),
            TypeOfValue.Enum => Convert(EnumValues.Select(t => t.Value), inArrayForm),
            TypeOfValue.Vector2 => Convert(Vector2Values, inArrayForm),
            TypeOfValue.Vector3 => Convert(Vector3Values, inArrayForm),
            TypeOfValue.Quaternion => Convert(QuaternionValuesInVector3.Select(t => Quaternion.Euler(t)), inArrayForm),
            TypeOfValue.Color => Convert(ColorValues, inArrayForm),
            TypeOfValue.LayerMask => Convert(Layers, inArrayForm),
            _ => null,
        };

        private object Convert<T>(IEnumerable<T> values, bool inArrayForm) =>
            inArrayForm ? values.ToArray() : values.ToList();

        public List<Vector3> GetQuaternionInVector3() => QuaternionValuesInVector3;

        /// <param name="isFlag">仅在类型为枚举（Enum）时适用。</param>
        public void SetType(Type _type, bool isFlag = false)
        {
            Type elementType = null;
            if (_type.IsArray)
            {
                _isArray = true;
                elementType = _type.GetElementType();
            }
            else if (_type.IsGenericType)
            {
                if (_type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    _isArray = false;
                    elementType = _type.GetGenericArguments()[0];
                }
            }
            else
            {
                throw new Exception("The type is neither array nor list.");
            }
            if (elementType.IsSubclassOf(typeof(Object)) || elementType == typeof(Object))
            {
                _typeOfValue = TypeOfValue.Object;
                _objectType = new(elementType);
            }
            else if (elementType == typeof(int))
            {
                _typeOfValue = TypeOfValue.Int;
            }
            else if (elementType == typeof(float))
            {
                _typeOfValue = TypeOfValue.Float;
            }
            else if (elementType == typeof(string))
            {
                _typeOfValue = TypeOfValue.String;
            }
            else if (elementType == typeof(bool))
            {
                _typeOfValue = TypeOfValue.Bool;
            }
            else if (elementType == typeof(Enum) || elementType.IsSubclassOf(typeof(Enum)))
            {
                _typeOfValue = TypeOfValue.Enum;
                _enumType = new(elementType);
                _isFlag = isFlag;
            }
            else if (elementType == typeof(Vector2))
            {
                _typeOfValue = TypeOfValue.Vector2;
            }
            else if (elementType == typeof(Vector3))
            {
                _typeOfValue = TypeOfValue.Vector3;
            }
            else if (elementType == typeof(Quaternion))
            {
                _typeOfValue = TypeOfValue.Quaternion;
            }
            else if (elementType == typeof(Color))
            {
                _typeOfValue = TypeOfValue.Color;
            }
            else if (elementType == typeof(LayerMask))
            {
                _typeOfValue = TypeOfValue.LayerMask;
            }
            else
            {
                throw new Exception("This type is not supported: " + _type);
            }
#if UNITY_EDITOR
            CurrentReorderableList = null;
#endif
        }

        public void UpdateListValue(int index, object newValue)
        {
            if (_typeOfValue == TypeOfValue.Object) _objects[index] = (Object)newValue;
            else if (_typeOfValue == TypeOfValue.String) StringValues[index] = (string)newValue;
            else if (_typeOfValue == TypeOfValue.Int) IntValues[index] = (int)newValue;
            else if (_typeOfValue == TypeOfValue.Float) FloatValues[index] = (float)newValue;
            else if (_typeOfValue == TypeOfValue.Bool) BoolValues[index] = (bool)newValue;
            else if (_typeOfValue == TypeOfValue.Enum) EnumValues[index].Value = (Enum)newValue;
            else if (_typeOfValue == TypeOfValue.Vector2) Vector2Values[index] = (Vector2)newValue;
            else if (_typeOfValue == TypeOfValue.Vector3) Vector3Values[index] = (Vector3)newValue;
            else if (_typeOfValue == TypeOfValue.Quaternion) QuaternionValuesInVector3[index] = (Vector3)newValue;
            else if (_typeOfValue == TypeOfValue.Color) ColorValues[index] = (Color)newValue;
            else if (_typeOfValue == TypeOfValue.LayerMask) Layers[index] = (LayerMask)newValue;
            else
            {
                Debug.LogError("TypeOfValue is None.");
            }
        }
    }
}