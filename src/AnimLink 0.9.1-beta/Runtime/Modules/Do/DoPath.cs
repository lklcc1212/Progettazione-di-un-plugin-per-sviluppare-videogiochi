namespace AnimLink
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    using static AnimLinkExtension;
    using static UtilityExtension;

    public sealed class DoPath : AnimationTweenBase<DoPath, Vector3[]>
    {
        #region Fields
        internal static readonly int MIN_SAMPLE_COUNT = 1;
        internal static readonly int MAX_SAMPLE_COUNT = 100;

        //

        private int _correctedPathSize;
        private int _pathSize;
        /// <summary>
        /// 原始路径
        /// </summary>
        private Vector3[] _rawPath;

        /// <summary>
        /// 当前路径
        /// </summary>
        private Vector3[] _currentPath;

        /// <summary>
        /// 正路径
        /// </summary>
        private Vector3[] _forwardPath;

        /// <summary>
        /// 反路径
        /// </summary>
        private Vector3[] _backwardPath;

        private PathLoop _loopType = PathLoop.FromStart;
        private PathType _pathType = PathType.LinearPath;

        private bool _isIncrement;
        private bool _isLoop;
        private bool _isPingPong;

        // Reset
        private Vector3 _originalPos;
        private Quaternion _originalRot;

        /// <summary>
        /// 跟随路径方向
        /// </summary>
        private bool _followPathRotation;
        /// <summary>
        /// 主朝向轴
        /// </summary>
        private Vector3 _facingAxis = Vector3.up;
        /// <summary>
        /// 是否在 easedT 递减时忽略方向反转。
        /// </summary>
        private bool _ignoreDecreasingEasedT;
        private float _prevEasedT;
        private Dimension _rotDimension = Dimension._2D;

        /// <summary>
        /// _originalRot是否储存了一次值。
        /// </summary>
        private bool _initializedOrigRot;

        /// <summary>
        /// 修正CatmullRom路径（首尾相连/首尾不相连）
        /// </summary>
        private bool _enableCatmullRomEndCorrection;

        private bool _pathDirty;
        private bool _segmentDirty;

        private int _segmentCount;
        private bool _is2D;

        //getter 
        private Func<int, float, Vector3> _getNewPosition;

        #region 修正速度
        // 整个路径的总长
        private float _overallLength = float.NaN;
        public float OverallLength
        {
            get
            {
                ProcessPathIfNeeded();

                return _overallLength;
            }
        }

        /// <summary>
        /// 用于存储每个段的查找表数据
        /// </summary>
        private class SegmentLookup
        {
            /// <summary>
            /// 当前曲线段的总弧长
            /// </summary>
            public float TotalLength;
            /// <summary>
            /// 对应采样点的 t 值（范围 0~1）
            /// </summary>
            public List<float> TSamples = new();
            /// <summary>
            /// 累计弧长数据，每个采样点对应的弧长    
            /// </summary>
            public List<float> ArcLengths = new();
        }

        // 段的查找表数据的列表(当前，正和反)
        private List<SegmentLookup> _currentSegmentLookups = new();
        private readonly List<SegmentLookup> _forwardSegmentLookups = new();
        private readonly List<SegmentLookup> _backwardSegmentLookups = new();

        // 存储每个段累计后的弧长，方便确定目标距离落在哪个段(当前，正和反)
        private List<float> _currentSegmentCumulativeLengths = new();
        private readonly List<float> _forwardSegmentCumulativeLengths = new();
        private readonly List<float> _backwardSegmentCumulativeLengths = new();

        /// <summary>
        /// 每段采样数（采样越多，精度(速度)越高）
        /// </summary>
        private int _samplesPerSegment = 10;
        #endregion
        #endregion

        #region Constructors

        public DoPath()
        {
            _getNewPosition = LinearPosition;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Configures a <see cref="DoPath"/> animation for a target <see cref="Transform"/> along a specified path.
        /// </summary>
        /// <param name="transform">The target transform to animate.</param>
        /// <param name="path">An array of <see cref="Vector3"/> points defining the path.</param>
        /// <param name="duration">The total duration of the path animation, in seconds.</param>
        public DoPath ConfigDoPath(Transform transform, Vector3[] path, float duration)
        {
            if (CheckPlayingAndResetID() || ValidateTargetAndAssign(transform, "transform")) return this;

            if (path == null)
            {
                LogWarning("[DoPath] path parameter is null.");
                return this;
            }

            Transform = transform;
            _rawPath = (Vector3[])path.Clone(); // 克隆一份原始路径
            _pathSize = path.Length;
            _correctedPathSize = _pathSize - 1;
            Duration = duration;
            MarkAllDirty();

            return this;
        }

        /// <summary>
        /// Sets the loop configuration for the <see cref="DoPath"/> animation.
        /// </summary>
        /// <param name="loops">
        /// The number of times to repeat the animation. 
        /// A value ≤ -1 indicates an infinite loop.
        /// </param>
        /// <param name="loopType">
        /// The type of loop to use for the animation, e.g., <see cref="PathLoop.PingPong"/> or <see cref="PathLoop.Increment"/>.
        /// </param>
        public DoPath SetLoops(int loops, PathLoop loopType)
        {
            if (!SetLoopsInternal(loops)) return this;

            if (loopType.HasMultipleFlags())
                LogWarning($"[DoPath] The loopType parameter has multiple flags, so the previous value will be kept({_loopType}).");
            else if (loopType != _loopType)
            {
                _loopType = loopType;
                RefreshLoopState();
                MarkSegmentDirty();
            }
            return this;
        }

        /// <summary>
        /// Sets the type of path interpolation to use for the path animation.
        /// </summary>
        /// <param name="pathType">
        /// The type of path to use, e.g., <see cref="PathType.Linear"/>, <see cref="PathType.CatmullRom"/>, or <see cref="PathType.CubicBezier"/>.
        /// </param>
        public DoPath SetPathType(PathType pathType)
        {
            if (CheckPlayingAndResetID()) return this;

            if (pathType.HasMultipleFlags())
            {
                LogWarning($"[DoPath] The loopType parameter has multiple flags, so the previous value ( {_loopType} ) will be kept.");
                return this;
            }

            _pathType = pathType;
            SelectPositionFunction();
            MarkSegmentDirty();
            return this;
        }

        /// <summary>
        /// Sets the number of sample points per segment for the path animation.
        /// </summary>
        /// <param name="samplesPerSegment">
        /// The number of samples per segment. Higher values produce smoother interpolation and more accurate speed control,
        /// but increase computational cost.
        /// <para>Valid range: 1 to 100 (default: 10).</para>
        /// </param>
        public DoPath SetSamplesPerSegment(int samplesPerSegment = 10)
        {
            if (CheckPlayingAndResetID()) return this;

            _samplesPerSegment = Math.Clamp(samplesPerSegment, MIN_SAMPLE_COUNT, MAX_SAMPLE_COUNT);
            MarkSegmentDirty();
            return this;
        }


        /// <summary>
        /// Sets whether the start and end points of a Catmull-Rom path affect each other in non-looping modes.
        /// <br>- For non-looping modes (FromStart or PingPong), the start and end points will influence each other if enabled.</br>
        /// <br>- For looping (Loop) mode, this setting has no effect; the path is always closed.</br>
        /// <br>- For increment (Increment) mode, this setting has no effect.</br>
        /// </summary>
        /// <param name="enablePathCorrection">
        /// If true, the path will be corrected so that the first and last points influence each other.
        /// </param>
        public DoPath SetCREndCorrection(bool enablePathCorrection)
        {
            if (CheckPlayingAndResetID()) return this;

            _enableCatmullRomEndCorrection = enablePathCorrection;
            MarkSegmentDirty();
            return this;
        }

        /// <summary>
        /// Enables or disables rotation along the path, and optionally specifies the main facing axis of the model.
        /// </summary>
        /// <param name="enable">Whether to enable rotation along the path.</param>
        /// <param name="facingAxis">
        /// Optional. The model's main facing axis.  
        /// Common values: <see cref="Vector3.up"/>, <see cref="Vector3.down"/>.
        /// </param>
        /// <param name="ignoreDecreasingEasedT">
        /// If true, the rotation will not flip when the easedT value decreases.
        /// </param>
        public DoPath SetPathRotation(bool enable, Vector3? facingAxis = null, bool ignoreDecreasingEasedT = false)
        {
            if (CheckPlaying())
                return this;

            _followPathRotation = enable;
            _ignoreDecreasingEasedT = ignoreDecreasingEasedT;

            if (facingAxis.HasValue)
            {
                if (_is2D = _rotDimension is Dimension._2D)
                {
                    if (AngleOffset.ContainsKey(facingAxis.Value))
                        _facingAxis = facingAxis.Value;
                    else
                        LogWarning($"[DoPath] The specified mainAxis {facingAxis.Value} is not in the list of available axes, so the original value {_facingAxis} is retained.");
                }
                else
                {
                    if (Valid3DAxis.Contains(facingAxis.Value))
                        _facingAxis = facingAxis.Value;
                    else
                        LogWarning($"[DoPath] The specified mainAxis {facingAxis.Value} is not a valid 3D axis (up/down), so the original value {_facingAxis} is retained.");
                }
            }

            return this;
        }

        /// <summary>
        /// Sets the rotation dimension (2D or 3D) for this path.
        /// In 2D mode, only the Z axis is affected (rotation occurs in the XY plane).
        /// In 3D mode, rotation is applied in full 3D space (XYZ axes).
        /// </summary>
        /// <param name="dimension">
        /// The target rotation dimension. Use 2D for XY-plane rotation around Z, 
        /// or 3D for full 3D rotation.
        /// </param>
        public DoPath SetRotDimension(Dimension dimension)
        {
            if (CheckPlaying())
                return this;

            if (dimension.HasMultipleFlags())
            {
                LogWarning($"[DoPath] The dimension parameter has multiple flags, so the previous value ( {_rotDimension} ) will be kept.");
                return this;
            }

            if (dimension is Dimension._3D && !Valid3DAxis.Contains(_facingAxis))
            {
                LogWarning($"[DoPath] The current mainAxis {_facingAxis} is not a valid 3D axis (up/down), so it will be reset to Vector3.up.");
                _facingAxis = Vector3.up;
            }

            _rotDimension = dimension;
            return this;
        }

        public override void Reset()
        {
            if (Transform && _initialized)
            {
                Transform.position = _originalPos;
                if (_followPathRotation && _initializedOrigRot)
                    Transform.rotation = _originalRot;
            }
        }

        public override DoPath SetGoal(Vector3[] goal)
        {
            if (CheckPlayingAndResetID()) return this;

            if (goal == null)
            {
                LogWarning("[DoPath] The goal parameter is null.");
                return this;
            }

            _rawPath = (Vector3[])goal.Clone(); // 克隆一份原始路径
            _pathSize = goal.Length;
            _correctedPathSize = _pathSize - 1;
            MarkAllDirty();
            return this;
        }
        #endregion

        #region Private API

        protected override bool VerifySetup()
        {
            if (!TargetObject)
            {
                LogWarning("[DoPath] TargetObject is null.");
                return false;
            }

            if (Duration <= 0)
            {
                LogWarning("[DoPath] Duration is less than or equal to 0.");
                return false;
            }

            int pathLength = _rawPath?.Length ?? 0;

            if (pathLength == 0)
            {
                LogWarning("[DoPath] Path is empty or null.");
                return false;
            }

            if (_pathType == PathType.CubicBezier)
            {
                if (_isLoop || (pathLength - 4) % 3 != 0 || pathLength < 4)
                {
                    LogWarning("[DoPath] For Cubic Bezier, the number of control points must be 4 + 3n! Cubic Bezier doesn't support loops.");
                    return false;
                }
            }
            else if (pathLength < 2)
            {
                LogWarning("[DoPath] For Linear and Catmull-Rom, the number of control points must be at least 2.");
                return false;
            }
            return true;
        }

        private void ProcessPathIfNeeded()
        {
            if (_pathDirty)
                BuildPaths();
            if (_segmentDirty)
                BuildSegments();
        }

        private void BuildPaths()
        {
            if (_isIncrement)
            {
                if (!Transform)
                {
                    return;
                }

                UpdateTargetPathIncrement();
                _forwardPath = _currentPath;
            }
            else
            {
                _forwardPath = _currentPath = (Vector3[])_rawPath.Clone();
            }

            if (_isPingPong)
            {
                _backwardPath = (Vector3[])_forwardPath.Clone();
                Array.Reverse(_backwardPath);
            }

            _pathDirty = false;
        }

        private void BuildSegments()
        {
            if (_forwardPath == null)
            {
                return;
            }

            bool isLinear = _pathType == PathType.LinearPath;
            bool isCubicBezier = _pathType == PathType.CubicBezier;

            GeneratePathSegments(_forwardPath, _forwardSegmentLookups, _forwardSegmentCumulativeLengths, isLinear, isCubicBezier);

            if (_isPingPong)
            {
                GeneratePathSegments(_backwardPath, _backwardSegmentLookups, _backwardSegmentCumulativeLengths, isLinear, isCubicBezier);
            }

            _currentSegmentLookups = _forwardSegmentLookups;
            _currentSegmentCumulativeLengths = _forwardSegmentCumulativeLengths;
            _segmentCount = _currentSegmentCumulativeLengths.Count - 1;

            _segmentDirty = false;
        }

        private void GeneratePathSegments(Vector3[] path, List<SegmentLookup> lookups, List<float> cumulativeLengths, bool isLinear, bool isCubicBezier)
        {
            if (isLinear)
                CalculateLinearPath(path, lookups, cumulativeLengths);
            else
                GenerateSegmentLookups(path, lookups, cumulativeLengths, isCubicBezier);
        }


        private void InitializeTransformForPath()
        {
            _originalPos = Transform.position;
            if (_followPathRotation)
            {
                _initializedOrigRot = true;
                _originalRot = Transform.rotation;
            }
        }

        /// <summary>
        /// 当路径没有变化时，将正向路径直接赋值给当前路径，或者在增量模式下进行更新。
        /// <para>
        /// 举例：如果当前 <see cref="_loopType"/> 是 <see cref="PathLoop.PingPong"/>，
        /// 这样可以避免之前 <see cref="_isPingPong"/> 的状态影响到 <see cref="_currentPath"/>。
        /// </para>
        /// </summary>
        private void AssignCurrentPathIfNeeded()
        {
            if (_isIncrement)
            {
                // 增量模式下，路径点可能会随 Transform 位置变化，所以需要更新 _currentPath
                // _segmentLookups 和 _segmentCumulativeLengths 不需要更新，结构与之前一致
                UpdateTargetPathIncrement();
            }
            else if (_isPingPong)
            {
                // 非增量模式且为 PingPong，直接使用正向路径数据
                _currentPath = _forwardPath;
                _currentSegmentLookups = _forwardSegmentLookups;
                _currentSegmentCumulativeLengths = _forwardSegmentCumulativeLengths;
            }
        }

        private void SelectPositionFunction()
        {
            _getNewPosition = _pathType switch
            {
                PathType.LinearPath => LinearPosition,
                PathType.CatmullRom => CatmullRomPosition,
                PathType.CubicBezier => CubicBezierPosition,
                _ => _getNewPosition
            };
        }

        protected override void PrepareAnimation()
        {
            bool nothingChanged = !_pathDirty && !_segmentDirty;

            // 路径有变化时，重新初始化路径
            ProcessPathIfNeeded();

            // 初始化 Transform（仅在动画刚开始时执行一次）
            if (_totalElapsedTime <= 0)
            {
                _prevEasedT = 0f;
                InitializeTransformForPath();

                //// 如果没有任何改变且动画刚开始，则更新当前路径
                //// 放在这里的原因：如果 _totalElapsedTime > 0，说明动画已经在进行中（可能是 Resume() 或 PlayBackward()），
                //// 这时就不需要再次赋值当前路径，避免覆盖现有状态。
                if (nothingChanged)
                    AssignCurrentPathIfNeeded();
            }

            _duration = _isPingPong ? Duration * 0.5f : Duration;
        }

        protected override bool UpdateAnimation()
        {
            if (!LoopCore(ReversePath, UpdateTargetPathIncrement))
                return false;

            UpdateElapsedTime();
            float t = Mathf.Clamp01(_elapsedTime / _duration);
            float easedT = EasedT(t, _ease);

            float targetDistance = easedT * _overallLength;
            int segmentIndex = GetSegmentStartDistanceIndex(targetDistance);
            float segmentStart = segmentIndex == 0 ? 0f : _currentSegmentCumulativeLengths[segmentIndex - 1];
            float distanceInSegment = targetDistance - segmentStart;

            Vector3 currentPos = Transform.position;
            Vector3 targetPos = _getNewPosition(segmentIndex, distanceInSegment);
            Transform.position = targetPos;

            if (_followPathRotation)
                UpdateRotationAlongPath(currentPos, targetPos, easedT);

            return true;
        }

        private void UpdateRotationAlongPath(Vector3 currentPos, Vector3 targetPos, float easedT)
        {
            Vector3 diff = targetPos - currentPos;

            // 判断是否有移动
            if (diff.sqrMagnitude < 1e-6f)
                return;

            if (_ignoreDecreasingEasedT && _prevEasedT > easedT)
                diff *= -1;

            _prevEasedT = easedT;

            if (GetRotationFromDirection(diff, out Quaternion newRot))
                Transform.rotation = newRot;
        }

        bool LoopCore(Action reverse, Action increment)
        {
            if (_playBackward ? _elapsedTime > 0f : _elapsedTime < _duration) return true;

            if (!UpdateLoopState(_loopType != PathLoop.PingPong))
                return false;

            HandleLoopReset(reverse, increment);

            return true;
        }

        void HandleLoopReset(Action reverse, Action increment)
        {
            _prevEasedT = 0;
            // 多余时间增加到当前loop
            _elapsedTime = _playBackward ? _duration + _elapsedTime : _elapsedTime - _duration;


            switch (_loopType)
            {
                case PathLoop.PingPong:
                    reverse();
                    break;
                case PathLoop.Increment:
                    increment();
                    break;
            }
        }

        void RefreshLoopState()
        {
            _isLoop = _loopType == PathLoop.Loop;
            _isIncrement = _loopType == PathLoop.Increment;
            _isPingPong = _loopType == PathLoop.PingPong;
        }

        // 对应 2D 的默认前向轴偏移角（相对于 X 轴）
        private static readonly Dictionary<Vector3, float> AngleOffset = new() {
            { Vector3.right, 0f }, // X 轴 → 0°
            { Vector3.up, 90f }, // Y 轴 → 90°
            { Vector3.left, -180f },   // -X 轴 → -180°
            { Vector3.down, -90f },   // -Y 轴 → -90°
            { Vector3.forward, 0f }, // X 轴 → 0°
            { Vector3.back, 180f } // X 轴 → 180°
        };

        private static readonly HashSet<Vector3> Valid3DAxis = new()
        {
            Vector3.up,
            Vector3.down,
        };

        bool GetRotationFromDirection(Vector3 direction, out Quaternion quaternion)
        {
            quaternion = Quaternion.identity;

            if (direction.sqrMagnitude <= 0f)
                return false;

            if (_is2D)
            {
                // 2D 模式：绕 Z 轴旋转
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                angle -= AngleOffset.GetValueOrDefault(_facingAxis, 0f);

                quaternion = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                // 3D 模式：根据指定前向轴生成旋转
                quaternion = Quaternion.LookRotation(direction.normalized, _facingAxis);
            }

            return true;
        }

        private Vector3 LinearPosition(int segmentIndex, float distanceInSegment)
        {
            float segmentLength = _currentSegmentLookups[segmentIndex].TotalLength;
            float t = distanceInSegment / segmentLength;
            int nextIndex = (segmentIndex + 1) % _pathSize;
            return Vector3.LerpUnclamped(_currentPath[segmentIndex], _currentPath[nextIndex], t);
        }

        private Vector3 CatmullRomPosition(int segmentIndex, float distanceInSegment)
        {
            float t = GetTForDistance(distanceInSegment, _currentSegmentLookups[segmentIndex]);
            bool wrap = _isLoop || _enableCatmullRomEndCorrection;

            int prev = wrap ? (segmentIndex - 1 + _pathSize) % _pathSize : Mathf.Max(segmentIndex - 1, 0);
            int next = wrap ? (segmentIndex + 1) % _pathSize : Mathf.Min(segmentIndex + 1, _correctedPathSize);
            int nextNext = wrap ? (segmentIndex + 2) % _pathSize : Mathf.Min(segmentIndex + 2, _correctedPathSize);

            return CatmullRom(
                _currentPath[prev],
                _currentPath[segmentIndex],
                _currentPath[next],
                _currentPath[nextNext],
                t
            );
        }

        private Vector3 CubicBezierPosition(int segmentIndex, float distanceInSegment)
        {
            float t = GetTForDistance(distanceInSegment, _currentSegmentLookups[segmentIndex]);
            segmentIndex *= 3;
            return CubicBezier(
                _currentPath[segmentIndex],
                _currentPath[segmentIndex + 1],
                _currentPath[segmentIndex + 2],
                _currentPath[segmentIndex + 3],
                t
            );
        }

        #region For Linear

        /// <summary>
        /// 计算整个路径的总长,每个段的总长和每一段的累计弧长（累加长度）并填充到列表中。
        /// </summary>
        private void CalculateLinearPath(Vector3[] path, List<SegmentLookup> segmentLookups, List<float> cumulativeLengths)
        {
            segmentLookups.Clear();
            cumulativeLengths.Clear();
            _overallLength = 0f;

            int segmentCount = _isLoop ? _pathSize : _correctedPathSize;

            for (int i = 0; i < segmentCount; i++)
            {
                int nextIndex = (i + 1) % _pathSize; // 循环时保证索引有效
                float segmentLength = Vector3.Distance(path[i], path[nextIndex]);
                segmentLookups.Add(new() { TotalLength = segmentLength });
                _overallLength += segmentLength;
                cumulativeLengths.Add(_overallLength);
            }
        }

        #endregion
        #region For Catmull-Rom & CubicBezier

        /// <summary>
        /// 生成段的查找表列表
        /// </summary>
        void GenerateSegmentLookups(Vector3[] path, List<SegmentLookup> segmentLookups, List<float> segmentCumulativeLengths, bool isCubicBezier)
        {
            segmentLookups.Clear();
            segmentCumulativeLengths.Clear();

            _overallLength = 0f;

            if (isCubicBezier)
            {
                int segmentCount = _isLoop ? _pathSize : _correctedPathSize;
                for (int i = 0; i < segmentCount; i += 3)
                {
                    SegmentLookup lookup = GenerateSegmentLookup(
                        path[i],
                        path[i + 1],
                        path[i + 2],
                        path[i + 3],
                        _samplesPerSegment,
                        isCubicBezier
                    );
                    segmentLookups.Add(lookup);
                    _overallLength += lookup.TotalLength;
                    segmentCumulativeLengths.Add(_overallLength);
                }
            }
            else
            {
                bool catmullEndCorrectionActive = !_isIncrement && _enableCatmullRomEndCorrection;
                bool useWrappedIndices = catmullEndCorrectionActive || _isLoop;
                int segmentCount = useWrappedIndices ? _pathSize : _correctedPathSize;
                int lastControlPointIndex = segmentCount - 1;

                int ClampIndex(int index) => useWrappedIndices
                    ? (index + segmentCount) % segmentCount
                    : Mathf.Clamp(index, 0, segmentCount);

                for (int i = 0; i < segmentCount; i++)
                {
                    // 非循环路径且启用端点修正时，不计算最后一段
                    if (catmullEndCorrectionActive && !_isLoop && i == lastControlPointIndex)
                        break;

                    int prevIndex = ClampIndex(i - 1);
                    int nextIndex = ClampIndex(i + 1);
                    int nextNextIndex = ClampIndex(i + 2);

                    SegmentLookup lookup = GenerateSegmentLookup(
                        path[prevIndex],
                        path[i],
                        path[nextIndex],
                        path[nextNextIndex],
                        _samplesPerSegment,
                        isCubicBezier
                    );
                    segmentLookups.Add(lookup);
                    _overallLength += lookup.TotalLength;
                    segmentCumulativeLengths.Add(_overallLength);
                }
            }
        }

        /// <summary>
        /// 生成由 4 个控制点构成的曲线段的弧长查找表列表
        /// </summary>
        static SegmentLookup GenerateSegmentLookup(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int samples, bool isCubicBezier)
        {
            SegmentLookup lookup = new();

            float totalLength = 0f;
            // t=0 的初始采样
            lookup.TSamples.Add(0f);
            lookup.ArcLengths.Add(0f);
            Vector3 previousPoint;

            if (isCubicBezier)
            {
                previousPoint = p0;
            }
            else
            {
                previousPoint = p1;
            }

            // 均匀采样 t，从 >0 到 1=<
            Vector3 currentPoint;
            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                if (isCubicBezier)
                    currentPoint = CubicBezier(p0, p1, p2, p3, t);
                else
                    currentPoint = CatmullRom(p0, p1, p2, p3, t);
                float segLength = Vector3.Distance(previousPoint, currentPoint);
                totalLength += segLength;
                lookup.TSamples.Add(t);
                lookup.ArcLengths.Add(totalLength);
                previousPoint = currentPoint;
            }
            lookup.TotalLength = totalLength;
            return lookup;
        }

        /// <summary>
        /// 根据段内的行进距离，通过查找表反求对应的 t 参数（线性插值）
        /// </summary>
        float GetTForDistance(float distance, SegmentLookup lookup)
        {
            if (distance <= 0f)
                return 0;
            if (distance >= lookup.TotalLength)
                return 1;

            int index = 0, high = lookup.ArcLengths.Count - 1;
            while (index < high)
            {
                int mid = (index + high) / 2;
                if (lookup.ArcLengths[mid] < distance)
                    index = mid + 1;
                else
                    high = mid;
            }
            // index 是第一个大于等于 distance 的位置

            float t1 = lookup.TSamples[index - 1];
            float t2 = lookup.TSamples[index];
            float d1 = lookup.ArcLengths[index - 1];
            float d2 = lookup.ArcLengths[index];

            float factor = (distance - d1) / (d2 - d1); // 计算的是 distance 在这个区间内的相对比例。
            /*
             例子：
                假设 d1 = 2, d2 = 5, distance = 3
                那么 factor = (3 - 2) / (5 - 2) = 1 / 3 = 0.333... 也就是 33.3%
                这表示 distance 在 d1 和 d2 之间的距离占整个区间的 33.3%。
             */

            return Mathf.Lerp(t1, t2, factor); // 线性插值
        }

        #endregion
        #region Other

        private void MarkAllDirty()
        {
            _pathDirty = true;
            _segmentDirty = true;
            _overallLength = float.NaN;
        }

        private void MarkSegmentDirty()
        {
            _segmentDirty = true;
            _overallLength = float.NaN;
        }

        /// <summary>
        /// 颠倒路径
        /// </summary>
        private void ReversePath()
        {
            if (_isReversed)
            {
                _currentPath = _forwardPath;
                _currentSegmentLookups = _forwardSegmentLookups;
                _currentSegmentCumulativeLengths = _forwardSegmentCumulativeLengths;
            }
            else
            {
                _currentPath = _backwardPath;
                _currentSegmentLookups = _backwardSegmentLookups;
                _currentSegmentCumulativeLengths = _backwardSegmentCumulativeLengths;
            }
            _isReversed = !_isReversed;
        }

        /// <summary>
        /// Binary Search(Lower Bound): 确定目标距离落在哪个曲线段上
        /// </summary>
        int GetSegmentStartDistanceIndex(float targetDistance)
        {
            int index = 0, high = _segmentCount;
            while (index < high)
            {
                int mid = (index + high) >> 1;
                float distance = _currentSegmentCumulativeLengths[mid];
                if (targetDistance <= distance || AlmostEqual(targetDistance, distance))
                    high = mid;
                else
                    index = mid + 1;
            }
            return index;
        }

        private void UpdateTargetPathIncrement()
        {
            int len = _rawPath.Length;
            if (_currentPath == null || _currentPath.Length != len)
                _currentPath = new Vector3[len];

            if (_playBackward)
            {
                int last = len - 1;
                // 从当前位置开始，反向构建路径
                _currentPath[last] = Transform.position - _rawPath[0];
                for (int i = last; i > 0; i--)
                    _currentPath[i - 1] = _currentPath[i] - _rawPath[i];
            }
            else
            {
                // 从当前位置开始，正向构建路径
                _currentPath[0] = Transform.position + _rawPath[0];

                for (int i = 1; i < len; i++)
                    _currentPath[i] = _currentPath[i - 1] + _rawPath[i];
            }
        }

        static bool AlmostEqual(float a, float b, float eps = 1e-5f)
        {
            return Math.Abs(a - b) <= eps;
        }
        #endregion
        #endregion
    }
}