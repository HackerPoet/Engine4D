//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiskCollider5D : Collider5D {
    public Vector5 center = Vector5.zero;
    public float radius = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector5 size = (Vector5)(Vector4.one * radius);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector5 NP(Vector5 localPt) {
        Vector4 d = (Vector4)(localPt - center);
        return center + (Vector5)(d * (radius / Mathf.Max(radius, d.magnitude)));
    }
}
