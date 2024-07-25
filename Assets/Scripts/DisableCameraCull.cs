using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SpatialTracking;

public class DisableCameraCull : MonoBehaviour {
    public static readonly Matrix4x4 hugeBounds = Matrix4x4.Ortho(-9999.0f, 9999.0f, -9999.0f, 9999.0f, -9999.0f, 9999.0f);
    void Awake() {
        //Remove culling and distance sorting since they aren't valid in 4D
        Camera camera = GetComponent<Camera>();
        camera.cullingMatrix = hugeBounds;
        camera.opaqueSortMode = OpaqueSortMode.NoDistanceSort;
        camera.useOcclusionCulling = false;

        //Also remove the tracked pose driver if this is not VR.
        TrackedPoseDriver tpd = GetComponent<TrackedPoseDriver>();
        if (tpd && !UnityEngine.XR.XRSettings.enabled) {
            Destroy(tpd);
        }
    }
}
