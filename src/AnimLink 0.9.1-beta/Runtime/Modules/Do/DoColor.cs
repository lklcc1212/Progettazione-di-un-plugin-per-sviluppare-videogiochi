namespace AnimLink
{
    using System;
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;
    using static AnimLinkExtension;
    using static UtilityExtension;
    using Object = UnityEngine.Object;

    public sealed class DoColor : DoValueLoop<DoColor, Color>
    {
        #region Fields
        private ColorCompType _componentType;
        private MaterialPropertyBlock _materialPropertyBlock = new();

        private Color _effectiveTargetColor;
        private Color _startColor;
        private Color _rawTargetColor;
        private Color _deltaColor;

        private bool _doAlpha;

        //Reset
        private Color _originalColor;
        /// <summary>
        /// MeshRenderer之前是否缺少_Color属性。
        /// </summary>
        private bool _lacksColorProperty;

        // getter/setter delegate
        private Action<Color> SetColorFunc;
        private Func<float> GetAlphaFunc;
        #endregion

        #region Public API

        /// <summary>
        /// Configures a <see cref="DoColor"/> animation on the specified target object.
        /// </summary>
        /// <param name="@object">The target object to animate (e.g., SpriteRenderer, MeshRenderer, Material, Graphic).</param>
        /// <param name="color">The target color to animate to.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <param name="componentType">
        /// Optional. Specifies the target component type explicitly. If null, the system will automatically detect and match the type based on the provided object.
        /// <list type="bullet">
        /// <item><description><see cref="AlphaCompType.Sprite"/>: 2D sprite renderer.</description></item>
        /// <item><description><see cref="AlphaCompType.Mesh"/>: 3D mesh renderer (supports multiple materials).</description></item>
        /// <item><description><see cref="AlphaCompType.Material"/>: Animates the material directly.</description></item>
        /// <item><description><see cref="AlphaCompType.Graphic"/>: UI elements such as Image, Text, or TMP_Text.</description></item>
        /// </list>
        /// </param>
        public DoColor ConfigDoColor(Object @object, Color color, float duration, ColorCompType? componentType = null)
        {
            if (CheckPlayingAndResetID() || ValidateTargetAndAssign(@object, "@object")) return this;


            if (componentType.HasValue && componentType.HasMultipleFlags())
            {
                LogWarning("[DoColor] The componentType parameter contains multiple flags; it will be treated as null.");
                componentType = null;
            }

            _rawTargetColor = NormalizeColor(color);
            SetComponentType(componentType);
            Duration = duration;
            return this;
        }

        /// <summary>
        /// Specifies the material index to animate when the target component type is <see cref="ColorCompType.Mesh"/>.
        /// </summary>
        /// <param name="materialIndex">
        /// The index of the material to animate. Must be within the range of materials on the <see cref="MeshRenderer"/>.
        /// </param>
        public DoColor SetMaterialIndex(int materialIndex)
        {
            if (CheckPlayingAndResetID()) return this;

            if (_componentType != ColorCompType.Mesh)
            {
                LogWarning("[DoColor] Current component type is not MeshRenderer, so the input value will be ignored.");
                return this;
            }

            if (IsNotValidMaterialIndex(materialIndex))
            {
                LogWarning($"[DoColor] The material index {materialIndex} is out of range.");
                return this;
            }

            _materialIndex = materialIndex;
            return this;
        }

        /// <summary>
        /// Determines whether the alpha channel should also be modified when animating the color.
        /// </summary>
        /// <param name="state">
        /// If <c>true</c>, the alpha value will be animated along with the color; 
        /// if <c>false</c>, the alpha value will remain unchanged.
        /// </param>
        public DoColor WithAlpha(bool state)
        {
            if (CheckPlayingAndResetID()) return this;

            _doAlpha = state;
            return this;
        }

        public override void Reset()
        {
            if (TargetObject && _initialized)
            {
                if (
#if UNITY_EDITOR
                    (_lacksColorProperty || !_isNotEditorPreview)
#else
                    _lacksColorProperty
#endif
                    && _componentType == ColorCompType.Mesh)
                {
                    _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                    _materialPropertyBlock = _materialPropertyBlock.Clear(MaterialPropertyBLockCleaner.Name_._Color);
                    _meshRenderer.SetPropertyBlock(_materialPropertyBlock, _materialIndex);
                }
                else
                {
                    SetColorFunc(_originalColor);
                }
#if UNITY_EDITOR
                Repaint();
#endif
            }
        }

        public override bool SetTarget(Object target)
        {
            if (CheckPlayingAndResetID()) return false;

            if (ValidateTargetAndAssign(target, "target")) return false;

            return SetComponentType(null);
        }

        public override DoColor SetGoal(Color goal)
        {
            if (CheckPlayingAndResetID()) return this;

            _rawTargetColor = NormalizeColor(goal);
            return this;
        }
        #endregion

        #region Private API

        private bool SetComponentType(ColorCompType? objectType)
        {
            bool result = true;
            ColorCompType typeToUse = objectType ?? DetectType(TargetObject, out result);

            if (!result)
            {
                LogWarning("[DoColor] Unsupported object type detected. TargetObject = null.");
                TargetObject = null;
                return false;
            }

            if (!AssignField(typeToUse, TargetObject))
            {
                LogWarning($"[DoColor] TargetObject type mismatch ({typeToUse}). It will be set to null.");
                TargetObject = null;
                _componentType = default;
                return false;
            }

            _componentType = typeToUse;
            return true;
        }

        private ColorCompType DetectType(Object obj, out bool result)
        {
            result = true;
            return obj switch
            {
                SpriteRenderer => ColorCompType.Sprite,
                MeshRenderer => ColorCompType.Mesh,
                Graphic => ColorCompType.Graphic,
                Material => ColorCompType.Material,
                _ => GetDefault(out result)
            };
        }

        private ColorCompType GetDefault(out bool result)
        {
            result = false;
            return ColorCompType.Sprite;
        }

        private bool AssignField(ColorCompType type, Object obj)
        {
            switch (type)
            {
                case ColorCompType.Sprite:
                    _spriteRenderer = obj as SpriteRenderer;
                    return _spriteRenderer != null;

                case ColorCompType.Mesh:
                    if ((_meshRenderer = obj as MeshRenderer) != null)
                    {
                        _materialIndex = 0;
                        return true;
                    }
                    return false;

                case ColorCompType.Graphic:
                    _graphic = obj as Graphic;
                    _isTmpText = _graphic is TMP_Text;
                    return _graphic != null;

                case ColorCompType.Material:
                    _material = obj as Material;
                    return _material != null;

                default:
                    return false;
            }
        }

        bool IsNotValidMaterialIndex(int materialIndex)
        {
            return materialIndex >= _meshRenderer.sharedMaterials.Length || materialIndex < 0;
        }

        private void InitDelegates()
        {
            switch (_componentType)
            {
                case ColorCompType.Sprite:
                    SetColorFunc = c => _spriteRenderer.color = c;
                    GetAlphaFunc = () => _spriteRenderer.color.a;
                    break;

                case ColorCompType.Mesh:
                    SetColorFunc = c =>
                    {
                        _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                        _materialPropertyBlock.SetColor("_Color", c);
                        _meshRenderer.SetPropertyBlock(_materialPropertyBlock, _materialIndex);
                    };
                    GetAlphaFunc = () =>
                    {
                        _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                        return _materialPropertyBlock.HasColor("_Color")
                            ? _materialPropertyBlock.GetColor("_Color").a
                            : _meshRenderer.sharedMaterials[_materialIndex].color.a;
                    };
                    break;

                case ColorCompType.Graphic:
                    SetColorFunc = c => _graphic.color = c;
                    GetAlphaFunc = () => _graphic.color.a;
                    break;

                case ColorCompType.Material:
                    SetColorFunc = c => _material.color = c;
                    GetAlphaFunc = () => _material.color.a;
                    break;

                default:
                    SetColorFunc = c => { };
                    GetAlphaFunc = () => 0f;
                    break;
            }
        }

        Color GetColor()
        {
            switch (_componentType)
            {
                case ColorCompType.Sprite:
                    return _spriteRenderer.color;
                case ColorCompType.Mesh:
                    _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                    if (_materialPropertyBlock.HasColor("_Color"))
                        return _materialPropertyBlock.GetColor("_Color");
                    else
                    {
                        return _meshRenderer.sharedMaterials[_materialIndex].color;
                    }
                case ColorCompType.Graphic:
                    return _graphic.color;
                case ColorCompType.Material:
                    return _material.color;
                default:
                    return Color.clear;
            }
        }

#if UNITY_EDITOR
        void Repaint()
        {
            if (!_isNotEditorPreview)
            {
                // 如果是Grafic组件，则特殊处理。
                if (_componentType == ColorCompType.Graphic)
                {
                    if (_isTmpText)
                        EditorUtility.SetDirty(_graphic);
                    else
                        Canvas.ForceUpdateCanvases();
                }
                WindowViewUtils.RepaintGameView(); // 强制刷新GameView
                SceneView.RepaintAll(); // 强制刷新SceneView
            }
        }
#endif

        protected override void PrepareAnimation()
        {
            _duration = _loopType == ValueLoop.PingPong ? Duration * 0.5f : Duration;
            if (_totalElapsedTime > 0) return;

            Color color = GetColor();
            if (_componentType == ColorCompType.Mesh)
            {
                _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                _lacksColorProperty = !_materialPropertyBlock.HasColor("_Color");
            }

            switch (_loopType)
            {
                case ValueLoop.Increment:
                case ValueLoop.Decrement:
                    _effectiveTargetColor = NormalizeColor(color + _deltaColor * (_loopType == ValueLoop.Increment ? 1 : -1));
                    break;
                case ValueLoop.PingPong:
                    if (_isReversed)
                        ReverseColor();
                    break;
            }

            InitDelegates();
            _startColor = color;
            _originalColor = color;
            CalculateTargetColor(color);
        }

        protected override bool UpdateAnimation()
        {
            if (!LoopCore(ReverseColor, UpdateColorIncrement))
                return false;

            UpdateElapsedTime();
            float t = Mathf.Clamp01(_elapsedTime / _duration);

            // 计算 easedT
            float easedT = EasedT(t, _ease);

            // 计算新的颜色
            Color newColor = Color.LerpUnclamped(_startColor, _effectiveTargetColor, easedT);
            if (!_doAlpha)
                newColor.a = GetAlphaFunc();
            SetColorFunc(newColor);

#if UNITY_EDITOR
            Repaint(); // EditorPreview 强制刷新
#endif

            //return LoopCore(ReverseColor, UpdateColorIncrement);
            return true;
        }

        void UpdateColorIncrement()
        {
            Color deltaColor = _playBackward ? _deltaColor * -1 : _deltaColor;
            if (_playBackward)
            {
                _effectiveTargetColor = _startColor;
                _startColor = NormalizeColor(_startColor + deltaColor);
            }
            else
            {
                _startColor = _effectiveTargetColor;
                _effectiveTargetColor = NormalizeColor(_effectiveTargetColor + deltaColor);
            }
        }

        void ReverseColor()
        {
            (_startColor, _effectiveTargetColor) = (_effectiveTargetColor, _startColor);
            _isReversed = !_isReversed;
        }

        private Color NormalizeColor(Color color)
        {
            color.r = Mathf.Clamp01(color.r);
            color.g = Mathf.Clamp01(color.g);
            color.b = Mathf.Clamp01(color.b);
            color.a = Mathf.Clamp01(color.a);
            return color;
        }

        private void CalculateTargetColor(Color currentColor)
        {
            switch (_loopType)
            {
                case ValueLoop.Increment:
                    _deltaColor = _rawTargetColor;
                    _effectiveTargetColor = NormalizeColor(currentColor + _deltaColor);
                    break;
                case ValueLoop.Decrement:
                    _deltaColor = _rawTargetColor * -1;
                    _effectiveTargetColor = NormalizeColor(currentColor + _deltaColor);
                    break;
                default: // FromStart and PingPong
                    _deltaColor = Color.clear;
                    _effectiveTargetColor = _rawTargetColor;
                    break;
            }
        }

        protected override bool IsTargetValid(Object target)
        {
            return true;
        }

        protected override void SetTargetInternal(Object target)
        {
            SetComponentType(null);
        }
        #endregion
    }
}