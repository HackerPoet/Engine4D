using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ShadowFilter : MonoBehaviour {
    public static readonly int shadowOpacityID = Shader.PropertyToID("_ShadowOpacity");
    public static float OPACITY_MUL = 1.0f;
    public static bool useWireframe = false;

    public Mesh shadowMesh;
    public Mesh wireMesh;

    private MeshFilter mf = null;
    private Mesh origMesh = null;

    [System.NonSerialized] public bool disableMesh = false;
    [System.NonSerialized] public bool disableShadow = false;

    private void Awake() {
        mf = GetComponent<MeshFilter>();
        origMesh = mf.sharedMesh;
    }

    public void SetMesh(Mesh mesh) {
        if (!mf) { Awake(); }
        mf.sharedMesh = mesh;
        origMesh = mesh;
    }

    public static void UpdateGlobalOpacity() {
        Shader.SetGlobalFloat(shadowOpacityID, OPACITY_MUL * (useWireframe ? 10.0f : 1.0f));
    }

    private void OnWillRenderObject() {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying) {
            return;
        }
#endif

        if (Camera.current.CompareTag("ShadowCamera")) {
            mf.sharedMesh = (disableShadow ? null : (useWireframe ? wireMesh : shadowMesh));
        } else {
            mf.sharedMesh = (disableMesh ? null : origMesh);
        }
    }

    //Reset original mesh after all rendering is completed (just in case it needs to be accessed)
    private void OnRenderObject() {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying) {
            return;
        }
#endif
        mf.sharedMesh = origMesh;
    }
}
