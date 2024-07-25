using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Object<D>))]
public abstract class ColliderND : MonoBehaviour {
    //Class for collision hits
    public struct Hit {
        public static readonly Hit Empty = new Hit() {
            displacement = VECTOR.zero,
            floorNormal = VECTOR.zero,
            collider = null
        };
        public VECTOR displacement;
        public VECTOR floorNormal;
        public Collider<D> collider;
    }

    [System.NonSerialized] public Object<D> obj<D>;
    [System.NonSerialized] public float frictionOverride = -1.0f;
    [System.NonSerialized] public float gravityStick = 0.0f;
    protected bool boundsCheck = false;
    public float restitution = 1.0f;
    public bool isFloor = false;
    public bool extendedRange = false;

    [HideInInspector] public VECTOR aabbMin = VECTOR.one * float.MaxValue;
    [HideInInspector] public VECTOR aabbMax = VECTOR.one * float.MinValue;

    public static Dictionary<int, ColliderGroup<D>> colliders = new();
    public static void UpdateColliders() {
        colliders = MakeColliderGroups(FindObjectsOfType<Collider<D>>());
    }
    public static Dictionary<int, ColliderGroup<D>> MakeColliderGroups(Collider<D>[] allColliders) {
        Dictionary<int, ColliderGroup<D>> groups = new();
        foreach (Collider<D> c in allColliders) {
            int id = c.gameObject.GetInstanceID();
            if (!groups.ContainsKey(id)) {
                groups.Add(id, new ColliderGroup<D>());
            }
            groups[id].Add(c);
        }
        return groups;
    }

    protected virtual void Awake() {
        obj<D> = GetComponent<Object<D>>();
    }

    public abstract VECTOR NP(VECTOR localPt);
    public VECTOR NPBounding(VECTOR localPt) {
        return VECTOR.Max(aabbMin, VECTOR.Min(aabbMax, localPt));
    }

    protected void ResetBoundingBox() {
        aabbMin = VECTOR.one * float.MaxValue;
        aabbMax = VECTOR.one * float.MinValue;
    }
    protected void AddBoundingPoint(VECTOR pt) {
        aabbMin = VECTOR.Min(aabbMin, pt);
        aabbMax = VECTOR.Max(aabbMax, pt);
    }

    public bool Collide(VECTOR worldPt, float radius, ref Hit hit) {
        //Get the <D> world transform for the object
        Transform<D> localToWorld<D> = obj<D>.WorldTransform<D>();
        Transform<D> worldToLocal<D> = localToWorld<D>.inverse;

        //Do actual collision
        return Collide(localToWorld<D>, worldToLocal<D>, worldPt, radius, ref hit);
    }
    public bool Collide(Transform<D> localToWorld<D>, Transform<D> worldToLocal<D>, VECTOR worldPt, float radius, ref Hit hit) {
        //Bounds check optimization
        VECTOR localPt = worldToLocal<D> * worldPt;
        if (boundsCheck) {
            VECTOR boundingNP = localToWorld<D> * NPBounding(localPt);
            if (VECTOR.Distance(worldPt, boundingNP) >= radius) {
                return false;
            }
        }

        //Handle sphere colliders
        VECTOR worldNP = localToWorld<D> * NP(localPt);
        hit.displacement = worldPt - worldNP;
        float dist = hit.displacement.magnitude;
        if (dist < radius && dist > 0.0f) {
            hit.floorNormal = hit.displacement * gravityStick;
            hit.displacement *= (radius - dist) / dist;
            hit.collider = this;
            return true;
        } else {
            return false;
        }
    }
}
