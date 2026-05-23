namespace AnimLink
{
    using System;
    using UnityEngine;

    using static UtilityExtension;

    using Random = UnityEngine.Random;

    public sealed class AdvancedShake : AnimationCore<AdvancedShake, float>
    {
        #region Fields

        private PosScaleRot _shakeType = PosScaleRot.Position;
        private Axis _activeAxes = Axis.X | Axis.Y | Axis.Z;
        private Vector3 _targetOffset;
        private Vector3 _initialPosition;
        private Vector3 _initialScale;
        private Quaternion _initialRotation;
        private float _magnitude = 0.5f;

        private float _frequency = 15f;
        private AnimationCurve _magnitudeCurve = AnimationCurve.Linear(0, 1, 1, 0);

        private AnimationCurve _returnCurve = new(new Keyframe(0, 0), new Keyframe(1, 1));

        private readonly Vector3[] _noiseSeeds = new Vector3[3];

        private bool _hasReturned; // 标记是否执行 SmoothReturn
        #endregion

        #region Properties

        /// <summary>
        /// Return duration after shaking.
        /// </summary>
        public float ReturnDuration { get; private set; }

        public override float Progress => Mathf.Clamp01(_elapsedTime / _duration);

        #endregion

        #region Public API

        /// <summary>
        /// Configures an <see cref="AdvancedShake"/> animation for the specified <see cref="Transform"/>.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="magnitude">The magnitude of the shake.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <param name="activeAxes">
        /// The axes along which to apply the shake. Can combine multiple values (e.g., Axis.X | Axis.Y for shaking along both X and Y axes).
        /// </param>
        public AdvancedShake ConfigAdvShake(Transform transform, float magnitude, float duration, Axis activeAxes)
        {
            if (CheckPlayingAndResetID() || ValidateTargetAndAssign(transform, "transform")) return this;

            if (activeAxes is Axis.None)
                LogWarning($"[AdvancedShake] The activeAxes parameter is None, so the previous value will be kept.");
            else
                _activeAxes = activeAxes;

            Transform = transform;
            Duration = _duration = duration;
            _magnitude = magnitude;
            return this;
        }

        /// <summary>
        /// Sets the duration for the object to return to its original position after shaking.
        /// </summary>
        /// <param name="returnDuration">
        /// Time in seconds for the object to return to its original position after the shake.
        /// Larger values result in a smoother return; a value of 0 returns immediately.
        /// </param>
        public AdvancedShake SetReturnDuration(float returnDuration)
        {
            if (CheckPlayingAndResetID()) return this;

            ReturnDuration = returnDuration;
            _duration = Duration + ReturnDuration;
            return this;
        }

        /// <summary>
        /// Sets the shake type for this <see cref="AdvancedShake"/> instance.
        /// </summary>
        /// <param name="shakeType">
        /// The type of shake to apply.
        /// </param>
        /// <remarks>
        /// Changing the shake type will reset the initialized state, preventing <see cref="Reset"/> from being used
        /// until reconfigured.
        /// </remarks>
        public AdvancedShake SetShakeType(PosScaleRot shakeType)
        {
            if (CheckPlayingAndResetID()) return this;

            if (shakeType.HasMultipleFlags())
            {
                LogWarning($"[AdvancedShake] The shakeType parameter has multiple flags, so the previous value ( {_shakeType} ) will be kept.");
                return this;
            }

            // 切换类型时，使得Reset()不能使用。
            if (shakeType != _shakeType)
                _initialized = false;

            _shakeType = shakeType;
            return this;
        }

        /// <summary>
        /// Sets custom animation curves for the <see cref="AdvancedShake"/> instance.
        /// </summary>
        /// <param name="magnitudeCurve">
        /// Optional curve defining the shake magnitude over time. If null, the previous curve is kept.
        /// </param>
        /// <param name="returnCurve">
        /// Optional curve defining how the object returns to its initial position after shaking. If null, the previous curve is kept.
        /// </param>
        public AdvancedShake SetCurves(AnimationCurve magnitudeCurve = null, AnimationCurve returnCurve = null)
        {
            if (CheckPlayingAndResetID()) return this;

            if (magnitudeCurve != null) _magnitudeCurve = magnitudeCurve;
            if (returnCurve != null) _returnCurve = returnCurve;
            return this;
        }

        /// <summary>
        /// Sets the shake frequency for the <see cref="AdvancedShake"/> instance.
        /// <para><strong>Note:</strong> You should call <see cref="ConfigAdvShake(Transform, float, float, Axis)"/> 
        /// or <see cref="AnimLinkExtension.AdvancedShake(Transform, float, float, Axis)"/> before using this method.</para>
        /// </summary>
        /// <param name="frequency">
        /// The number of shake oscillations per unit of animation duration. Default is 15.
        /// </param>
        public AdvancedShake SetFrequency(float frequency = 15f)
        {
            if (CheckPlayingAndResetID()) return this;

            if (Duration <= 0)
            {
                LogWarning($"[AdvancedShake] Invalid duration ({Duration}). Frequency cannot be set when duration is non-positive. The previous value will be kept.");
                return this;
            }

            _frequency = frequency * Duration;
            return this;
        }

        public override AdvancedShake SetGoal(float goal)
        {
            if (CheckPlayingAndResetID()) return this;

            _magnitude = goal;
            return this;
        }

        /// <summary>
        /// Sets the target value (goal) for this animation.
        /// </summary>
        /// <param name="magnitude">The magnitude of the shake.</param>
        /// <param name="activeAxes">
        /// The axes along which to apply the shake. Can combine multiple values (e.g., Axis.X | Axis.Y for shaking along both X and Y axes).
        /// </param>
        public AdvancedShake SetGoal(float magnitude, Axis activeAxes)
        {
            if (CheckPlayingAndResetID()) return this;

            if (activeAxes is Axis.None)
                LogWarning($"[AdvancedShake] The activeAxes parameter is None, so the previous value will be kept.");
            else
                _activeAxes = activeAxes;

            _magnitude = magnitude;
            return this;
        }

        public override void Reset()
        {
            if (Transform && _initialized)
            {
                switch (_shakeType)
                {
                    case PosScaleRot.Position:
                        Transform.position = _initialPosition;
                        break;
                    case PosScaleRot.Scale:
                        Transform.localScale = _initialScale;
                        break;
                    case PosScaleRot.Rotation:
                        Transform.rotation = _initialRotation;
                        break;
                }
            }
        }
        #endregion

        #region Private API

        private void CacheInitialState()
        {
            _initialPosition = Transform.position;
            _initialScale = Transform.localScale;
            _initialRotation = Transform.rotation;

            _targetOffset = Vector3.zero;
        }

        private void GenerateNoiseSeeds()
        {
            // 为每个轴生成一个随机种子，确保每次抖动的随机性
            for (int i = 0; i < 3; i++)
            {
                _noiseSeeds[i] = new Vector3(
                    Random.Range(-1000f, 1000f),
                    Random.Range(-1000f, 1000f),
                    Random.Range(-1000f, 1000f)
                );
            }
        }

        protected override void PrepareAnimation()
        {
            if (_totalElapsedTime > 0) return;

            CacheInitialState();
            GenerateNoiseSeeds();
            _hasReturned = false;
        }

        protected override bool UpdateAnimation()
        {
            float elapsed = _elapsedTime;

            if (elapsed >= _duration) return false;

            UpdateElapsedTime(); // 更新时间

            if (!_hasReturned)
            {
                float duration = Duration;

                // 主动画阶段
                if (elapsed <= duration)
                {
                    float t = elapsed / duration;
                    UpdateTargetOffset(t, _magnitudeCurve.Evaluate(t));
                    ApplyOffset(_targetOffset);
                    return true; // 继续执行
                }
                else
                {
                    // 超过 Duration，进入 SmoothReturn 阶段
                    _hasReturned = true;
                }
            }

            // SmoothReturn 阶段
            if (_hasReturned)
            {
                float t = elapsed / _duration;

                float curveValue = _returnCurve.Evaluate(t);
                Vector3 currentOffset = Vector3.Lerp(_targetOffset, Vector3.zero, curveValue);
                ApplyOffset(currentOffset);
                return true; // 继续执行
            }

            return true;
        }


        private void UpdateTargetOffset(float time, float magnitudeScale)
        {
            float noiseTime = time * _frequency;
            _targetOffset = Vector3.zero;

            // 使用Perlin噪声生成平滑的随机偏移
            /*
             Mathf.PerlinNoise(x, y) -> 生成一个在0到1之间的平滑随机值
             ... * 2 - 1 -> 将值映射到-1到1之间
            */
            if (_activeAxes.HasFlag(Axis.X))
                _targetOffset.x = (Mathf.PerlinNoise(_noiseSeeds[0].x, noiseTime) * 2 - 1);
            if (_activeAxes.HasFlag(Axis.Y))
                _targetOffset.y = (Mathf.PerlinNoise(_noiseSeeds[1].y, noiseTime) * 2 - 1);
            if (_activeAxes.HasFlag(Axis.Z))
                _targetOffset.z = (Mathf.PerlinNoise(_noiseSeeds[2].z, noiseTime) * 2 - 1);

            _targetOffset *= _magnitude * magnitudeScale;
        }

        private void ApplyOffset(Vector3 offset)
        {
            switch (_shakeType)
            {
                case PosScaleRot.Position:
                    Transform.position = _initialPosition + offset;
                    break;
                case PosScaleRot.Scale:
                    Transform.localScale = _initialScale + offset;
                    break;
                case PosScaleRot.Rotation:
                    Vector3 euler = offset;
                    Transform.rotation = _initialRotation * Quaternion.Euler(euler);
                    break;
            }
        }
        #endregion
    }
}
