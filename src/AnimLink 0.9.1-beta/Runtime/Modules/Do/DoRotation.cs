namespace AnimLink
{
    using UnityEngine;

    using static AnimLinkExtension;
    using static UtilityExtension;

    public sealed class DoRotation : DoBaseLoop<DoRotation, Vector3>
    {
        #region Fields

        private RotateMode _rotateMode = RotateMode.Fast;
        private bool _applyInLocalSpace;

        private Vector3 _startEuler;
        private Vector3 _effectiveTargetEuler;
        private Vector3 _rawTargetEuler;
        private Vector3 _changeEuler;

        //Reset
        private Vector3 _originalEuler;

        #endregion

        #region Public API

        /// <summary>
        /// Configures a <see cref="DoRotation"/> animation for the specified <see cref="Transform"/> using Euler angles.
        /// </summary>
        /// <param name="transform">The target transform to animate.</param>
        /// <param name="euler">The target rotation in Euler angles (degrees).</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        public DoRotation ConfigDoRotation(Transform transform, Vector3 euler, float duration)
        {
            if (CheckPlayingAndResetID() || ValidateTargetAndAssign(transform, "transfrom")) return this;

            Transform = transform;
            Duration = duration;
            _rawTargetEuler = euler;
            return this;
        }

        /// <summary>
        /// Sets the coordinate space for the position animation (World or Self). 
        /// </summary>
        /// <param name="space">The coordinate space to use: <see cref="Space.World"/> or <see cref="Space.Self"/>.</param>
        public DoRotation SetSpace(Space space)
        {
            if (CheckPlayingAndResetID()) return this;

            if (space.HasMultipleFlags())
            {
                LogWarning($"[DoRotation] The space parameter has multiple flags, so the previous value ({(_applyInLocalSpace ? Space.Self : Space.World)}) will be kept.");
                return this;
            }

            bool isLocal = space is Space.Self;

            // 切换类型时，使得Reset()不能使用。
            if (_applyInLocalSpace != isLocal)
                _initialized = false;


            _applyInLocalSpace = isLocal;
            return this;
        }

        /// <summary>
        /// Sets the rotation mode for this animation.
        /// <para>RotateMode.Fast: standard rotation.</para>
        /// <para>RotateMode.FastBeyond360: allows rotation beyond 360°.</para>
        /// </summary>
        /// <param name="rotateMode">The desired rotation mode.</param>
        public DoRotation SetRotateMode(RotateMode rotateMode)
        {
            if (CheckPlayingAndResetID()) return this;

            if (rotateMode.HasMultipleFlags())
            {
                LogWarning($"[DoRotation] The rotateMode parameter has multiple flags, so the previous value ({_rotateMode}) will be kept.");
                return this;
            }

            _rotateMode = rotateMode;
            return this;
        }

        public override void Reset()
        {
            if (Transform && _initialized)
            {
                if (_applyInLocalSpace) Transform.localEulerAngles = _originalEuler;
                else Transform.eulerAngles = _originalEuler;
            }
        }

        public override DoRotation SetGoal(Vector3 goal)
        {
            if (CheckPlayingAndResetID()) return this;

            _rawTargetEuler = goal;
            return this;
        }

        #endregion

        #region Private API

        /// <summary>
        /// 将欧拉角归一化到 [-359,359] 范围内，避免角度累加超过一整圈。
        /// </summary>
        /// <param name="euler">原始欧拉角（度数）。</param>
        /// <returns>归一化后的欧拉角，每个分量均在 [-359,359] 之间。</returns>
        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            euler.x %= 360;
            euler.y %= 360;
            euler.z %= 360;
            return euler;
        }

        /// <summary>
        /// 计算从 current 到 target 的最短旋转角度差。
        /// 使用 Mathf.Repeat 将差值映射到 (-180,180) 区间，确保选择最小旋转方向。
        /// </summary>
        /// <param name="current">当前角度（度数）。</param>
        /// <param name="target">目标角度（度数）。</param>
        /// <returns>两者之间的最小角度差，可能为正（顺时针）或负（逆时针）。</returns>
        private static float DeltaAngle(float current, float target)
        {
            //(-180,180]
            float delta = Mathf.Repeat((target - current + 180f), 360f) - 180f;

            //(-180,180)
            if (delta == -180f && target - current > 0f)
            {
                return 180f;
            }

            return delta;
        }

        protected override void PrepareAnimation()
        {
            // 设置动画总时长
            _duration = _loopType == BaseLoop.PingPong ? Duration * 0.5f : Duration;

            if (_totalElapsedTime > 0)
                return;

            // 保存原始欧拉角
            _originalEuler = _applyInLocalSpace ? Transform.localEulerAngles : Transform.eulerAngles;

            // 初始化起始欧拉角
            _startEuler = _originalEuler;

            bool isIncrement = _loopType == BaseLoop.Increment;

            // 根据旋转模式处理欧拉角
            switch (_rotateMode)
            {
                case RotateMode.Fast:
                    PrepareFastRotation(isIncrement);
                    break;
                case RotateMode.FastBeyond360:
                    PrepareFastBeyond360Rotation(isIncrement);
                    break;
            }
        }

        private void PrepareFastRotation(bool isIncrement)
        {
            if (isIncrement)
            {
                _changeEuler = NormalizeEuler(_rawTargetEuler);
            }
            else
            {
                _changeEuler.x = DeltaAngle(_startEuler.x, _rawTargetEuler.x);
                _changeEuler.y = DeltaAngle(_startEuler.y, _rawTargetEuler.y);
                _changeEuler.z = DeltaAngle(_startEuler.z, _rawTargetEuler.z);
            }

            _effectiveTargetEuler = _startEuler + _changeEuler;
        }

        private void PrepareFastBeyond360Rotation(bool isIncrement)
        {
            if (isIncrement)
            {
                _changeEuler = _rawTargetEuler;
                _effectiveTargetEuler = _startEuler + _changeEuler;
            }
            else
            {
                _changeEuler = _rawTargetEuler - _startEuler;
            }
        }

        protected override bool UpdateAnimation()
        {
            if (!LoopCore(ReverseEuler, UpdateEurelIncrement))
                return false;

            UpdateElapsedTime();
            float t = Mathf.Clamp01(_elapsedTime / _duration);
            float easedT = EasedT(t, _ease);

            if (_rotateMode == RotateMode.Fast)
            {
                Quaternion currentRotation = Quaternion.Euler(Vector3.LerpUnclamped(_startEuler, _effectiveTargetEuler, easedT));
                SetRotation(currentRotation);
            }
            else // FastBeyond360(旋转超过360度)
            {
                Vector3 currentEuler = _startEuler + _changeEuler * easedT;
                SetEurel(currentEuler);
            }

            return true;
        }

        void ReverseEuler()
        {
            (_startEuler, _effectiveTargetEuler) = (_effectiveTargetEuler, _startEuler);
            _changeEuler = -_changeEuler;
            _isReversed = !_isReversed;
        }

        void UpdateEurelIncrement()
        {
            if (_playBackward)
            {
                _effectiveTargetEuler = _startEuler;
                _startEuler -= _changeEuler;
            }
            else
            {
                _startEuler = _effectiveTargetEuler;
                _effectiveTargetEuler += _changeEuler;
            }
        }

        private void SetEurel(Vector3 newEurel)
        {
            if (_applyInLocalSpace) Transform.localEulerAngles = newEurel;
            else Transform.eulerAngles = newEurel;
        }

        private void SetRotation(Quaternion newRotation)
        {
            if (_applyInLocalSpace) Transform.localRotation = newRotation;
            else Transform.rotation = newRotation;
        }
        #endregion
    }
}

