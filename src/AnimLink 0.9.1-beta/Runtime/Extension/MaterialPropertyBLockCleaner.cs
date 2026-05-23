using System.Collections.Generic;

using UnityEngine;

namespace AnimLink
{
    /// <summary>
    /// Provides functionality to clean (clear) specific properties from a <see cref="MaterialPropertyBlock"/>
    /// while preserving the others.
    /// </summary>
    static public class MaterialPropertyBLockCleaner
    {
        private static readonly Dictionary<Name_, int> _nameIndexMap = new()
        {
            {Name_._Color, 0},
            {Name_._BaseColor, 1},
            {Name_._EmissionColor, 2},
            {Name_._SpecColor,3},
            {Name_._MainTex,4},
            {Name_._EmissionMap,5},
            {Name_._BumpMap,6},
            {Name_._OcclusionMap,7},
            {Name_._SpecGlossMap,8},
            {Name_._ParallaxMap,9},
            {Name_._DetailAlbedoMap,10},
            {Name_._DetailNormalMap,11},
            {Name_._Metallic,12},
            {Name_._Glossiness,13},
            {Name_._Smoothness,14},
            {Name_._Cutoff,15},
            {Name_._MainTex_ST,16},
            {Name_._DetailAlbedoMap_ST,17},
            {Name_._DetailNormalMap_ST,18},
            {Name_._BumpMap_ST,19},
            {Name_._ObjectToWorld,20},
            {Name_._WorldToObject,21},
            {Name_._WorldToCamera,22},
            {Name_._ViewProj,23},
        };

        public enum Name_
        {
            //Color
            _Color = 1,
            _BaseColor = 2,
            _EmissionColor = 4,
            _SpecColor = 8,
            //Texture
            _MainTex = 16,
            _EmissionMap = 32,
            _BumpMap = 64,
            _OcclusionMap = 128,
            _SpecGlossMap = 256,
            _ParallaxMap = 512,
            _DetailAlbedoMap = 1024,
            _DetailNormalMap = 2048,
            //Float
            _Metallic = 4096,
            _Glossiness = 8192,
            _Smoothness = 16384,
            _Cutoff = 32768,
            //Vector
            _MainTex_ST = 65536,
            _DetailAlbedoMap_ST = 131072,
            _DetailNormalMap_ST = 262144,
            _BumpMap_ST = 524288,
            //Matrix
            _ObjectToWorld = 1048576,
            _WorldToObject = 2097152,
            _WorldToCamera = 4194304,
            _ViewProj = 8388608,
        }

        private static readonly string[] _propertyNames = {
        //Color
        "_Color",
        "_BaseColor",
       "_EmissionColor",
        "_SpecColor",
        //Texture
        "_MainTex",
        "_EmissionMap",
        "_BumpMap",
        "_OcclusionMap",
       "_SpecGlossMap",
        "_ParallaxMap",
        "_DetailAlbedoMap",
        "_DetailNormalMap",
        //Float
        "_Metallic",
        "_Glossiness",
        "_Smoothness",
        "_Cutoff",
        //Vector
        "_MainTex_ST",
        "_DetailAlbedoMap_ST",
        "_DetailNormalMap_ST",
        "_BumpMap_ST",
        //Matrix
        "_ObjectToWorld",
        "_WorldToObject",
       "_WorldToCamera",
        "_ViewProj"
    };

        readonly static Name_ _allProperties =
        Name_._Color | Name_._BaseColor |
        Name_._EmissionColor | Name_._SpecColor |
        Name_._MainTex | Name_._EmissionMap |
        Name_._BumpMap | Name_._OcclusionMap |
        Name_._SpecGlossMap | Name_._ParallaxMap |
        Name_._DetailAlbedoMap | Name_._DetailNormalMap |
        Name_._Metallic | Name_._Glossiness |
        Name_._Smoothness | Name_._Cutoff |
        Name_._MainTex_ST | Name_._DetailAlbedoMap_ST |
        Name_._DetailNormalMap_ST | Name_._BumpMap_ST |
        Name_._ObjectToWorld | Name_._WorldToObject |
        Name_._WorldToCamera | Name_._ViewProj;

        /// <summary>
        /// Creates a new <see cref="MaterialPropertyBlock"/> by clearing specified properties while keeping others.
        /// <para>Note 1: If a type you need is missing, you may need to add it manually in the code.</para>
        /// <para>Note 2: The clear operation may remove unsupported property types.</para>
        /// </summary>
        /// <param name="materialPropertyBlock">The original <see cref="MaterialPropertyBlock"/> to copy from.</param>
        /// <param name="names">The set of property names to clear.</param>
        /// <returns>A new <see cref="MaterialPropertyBlock"/> with only the properties not included in <paramref name="names"/>.</returns>
        static public MaterialPropertyBlock Clear(this MaterialPropertyBlock materialPropertyBlock, Name_ names)
        {
            MaterialPropertyBlock newMaterialPropertyBlock = new();
            Name_ remainingNames = _allProperties & ~names;
            foreach (Name_ name_ in remainingNames.GetFlags())
            {
                int index = _nameIndexMap[name_];
                if (index >= 20)
                {
                    if (materialPropertyBlock.HasMatrix(_propertyNames[index]))
                        newMaterialPropertyBlock.SetMatrix(_propertyNames[index], materialPropertyBlock.GetMatrix(_propertyNames[index]));
                }
                else if (index >= 16)
                {
                    if (materialPropertyBlock.HasVector(_propertyNames[index]))
                        newMaterialPropertyBlock.SetVector(_propertyNames[index], materialPropertyBlock.GetVector(_propertyNames[index]));
                }
                else if (index >= 12)
                {
                    if (materialPropertyBlock.HasFloat(_propertyNames[index]))
                        newMaterialPropertyBlock.SetFloat(_propertyNames[index], materialPropertyBlock.GetFloat(_propertyNames[index]));
                }
                else if (index >= 4)
                {
                    if (materialPropertyBlock.HasTexture(_propertyNames[index]))
                        newMaterialPropertyBlock.SetTexture(_propertyNames[index], materialPropertyBlock.GetTexture(_propertyNames[index]));
                }
                else
                {
                    if (materialPropertyBlock.HasColor(_propertyNames[index]))
                        newMaterialPropertyBlock.SetColor(_propertyNames[index], materialPropertyBlock.GetColor(_propertyNames[index]));
                }
            }
            return newMaterialPropertyBlock;
        }
    }
}