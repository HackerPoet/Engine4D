#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;

public class GeneralEditor : ShaderGUI {
    public readonly string[] allKeywords = new string[] {
        "VERTEX_AO",
        "USE_DITHER",
        "DOUBLE_SIDED_N",
        "LOCAL_UV",
    };

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
        // If we are not visible, return.
        base.OnGUI(materialEditor, properties);
        if (!materialEditor.isVisible)
            return;

        // Get the current compile flags
        Material targetMat = materialEditor.target as Material;
        string[] keyWords = targetMat.shaderKeywords;
        LocalKeywordSpace keywordSpace = targetMat.shader.keywordSpace;

        // If toggle has changed, add keywords to multi-compile list
        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < allKeywords.Length; i++) {
            //Don't display a toggle if the keyword is not a shader feature
            if (!keywordSpace.FindKeyword(allKeywords[i]).isValid) { continue; }

            //Show and update a toggle for the shader keyword
            bool hasWord = (Array.IndexOf(keyWords, allKeywords[i]) >= 0);
            hasWord = EditorGUILayout.Toggle(allKeywords[i], hasWord);
            if (hasWord) {
                targetMat.EnableKeyword(allKeywords[i]);
            } else {
                targetMat.DisableKeyword(allKeywords[i]);
            }
        }

        // Update the material with the new values
        if (EditorGUI.EndChangeCheck()) {
            EditorUtility.SetDirty(targetMat);
        }
    }
}
#endif
