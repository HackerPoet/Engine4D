using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CreateNDMenu {
    [MenuItem("GameObject/Create Object 4D", false, 0)]
    public static void CreateObject4D(MenuCommand menuCommand) {
        CreateObjectND(menuCommand, false);
    }

    [MenuItem("GameObject/Create Object 5D", false, 0)]
    public static void CreateObject5D(MenuCommand menuCommand) {
        CreateObjectND(menuCommand, true);
    }

    private static void CreateObjectND(MenuCommand menuCommand, bool is5D) {
        // Create a custom game object
        GameObject obj = new GameObject("Object" + (is5D ? "5D" : "4D"));
        GameObject menuContext = menuCommand.context as GameObject;
        GameObject prefabRoot = PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot;
        if (menuContext) {
            GameObjectUtility.SetParentAndAlign(obj, menuContext);
        } else if (prefabRoot) {
            GameObjectUtility.SetParentAndAlign(obj, prefabRoot);
        }

        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();
        obj.AddComponent<ShadowFilter>();
        if (is5D) {
            obj.AddComponent<Object5D>();
        } else {
            obj.AddComponent<Object4D>();
        }
        Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);
        Selection.activeObject = obj;
    }
}
