using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Note: ConicFrustumCollider does not include caps on top or bottom
public class ConicFrustumColliderND : Collider<D> {
    public VECTOR tip = VECTOR.zero;
    public VECTOR h = VECTOR.zero;
    public float nearRatio = 0.5f;
    public float r = 1.0f;

    protected override void Awake() {
        base.Awake();
        VECTOR center = tip + h * (0.5f * nearRatio + 0.5f);
        VECTOR size = VECTOR.one * Mathf.Max(0.5f * (1.0f - nearRatio) * h.magnitude, r);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override VECTOR NP(VECTOR localPt) {
        VECTOR p = localPt - tip;
        float hh = VECTOR.Dot(h, h);
        float ph = VECTOR.Dot(h, p);
        VECTOR a = h * (ph / hh);
        VECTOR q = p - a;
        float qq = VECTOR.Dot(q, q);
        if (qq < 1e-8f) { return VECTOR.one * 99999.0f; }
        VECTOR L = h + q * (r / Mathf.Sqrt(qq));
        float Lp = VECTOR.Dot(L, p);
        float LL = VECTOR.Dot(L, L);
        return tip + L * Mathf.Clamp(Lp / LL, nearRatio, 1.0f);
    }
}
