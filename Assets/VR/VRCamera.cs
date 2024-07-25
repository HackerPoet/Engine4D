using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;

public class VRCamera : MonoBehaviour {
    public Camera uiCamera;

    void Awake() {
        Debug.Assert(uiCamera != null);
        if (UnityEngine.XR.XRSettings.enabled) {
            //Change UI camera settings
            uiCamera.orthographic = false;
            uiCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            uiCamera.gameObject.AddComponent<DisableCameraCull>();
            uiCamera.gameObject.AddComponent<TrackedPoseDriver>();

            //Set VR-specific shader keyword
            Shader.EnableKeyword("VR_RENDER");

            //Change Canvas rendering type
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            foreach (Canvas canvas in canvases) {
                canvas.renderMode = RenderMode.WorldSpace;
                RectTransform rt = canvas.GetComponent<RectTransform>();
                rt.localPosition = new Vector3(0.0f, 1.5f, 4.0f);
                rt.localScale = Vector3.one * 0.006f;
            }
        } else {
            //Reset VR-specific shader keyword
            Shader.DisableKeyword("VR_RENDER");
        }
    }
}
