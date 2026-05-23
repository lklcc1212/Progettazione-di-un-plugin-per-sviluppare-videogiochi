#if UNITY_EDITOR
namespace AnimLink
{
    using Unity.VisualScripting;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    internal static class SceneUtils
    {

        /// <summary>
        /// 强制标记场景为脏并保存
        /// </summary>
        /// <param name="targetObject">需要标记为脏并保存其所在场景的 Unity 对象</param>
        public static void MarkDirtyAndSaveScene(Object targetObject)
        {
            // 强制保存逻辑
            if (targetObject != null)
            {
                EditorUtility.SetDirty(targetObject);
                Scene scene = targetObject.GameObject().scene;
                if (scene.IsValid() && scene.path.Split(".")[^1] == "unity") // 检查场景是否有效
                {
                    EditorSceneManager.SaveScene(scene); // 保存场景
                }
            }
        }
    }
}
#endif
