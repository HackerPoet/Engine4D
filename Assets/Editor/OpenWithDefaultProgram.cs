using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class OpenWithDefaultProgram : MonoBehaviour {
    [MenuItem("Assets/Open with default program")]
    private static void OpenAssetByDefaultProgram() {
        var selected = Selection.activeObject;
        Application.OpenURL("File:" + AssetDatabase.GetAssetPath(selected));
    }
}