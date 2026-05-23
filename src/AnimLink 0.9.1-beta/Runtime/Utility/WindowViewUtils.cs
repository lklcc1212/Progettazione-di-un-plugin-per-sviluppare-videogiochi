#if UNITY_EDITOR
namespace AnimLink
{
    using System;

    using UnityEditor;
    using UnityEngine;

    internal static class WindowViewUtils
    {
        public static void RepaintGameView()
        {
            //通过放射获取GameView的Type
            Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            //获取所有GameView的实例
            var gameViews = Resources.FindObjectsOfTypeAll(gameViewType);
            //遍历
            foreach (var gv in gameViews)
            {
                //获取EditorWindow
                if (gv is EditorWindow gameView)
                {
                    //调用Repaint方法
                    gameView.Repaint();
                }
            }
        }

        public static void RepaintInspectorWindow()
        {
            //通过放射获取InspectorWindow的Type
            Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            //获取所有GameView的实例
            var gameViews = Resources.FindObjectsOfTypeAll(gameViewType);
            //遍历
            foreach (var gv in gameViews)
            {
                //获取EditorWindow
                if (gv is EditorWindow gameView)
                {
                    //调用Repaint方法
                    gameView.Repaint();
                }
            }
        }
    }
}
#endif