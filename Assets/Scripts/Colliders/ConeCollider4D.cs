//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConeCollider4D : Collider4D {
    public Vector4 tip = Vector4.zero;
    public Vector4 h = Vector4.zero;
    public float r = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector4 center = tip + h * 0.5f;
        Vector4 size = Vector4.one * Mathf.Max(0.5f * h.magnitude, r);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector4 NP(Vector4 localPt) {
        return NPFromTip(localPt - tip) + tip;
    }

    protected Vector4 NPFromTip(Vector4 p) {
        float hh = Vector4.Dot(h, h);
        float ph = Vector4.Dot(h, p);
        Vector4 a = h * (ph / hh);
        Vector4 q = p - a;
        float qq = Vector4.Dot(q, q);
        if (ph < hh) {
            //Point is on the pointy side of cone
            if (ph > 0.0f && (qq * hh < Vector4.Dot(a, a) * r*r)) {
                //Point is inside the cone
                return p;
            } else {
                //Point is outside the cone
                Vector4 b = h + r * q.normalized;
                float bb = Vector4.Dot(b, b);
                float pb = Vector4.Dot(p, b);
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
