using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConinderCollider4D : ConeCollider4D {
    public Vector4 depth = Vector4.zero;

    protected override void Awake() {
        base.Awake();
        Vector4 center = tip + h * 0.5f + depth * 0.5f;
        float m = Mathf.Max(0.5f * h.magnitude, 0.5f * depth.magnitude);
        Vector4 size = Vector4.one * Mathf.Max(m, r);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector4 NP(Vector4 localPt) {
        Vector4 p = localPt - tip;
        float depthRatio = Vector4.Dot(p, depth) / depth.sqrMagnitude;
        return NPFromTip(p - depth * depthRatio) + depth * Mathf.Clamp01(depthRatio) + tip;
    }
}
