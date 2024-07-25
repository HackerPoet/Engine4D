//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderGroup5D {
    public List<Collider5D> colliders = new();
    private Vector5 aabbMin = float.MaxValue * Vector5.one;
    private Vector5 aabbMax = float.MinValue * Vector5.one;

    public void Add(Collider5D collider) {
        Debug.Assert(collider.aabbMin.x != float.MaxValue);
        Debug.Assert(collider.aabbMax.x != float.MinValue);
        colliders.Add(collider);
        aabbMin = Vector5.Min(aabbMin, collider.aabbMin);
        aabbMax = Vector5.Max(aabbMax, collider.aabbMax);
    }

    public bool IntersectsAABB(Transform5D localToWorld5D, Transform5D worldToLocal5D, Vector5 worldPt, float radius) {
        Vector5 localPt = worldToLocal5D * worldPt;
        Vector5 boundingNP = localToWorld5D * Vector5.Min(Vector5.Max(localPt, aabbMin), aabbMax);
        return Vector5.Distance(worldPt, boundingNP) <= radius;
    }
}
