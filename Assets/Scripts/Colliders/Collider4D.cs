//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Object4D))]
public abstract class Collider4D : MonoBehaviour {
    //Class for collision hits
    public struct Hit {
        public static readonly Hit Empty = new Hit() {
            displacement = Vector4.zero,
            floorNormal = Vector4.zero,
            collider = null
        };
        public Vector4 displacement;
        public Vector4 floorNormal;
        public Collider4D collider;
    }

    [System.NonSerialized] public Object4D obj4D;
    [System.NonSerialized] public float frictionOverride = -1.0f;
    [System.NonSerialized] public float gravityStick = 0.0f;
    protected bool boundsCheck = false;
    public float restitution = 1.0f;
    public bool isFloor = false;
    public bool extendedRange = false;

    [HideInInspector] public Vector4 aabbMin = Vector4.one * float.MaxValue;
    [HideInInspector] public Vector4 aabbMax = Vector4.one * float.MinValue;

    public static Dictionary<int, ColliderGroup4D> colliders = new();
    public static void UpdateColliders() {
        colliders = MakeColliderGroups(FindObjectsOfType<Collider4D>());
    }
    public static Dictionary<int, ColliderGroup4D> MakeColliderGroups(Collider4D[] allColliders) {
        Dictionary<int, ColliderGroup4D> groups = new();
        foreach (Collider4D c in allColliders) {
            int id = c.gameObject.GetInstanceID();
            if (!groups.ContainsKey(id)) {
                groups.Add(id, new ColliderGroup4D());
            }
            groups[id].Add(c);
        }
        return groups;
    }

    protected virtual void Awake() {
        obj4D = GetComponent<Object4D>();
    }

    public abstract Vector4 NP(Vector4 localPt);
    public Vector4 NPBounding(Vector4 localPt) {
        return Vector4.Max(aabbMin, Vector4.Min(aabbMax, localPt));
    }

    protected void ResetBoundingBox() {
        aabbMin = Vector4.one * float.MaxValue;
        aabbMax = Vector4.one * float.MinValue;
    }
    protected void AddBoundingPoint(Vector4 pt) {
        aabbMin = Vector4.Min(aabbMin, pt);
        aabbMax = Vector4.Max(aabbMax, pt);
    }

    public bool Collide(Vector4 worldPt, float radius, ref Hit hit) {
        //Get the 4D world transform for the object
        Transform4D localToWorld4D = obj4D.WorldTransform4D();
        Transform4D worldToLocal4D = localToWorld4D.inverse;

        //Do actual collision
        return Collide(localToWorld4D, worldToLocal4D, worldPt, radius, ref hit);
    }
    public bool Collide(Transform4D localToWorld4D, Transform4D worldToLocal4D, Vector4 worldPt, float radius, ref Hit hit) {
        //Bounds check optimization
        Vector4 localPt = worldToLocal4D * worldPt;
        if (boundsCheck) {
            Vector4 boundingNP = localToWorld4D * NPBounding(localPt);
            if (Vector4.Distance(worldPt, boundingNP) >= radius) {
                return false;
            }
        }

        //Handle sphere colliders
        Vector4 worldNP = localToWorld4D * NP(localPt);
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
