namespace AnimLink
{
    using System;
    using System.Linq;
    using UnityEngine;
    using static UtilityExtension;
    using Random = UnityEngine.Random;

    public sealed class Shake : AnimationCore<Shake, float>
    {
        #region Fields

        private Axis _activeAxes = Axis.X | Axis.Y | Axis.Z;
        private Axis[] _values;
        private PosScale _shakeType = PosScale.Position;
        public Vector3 FromPos_Scale { get; private set; }
        private float _magnitude = 0.1f;

        private Axis[] _axes;
        private Vector3 _currentOffset; // 当前帧偏移量
        #endregion

        #region Properties

        public override float Progress => Mathf.Clamp01(_elapsedTime / _duration);

        #endregion

        #region Public API

        /// <summary>
        /// Configures an <see cref="Shake"/> animation for the specified <see cref="Transform"/>.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="magnitude">The magnitude of the shake.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <param name="activeAxes">
        /// The axes along which to apply the shake. Can combine multiple values (e.g., Axis.X | Axis.Y for shaking along both X and Y axes).
        /// </param>
        public Shake ConfigShake(Transform transform, float magnitude, float duration, Axis activeAxes)
        {
            if (CheckPlayingAndResetID() || ValidateTargetAndAssign(transform, "transform")) return this;

            if (activeAxes is Axis.None)
                LogWarning($"[Shake] The activeAxes parameter is None, so the previous value will be kept.");
            else
                _activeAxes = activeAxes;

            Transform = transform;
            Duration = _duration = duration;
            _magnitude = magnitude;
            return this;
        }

        /// <summary>
        /// Sets the shake type for this <see cref="Shake"/> instance.
        /// </summary>
        /// <param name="shakeType">
        /// The type of shake to apply.
        /// </param>
        /// <remarks>
        /// Changing the shake type will reset the initialized state, preventing <see cref="Reset"/> from being used
        /// until reconfigured.
        /// </remarks>
        public Shake SetShakeType(PosScale shakeType)
        {
            if (CheckPlayingAndResetID()) return this;

            if (shakeType.HasMultipleFlags())
            {
                LogWarning($"[Shake] The shakeType parameter has multiple flags, so the previous value ( {_shakeType} ) will be kept.");
                return this;
            }

            // 切换类型时，使得Reset()不能使用。
            if (shakeType != _shakeType)
                _initialized = false;

            _shakeType = shakeType;
            return this;
        }

        public override void Reset()
        {
            if (Transform && _initialized)
                if (_shakeType == PosScale.Position)
                {
                    Transform.position = FromPos_Scale;
                }
                else
                {
                    Transform.localScale = FromPos_Scale;
                }
        }

        public override Shake SetGoal(float magnitude)
        {
            if (CheckPlayingAndResetID()) return this;

            _magnitude = magnitude;
            return this;
        }

        /// <summary>
        /// Sets the target value (goal) for this animation.
        /// </summary>
        /// <param name="magnitude">The magnitude of the shake.</param>
        /// <param name="activeAxes">
        /// The axes along which to apply the shake. Can combine multiple values (e.g., Axis.X | Axis.Y for shaking along both X and Y axes).
        /// </param>
        public Shake SetGoal(float magnitude, Axis activeAxes)
        {
            if (CheckPlayingAndResetID()) return this;

            if (activeAxes is Axis.None)
                LogWarning($"[Shake] The activeAxes parameter is None, so the previous value will be kept.");
            else
                _activeAxes = activeAxes;

            _magnitude = magnitude;
            return this;
        }
        #endregion

        #region Private API

        protected override void PrepareAnimation()
        {
            // 初始化状态
            if (_totalElapsedTime <= 0)
            {
                _axes = _activeAxes.GetFlags().ToArray();
                if (_shakeType == PosScale.Position)
                    FromPos_Scale = Transform.position;
                else
                    FromPos_Scale = Transform.localScale;
            }
        }

        protected override bool UpdateAnimation()
        {
            if (_elapsedTime >= Duration)
            {
                // 动画完成
                Reset();
                return false;
            }

            // 更新时间
            UpdateElapsedTime();

            // 每帧生成随机增量
            foreach (Axis value in _axes)
            {
                switch (value)
                {
                    case Axis.X:
                        _currentOffset.x = Random.Range(-1f, 1f) * _magnitude;
                        break;
                    case Axis.Y:
                        _currentOffset.y = Random.Range(-1f, 1f) * _magnitude;
                        break;
                    case Axis.Z:
                        _currentOffset.z = Random.Range(-1f, 1f) * _magnitude;
                        break;
                }
            }

            // 应用增量
            if (_shakeType == PosScale.Position)
                Transform.position = FromPos_Scale + _currentOffset;
            else
                Transform.localScale = FromPos_Scale + _currentOffset;

            return true; // 继续执行
        }
        #endregion
    }
}