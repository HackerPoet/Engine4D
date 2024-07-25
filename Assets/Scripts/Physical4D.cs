//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Physical4D : MonoBehaviour {
    public static float GRAVITY = -9.81f;       //Acceleration on Y-axis (m/s^2)
    public const float MAX_UP_VELOCITY_GROUNDED = 1.0f;

    public float velocityDecay = 1.0f;         //Seconds to reach half-velocity (s)
    public bool collisionsEnabled = true;
    public float colliderRadius = 1.0f;
    public bool elastic = false;
    public float limitSlope = 0.0f;
    public float restitution = 1.0f;
    public bool extendedRange = false;

    [System.NonSerialized] public Vector4 velocity = Vector4.zero;
    [System.NonSerialized] public HashSet<Collider4D> lastHit = new(); //Since last call to "HandleColliders"
    [System.NonSerialized] public Vector4 gravityDirection = (Vector4)Vector3.up;
    [System.NonSerialized] public bool useGravity = true;
    protected bool isGrounded = false;
    private bool walking { get { return limitSlope > 0.0f; } }

    protected Collider4D GetAnyHit() {
        if (lastHit.Count == 0) { return null; }
        var enumerator = lastHit.GetEnumerator();
        enumerator.MoveNext();
        return enumerator.Current;
    }

    //NOTE: This should happen in FixedUpdate
    protected Vector4 UpdatePhysics(Vector4 pos4D, float timeStep, bool clearHits = true) {
        //Multiply the drag force at slower speeds
        float dragMul = (walking ? 0.0f : 10.0f * Mathf.Exp(-1.8f * velocity.magnitude));
        float decay = velocityDecay;
        Collider4D anyHit = GetAnyHit();
        if (!walking && anyHit != null && anyHit.frictionOverride > 0.0f) {
            decay = anyHit.frictionOverride;
        }
        decay /= (1.0f + dragMul);

        //Apply velocity decay
        float velocity_decay = Mathf.Pow(2.0f, -timeStep / decay);
        float origUp = Vector4.Dot(velocity, gravityDirection);
        velocity *= velocity_decay;
        if (useGravity) {
            velocity += (origUp * (1.0f - velocity_decay)) * gravityDirection;
        }

        //Apply gravity first if not walking
        if (useGravity && (!walking || !collisionsEnabled)) {
            velocity += gravityDirection * (GRAVITY * timeStep);
        }

        //Reset grounded state
        isGrounded = false;

        //Find velocity step
        Vector4 vStep = velocity * timeStep;

        //Update collisions
        Vector4 newPos = pos4D + vStep;
        if (collisionsEnabled) {
            //Get collider delta
            Vector4 stepPos = newPos;
            newPos = HandleColliders(stepPos, out float maxSinUp, clearHits);
            Vector4 delta = newPos - pos4D;

            //Handle special code for walking
            if (walking) {
                bool grounded = (maxSinUp > limitSlope);
                if (grounded) {
                    //Limit upward velocity so you don't jump going uphill
                    float upVelocity = Vector4.Dot(velocity, gravityDirection);
                    velocity += gravityDirection * Mathf.Min(0.0f, MAX_UP_VELOCITY_GROUNDED - upVelocity);
                    isGrounded = true;
                } else if (useGravity) {
                    //Apply gravity
                    velocity += gravityDirection * (GRAVITY * timeStep);
                }
                newPos = pos4D + delta;
            }

            //For colliders that push make sure we maintain the minimum velocity
            Vector4 colliderDelta = newPos - stepPos;
            float colliderDeltaUp = Vector4.Dot(delta, gravityDirection);
            if ((colliderDelta.sqrMagnitude - colliderDeltaUp * colliderDeltaUp) > vStep.sqrMagnitude) {
                velocity = 0.5f * delta / timeStep;
            }
        }

        return newPos;
    }

    protected Vector4 HandleColliders(Vector4 pos4D, out float maxSinUp, bool clearHits = true) {
        return HandleColliders(pos4D, Collider4D.colliders, out maxSinUp, clearHits);
    }

    protected Vector4 HandleColliders(Vector4 pos4D, Dictionary<int, ColliderGroup4D> colliderGroups, out float maxSinUp, bool clearHits = true) {
        //Compute the new position after collision
        if (clearHits) { lastHit.Clear(); }
        Collider4D.Hit hit = Collider4D.Hit.Empty;
        Vector4 origPos4D = pos4D;
        maxSinUp = 0.0f;
        if (colliderGroups == null || colliderGroups.Count == 0) { return pos4D; }
        float bestDistFloor = float.MaxValue;
        Vector4 bestFloorNormal = Vector4.zero;
        float maxRest = 0.0f;
        foreach (KeyValuePair<int, ColliderGroup4D> kv in colliderGroups) {
            //Cache object transforms for this group
            ColliderGroup4D colliderGroup = kv.Value;
            Object4D colliderObj = colliderGroup.colliders[0].obj4D;
            if (colliderObj == null) {
                LogReport.Error("Collider4D was not removed properly.");
                continue;
            }
            if (!colliderObj.isActiveAndEnabled) { continue; }
            Transform4D localToWorld4D = colliderObj.WorldTransform4D();
            Transform4D worldToLocal4D = localToWorld4D.inverse;
            if (!colliderGroup.IntersectsAABB(localToWorld4D, worldToLocal4D, pos4D, colliderRadius)) { continue; }
            foreach (Collider4D collider in colliderGroup.colliders) {
                if (!extendedRange && collider.extendedRange) { continue; }
                if (collider.Collide(localToWorld4D, worldToLocal4D, pos4D, colliderRadius, ref hit)) {
                    pos4D += hit.displacement;
                }
                float dMag = hit.displacement.magnitude;
                if (dMag > 0.0f) {
                    if (hit.collider) {
                        lastHit.Add(hit.collider);
                        maxRest = Mathf.Max(hit.collider.restitution, maxRest);
                    }
                    if (dMag < colliderRadius * 1.01f) {
                        float sUp = Vector4.Dot(hit.displacement, gravityDirection) / dMag;
                        maxSinUp = Mathf.Max(maxSinUp, sUp);
                    }
                    if (hit.floorNormal.sqrMagnitude > 0.0f && dMag < bestDistFloor) {
                        bestDistFloor = dMag;
                        bestFloorNormal = hit.floorNormal;
                    }
                }
            }
        }

        //Adjust gravity if sticky
        if (bestFloorNormal.sqrMagnitude > 0.0f) {
            gravityDirection = bestFloorNormal / bestFloorNormal.magnitude;
        }

        //Get the overall displacement
        Vector4 displacement = pos4D - origPos4D;
        float displacementMagSq = displacement.sqrMagnitude;

        //Compute new velocity after collision
        if (displacementMagSq > 1e-12f) {
            //If collider displaces sufficiently close to the  direction of gravity,
            //then just make that the displacement to prevent drifting.
            if (Transform4D.Angle(displacement, gravityDirection) < 1.0f) {
                displacement = gravityDirection * displacement.magnitude;
            }

            //Update velocity after collision
            float dotProd = Vector4.Dot(displacement, velocity);
            if (elastic) {
                //HACK: If just barely intersecting, skip a frame to prevent bounce oscillation
                if (displacementMagSq > 1e-10 || velocity.magnitude < 0.5f) {
                    float bounce = 1.0f + restitution * maxRest;
                    velocity -= displacement * (bounce * dotProd / displacementMagSq);
                }
            } else {
                bool cancelUp = (maxSinUp <= limitSlope && walking);
                float origVUp = Vector4.Dot(velocity, gravityDirection);
                velocity -= displacement * (dotProd / displacementMagSq);
                if (cancelUp) {
                    velocity += gravityDirection * (origVUp - Vector4.Dot(velocity, gravityDirection));
                }
            }
        }

        //Return the result
        return pos4D;
    }
}
