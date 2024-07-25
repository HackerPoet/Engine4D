//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxCollider4D : Collider4D {
    public Vector4 pos = Vector4.zero;
    public Vector4 size = Vector4.one;
    public Matrix4x4 basis = Matrix4x4.identity;

    protected override void Awake() {
        base.Awake();
        UpdateBoundingPoints();
    }

    public void UpdateBoundingPoints() {
        ResetBoundingBox();
        for (int i = 0; i < (1 << 4); ++i) {
            Vector4 s = size;
            for (int j = 0; j < 4; ++j) {
                if ((i & (1 << j)) != 0) { s[j] = -s[j]; }
            }
            AddBoundingPoint(pos + basis * s);
        }
    }

    public override Vector4 NP(Vector4 localPt) {
        Vector4 p = basis.transpose * (localPt - pos);
        p = Vector4.Max(Vector4.Min(p, size), -size);
        return pos + basis * p;
    }
}
