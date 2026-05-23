namespace AnimLink
{
    using System;
    using UnityEngine;

    using static UtilityExtension;

    public sealed class DoJolt : AnimationCore<DoJolt, Vector3>
    {
        #region Fields

        private PosScaleRot _joltType = PosScaleRot.Position;
        private int _currentIntervalIndex = 0;
        private float _currentInterval;
        private float[] _intervals;
        private Vector3[] _vecOffsets;
        private Quaternion[] _rotOffsets;
        private int _correctedSize;

        private bool _isRotJolt;

        private Vector3 _lastOffsetV;
        private Vector3 _strVector;
        private Quaternion _lastOffsetQ;
        private Quaternion _strRot;

        private int _vibrato = 10;
        private float _elasticity = .5f;

        //getter/setter target
        private Func<Vector3> _getter;
        private Action<Vector3> _setter;

        //Reset
        private Vector3 _originalState;

        #endregion

        #region Properties

        public override float Progress => Mathf.Clamp01(_currentLoopTime / _duration);

        #endregion

        #region Public API

        /// <summary>
        /// Configures the <see cref="DoJolt"/> instance with the target <see cref="Transform"/> and jolt parameters.
        /// </summary>
        /// <param name="transform">The target <see cref="Transform"/> to apply the jolt to.</param>
        /// <param name="direction">The direction and magnitude of the jolt.</param>
        /// <param name="duration">The total duration of the jolt animation in seconds.</param>
        /// <param name="vibrato">The number of oscillations during the jolt. Default is 10.</param>
        /// <param name="elasticity">Amount (0–1) the vector overshoots when bouncing back. 1 = full oscillation; 0 = only to start. Default is 0.5.</param>
        public DoJolt ConfigDoJolt(Transform transform, Vector3 direction, float duration, int vibrato = 10, float elasticity = .5f)
        {
            if (CheckPlayingAndResetID() || ValidateTargetAndAssign(transform, "transform")) return this;

            Transform = transform;
            Duration = _duration = duration;
            GenerateJoltData(direction, duration, vibrato, elasticity);
            return this;
        }

        /// <summary>
        /// Sets the type of jolt to apply to the target transform.
        /// </summary>
        /// <param name="joltType">
        /// The <see cref="PosScaleRot"/> value indicating which property (Position, Scale, Rotation) to apply the jolt to.
        /// </param>
        public DoJolt SetJoltType(PosScaleRot joltType)
        {
            if (CheckPlayingAndResetID()) return this;

            if (joltType.HasMultipleFlags())
            {
                LogWarning($"[DoJolt] The joltType parameter has multiple flags, so the previous value ( {_joltType} ) will be kept.");
                return this;
            }

            if (joltType != _joltType)
                _initialized = false;

            _joltType = joltType;

            return this;
        }

        public override void Reset()
        {
            if (Transform && _initialized)
            {
                switch (_joltType)
                {
                    case PosScaleRot.Position:
                        Transform.position = _originalState;
                        break;
                    case PosScaleRot.Rotation:
                        Transform.eulerAngles = _originalState;
                        break;
                    case PosScaleRot.Scale:
                        Transform.localScale = _originalState;
                        break;
                }
            }
        }

        public override DoJolt SetGoal(Vector3 goal)
        {
            if (CheckPlayingAndResetID()) return this;

            GenerateJoltData(goal, Duration, _vibrato, _elasticity);
            return this;
        }

        /// <summary>
        /// Sets the target value (goal) for this animation.
        /// </summary>
        /// <param name="direction">The direction and magnitude of the jolt.</param>
        /// <param name="vibrato">The number of oscillations during the jolt. Default is 10.</param>
        /// <param name="elasticity">Amount (0–1) the vector overshoots when bouncing back. 1 = full oscillation; 0 = only to start. Default is 0.5.</param>
        public DoJolt SetGoal(Vector3 direction, int vibrato = 10, float elasticity = .5f)
        {
            if (CheckPlayingAndResetID()) return this;

            GenerateJoltData(direction, Duration, vibrato, elasticity);
            return this;
        }
        #endregion

        #region Private API

        protected override void PrepareAnimation()
        {
            switch (_joltType)
            {
                case PosScaleRot.Position:
                case PosScaleRot.Scale:
                    _isRotJolt = false;
                    InitializeVector();
                    break;

                case PosScaleRot.Rotation:
                    _isRotJolt = true;
                    InitializeRotation();
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported type: {_joltType}");
            }
        }
        private void InitializeVector()
        {
            // 设置 Getter / Setter
            if (_joltType == PosScaleRot.Position)
            {
                _getter = () => Transform.position;
                _setter = p => Transform.position = p;
            }
            else // Scale
            {
                _getter = () => Transform.localScale;
                _setter = p => Transform.localScale = p;
            }

            InitializeVectorOffsets();
        }

        private void InitializeRotation()
        {
            if (_totalElapsedTime <= 0)
            {
                InitializeRotationOffsets();
                _lastOffsetQ = Quaternion.identity;
                _currentIntervalIndex = 0;
                _strRot = Transform.rotation;
                _originalState = _strRot.eulerAngles;
                _currentInterval = _intervals[_currentIntervalIndex];
            }
            else
            {
                _lastOffsetQ = GetPreviousRotationOffset();
            }
        }

        private void InitializeRotationOffsets()
        {
            int count = _vecOffsets.Length;

            // 分配或重分配
            if (_rotOffsets == null || _rotOffsets.Length != count)
                _rotOffsets = new Quaternion[count];

            // 生成旋转
            for (int i = 0; i < count; i++)
                _rotOffsets[i] = Quaternion.Euler(_vecOffsets[i]);
        }

        private Quaternion GetPreviousRotationOffset()
        {
            int index = _currentIntervalIndex - (_playBackward ? 0 : 1);
            return _rotOffsets[index];
        }

        private void InitializeVectorOffsets()
        {
            if (_totalElapsedTime <= 0)
            {
                _lastOffsetV = Vector3.zero;
                _currentIntervalIndex = 0;
                _strVector = _originalState = _getter();
                _currentInterval = _intervals[_currentIntervalIndex];
            }
            else
            {
                _lastOffsetV = GetPreviousVectorOffset();
            }
        }

        private Vector3 GetPreviousVectorOffset()
        {
            int index = _currentIntervalIndex - (_playBackward ? 0 : 1);
            return _vecOffsets[index];
        }

        private Vector3 GetVectorTargetOffset()
        {
            if (_playBackward)
                return _currentIntervalIndex == 0 ? Vector3.zero : _vecOffsets[_currentIntervalIndex - 1];
            else
                return _vecOffsets[_currentIntervalIndex];
        }

        private Quaternion GetQuaternionTargetOffSet()
        {
            if (_playBackward)
                return _currentIntervalIndex == 0 ? Quaternion.identity : _rotOffsets[_currentIntervalIndex - 1];
            else
                return _rotOffsets[_currentIntervalIndex];
        }

        float GetT()
        {
            // 更新时间并计算归一化 t
            UpdateElapsedTime();
            return Mathf.Clamp01(_playBackward
                ? 1 - _elapsedTime / _currentInterval
                : _elapsedTime / _currentInterval);
        }

        protected override bool UpdateAnimation()
        {
            if (_isRotJolt)
                return AnimateRotation();
            else
                return AnimateVector();
        }

        private bool AnimateRotation()
        {
            Quaternion targetOffset = GetQuaternionTargetOffSet();

            if (!LoopCore(_playBackward, ref _lastOffsetQ, ref targetOffset))
                return false;

            Quaternion newOffset = Quaternion.Slerp(_lastOffsetQ, targetOffset, GetT());
            Transform.rotation = _strRot * newOffset;

            return true;
        }

        private bool AnimateVector()
        {
            Vector3 targetOffset = GetVectorTargetOffset();

            if (!LoopCore(_playBackward, ref _lastOffsetV, ref targetOffset))
                return false;

            Vector3 newOffset = Vector3.Lerp(_lastOffsetV, targetOffset, GetT());

            _setter(_strVector + newOffset);

            return true;
        }

        private bool LoopCore(bool backward, ref Vector3 lastOffset, ref Vector3 targetOffset)
        {
            // 判断是否到达边界
            if (backward)
            {
                if (_elapsedTime > 0f) return true;
            }
            else
            {
                if (_elapsedTime < _currentInterval) return true;
            }

            lastOffset = targetOffset;

            if (backward)
            {
                float overflow = _elapsedTime;
                if (--_currentIntervalIndex < 0) return false;
                _elapsedTime = _intervals[_currentIntervalIndex] + overflow;
            }
            else
            {
                // 避免多余时间叠加。
                _elapsedTime -= _intervals[_currentIntervalIndex];
                // 这里跳过最一个（_intervals.Length - 1），因为它的持续时间为0s
                if (++_currentIntervalIndex >= _correctedSize) return false;
            }

            _currentInterval = _intervals[_currentIntervalIndex];
            //更新TargetOffSet
            targetOffset = GetVectorTargetOffset();
            return true;
        }

        private bool LoopCore(bool backward, ref Quaternion lastOffset, ref Quaternion targetOffset)
        {
            // 判断是否到达边界
            if (backward)
            {
                if (_elapsedTime > 0f) return true;
            }
            else
            {
                if (_elapsedTime < _currentInterval) return true;
            }

            lastOffset = targetOffset;

            if (backward)
            {
                float overflow = _elapsedTime;
                if (--_currentIntervalIndex < 0) return false;
                _elapsedTime = _intervals[_currentIntervalIndex] + overflow;
            }
            else
            {
                // 避免多余时间叠加。
                _elapsedTime -= _intervals[_currentIntervalIndex];
                // 这里跳过最一个（_intervals.Length - 1），因为它的持续时间为0s
                if (++_currentIntervalIndex >= _correctedSize) return false;
            }

            _currentInterval = _intervals[_currentIntervalIndex];
            //更新TargetOffSet
            targetOffset = GetQuaternionTargetOffSet();
            return true;
        }



        public void GenerateJoltData(Vector3 direction, float duration, int vibrato, float elasticity)
        {
            elasticity = Mathf.Clamp01(elasticity);

            float totalMagnitude = direction.magnitude;          // 总震动幅度
            int vibrationCount = (int)(vibrato * duration);      // 震动点数
            if (vibrationCount < 2) vibrationCount = 2;

            float attenuationPerStep = totalMagnitude / vibrationCount;  // 每段衰减量
            _intervals = new float[vibrationCount + 1];               // 每段时间间隔
            float totalInterval = 0f;

            // 计算每段震动的时间间隔（先长后短）
            // vibrationCount = 震动次数，duration = 总持续时间
            for (int i = 0; i < vibrationCount; i++)
            {
                // 当前震动进度比例（1/N, 2/N, ... N/N）
                float progressRatio = (i + 1) / (float)vibrationCount;

                // 根据比例计算当前震动步长（未归一化）
                float stepDuration = duration * progressRatio;

                // 累加总步长，用于后续归一化
                totalInterval += stepDuration;

                // 暂存当前步长
                _intervals[i] = stepDuration;
            }

            // 归一化，使所有步长加起来正好等于 duration
            float scaleFactor = duration / totalInterval;

            // 应用归一化系数到每个步长
            for (int j = 0; j < vibrationCount; j++)
            {
                _intervals[j] *= scaleFactor;  // intervals 现在是最终每段震动的时间
            }

            _vecOffsets = new Vector3[vibrationCount + 1];   // 每段震动偏移
            _vecOffsets[0] = direction;
            totalMagnitude -= attenuationPerStep;
            _vecOffsets[^1] = Vector3.zero;

            for (int k = 1; k < vibrationCount - 1; k++)
            {
                if (k % 2 != 0)
                {
                    _vecOffsets[k] = -Vector3.ClampMagnitude(direction, totalMagnitude * elasticity);
                }
                else
                {
                    _vecOffsets[k] = Vector3.ClampMagnitude(direction, totalMagnitude);
                }

                totalMagnitude -= attenuationPerStep;
            }

            _correctedSize = vibrationCount;
        }
        #endregion
    }
}