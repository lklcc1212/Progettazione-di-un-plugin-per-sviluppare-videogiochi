namespace AnimLink
{
    using System.Collections.Generic;
    using System.Reflection;

    using UnityEngine;

    /// <summary>
    /// 用于序列化方法的详细信息,包括方法名称,返回类型,参数类型,访问修饰符等。
    /// 该类可在反序列化时根据保存的元数据恢复'MethodInfo'。
    /// </summary>
    [System.Serializable]
    public class SerializableMethodInfo : ISerializationCallbackReceiver
    {
        public SerializableMethodInfo(MethodInfo aMethodInfo)
        {
            MethodInfo = aMethodInfo;
            OnBeforeSerialize();
        }

        public MethodInfo MethodInfo;
        public SerializableType Type;
        public string MethodName;
        public List<SerializableType> Parameters = null;
        public int Flags = 0;
        public SerializableType ReturnType;

        public void OnBeforeSerialize()
        {
            if (MethodInfo == null)
                return;
            Flags = 0;
            Type = new SerializableType(MethodInfo.DeclaringType);
            MethodName = MethodInfo.Name;
            ReturnType = new(MethodInfo.ReturnType);
            if (MethodInfo.IsPrivate)
                Flags |= (int)BindingFlags.NonPublic;
            else
                Flags |= (int)BindingFlags.Public;
            if (MethodInfo.IsStatic)
                Flags |= (int)BindingFlags.Static;
            else
                Flags |= (int)BindingFlags.Instance;
            var p = MethodInfo.GetParameters();
            if (p != null && p.Length > 0)
            {
                Parameters = new List<SerializableType>(p.Length);
                for (int i = 0; i < p.Length; i++)
                {
                    Parameters.Add(new SerializableType(p[i].ParameterType));
                }
            }
            else
                Parameters = null;
        }

        public void OnAfterDeserialize()
        {
            if (Type == null || string.IsNullOrEmpty(MethodName))
                return;
            var t = Type.Type;
            if (t == null)
                return;
            System.Type[] param = null;
            if (Parameters != null && Parameters.Count > 0)
            {
                param = new System.Type[Parameters.Count];
                for (int i = 0; i < Parameters.Count; i++)
                {
                    param[i] = Parameters[i].Type;
                }
            }
            if (param == null)
                MethodInfo = t.GetMethod(MethodName, (BindingFlags)Flags);
            else
                MethodInfo = t.GetMethod(MethodName, (BindingFlags)Flags, null, param, null);
            NullIfParametersIsNotSame();
        }

        public void NullIfParametersIsNotSame()
        {
            if (MethodInfo == null)
                return;
            var p = MethodInfo.GetParameters();
            if (p.Length != Parameters.Count)
            {
                MethodInfo = null;
                return;
            }
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].ParameterType != Parameters[i].Type)
                {
                    MethodInfo = null;
                    return;
                }
            }
        }
    }

    // |= Merge enum
}