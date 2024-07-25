using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpherinderColliderND : Collider<D> {
    public VECTOR center = VECTOR.zero;
    public VECTOR height = (VECTOR)Vector3.up;
    public float radius = 1.0f;
    public bool open = false;

    protected override void Awake() {
        base.Awake();
        VECTOR size = Mathf.Max(height.magnitude, radius) * VECTOR.one;
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override VECTOR NP(VECTOR localPt) {
        VECTOR p = localPt - center;
        float dp = VECTOR.Dot(p, height) / height.sqrMagnitude;
        VECTOR h = height * Mathf.Clamp(dp, -1.0f, 1.0f);
        VECTOR r = p - dp * height;
        float rMag = r.magnitude;
        if (open || rMag > radius) {
            if (rMag < 1e-6f) { return VECTOR.one * 99999.0f; }
            r *= radius / rMag;
        }
        return center + r + h;
    }
}
