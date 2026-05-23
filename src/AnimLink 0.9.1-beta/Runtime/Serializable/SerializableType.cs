namespace AnimLink
{
    using System;

    using UnityEngine;

    [Serializable]
    public class SerializableType : ISerializationCallbackReceiver
    {
        [NonSerialized]
        public Type Type;
        public string TypeName;

        public SerializableType(Type aType)
        {
            Type = aType;
            TypeName = aType.AssemblyQualifiedName;
        }

        public void OnBeforeSerialize()
        {
            if (Type != null)
                TypeName = Type.AssemblyQualifiedName;
            else
                TypeName = null;
        }

        public void OnAfterDeserialize()
        {
            if (TypeName != null)
                Type = Type.GetType(TypeName);
            else
                Type = null;
        }
    }

}