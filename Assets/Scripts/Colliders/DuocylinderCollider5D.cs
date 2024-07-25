using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuocylinderCollider5D : Collider5D {
    public Vector5 center = Vector5.zero;
    public float radiusXY = 1.0f;
    public float radiusZW = 1.0f;
    public float height = 1.0f;

    protected override void Awake() {
        base.Awake();
        Vector5 size = new Vector5(radiusXY, radiusXY, radiusZW, radiusZW, height);
        aabbMin = center - size;
        aabbMax = center + size;
    }

    public override Vector5 NP(Vector5 localPt) {
        Vector5 d = localPt - center;
        Vector5 np = (Vector5)NPFromCenter((Vector4)d);
        np.v = Mathf.Clamp(d.v, -height, height);
        return np + center;
    }
    protected Vector4 NPFromCenter(Vector4 d) {
        Vector2 xy = new Vector2(d.x, d.y);
        Vector2 zw = new Vector2(d.z, d.w);
        xy *= radiusXY / Mathf.Max(xy.magnitude, radiusXY);
        zw *= radiusZW / Mathf.Max(zw.magnitude, radiusZW);
        return new Vector4(xy.x, xy.y, zw.x, zw.y);
    }
}
