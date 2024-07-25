//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpherinderCollider5D : Collider5D {
    public Vector5 center = Vector5.zero;
    public Vector5 height = (Vector5)Vector3.up;
    public float radius = 1.0f;
    public bool open = false;

    protected override void Awake() {
        base.Awake();
        Vector5 size = Mathf.Max(height.magnitude, radius) * Vector5.one;
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector5 NP(Vector5 localPt) {
        Vector5 p = localPt - center;
        float dp = Vector5.Dot(p, height) / height.sqrMagnitude;
        Vector5 h = height * Mathf.Clamp(dp, -1.0f, 1.0f);
        Vector5 r = p - dp * height;
        float rMag = r.magnitude;
        if (open || rMag > radius) {
            if (rMag < 1e-6f) { return Vector5.one * 99999.0f; }
            r *= radius / rMag;
        }
        return center + r + h;
    }
}
