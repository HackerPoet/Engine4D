//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpherinderCollider4D : Collider4D {
    public Vector4 center = Vector4.zero;
    public Vector4 height = (Vector4)Vector3.up;
    public float radius = 1.0f;
    public bool open = false;

    protected override void Awake() {
        base.Awake();
        Vector4 size = Mathf.Max(height.magnitude, radius) * Vector4.one;
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector4 NP(Vector4 localPt) {
        Vector4 p = localPt - center;
        float dp = Vector4.Dot(p, height) / height.sqrMagnitude;
        Vector4 h = height * Mathf.Clamp(dp, -1.0f, 1.0f);
        Vector4 r = p - dp * height;
        float rMag = r.magnitude;
        if (open || rMag > radius) {
            if (rMag < 1e-6f) { return Vector4.one * 99999.0f; }
            r *= radius / rMag;
        }
        return center + r + h;
    }
}
