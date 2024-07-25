//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderGroup4D {
    public List<Collider4D> colliders = new();
    private Vector4 aabbMin = float.MaxValue * Vector4.one;
    private Vector4 aabbMax = float.MinValue * Vector4.one;

    public void Add(Collider4D collider) {
        Debug.Assert(collider.aabbMin.x != float.MaxValue);
        Debug.Assert(collider.aabbMax.x != float.MinValue);
        colliders.Add(collider);
        aabbMin = Vector4.Min(aabbMin, collider.aabbMin);
        aabbMax = Vector4.Max(aabbMax, collider.aabbMax);
    }

    public bool IntersectsAABB(Transform4D localToWorld4D, Transform4D worldToLocal4D, Vector4 worldPt, float radius) {
        Vector4 localPt = worldToLocal4D * worldPt;
        Vector4 boundingNP = localToWorld4D * Vector4.Min(Vector4.Max(localPt, aabbMin), aabbMax);
        return Vector4.Distance(worldPt, boundingNP) <= radius;
    }
}
