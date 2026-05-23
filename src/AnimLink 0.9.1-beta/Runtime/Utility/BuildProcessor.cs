#if UNITY_EDITOR
namespace AnimLink
{
    using System;

    using UnityEditor.Build.Reporting;
    using UnityEditor.Build;

    sealed internal class BuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public static Action BuildAction;
        /// <summary>
        /// 此方法在构建过程开始之前调用。
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            BuildAction?.Invoke();
        }
    }
}
#endif