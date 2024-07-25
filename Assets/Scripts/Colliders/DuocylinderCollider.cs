using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuocylinderCollider : Collider4D {
    public Vector4 center = Vector4.zero;
    public float radiusXY = 1.0f;
    public float radiusZW = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector4 size = new Vector4(radiusXY, radiusXY, radiusZW, radiusZW);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector4 NP(Vector4 localPt) {
        Vector4 d = localPt - center;
        Vector2 xy = new Vector2(d.x, d.y);
        Vector2 zw = new Vector2(d.z, d.w);
        xy *= radiusXY / Mathf.Max(xy.magnitude, radiusXY);
        zw *= radiusZW / Mathf.Max(zw.magnitude, radiusZW);
        return center + new Vector4(xy.x, xy.y, zw.x, zw.y);
    }
}
