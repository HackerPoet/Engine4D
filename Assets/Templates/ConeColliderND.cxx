using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConeColliderND : Collider<D> {
    public VECTOR tip = VECTOR.zero;
    public VECTOR h = VECTOR.zero;
    public float r = 1.0f;

    protected override void Awake() {
        base.Awake();
        VECTOR center = tip + h * 0.5f;
        VECTOR size = VECTOR.one * Mathf.Max(0.5f * h.magnitude, r);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override VECTOR NP(VECTOR localPt) {
        return NPFromTip(localPt - tip) + tip;
    }

    protected VECTOR NPFromTip(VECTOR p) {
        float hh = VECTOR.Dot(h, h);
        float ph = VECTOR.Dot(h, p);
        VECTOR a = h * (ph / hh);
        VECTOR q = p - a;
        float qq = VECTOR.Dot(q, q);
        if (ph < hh) {
            //Point is on the pointy side of cone
            if (ph > 0.0f && (qq * hh < VECTOR.Dot(a, a) * r*r)) {
                //Point is inside the cone
                return p;
            } else {
                //Point is outside the cone
                VECTOR b = h + r * q.normalized;
                float bb = VECTOR.Dot(b, b);
                float pb = VECTOR.Dot(p, b);
                return b * Mathf.Max(pb / bb, 0.0f);
            }
        } else {
            //Point is on the flat side of the cone
            if (r*r >= qq) {
                //Point is outside the cylinder
                return h + q;
            } else {
                //Point is inside the cylinder
                return h + q * (r / Mathf.Sqrt(qq));
            }
        }
    }
}
