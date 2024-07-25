using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderGroupND {
    public List<Collider<D>> colliders = new();
    private VECTOR aabbMin = float.MaxValue * VECTOR.one;
    private VECTOR aabbMax = float.MinValue * VECTOR.one;

    public void Add(Collider<D> collider) {
        Debug.Assert(collider.aabbMin.x != float.MaxValue);
        Debug.Assert(collider.aabbMax.x != float.MinValue);
        colliders.Add(collider);
        aabbMin = VECTOR.Min(aabbMin, collider.aabbMin);
        aabbMax = VECTOR.Max(aabbMax, collider.aabbMax);
    }

    public bool IntersectsAABB(Transform<D> localToWorld<D>, Transform<D> worldToLocal<D>, VECTOR worldPt, float radius) {
        VECTOR localPt = worldToLocal<D> * worldPt;
        VECTOR boundingNP = localToWorld<D> * VECTOR.Min(VECTOR.Max(localPt, aabbMin), aabbMax);
        return VECTOR.Distance(worldPt, boundingNP) <= radius;
    }
}
