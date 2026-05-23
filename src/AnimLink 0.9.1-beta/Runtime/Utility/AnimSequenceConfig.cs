namespace AnimLink
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    using UnityEngine;
    using UnityEngine.Events;

    using static AnimLinkExtension;
    using static UtilityExtension;

    using Object = UnityEngine.Object;

    /// <summary>
    /// A visual panel class for setting up AnimationSequence in Unity.
    /// </summary>
    [Serializable]
    public sealed class AnimSequenceConfig
    {
        public AnimSequenceConfig(AnimStepConfig[] configs)
        {
            if (StepConfigs != null)
                StepConfigs = configs;
        }

        public AnimSequenceConfig() { }

        internal enum StepType
        {
            None,
            Do,
            Shake,
            AdvancedShake,
            Delay,
            Function
        }

        [Serializable]
        public sealed class AnimStepConfig
        {
            /// <summary>
            /// _index: -2 = 单独; -1 = 被删除; >=0 在DS_Tool,Array或List里
            /// </summary>
            [SerializeField]
            private int _index = -2;
            /// <summary>
            /// Index: -2 = 单独; -1 = 被删除; >=0 在AnimationCommanGroup,Array或List里
            /// </summary>
            internal int Index => _index;

            #region ID
            [SerializeField]
            private string _showPathTempID = string.Empty;
            [SerializeField]
            private string _animationPreviewTempID = string.Empty;
            internal string AnimationPreviewTempID => _animationPreviewTempID;
            private static readonly List<string> IDs = new();
            private string GetNewShowPathID()
            {
                return GetID(ref _showPathTempID);
            }
            internal string GetNewAnimationPreviewTempID()
            {
                return GetID(ref _animationPreviewTempID);
            }
            private string GetID(ref string target)
            {
                IDs.Remove(target);
                while (true)
                {
                    string newID = Guid.NewGuid().ToString("N");
                    if (!IDs.Contains(newID))
                    {
                        IDs.Add(newID);
                        target = newID;
                        return newID;
                    }
                }
            }
            #endregion

            [Tooltip("Directly execute the next.")]
            [SerializeField] internal bool ExecuteTheNext;
            [SerializeField] internal StepType Type = StepType.None;
            [SerializeField] internal Space Space = Space.World;
            [SerializeField]
            private bool _foldout;
            [SerializeField] internal bool UseFixedDeltaTime;
            [SerializeField] internal Transform Transform;
            [Tooltip("-1 equals infinity")]
            [SerializeField] internal int Loops = 1;
            [SerializeField] internal float Duration;

            //Animation Preview

            /// <summary>
            /// 当切换类型时，请求取消预览动画。
            /// </summary>
            [SerializeField] internal bool CancelPreviewAnimation;
            internal CancellationTokenSource PreviewEditorTokenSource;
            [SerializeField] private bool _resetAfterPreview = true;
            internal bool ResetAfterPreview => _resetAfterPreview;

            //AnimationType

            [SerializeField] internal BaseLoop LoopType1 = BaseLoop.FromStart;
            [SerializeField] internal PathLoop LoopType2 = PathLoop.FromStart;
            [SerializeField] internal ValueLoop LoopType3 = ValueLoop.FromStart;

            #region  Do

            internal enum DOType
            {
                None,
                DoPath,
                DoPosition,
                DoScale,
                DoRotation,
                DoAlpha,
                DoColor,
                DoJolt

            }
            [SerializeField] internal DOType DoType = DOType.None;

            //DoPath
            [SerializeField] internal Vector3[] Vectors;
            [SerializeField] internal Transform[] Transforms;
            [SerializeField] internal PathType PathType = PathType.LinearPath;
            [Tooltip("True = Linear | False = Catmull Rom")]
            [SerializeField] internal bool Linear;
            [SerializeField] internal AnimationCurve AnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);
            [SerializeField] internal bool TransformsToVectors;
            [SerializeField] internal bool FollowPathRotation;
            [Tooltip("Whether to ignore direction reversals when decrementing easedT.")]
            [SerializeField] internal bool IgnoreDecreasingEasedT;
            [Tooltip("2D valid inputs: Vector3.right/left,  Vector3.up/down and Vector3.forward/back \n3D valid inputs: Vector3.up/down")]
            [SerializeField] internal Vector3 FacingAxis = Vector3.up;
            [Tooltip("Use 2D for XY-plane rotation around Z, or 3D for full 3D rotation.")]
            [SerializeField] internal Dimension RotationDimension = Dimension._2D;
            [Tooltip("Tip: enable show path.")]
            [SerializeField] internal bool EnableCatmullRomCorrection;
            [Tooltip("Number of samples per segment (the more samples, the more stable the speed and the smoother the path display.)")]
            [SerializeField] internal int SamplesPerSegment = 10;
#if UNITY_EDITOR
            [SerializeField]
            private bool _showPath;
            [SerializeField]
            private Color _lineColor = Color.black;
            internal CancellationTokenSource ShowPathCancellationTokenSource;
            internal async void StartShowingPathAsync(CancellationToken token, MonoBehaviour mono, string path)
            {
                string initialID = GetNewShowPathID();

                // 注册回调
                EditorApplication.playModeStateChanged += handleModeChange;
                SceneView.duringSceneGui += onSceneGUIDelegate;
                BuildProcessor.BuildAction += onBuildTriggered;
                SceneView.RepaintAll();

                // 如果index = -2，说明是单独存在的setting，不是Array或List里的
                if (_index != -2)
                    path = path.ReplaceLastArrayDataOccurrence();

                // 判断当前对象是否有效
                bool IsValid()
                {
                    if (_index == -1)
                        return false;

                    object obj = GetObjectInstanceByReflection(path, mono);

                    if (obj == null)
                        return false;

                    // 如果是Array或List
                    if (obj is IEnumerable<AnimStepConfig> settings)
                        return settings.Any(item => ReferenceEquals(this, item));

                    // 单独存在的setting
                    return true;
                }

                // 判断是否可以显示路径
                bool canShow() => _showPath && IsValid() && DoType == DOType.DoPath &&
                    Type == StepType.Do && !token.IsCancellationRequested &&
                    !EditorApplication.isCompiling && mono;

                while (canShow())
                {
                    await Task.Delay(250);
                }

                // 退出显示路径(注销回调)
                ShowPathCancellationTokenSource?.Cancel();
                handleExitEditMode();

                #region Methods
                void onSceneGUIDelegate(SceneView sceneView)
                {
                    //仅Array或List: 当两个setting被互换时，id会发生改变。
                    if (initialID == _showPathTempID)
                        ShowPath();
                    else
                        handleExitEditMode();
                }

                void handleModeChange(PlayModeStateChange state)
                {
                    if (state == PlayModeStateChange.ExitingEditMode)
                    {
                        handleExitEditMode();
                    }
                }

                void onBuildTriggered()
                {
                    handleExitEditMode();
                }

                void handleExitEditMode()
                {
                    SceneView.duringSceneGui -= onSceneGUIDelegate;
                    SceneView.RepaintAll();

                    EditorApplication.playModeStateChanged -= handleModeChange;
                    BuildProcessor.BuildAction -= onBuildTriggered;

                    ShowPathCancellationTokenSource = null;
                }
                #endregion
            }

            #region Show path
            private void ShowPath()
            {
                bool isLoop = LoopType2 == PathLoop.Loop;
                bool isIncrement = LoopType2 == PathLoop.Increment;

                if (isIncrement && !Transform)
                {
                    LogWarning("[DoPath] Please add Transform to display the path.");
                    return;
                }

                List<Vector3> vectors = GetPathVectors(isIncrement);
                if (vectors == null || vectors.Count < 2) return;

                bool catmullEndCorrectionActive = PathType == PathType.CatmullRom && EnableCatmullRomCorrection && !isIncrement;

                int pathLength =
                    isLoop || catmullEndCorrectionActive
                    ? vectors.Count : vectors.Count - 1;

                switch (PathType)
                {
                    case PathType.LinearPath:
                        DrawLinearPath(vectors, pathLength, _lineColor);
                        break;
                    case PathType.CatmullRom:
                        DrawCatmullRomPath(vectors, pathLength, isLoop, catmullEndCorrectionActive, _lineColor);
                        break;
                    case PathType.CubicBezier:
                        DrawCubicBezierPath(vectors, pathLength, _lineColor);
                        break;
                }
            }


            private List<Vector3> GetPathVectors(bool isIncrement)
            {
                List<Vector3> vectors = new();

                if (TransformsToVectors)
                {
                    if (Transforms.Length > 1)
                    {
                        Vector3 curPosition = isIncrement ? Transform.position : Vector3.zero;

                        foreach (var t in Transforms)
                        {
                            if (t == null) return null;
                            Vector3 newPos = isIncrement ? t.position + curPosition : t.position;
                            vectors.Add(newPos);
                            curPosition = newPos;
                        }
                    }
                }
                else
                {
                    if (Vectors.Length > 1)
                    {
                        vectors = new List<Vector3>(Vectors);
                        if (isIncrement)
                        {
                            Vector3 curPosition = Transform.position;
                            for (int i = 0; i < vectors.Count; i++)
                            {
                                vectors[i] += curPosition;
                                curPosition = vectors[i];
                            }
                        }
                    }
                }

                return vectors.Count > 1 ? vectors : null;
            }

            private void DrawLinearPath(List<Vector3> vectors, int pathLength, Color color)
            {
                for (int i = 0; i < pathLength; i++)
                {
                    int nextIndex = (i + 1) % vectors.Count;
                    Debug.DrawLine(vectors[i], vectors[nextIndex], color);
                }
            }

            private void DrawCatmullRomPath(List<Vector3> vectors, int pointCount, bool isLoop, bool catmullEndCorrectionActive, Color color)
            {
                Vector3 prevPoint = vectors[0];
                int lastControlPointIndex = pointCount - 1;

                bool useWrappedIndices = catmullEndCorrectionActive || isLoop;
                float step = 1f / SamplesPerSegment;

                int ClampIndex(int index) => useWrappedIndices
                    ? (index + pointCount) % pointCount
                    : Mathf.Clamp(index, 0, pointCount);

                for (int i = 0; i < pointCount; i++)
                {
                    // 非循环路径且启用端点修正时，不计算最后一段
                    if (catmullEndCorrectionActive && !isLoop && i == lastControlPointIndex)
                        break;

                    int prevIndex = ClampIndex(i - 1);
                    int nextIndex = ClampIndex(i + 1);
                    int next2Index = ClampIndex(i + 2);

                    Vector3 p0 = vectors[prevIndex];
                    Vector3 p1 = vectors[i];
                    Vector3 p2 = vectors[nextIndex];
                    Vector3 p3 = vectors[next2Index];

                    for (int s = 1; s <= SamplesPerSegment; s++)
                    {
                        float t = s * step;
                        Vector3 newPoint = CatmullRom(p0, p1, p2, p3, t);

                        Debug.DrawLine(prevPoint, newPoint, color);
                        prevPoint = newPoint;
                    }
                }
            }

            private void DrawCubicBezierPath(List<Vector3> vectors, int pathLength, Color color)
            {
                if ((vectors.Count - 4) % 3 != 0)
                    return;
                Vector3 prevPoint = vectors[0];

                for (int i = 0; i < pathLength; i += 3)
                {

                    for (int s = 1; s <= SamplesPerSegment; s++)
                    {
                        float t = (float)s / SamplesPerSegment;
                        Vector3 newPoint = CubicBezier(
                            vectors[i], vectors[i + 1], vectors[i + 2], vectors[i + 3], t
                        );

                        Debug.DrawLine(prevPoint, newPoint, color);
                        prevPoint = newPoint;
                    }
                }
            }
            #endregion

            ~AnimStepConfig()
            {
                IDs.Remove(_showPathTempID);
                IDs.Remove(_animationPreviewTempID);
            }
#endif

            //DoPosition // DoScale // DoRotation
            [SerializeField] internal float Value;
            [SerializeField] internal bool IsModifyingAxis;
            [SerializeField] internal Axis Axis = Axis.X;
            [SerializeField] internal RotateMode RotateMode = RotateMode.Fast;
            [SerializeField] internal Vector3 Angle;
            [SerializeField] internal Ease Ease = Ease.Linear;
            [SerializeField] internal Vector3 Position;
            [SerializeField] internal Vector3 Scale;
            [SerializeField] internal bool TrToVec3;
            [SerializeField] internal Transform TargetTransform;

            //DoAlpha
            [SerializeField] internal float Alpha;
            [SerializeField] internal AlphaCompType AlphaCompType = AlphaCompType.Sprite;
            [SerializeField] internal Object Component;
            [SerializeField] internal int MaterialIndex;

            //DoColor
            [SerializeField] internal Color Color;
            [SerializeField] internal bool WithAlpha;
            [SerializeField] internal ColorCompType ColorCompType = ColorCompType.Sprite;

            //DoJolt
            [SerializeField] internal Vector3 Direction;
            [SerializeField] internal int Vibratio = 10;
            [SerializeField] internal float Elasticity = 0.5f;
            [SerializeField] internal PosScaleRot JoltType;
            #endregion

            #region Shake

            [SerializeField] internal float Magnitude = 0.1f;
            [SerializeField] internal Rigidbody2D Rigidbody2D;
            [SerializeField] internal PosScale ShakeType = PosScale.Position;
            [SerializeField] internal PosScaleRot AdvancedShakeType = PosScaleRot.Position;
            [SerializeField] internal Axis ActiveAxes = Axis.X;
            #endregion

            #region AdvancedShake

            [SerializeField] internal AnimationCurve MagnitudeCurve = AnimationCurve.Linear(0, 1, 1, 1);
            [SerializeField] internal AnimationCurve ReturnCurve = AnimationCurve.Linear(0, 0, 1, 1);
            [SerializeField] internal float Frequency = 10;
            [SerializeField] internal float ReturnDuration;

            #endregion

            #region Delay

            [SerializeField] internal float DelayTime;
            [SerializeField] internal bool RealTime;
            #endregion

            #region Function

            [SerializeField] internal UnityEvent Function;

            //Function Plus
            [SerializeField] internal bool UseFunctionPlus;
            [SerializeField] internal FunctionPlus FunctionPlus;
            [HelpBox("The system will wait until the method execution is complete.")]
            [SerializeField] internal bool WaitingForMethodExecutionComplete;
            #endregion
        }

        /// <summary>
        /// The array of animation step configurations that define this sequence.
        /// Each configuration specifies the parameters for a specific animation, delay, or function step.
        /// </summary>
        public AnimStepConfig[] StepConfigs;

        [NonSerialized]
        private int _activeSubAnimCount;
        internal int ActiveSubAnimCount
        {
            get => _activeSubAnimCount;
            set { _activeSubAnimCount = Math.Max(0, value); }
        }
        internal CancellationTokenSource MainAnimPreviewTokenSource;
        [SerializeField] private bool _resetAfterMainPreview = true;
        internal bool ResetAfterMainPreview => _resetAfterMainPreview;
        [SerializeField]
        private bool _foldout1;
        [SerializeField]
        private bool _foldout2;
        [SerializeField]
        private int _switchIndex1;
        [SerializeField]
        private int _switchIndex2;
        [SerializeField]
        private int _removeIndex;
        [SerializeField]
        private int _addIndex;
        [SerializeField]
        private int _needRemoveIndex;
    }
}