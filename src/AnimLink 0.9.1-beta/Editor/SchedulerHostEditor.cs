using AnimLink;
using UnityEditor;

[CustomEditor(typeof(SchedulerHost))]
public class SchedulerHostEditor : Editor
{
    private void OnEnable()
    {
        // 注册回调，每帧触发 Inspector 重绘
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        // 注销回调
        EditorApplication.update -= Repaint;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField($"Active Coroutine Count: {CoroutineScheduler.ActiveCoroutineCount}");
        EditorGUILayout.LabelField($"Active Update Count: {FrameScheduler.ActiveUpdateFuncCount}");
        EditorGUILayout.LabelField($"Active Fixed Update Count: {FixedScheduler.ActiveFixedFuncCount}");
    }
}

