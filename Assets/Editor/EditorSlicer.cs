#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorSlicer : EditorWindow {
    public static float sliceW = 0.0f;
    public static float sliceV = 0.0f;
    public static bool is5D = false;
    public static float prevSliceW = 0.0f;
    public static float prevSliceV = 0.0f;
    public static bool prevIs5D = false;

    [MenuItem("4D/Slicer Window...")]
    public static void Init() {
        EditorWindow window = GetWindow(typeof(EditorSlicer));
        window.Show();
    }

    void OnGUI() {
        sliceW = EditorGUILayout.FloatField(EditorVolume.isVolume ? "Y" : "W", sliceW);
        sliceV = EditorGUILayout.FloatField("V", sliceV);
        is5D = EditorGUILayout.Toggle(new GUIContent("Use 5D"), is5D);
    }

    void OnInspectorUpdate() {
        if (sliceW != prevSliceW || sliceV != prevSliceV) {
            Shader.SetGlobalFloat("_EditorSliceW", sliceW);
            Shader.SetGlobalFloat("_EditorSliceV", sliceV);
            SceneView.RepaintAll();
            prevSliceW = sliceW;
            prevSliceV = sliceV;
        }
        if (is5D != prevIs5D) {
            EditorVolume.UpdateShaders();
            SceneView.RepaintAll();
            prevIs5D = is5D;
        }
    }
}
#endif
