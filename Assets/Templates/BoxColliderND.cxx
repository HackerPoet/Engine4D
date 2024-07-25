using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxColliderND : Collider<D> {
    public VECTOR pos = VECTOR.zero;
    public VECTOR size = VECTOR.one;
    public MATRIX basis = MATRIX.identity;

    protected override void Awake() {
        base.Awake();
        UpdateBoundingPoints();
    }

    public void UpdateBoundingPoints() {
        ResetBoundingBox();
        for (int i = 0; i < (1 << DIMS); ++i) {
            VECTOR s = size;
            for (int j = 0; j < DIMS; ++j) {
                if ((i & (1 << j)) != 0) { s[j] = -s[j]; }
            }
            AddBoundingPoint(pos + basis * s);
        }
    }

    public override VECTOR NP(VECTOR localPt) {
        VECTOR p = basis.transpose * (localPt - pos);
        p = VECTOR.Max(VECTOR.Min(p, size), -size);
        return pos + basis * p;
    }
}
