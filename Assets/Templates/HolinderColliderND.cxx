using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HolinderColliderND : Collider<D> {
    public VECTOR center = VECTOR.zero;
    public VECTOR normal = (VECTOR)Vector3.up;
    public float radiusInner = 1.0f;
    public float radiusOuter = 2.0f;
    public float height = 1.0f;

    protected override void Awake() {
        base.Awake();
        VECTOR size = Mathf.Max(radiusOuter, height) * VECTOR.one;
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override VECTOR NP(VECTOR localPt) {
        VECTOR d = localPt - center;
        float dn = VECTOR.Dot(normal, d);
        VECTOR subD = d - normal * dn;
        float wallDist = subD.magnitude;
        if (wallDist < 1e-4f) { return VECTOR.one * 99999.0f;  }
        float r = Mathf.Clamp(wallDist, radiusInner, radiusOuter);
        VECTOR result = subD * (r / wallDist);
        result += normal * Mathf.Clamp(dn, -height, height);
        return result + center;
    }
}
