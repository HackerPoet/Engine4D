//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Note: ConicFrustumCollider does not include caps on top or bottom
public class ConicFrustumCollider4D : Collider4D {
    public Vector4 tip = Vector4.zero;
    public Vector4 h = Vector4.zero;
    public float nearRatio = 0.5f;
    public float r = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector4 center = tip + h * (0.5f * nearRatio + 0.5f);
        Vector4 size = Vector4.one * Mathf.Max(0.5f * (1.0f - nearRatio) * h.magnitude, r);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector4 NP(Vector4 localPt) {
        Vector4 p = localPt - tip;
        float hh = Vector4.Dot(h, h);
        float ph = Vector4.Dot(h, p);
        Vector4 a = h * (ph / hh);
        Vector4 q = p - a;
        float qq = Vector4.Dot(q, q);
        if (qq < 1e-8f) { return Vector4.one * 99999.0f; }
        Vector4 L = h + q * (r / Mathf.Sqrt(qq));
        float Lp = Vector4.Dot(L, p);
        float LL = Vector4.Dot(L, L);
        return tip + L * Mathf.Clamp(Lp / LL, nearRatio, 1.0f);
    }
}
