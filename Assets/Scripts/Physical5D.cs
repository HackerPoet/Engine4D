//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Physical5D : MonoBehaviour {
    public static float GRAVITY = -9.81f;       //Acceleration on Y-axis (m/s^2)
    public const float MAX_UP_VELOCITY_GROUNDED = 1.0f;

    public float velocityDecay = 1.0f;         //Seconds to reach half-velocity (s)
    public bool collisionsEnabled = true;
    public float colliderRadius = 1.0f;
    public bool elastic = false;
    public float limitSlope = 0.0f;
    public float restitution = 1.0f;
    public bool extendedRange = false;

    [System.NonSerialized] public Vector5 velocity = Vector5.zero;
    [System.NonSerialized] public HashSet<Collider5D> lastHit = new(); //Since last call to "HandleColliders"
    [System.NonSerialized] public Vector5 gravityDirection = (Vector5)Vector3.up;
    [System.NonSerialized] public bool useGravity = true;
    protected bool isGrounded = false;
    private bool walking { get { return limitSlope > 0.0f; } }

    protected Collider5D GetAnyHit() {
        if (lastHit.Count == 0) { return null; }
        var enumerator = lastHit.GetEnumerator();
        enumerator.MoveNext();
        return enumerator.Current;
    }

    //NOTE: This should happen in FixedUpdate
    protected Vector5 UpdatePhysics(Vector5 pos5D, float timeStep, bool clearHits = true) {
        //Multiply the drag force at slower speeds
        float dragMul = (walking ? 0.0f : 10.0f * Mathf.Exp(-1.8f * velocity.magnitude));
        float decay = velocityDecay;
        Collider5D anyHit = GetAnyHit();
        if (!walking && anyHit != null && anyHit.frictionOverride > 0.0f) {
            decay = anyHit.frictionOverride;
        }
        decay /= (1.0f + dragMul);

        //Apply velocity decay
        float velocity_decay = Mathf.Pow(2.0f, -timeStep / decay);
        float origUp = Vector5.Dot(velocity, gravityDirection);
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
        Vector5 vStep = velocity * timeStep;

        //Update collisions
        Vector5 newPos = pos5D + vStep;
        if (collisionsEnabled) {
            //Get collider delta
            Vector5 stepPos = newPos;
            newPos = HandleColliders(stepPos, out float maxSinUp, clearHits);
            Vector5 delta = newPos - pos5D;

            //Handle special code for walking
            if (walking) {
                bool grounded = (maxSinUp > limitSlope);
                if (grounded) {
                    //Limit upward velocity so you don't jump going uphill
                    float upVelocity = Vector5.Dot(velocity, gravityDirection);
                    velocity += gravityDirection * Mathf.Min(0.0f, MAX_UP_VELOCITY_GROUNDED - upVelocity);
                    isGrounded = true;
                } else if (useGravity) {
                    //Apply gravity
                    velocity += gravityDirection * (GRAVITY * timeStep);
                }
                newPos = pos5D + delta;
            }

            //For colliders that push make sure we maintain the minimum velocity
            Vector5 colliderDelta = newPos - stepPos;
            float colliderDeltaUp = Vector5.Dot(delta, gravityDirection);
            if ((colliderDelta.sqrMagnitude - colliderDeltaUp * colliderDeltaUp) > vStep.sqrMagnitude) {
                velocity = 0.5f * delta / timeStep;
            }
        }

        return newPos;
    }

    protected Vector5 HandleColliders(Vector5 pos5D, out float maxSinUp, bool clearHits = true) {
        return HandleColliders(pos5D, Collider5D.colliders, out maxSinUp, clearHits);
    }

    protected Vector5 HandleColliders(Vector5 pos5D, Dictionary<int, ColliderGroup5D> colliderGroups, out float maxSinUp, bool clearHits = true) {
        //Compute the new position after collision
        if (clearHits) { lastHit.Clear(); }
        Collider5D.Hit hit = Collider5D.Hit.Empty;
        Vector5 origPos5D = pos5D;
        maxSinUp = 0.0f;
        if (colliderGroups == null || colliderGroups.Count == 0) { return pos5D; }
        float bestDistFloor = float.MaxValue;
        Vector5 bestFloorNormal = Vector5.zero;
        float maxRest = 0.0f;
        foreach (KeyValuePair<int, ColliderGroup5D> kv in colliderGroups) {
            //Cache object transforms for this group
            ColliderGroup5D colliderGroup = kv.Value;
            Object5D colliderObj = colliderGroup.colliders[0].obj5D;
            if (colliderObj == null) {
                LogReport.Error("Collider5D was not removed properly.");
                continue;
            }
            if (!colliderObj.isActiveAndEnabled) { continue; }
            Transform5D localToWorld5D = colliderObj.WorldTransform5D();
            Transform5D worldToLocal5D = localToWorld5D.inverse;
            if (!colliderGroup.IntersectsAABB(localToWorld5D, worldToLocal5D, pos5D, colliderRadius)) { continue; }
            foreach (Collider5D collider in colliderGroup.colliders) {
                if (!extendedRange && collider.extendedRange) { continue; }
                if (collider.Collide(localToWorld5D, worldToLocal5D, pos5D, colliderRadius, ref hit)) {
                    pos5D += hit.displacement;
                }
                float dMag = hit.displacement.magnitude;
                if (dMag > 0.0f) {
                    if (hit.collider) {
                        lastHit.Add(hit.collider);
                        maxRest = Mathf.Max(hit.collider.restitution, maxRest);
                    }
                    if (dMag < colliderRadius * 1.01f) {
                        float sUp = Vector5.Dot(hit.displacement, gravityDirection) / dMag;
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
        Vector5 displacement = pos5D - origPos5D;
        float displacementMagSq = displacement.sqrMagnitude;

        //Compute new velocity after collision
        if (displacementMagSq > 1e-12f) {
            //If collider displaces sufficiently close to the  direction of gravity,
            //then just make that the displacement to prevent drifting.
            if (Transform5D.Angle(displacement, gravityDirection) < 1.0f) {
                displacement = gravityDirection * displacement.magnitude;
            }

            //Update velocity after collision
            float dotProd = Vector5.Dot(displacement, velocity);
            if (elastic) {
                //HACK: If just barely intersecting, skip a frame to prevent bounce oscillation
                if (displacementMagSq > 1e-10 || velocity.magnitude < 0.5f) {
                    float bounce = 1.0f + restitution * maxRest;
                    velocity -= displacement * (bounce * dotProd / displacementMagSq);
                }
            } else {
                bool cancelUp = (maxSinUp <= limitSlope && walking);
                float origVUp = Vector5.Dot(velocity, gravityDirection);
                velocity -= displacement * (dotProd / displacementMagSq);
                if (cancelUp) {
                    velocity += gravityDirection * (origVUp - Vector5.Dot(velocity, gravityDirection));
                }
            }
        }

        //Return the result
        return pos5D;
    }
}
