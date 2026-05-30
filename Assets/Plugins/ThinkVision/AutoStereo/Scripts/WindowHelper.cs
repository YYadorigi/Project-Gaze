using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// Window position helper
/// </summary>
public static class WindowHelper
{
    public static Vector4 WindowCached;
    public static Vector4 CheckPositionOnce()
    {
#if (UNITY_EDITOR)
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        EditorWindow GameWindow = EditorWindow.GetWindow(T);

        PropertyInfo f = T.GetProperty("targetInParent", BindingFlags.Instance | BindingFlags.NonPublic);//use clippedTargetInParent before Unity 2017
        if (f != null)
        {
            Rect offsets = (Rect)f.GetValue(GameWindow, null);
            //Debug.Log(" GameWindow " + GameWindow.position);
            WindowCached = new Vector4(GameWindow.position.xMin + offsets.x, GameWindow.position.yMin + offsets.y,
                GameWindow.position.width - offsets.x, GameWindow.position.height - offsets.y / 2);
            //Debug.Log("offsets.y = " + offsets.y + " GameWindow.position.height = " + GameWindow.position.height);
            return WindowCached;
        }
        return Vector4.zero;
#else
        WindowHandler.GetWindowInfo2();        
        WindowCached =  new Vector4(WindowHandler.position.x, WindowHandler.position.y,
            WindowHandler.width, WindowHandler.height);
        //Debug.Log("WindowCached = " + WindowCached);
        return WindowCached;
#endif
    }
    public static Vector2 MappingFullScreenPos(Vector3 unityPos)
    {
        float pointSystemCoordinatex = WindowCached.x + unityPos.x;
        float pointSystemCoordinatey = WindowCached.y + (WindowCached.w - unityPos.y);
        Vector2 pos = new Vector2(pointSystemCoordinatex, pointSystemCoordinatey);
        //Debug.Log("to = " + pos + " from = " + unityPos + " window = " + WindowCached);
        return pos;
    }
}
