using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiskColliderND : Collider<D> {
    public VECTOR center = VECTOR.zero;
    public float radius = 1.0f;

    protected override void Awake() {
        base.Awake();
        VECTOR size = (VECTOR)(SUBVECTOR.one * radius);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override VECTOR NP(VECTOR localPt) {
        SUBVECTOR d = (SUBVECTOR)(localPt - center);
        return center + (VECTOR)(d * (radius / Mathf.Max(radius, d.magnitude)));
    }
}
