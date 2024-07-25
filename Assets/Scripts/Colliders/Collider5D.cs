//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Object5D))]
public abstract class Collider5D : MonoBehaviour {
    //Class for collision hits
    public struct Hit {
        public static readonly Hit Empty = new Hit() {
            displacement = Vector5.zero,
            floorNormal = Vector5.zero,
            collider = null
        };
        public Vector5 displacement;
        public Vector5 floorNormal;
        public Collider5D collider;
    }

    [System.NonSerialized] public Object5D obj5D;
    [System.NonSerialized] public float frictionOverride = -1.0f;
    [System.NonSerialized] public float gravityStick = 0.0f;
    protected bool boundsCheck = false;
    public float restitution = 1.0f;
    public bool isFloor = false;
    public bool extendedRange = false;

    [HideInInspector] public Vector5 aabbMin = Vector5.one * float.MaxValue;
    [HideInInspector] public Vector5 aabbMax = Vector5.one * float.MinValue;

    public static Dictionary<int, ColliderGroup5D> colliders = new();
    public static void UpdateColliders() {
        colliders = MakeColliderGroups(FindObjectsOfType<Collider5D>());
    }
    public static Dictionary<int, ColliderGroup5D> MakeColliderGroups(Collider5D[] allColliders) {
        Dictionary<int, ColliderGroup5D> groups = new();
        foreach (Collider5D c in allColliders) {
            int id = c.gameObject.GetInstanceID();
            if (!groups.ContainsKey(id)) {
                groups.Add(id, new ColliderGroup5D());
            }
            groups[id].Add(c);
        }
        return groups;
    }

    protected virtual void Awake() {
        obj5D = GetComponent<Object5D>();
    }

    public abstract Vector5 NP(Vector5 localPt);
    public Vector5 NPBounding(Vector5 localPt) {
        return Vector5.Max(aabbMin, Vector5.Min(aabbMax, localPt));
    }

    protected void ResetBoundingBox() {
        aabbMin = Vector5.one * float.MaxValue;
        aabbMax = Vector5.one * float.MinValue;
    }
    protected void AddBoundingPoint(Vector5 pt) {
        aabbMin = Vector5.Min(aabbMin, pt);
        aabbMax = Vector5.Max(aabbMax, pt);
    }

    public bool Collide(Vector5 worldPt, float radius, ref Hit hit) {
        //Get the 5D world transform for the object
        Transform5D localToWorld5D = obj5D.WorldTransform5D();
        Transform5D worldToLocal5D = localToWorld5D.inverse;

        //Do actual collision
        return Collide(localToWorld5D, worldToLocal5D, worldPt, radius, ref hit);
    }
    public bool Collide(Transform5D localToWorld5D, Transform5D worldToLocal5D, Vector5 worldPt, float radius, ref Hit hit) {
        //Bounds check optimization
        Vector5 localPt = worldToLocal5D * worldPt;
        if (boundsCheck) {
            Vector5 boundingNP = localToWorld5D * NPBounding(localPt);
            if (Vector5.Distance(worldPt, boundingNP) >= radius) {
                return false;
            }
        }

        //Handle sphere colliders
        Vector5 worldNP = localToWorld5D * NP(localPt);
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
