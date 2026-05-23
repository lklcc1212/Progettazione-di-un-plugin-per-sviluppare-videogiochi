namespace AnimLink
{
    using System;

    using UnityEngine;

    [Serializable]
    public class SerializableEnum : ISerializationCallbackReceiver
    {
        [NonSerialized]
        public Enum Value;
        public SerializableType Type;
        public string StringValue;

        public SerializableEnum(Enum _value)
        {
            if (_value != null)
                Value = _value;
        }

        public void OnBeforeSerialize()
        {
            if (Value == null)
                return;
            Type = new SerializableType(Value.GetType());
            StringValue = Value.ToString();
        }

        public void OnAfterDeserialize()
        {
            if (Type == null || string.IsNullOrEmpty(StringValue))
                return;
            if (Type.Type == null)
                return;
            Value = (Enum)Enum.Parse(Type.Type, StringValue);
        }
    }
}