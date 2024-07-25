using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FindAllRefs : MonoBehaviour {
    [MenuItem("Assets/Find All References")]
    private static void OpenAssetByDefaultProgram() {
        //Get the asset being referenced
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string[] searches = new string[] { "t:GameObject", "t:Scene" };

        //Look for references
        foreach (string search in searches) {
            string firstRef = CheckForReferences(assetPath, search);
            if (firstRef.Length > 0) {
                Debug.Log("Reference of " + Selection.activeObject.name + " found in: " + firstRef);
                return;
            }
        }

        //No references found
        Debug.Log(Selection.activeObject.name + " is unused.");
    }

    private static string CheckForReferences(string assetPath, string search) {
        //Get all scenes in the project
        string[] guids = AssetDatabase.FindAssets(search);
        string[] allPaths = new string[1];
        foreach (string guid in guids) {
            //Get path name for object
            allPaths[0] = AssetDatabase.GUIDToAssetPath(guid);

            //Don't let objects reference themselves
            if (allPaths[0] == assetPath) { continue; }

            //Check if any of the scenes reference the target asset
            string[] dependencies = AssetDatabase.GetDependencies(allPaths, true);
            HashSet<string> dependencySet = new HashSet<string>();
            foreach (string str in dependencies) {
                if (str == assetPath) {
                    return allPaths[0];
                }
            }
        }
        return "";
    }
}