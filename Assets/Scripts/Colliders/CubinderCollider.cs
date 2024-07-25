using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubinderCollider : Collider4D {
    public Vector4 center = Vector4.zero;
    public float radius = 1.0f;
    public float height1 = 1.0f;
    public float height2 = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector4 size = new Vector4(radius, radius, height1, height2);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector4 NP(Vector4 localPt) {
        Vector4 d = localPt - center;
        float m = radius / Mathf.Max(Mathf.Sqrt(d.x * d.x + d.y * d.y), radius);
        float z = Mathf.Clamp(d.z, -height1, height1);
        float w = Mathf.Clamp(d.w, -height2, height2);
        Vector4 np = center + new Vector4(d.x * m, d.y * m, z, w);
        return np;
    }
}
