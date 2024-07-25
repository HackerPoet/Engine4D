//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HolinderCollider4D : Collider4D {
    public Vector4 center = Vector4.zero;
    public Vector4 normal = (Vector4)Vector3.up;
    public float radiusInner = 1.0f;
    public float radiusOuter = 2.0f;
    public float height = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector4 size = Mathf.Max(radiusOuter, height) * Vector4.one;
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector4 NP(Vector4 localPt) {
        Vector4 d = localPt - center;
        float dn = Vector4.Dot(normal, d);
        Vector4 subD = d - normal * dn;
        float wallDist = subD.magnitude;
        if (wallDist < 1e-4f) { return Vector4.one * 99999.0f;  }
        float r = Mathf.Clamp(wallDist, radiusInner, radiusOuter);
        Vector4 result = subD * (r / wallDist);
        result += normal * Mathf.Clamp(dn, -height, height);
        return result + center;
    }
}
