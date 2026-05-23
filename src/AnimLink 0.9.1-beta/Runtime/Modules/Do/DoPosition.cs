namespace AnimLink
{
    using System;

    using UnityEngine;

    using static AnimLinkExtension;
    using static UtilityExtension;

    public sealed class DoPosition : DoBaseLoop<DoPosition, Vector3>
    {
        #region Fields

        private int _axisIndex = 0;
        private bool _applyInLocalSpace;
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
        private Action<float> _setPosition;
        private Action _reverse;
        private Action _increment;
        #endregion

        #region Public API

        /// <summary>
        /// Configures a <see cref="DoPosition"/> animation for the given <see cref="Transform"/> along a single axis.
        /// </summary>
        /// <param name="transform">The target <see cref="Transform"/> to animate.</param>
        /// <param name="target">The target position or incremental value (depending on animation type).</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <param name="axis">
        /// The axis along which to apply the movement.  
        /// Valid values: <see cref="Axis.X"/>, <see cref="Axis.Y"/>, <see cref="Axis.Z"/>.
        /// </param>
        public DoPosition ConfigDoPos(Transform transform, float target, float duration, Axis axis)
        {
            if (CheckPlayingAndResetID()) return this;

            if (!transform)
            {
                LogWarning("[DoPosition] The transform parameter null.");
                return this;
            }

            if (axis.HasMultipleFlags(true))
            {
                LogWarning($"[DoPosition] The axis parameter has multiple flags, so the previous value ( {Enum.GetNames(typeof(Axis))[_axisIndex]} ) will be kept.");
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
        /// Configures a positional animation for the given <see cref="Transform"/> using a target <see cref="Vector3"/> position.
        /// </summary>
        /// <param name="transform">The target <see cref="Transform"/> to animate.</param>
        /// <param name="position">The target world position for the animation.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        public DoPosition ConfigDoPos(Transform transform, Vector3 position, float duration)
        {
            if (CheckPlayingAndResetID()) return this;

            if (!transform)
            {
                LogWarning("[DoPosition] The transform parameter is null.");
                return this;
            }

            //ValidateTargetAndAssign()
            if (transform != TargetObject || _isUsingAxis)
                _initialized = false;

            TargetObject = transform;
            Transform = transform;
            Duration = duration;
            _isUsingAxis = false;
            _rawTargetVector = position;
            return this;
        }

        /// <summary>
        /// Sets the coordinate space for the position animation (World or Self). 
        /// </summary>
        /// <param name="space">The coordinate space to use: <see cref="Space.World"/> or <see cref="Space.Self"/>.</param>
        public DoPosition SetSpace(Space space)
        {
            if (CheckPlayingAndResetID()) return this;

            if (space.HasMultipleFlags())
            {
                LogWarning($"[DoPosition] The space parameter has multiple flags, so the previous value ({(_applyInLocalSpace ? Space.Self : Space.World)}) will be kept.");
                return this;
            }

            bool isLocal = space is Space.Self;

            // 切换类型时，使得Reset()不能使用。
            if (_applyInLocalSpace != isLocal)
                _initialized = false;


            _applyInLocalSpace = isLocal;
            return this;
        }

        public override void Reset()
        {
            if (Transform && _initialized)
            {
                if (_isUsingAxis)
                    SetPosition(_originalValue);
                else
                    SetPosition(_originalVector);
            }
        }

        public override DoPosition SetGoal(Vector3 goal)
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
        /// <param name="goal">The target position or incremental value (depending on animation type).</param>
        /// <param name="axis">
        /// The axis along which to apply the movement.  
        /// Valid values: <see cref="Axis.X"/>, <see cref="Axis.Y"/>, <see cref="Axis.Z"/>.
        /// </param>
        public DoPosition SetGoal(float goal, Axis axis)
        {
            if (CheckPlayingAndResetID()) return this;

            if (axis.HasMultipleFlags(true))
            {
                LogWarning($"[DoPosition] The axis parameter has multiple flags, so the previous value ( {Enum.GetNames(typeof(Axis))[_axisIndex]} ) will be kept.");
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

            if (_totalElapsedTime > 0f) return;

            // 根据是否按轴处理，初始化对应的值
            if (_isUsingAxis)
            {
                InitializeAxisPosition();
            }
            else
            {
                InitializeVectorPosition();
            }

            // 缓存操作委托
            CacheDelegates();
        }

        private void InitializeAxisPosition()
        {
            float fromValue = _applyInLocalSpace ? Transform.localPosition[_axisIndex] : Transform.position[_axisIndex];

            _effectiveTargetValue = _loopType is BaseLoop.Increment ?
                _rawTargetValue + fromValue : _rawTargetValue;

            _startValue = fromValue;
            _originalValue = fromValue;
        }

        private void InitializeVectorPosition()
        {
            Vector3 fromVector = _applyInLocalSpace ? Transform.localPosition : Transform.position;

            _effectiveTargetVector = _loopType is BaseLoop.Increment ?
                _rawTargetVector + fromVector : _rawTargetVector;

            _startVector = fromVector;
            _originalVector = _startVector;
        }

        private void CacheDelegates()
        {
            _setPosition = _isUsingAxis ? AxisPositionSetter : VectorPositionSetter;
            _reverse = _isUsingAxis ? ReverseValue : ReverseVector;
            _increment = _isUsingAxis ? UpdateValueIncrement : UpdateVectorIncrement;
        }

        protected override bool UpdateAnimation()
        {
            if (!LoopCore(_reverse, _increment))
                return false;

            UpdateElapsedTime();
            float t = Mathf.Clamp01(_elapsedTime / _duration);

            _setPosition(t);

            return true;
        }

        void AxisPositionSetter(float t)
        {
            float easedValue = Mathf.LerpUnclamped(_startValue, _effectiveTargetValue, EasedT(t, _ease));
            SetPosition(easedValue);
        }

        void VectorPositionSetter(float t)
        {
            Vector3 easedVector = Vector3.LerpUnclamped(_startVector, _effectiveTargetVector, EasedT(t, _ease));
            SetPosition(easedVector);
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
                _effectiveTargetValue = _startValue;  // 上轮目标值 = 起点
                _startValue -= _rawTargetValue; // 起点 += 增量
            }
            else
            {
                _startValue = _effectiveTargetValue;   // 起点 = 上轮目标值
                _effectiveTargetValue += _rawTargetValue; // 目标值 += 增量
            }
        }

        void UpdateVectorIncrement()
        {
            if (_playBackward)
            {
                _effectiveTargetVector = _startVector;  // 上轮目标值 = 起点
                _startVector -= _rawTargetVector; // 起点 -= 增量
            }
            else
            {
                _startVector = _effectiveTargetVector;   // 起点 = 上轮目标值
                _effectiveTargetVector += _rawTargetVector; // 目标值 += 增量
            }
        }

        void SetPosition(float value)
        {
            Vector3 position = _applyInLocalSpace ? Transform.localPosition : Transform.position;
            position[_axisIndex] = value;
            if (_applyInLocalSpace)
                Transform.localPosition = position;
            else
                Transform.position = position;
        }

        void SetPosition(Vector3 position)
        {
            if (_applyInLocalSpace)
                Transform.localPosition = position;
            else
                Transform.position = position;
        }
        #endregion
    }
}