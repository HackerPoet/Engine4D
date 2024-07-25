#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

//Ensure class initializer is called whenever scripts recompile
[InitializeOnLoadAttribute]
public static class EditorVolume {
    public static bool isVolume = false;

    //Register an event handler when the class is initialized
    static EditorVolume() {
        EditorApplication.playModeStateChanged -= onPlayModeState;
        EditorApplication.playModeStateChanged += onPlayModeState;
        System.Reflection.FieldInfo info = typeof(EditorApplication).GetField("globalEventHandler", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (info != null) {
            EditorApplication.CallbackFunction value = (EditorApplication.CallbackFunction)info.GetValue(null);
            value -= onKeyPressed;
            value += onKeyPressed;
            info.SetValue(null, value);
        }
    }

    private static void onPlayModeState(PlayModeStateChange state) {
        //Activate script AFTER play-mode exits
        if (state == PlayModeStateChange.EnteredEditMode) {
            UpdateShaders();
        }
    }

    private static void onKeyPressed() {
        if (Event.current.rawType == EventType.KeyDown && Event.current.keyCode == KeyCode.F10) {
            isVolume = !isVolume;
            UpdateShaders();
        }
    }

    public static void UpdateShaders() {
        SceneView sv = SceneView.lastActiveSceneView;
        Shader.DisableKeyword(isVolume ? "IS_EDITOR" : "IS_EDITOR_V");
        Shader.EnableKeyword(isVolume? "IS_EDITOR_V" : "IS_EDITOR");
        Shader.DisableKeyword("FOG");
        Shader.SetGlobalFloat("_DitherDist", 0.0f);
        Shader.SetGlobalFloat("_DitherRadius", 0.0f);
        if (EditorSlicer.is5D) { Shader.EnableKeyword("IS_5D"); } else { Shader.DisableKeyword("IS_5D"); }
        Texture2D lutTexture = Resources.Load<Texture2D>(EditorSlicer.is5D ? "LUT5D" : "LUT4D");
        Shader.SetGlobalTexture("_LUT", lutTexture);
        Debug.Assert(lutTexture != null);
        Texture3D noiseTexture = Resources.Load<Texture3D>("Noise");
        Shader.SetGlobalTexture("_NOISE", noiseTexture);
        Debug.Assert(noiseTexture != null);
        if (sv) {
            sv.camera.cullingMatrix = DisableCameraCull.hugeBounds;
        }
        if (EditorWindow.HasOpenInstances<EditorSlicer>()) {
            EditorWindow.GetWindow<EditorSlicer>()?.Repaint();
        }
    }
}
#endif
