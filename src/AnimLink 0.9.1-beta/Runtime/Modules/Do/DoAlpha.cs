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

    public sealed class DoAlpha : DoValueLoop<DoAlpha, float>
    {
        #region Fields
        private AlphaCompType _componentType;
        private MaterialPropertyBlock _materialPropertyBlock = new();
        private bool _isCanvas;

        private float _effectiveTargetAlpha;
        private float _startAlpha;
        private float _rawTargetAlpha;
        private float _deltaAlpha;

        // Reset
        private float _originalAlpha = 1;
        /// <summary>
        /// MeshRenderer之前是否缺少_Color属性。
        /// </summary>
        private bool _lacksColorProperty;

        // getter/setter delegate
        private Action<Color> SetColorFunc;
        private Func<Color> GetColorFunc;

        #endregion

        #region Public API

        /// <summary>
        /// Configures a <see cref="DoAlpha"/> animation for the specified target object.
        /// </summary>
        /// <param name="object">
        /// The target object whose alpha will be animated. Can be a <see cref="SpriteRenderer"/>, <see cref="MeshRenderer"/>, <see cref="Material"/>, 
        /// <see cref="Graphic"/> (Image/Text/TMP_Text), or <see cref="CanvasGroup"/>.
        /// </param>
        /// <param name="alpha">Target opacity value, ranging from 0.0 (fully transparent) to 1.0 (fully opaque).</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="componentType">
        /// Optional. Specifies the target component type explicitly. If null, the system will automatically detect and match the type based on the provided object.
        /// <list type="bullet">
        /// <item><description><see cref="AlphaCompType.Sprite"/>: 2D sprite renderer.</description></item>
        /// <item><description><see cref="AlphaCompType.Mesh"/>: 3D mesh renderer (supports multiple materials).</description></item>
        /// <item><description><see cref="AlphaCompType.Material"/>: Animates the material directly.</description></item>
        /// <item><description><see cref="AlphaCompType.Graphic"/>: UI elements such as Image, Text, or TMP_Text.</description></item>
        /// <item><description><see cref="AlphaCompType.CanvasGroup"/>: Controls overall opacity of a UI group.</description></item>
        /// </list>
        /// </param>
        public DoAlpha ConfigDoAlpha(Object @object, float alpha, float duration, AlphaCompType? componentType = null)
        {
            if (CheckPlayingAndResetID() || ValidateTargetAndAssign(@object, "@object")) return this;

            if (componentType.HasValue && componentType.HasMultipleFlags())
            {
                LogWarning("[DoAlpha] The componentType parameter contains multiple flags; it will be treated as null.");
                componentType = null;
            }

            _rawTargetAlpha = Mathf.Clamp01(alpha);
            SetComponentType(componentType);
            Duration = duration;
            return this;
        }

        /// <summary>
        /// Specifies the material index to animate when the target component type is <see cref="AlphaCompType.Mesh"/>.
        /// </summary>
        /// <param name="materialIndex">
        /// The index of the material to animate. Must be within the range of materials on the <see cref="MeshRenderer"/>.
        /// </param>
        public DoAlpha SetMaterialIndex(int materialIndex)
        {
            if (CheckPlayingAndResetID()) return this;

            if (_componentType != AlphaCompType.Mesh)
            {
                LogWarning("[DoAlpha] Current component type is not MeshRenderer, so the input value will be ignored.");
                return this;
            }

            if (IsNotValidMaterialIndex(materialIndex))
            {
                LogWarning($"[DoAlpha] The material index {materialIndex} is out of range.");
                return this;
            }

            _materialIndex = materialIndex;
            return this;
        }

        public override void Reset()
        {
            if (TargetObject && _initialized)
            {
                if (_componentType == AlphaCompType.CanvasGroup)
                {
                    _canvasGroup.alpha = _originalAlpha;
                }
                else if (
#if UNITY_EDITOR
                    (_lacksColorProperty || !_isNotEditorPreview)
#else
                    _lacksColorProperty
#endif
                    && _componentType == AlphaCompType.Mesh)
                {
                    _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                    _materialPropertyBlock = _materialPropertyBlock.Clear(MaterialPropertyBLockCleaner.Name_._Color);
                    _meshRenderer.SetPropertyBlock(_materialPropertyBlock, _materialIndex);
                }
                else
                {
                    Color originalColor = GetColorFunc();
                    originalColor.a = _originalAlpha;
                    SetColorFunc(originalColor);
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
            return SetComponentType(null); ;
        }

        public override DoAlpha SetGoal(float goal)
        {
            if (CheckPlayingAndResetID()) return this;

            _rawTargetAlpha = Mathf.Clamp01(goal);
            return this;
        }
        #endregion

        #region Private API

        private bool SetComponentType(AlphaCompType? objectType)
        {
            bool result = true;
            AlphaCompType typeToUse = objectType ?? DetectType(TargetObject, out result);

            if (!result)
            {
                LogWarning("[DoAlpha] Unsupported object type detected. TargetObject = null.");
                TargetObject = null;
                return false;
            }

            if (!AssignField(typeToUse, TargetObject))
            {
                LogWarning($"[DoAlpha] TargetObject type mismatch ({typeToUse}). It will be set to null.");
                TargetObject = null;
                _componentType = default;
                return false;
            }

            _componentType = typeToUse;
            _isCanvas = typeToUse is AlphaCompType.CanvasGroup;
            return true;
        }

        private AlphaCompType DetectType(Object obj, out bool result)
        {
            result = true;
            return obj switch
            {
                SpriteRenderer => AlphaCompType.Sprite,
                MeshRenderer => AlphaCompType.Mesh,
                CanvasGroup => AlphaCompType.CanvasGroup,
                Graphic => AlphaCompType.Graphic,
                Material => AlphaCompType.Material,
                _ => GetDefault(out result)
            };
        }

        private AlphaCompType GetDefault(out bool result)
        {
            result = false;
            return AlphaCompType.Sprite;
        }


        private bool AssignField(AlphaCompType type, Object obj)
        {
            switch (type)
            {
                case AlphaCompType.Sprite:
                    _spriteRenderer = obj as SpriteRenderer;
                    return _spriteRenderer != null;

                case AlphaCompType.Mesh:
                    _meshRenderer = obj as MeshRenderer;
                    return _meshRenderer != null;

                case AlphaCompType.CanvasGroup:
                    _canvasGroup = obj as CanvasGroup;
                    return _canvasGroup != null;

                case AlphaCompType.Graphic:
                    _graphic = obj as Graphic;
                    _isTmpText = _graphic is TMP_Text;
                    return _graphic != null;

                case AlphaCompType.Material:
                    if ((_meshRenderer = obj as MeshRenderer) != null)
                    {
                        _materialIndex = 0;
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        bool IsNotValidMaterialIndex(int materialIndex)
        {
            return materialIndex >= _meshRenderer.sharedMaterials.Length || materialIndex < 0;
        }

        float GetAlpha()
        {
            switch (_componentType)
            {
                case AlphaCompType.Sprite:
                    return _spriteRenderer.color.a;
                case AlphaCompType.CanvasGroup:
                    return _canvasGroup.alpha;
                case AlphaCompType.Mesh:
                    _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                    if (_materialPropertyBlock.HasColor("_Color"))
                        return _materialPropertyBlock.GetColor("_Color").a;
                    else
                        return _meshRenderer.sharedMaterials[_materialIndex].color.a;
                case AlphaCompType.Graphic:
                    return _graphic.color.a;
                case AlphaCompType.Material:
                    return _material.color.a;
                default:
                    return 0;
            }
        }

        private void InitDelegates()
        {
            switch (_componentType)
            {
                case AlphaCompType.Sprite:
                    SetColorFunc = c => _spriteRenderer.color = c;
                    GetColorFunc = () => _spriteRenderer.color;
                    break;

                case AlphaCompType.Mesh:
                    SetColorFunc = c =>
                    {
                        _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                        _materialPropertyBlock.SetColor("_Color", c);
                        _meshRenderer.SetPropertyBlock(_materialPropertyBlock, _materialIndex);
                    };
                    GetColorFunc = () =>
                    {
                        _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                        return _materialPropertyBlock.HasColor("_Color")
                            ? _materialPropertyBlock.GetColor("_Color")
                            : _meshRenderer.sharedMaterials[_materialIndex].color;
                    };
                    break;

                case AlphaCompType.Graphic:
                    SetColorFunc = c => _graphic.color = c;
                    GetColorFunc = () => _graphic.color;
                    break;

                case AlphaCompType.Material:
                    SetColorFunc = c => _material.color = c;
                    GetColorFunc = () => _material.color;
                    break;

                default:
                    SetColorFunc = c => { };
                    GetColorFunc = () => Color.clear;
                    break;
            }
        }

#if UNITY_EDITOR
        void Repaint()
        {
            if (!_isNotEditorPreview)
            {
                // 如果是Grafic组件，则特殊处理。
                if (_componentType == AlphaCompType.Graphic)
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

            float alpha = GetAlpha();
            if (_componentType == AlphaCompType.Mesh)
            {
                _meshRenderer.GetPropertyBlock(_materialPropertyBlock, _materialIndex);
                _lacksColorProperty = !_materialPropertyBlock.HasColor("_Color");
            }

            InitDelegates();
            _startAlpha = alpha;
            _originalAlpha = alpha;
            CalculateTargetAlpha(alpha);
        }

        protected override bool UpdateAnimation()
        {
            if (!LoopCore(ReverseAlpha, UpdateAlphaIncrement))
                return false;

            UpdateElapsedTime();
            float t = Mathf.Clamp01(_elapsedTime / _duration);

            // 更新 alpha
            float newAlpha = Mathf.LerpUnclamped(_startAlpha, _effectiveTargetAlpha, EasedT(t, _ease));
            if (_isCanvas)
                _canvasGroup.alpha = newAlpha;
            else
            {
                Color newColor = GetColorFunc();
                newColor.a = newAlpha;
                SetColorFunc(newColor);
            }

#if UNITY_EDITOR
            Repaint(); // 编辑器预览刷新
#endif
            return true;
        }

        void ReverseAlpha()
        {
            (_startAlpha, _effectiveTargetAlpha) = (_effectiveTargetAlpha, _startAlpha);
            _isReversed = !_isReversed;
        }

        void UpdateAlphaIncrement()
        {
            float deltaAlpha = _playBackward ? -_deltaAlpha : _deltaAlpha;
            if (_playBackward)
            {
                _effectiveTargetAlpha = _startAlpha;
                _startAlpha = Mathf.Clamp01(_startAlpha + deltaAlpha);
            }
            else
            {
                _startAlpha = _effectiveTargetAlpha;
                _effectiveTargetAlpha = Mathf.Clamp01(_effectiveTargetAlpha + deltaAlpha);
            }
        }

        private void CalculateTargetAlpha(float currentAlpha)
        {
            switch (_loopType)
            {
                case ValueLoop.Increment:
                    _deltaAlpha = _rawTargetAlpha;
                    _effectiveTargetAlpha = Mathf.Clamp01(currentAlpha + _deltaAlpha);
                    break;
                case ValueLoop.Decrement:
                    _deltaAlpha = -_rawTargetAlpha;
                    _effectiveTargetAlpha = Mathf.Clamp01(currentAlpha + _deltaAlpha);
                    break;
                default: // FromStart and PingPong
                    _deltaAlpha = 0;
                    _effectiveTargetAlpha = _rawTargetAlpha;
                    break;
            }
        }
        #endregion
    }
}