using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereColliderND : Collider<D> {
    public VECTOR center = VECTOR.zero;
    public float radius = 1.0f;

    protected override void Awake() {
        base.Awake();
        VECTOR size = radius * VECTOR.one;
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override VECTOR NP(VECTOR localPt) {
        return center + radius * (localPt - center).normalized;
    }
}
