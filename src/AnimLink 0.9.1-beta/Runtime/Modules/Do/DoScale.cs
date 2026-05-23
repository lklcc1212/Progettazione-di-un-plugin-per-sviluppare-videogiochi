namespace AnimLink
{
    using System;

    using UnityEngine;

    using static AnimLinkExtension;
    using static UnityEngine.GraphicsBuffer;
    using static UtilityExtension;

    public sealed class DoScale : DoBaseLoop<DoScale, Vector3>
    {
        #region Fields
        private int _axisIndex = 0;
        private bool _isUsingAxis;

        private float _rawTargetValue;
        private float _startValue;
        private float _effectiveTargetValue;

        private Vector3 _rawTargetVector;
        private Vector3 _startVector;
        private Vector3 _effectiveTargetVector;

        //Reset
        private float _originalValue;
        private Vector3 _originalVector;
        //setter/reverse/increment
        private Action<float> _setScale;
        private Action _reverse;
        private Action _increment;
        #endregion

        #region Public API

        /// <summary>
        /// Configures a <see cref="DoScale"/> animation on the specified <see cref="Transform"/> using a single axis.
        /// </summary>
        /// <param name="transform">The target Transform to scale.</param>
        /// <param name="target">The target scale value along the specified axis.</param>
        /// <param name="duration">The duration of the scaling animation in seconds.</param>
        /// <param name="axis">
        /// The axis to scale along. Options are <see cref="Axis.X"/>, <see cref="Axis.Y"/>, or <see cref="Axis.Z"/>.
        /// </param>
        public DoScale ConfigDoScale(Transform transform, float target, float duration, Axis axis)
        {
            if (CheckPlayingAndResetID()) return this;

            if (!transform)
            {
                LogWarning("[DoScale] The transform parameter null.");
                return this;
            }

            if (axis.HasMultipleFlags(true))
            {
                LogWarning($"[DoScale] The axis parameter has multiple flags or is 0 (None), so the previous value ( {Enum.GetNames(typeof(Axis))[_axisIndex + 1]} ) will be kept.");
            }
            else
                _axisIndex = (int)axis >> 1;

            //ValidateTargetAndAssign()
            if (transform != TargetObject || !_isUsingAxis)
                _initialized = false;

            TargetObject = transform;
            Transform = transform;
            Duration = duration;
            _isUsingAxis = true;
            _rawTargetValue = target;
            return this;
        }

        /// <summary>
        /// Configures a <see cref="DoScale"/> animation on the specified <see cref="Transform"/> using a Vector3 scale.
        /// </summary>
        /// <param name="transform">The target Transform to scale.</param>
        /// <param name="scale">The target local scale for the transform.</param>
        /// <param name="duration">The duration of the scaling animation in seconds.</param>
        public DoScale ConfigDoScale(Transform transform, Vector3 scale, float duration)
        {
            if (CheckPlayingAndResetID()) return this;

            if (!transform)
            {
                LogWarning("[DoScale] The transform parameter is null.");
                return this;
            }

            //ValidateTargetAndAssign()
            if (transform != TargetObject || _isUsingAxis)
                _initialized = false;

            TargetObject = transform;
            Transform = transform;
            Duration = duration;
            _isUsingAxis = false;
            _rawTargetVector = scale;
            return this;
        }

        public override void Reset()
        {
            if (Transform && _initialized)
            {
                if (_isUsingAxis)
                {
                    SetScale(_originalValue);
                }
                else
                {

                    SetScale(_originalVector);
                }
            }
        }

        public override DoScale SetGoal(Vector3 goal)
        {
            if (CheckPlayingAndResetID()) return this;

            if (_isUsingAxis)
                _initialized = false;

            _isUsingAxis = false;
            _rawTargetVector = goal;
            return this;
        }

        /// <summary>
        /// Sets the target value (goal) for this animation.
        /// </summary>
        /// <param name="goal">The target scale value along the specified axis.</param>
        /// <param name="axis">
        /// The axis to scale along. Options are <see cref="Axis.X"/>, <see cref="Axis.Y"/>, or <see cref="Axis.Z"/>.
        /// </param>
        public DoScale SetGoal(float goal, Axis axis)
        {
            if (CheckPlayingAndResetID()) return this;

            if (axis.HasMultipleFlags(true))
            {
                LogWarning($"[DoScale] The axis parameter has multiple flags or is 0 (None), so the previous value ( {Enum.GetNames(typeof(Axis))[_axisIndex + 1]} ) will be kept.");
            }
            else
                _axisIndex = (int)axis >> 1;

            if (!_isUsingAxis)
                _initialized = false;

            _isUsingAxis = true;
            _rawTargetValue = goal;
            return this;
        }
        #endregion

        #region Private API

        protected override void PrepareAnimation()
        {
            _duration = _loopType == BaseLoop.PingPong ? Duration * 0.5f : Duration;

            if (_totalElapsedTime > 0)
                return;

            // 根据是否按轴处理，初始化对应的值
            if (_isUsingAxis)
            {
                InitializeAxisScale();
            }
            else
            {
                InitializeVectorScale();
            }

            // 缓存操作委托
            CacheDelegates();
        }

        private void InitializeAxisScale()
        {
            _startValue = Transform.localScale[_axisIndex];

            _effectiveTargetValue = _loopType == BaseLoop.Increment ?
                _startValue + _rawTargetValue : _rawTargetValue;

            _originalValue = _startValue;
        }

        private void InitializeVectorScale()
        {
            _startVector = Transform.localScale;

            _effectiveTargetVector = _loopType == BaseLoop.Increment ?
                _startVector + _rawTargetVector : _rawTargetVector;

            _originalVector = _startVector;
        }

        private void CacheDelegates()
        {
            _setScale = _isUsingAxis ? AxisScaleSetter : VectorScaleSetter;
            _reverse = _isUsingAxis ? ReverseValue : ReverseVector;
            _increment = _isUsingAxis ? UpdateValueIncrement : UpdateVectorIncrement;
        }

        protected override bool UpdateAnimation()
        {
            if (!LoopCore(_reverse, _increment))
                return false;

            UpdateElapsedTime();
            float t = Mathf.Clamp01(_elapsedTime / _duration);

            _setScale(t);

            return true;
        }

        void AxisScaleSetter(float t)
        {
            SetScale(Mathf.LerpUnclamped(_startValue, _effectiveTargetValue, EasedT(t, _ease)));
        }

        void VectorScaleSetter(float t)
        {
            SetScale(Vector3.LerpUnclamped(_startVector, _effectiveTargetVector, EasedT(t, _ease)));
        }

        void ReverseValue()
        {
            (_effectiveTargetValue, _startValue) = (_startValue, _effectiveTargetValue);
            _isReversed = !_isReversed;
        }

        void ReverseVector()
        {
            (_effectiveTargetVector, _startVector) = (_startVector, _effectiveTargetVector);
            _isReversed = !_isReversed;
        }

        void UpdateValueIncrement()
        {
            if (_playBackward)
            {
                _effectiveTargetValue = _startValue;
                _startValue -= _rawTargetValue;
            }
            else
            {
                _startValue = _effectiveTargetValue;
                _effectiveTargetValue += _rawTargetValue;
            }
        }

        void UpdateVectorIncrement()
        {
            if (_playBackward)
            {
                _effectiveTargetVector = _startVector;
                _startVector -= _rawTargetVector;
            }
            else
            {
                _startVector = _effectiveTargetVector;
                _effectiveTargetVector += _rawTargetVector;
            }
        }

        private void SetScale(float easedValue)
        {
            Vector3 scale = Transform.localScale;
            scale[_axisIndex] = easedValue;
            Transform.localScale = scale;
        }

        private void SetScale(Vector3 scale)
        {
            Transform.localScale = scale;
        }
        #endregion
    }
}