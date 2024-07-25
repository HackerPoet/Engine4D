//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereCollider5D : Collider5D {
    public Vector5 center = Vector5.zero;
    public float radius = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector5 size = radius * Vector5.one;
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector5 NP(Vector5 localPt) {
        return center + radius * (localPt - center).normalized;
    }
}
