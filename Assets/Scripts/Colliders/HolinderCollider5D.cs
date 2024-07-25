//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HolinderCollider5D : Collider5D {
    public Vector5 center = Vector5.zero;
    public Vector5 normal = (Vector5)Vector3.up;
    public float radiusInner = 1.0f;
    public float radiusOuter = 2.0f;
    public float height = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector5 size = Mathf.Max(radiusOuter, height) * Vector5.one;
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector5 NP(Vector5 localPt) {
        Vector5 d = localPt - center;
        float dn = Vector5.Dot(normal, d);
        Vector5 subD = d - normal * dn;
        float wallDist = subD.magnitude;
        if (wallDist < 1e-4f) { return Vector5.one * 99999.0f;  }
        float r = Mathf.Clamp(wallDist, radiusInner, radiusOuter);
        Vector5 result = subD * (r / wallDist);
        result += normal * Mathf.Clamp(dn, -height, height);
        return result + center;
    }
}
