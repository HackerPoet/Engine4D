//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxCollider5D : Collider5D {
    public Vector5 pos = Vector5.zero;
    public Vector5 size = Vector5.one;
    public Matrix5x5 basis = Matrix5x5.identity;

    protected override void Awake() {
        base.Awake();
        UpdateBoundingPoints();
    }

    public void UpdateBoundingPoints() {
        ResetBoundingBox();
        for (int i = 0; i < (1 << 5); ++i) {
            Vector5 s = size;
            for (int j = 0; j < 5; ++j) {
                if ((i & (1 << j)) != 0) { s[j] = -s[j]; }
            }
            AddBoundingPoint(pos + basis * s);
        }
    }

    public override Vector5 NP(Vector5 localPt) {
        Vector5 p = basis.transpose * (localPt - pos);
        p = Vector5.Max(Vector5.Min(p, size), -size);
        return pos + basis * p;
    }
}
