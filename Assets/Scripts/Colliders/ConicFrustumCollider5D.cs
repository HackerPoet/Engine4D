//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Note: ConicFrustumCollider does not include caps on top or bottom
public class ConicFrustumCollider5D : Collider5D {
    public Vector5 tip = Vector5.zero;
    public Vector5 h = Vector5.zero;
    public float nearRatio = 0.5f;
    public float r = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector5 center = tip + h * (0.5f * nearRatio + 0.5f);
        Vector5 size = Vector5.one * Mathf.Max(0.5f * (1.0f - nearRatio) * h.magnitude, r);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector5 NP(Vector5 localPt) {
        Vector5 p = localPt - tip;
        float hh = Vector5.Dot(h, h);
        float ph = Vector5.Dot(h, p);
        Vector5 a = h * (ph / hh);
        Vector5 q = p - a;
        float qq = Vector5.Dot(q, q);
        if (qq < 1e-8f) { return Vector5.one * 99999.0f; }
        Vector5 L = h + q * (r / Mathf.Sqrt(qq));
        float Lp = Vector5.Dot(L, p);
        float LL = Vector5.Dot(L, L);
        return tip + L * Mathf.Clamp(Lp / LL, nearRatio, 1.0f);
    }
}
