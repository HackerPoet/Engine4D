//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConeCollider5D : Collider5D {
    public Vector5 tip = Vector5.zero;
    public Vector5 h = Vector5.zero;
    public float r = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector5 center = tip + h * 0.5f;
        Vector5 size = Vector5.one * Mathf.Max(0.5f * h.magnitude, r);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector5 NP(Vector5 localPt) {
        return NPFromTip(localPt - tip) + tip;
    }

    protected Vector5 NPFromTip(Vector5 p) {
        float hh = Vector5.Dot(h, h);
        float ph = Vector5.Dot(h, p);
        Vector5 a = h * (ph / hh);
        Vector5 q = p - a;
        float qq = Vector5.Dot(q, q);
        if (ph < hh) {
            //Point is on the pointy side of cone
            if (ph > 0.0f && (qq * hh < Vector5.Dot(a, a) * r*r)) {
                //Point is inside the cone
                return p;
            } else {
                //Point is outside the cone
                Vector5 b = h + r * q.normalized;
                float bb = Vector5.Dot(b, b);
                float pb = Vector5.Dot(p, b);
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
