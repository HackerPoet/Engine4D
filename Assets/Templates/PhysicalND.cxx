using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalND : MonoBehaviour {
    public static float GRAVITY = -9.81f;       //Acceleration on Y-axis (m/s^2)
    public const float MAX_UP_VELOCITY_GROUNDED = 1.0f;

    public float velocityDecay = 1.0f;         //Seconds to reach half-velocity (s)
    public bool collisionsEnabled = true;
    public float colliderRadius = 1.0f;
    public bool elastic = false;
    public float limitSlope = 0.0f;
    public float restitution = 1.0f;
    public bool extendedRange = false;

    [System.NonSerialized] public VECTOR velocity = VECTOR.zero;
    [System.NonSerialized] public HashSet<Collider<D>> lastHit = new(); //Since last call to "HandleColliders"
    [System.NonSerialized] public VECTOR gravityDirection = (VECTOR)Vector3.up;
    [System.NonSerialized] public bool useGravity = true;
    protected bool isGrounded = false;
    private bool walking { get { return limitSlope > 0.0f; } }

    protected Collider<D> GetAnyHit() {
        if (lastHit.Count == 0) { return null; }
        var enumerator = lastHit.GetEnumerator();
        enumerator.MoveNext();
        return enumerator.Current;
    }

    //NOTE: This should happen in FixedUpdate
    protected VECTOR UpdatePhysics(VECTOR pos<D>, float timeStep, bool clearHits = true) {
        //Multiply the drag force at slower speeds
        float dragMul = (walking ? 0.0f : 10.0f * Mathf.Exp(-1.8f * velocity.magnitude));
        float decay = velocityDecay;
        Collider<D> anyHit = GetAnyHit();
        if (!walking && anyHit != null && anyHit.frictionOverride > 0.0f) {
            decay = anyHit.frictionOverride;
        }
        decay /= (1.0f + dragMul);

        //Apply velocity decay
        float velocity_decay = Mathf.Pow(2.0f, -timeStep / decay);
        float origUp = VECTOR.Dot(velocity, gravityDirection);
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
        VECTOR vStep = velocity * timeStep;

        //Update collisions
        VECTOR newPos = pos<D> + vStep;
        if (collisionsEnabled) {
            //Get collider delta
            VECTOR stepPos = newPos;
            newPos = HandleColliders(stepPos, out float maxSinUp, clearHits);
            VECTOR delta = newPos - pos<D>;

            //Handle special code for walking
            if (walking) {
                bool grounded = (maxSinUp > limitSlope);
                if (grounded) {
                    //Limit upward velocity so you don't jump going uphill
                    float upVelocity = VECTOR.Dot(velocity, gravityDirection);
                    velocity += gravityDirection * Mathf.Min(0.0f, MAX_UP_VELOCITY_GROUNDED - upVelocity);
                    isGrounded = true;
                } else if (useGravity) {
                    //Apply gravity
                    velocity += gravityDirection * (GRAVITY * timeStep);
                    //[EXCLUDE START]
                    //Not grounded, so collision should not push you in the up direction
                    if (limitSlope < 1.0f) {
                        float upDelta = VECTOR.Dot(delta, gravityDirection);
                        delta += gravityDirection * Mathf.Min(0.0f, -upDelta);
                    }
                    //[EXCLUDE END]
                }
                newPos = pos<D> + delta;
            }

            //For colliders that push make sure we maintain the minimum velocity
            VECTOR colliderDelta = newPos - stepPos;
            float colliderDeltaUp = VECTOR.Dot(delta, gravityDirection);
            if ((colliderDelta.sqrMagnitude - colliderDeltaUp * colliderDeltaUp) > vStep.sqrMagnitude) {
                velocity = 0.5f * delta / timeStep;
            }
        }

        return newPos;
    }

    protected VECTOR HandleColliders(VECTOR pos<D>, out float maxSinUp, bool clearHits = true) {
        return HandleColliders(pos<D>, Collider<D>.colliders, out maxSinUp, clearHits);
    }

    protected VECTOR HandleColliders(VECTOR pos<D>, Dictionary<int, ColliderGroup<D>> colliderGroups, out float maxSinUp, bool clearHits = true) {
        //Compute the new position after collision
        if (clearHits) { lastHit.Clear(); }
        Collider<D>.Hit hit = Collider<D>.Hit.Empty;
        VECTOR origPos<D> = pos<D>;
        maxSinUp = 0.0f;
        if (colliderGroups == null || colliderGroups.Count == 0) { return pos<D>; }
        float bestDistFloor = float.MaxValue;
        VECTOR bestFloorNormal = VECTOR.zero;
        float maxRest = 0.0f;
        foreach (KeyValuePair<int, ColliderGroup<D>> kv in colliderGroups) {
            //Cache object transforms for this group
            ColliderGroup<D> colliderGroup = kv.Value;
            Object<D> colliderObj = colliderGroup.colliders[0].obj<D>;
            if (colliderObj == null) {
                LogReport.Error("Collider<D> was not removed properly.");
                continue;
            }
            if (!colliderObj.isActiveAndEnabled) { continue; }
            Transform<D> localToWorld<D> = colliderObj.WorldTransform<D>();
            Transform<D> worldToLocal<D> = localToWorld<D>.inverse;
            if (!colliderGroup.IntersectsAABB(localToWorld<D>, worldToLocal<D>, pos<D>, colliderRadius)) { continue; }
            foreach (Collider<D> collider in colliderGroup.colliders) {
                if (!extendedRange && collider.extendedRange) { continue; }
                if (collider.Collide(localToWorld<D>, worldToLocal<D>, pos<D>, colliderRadius, ref hit)) {
                    pos<D> += hit.displacement;
                }
                float dMag = hit.displacement.magnitude;
                if (dMag > 0.0f) {
                    if (hit.collider) {
                        lastHit.Add(hit.collider);
                        maxRest = Mathf.Max(hit.collider.restitution, maxRest);
                    }
                    if (dMag < colliderRadius * 1.01f) {
                        float sUp = VECTOR.Dot(hit.displacement, gravityDirection) / dMag;
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
        VECTOR displacement = pos<D> - origPos<D>;
        float displacementMagSq = displacement.sqrMagnitude;

        //Compute new velocity after collision
        if (displacementMagSq > 1e-12f) {
            //If collider displaces sufficiently close to the  direction of gravity,
            //then just make that the displacement to prevent drifting.
            if (Transform<D>.Angle(displacement, gravityDirection) < 1.0f) {
                displacement = gravityDirection * displacement.magnitude;
            }

            //Update velocity after collision
            float dotProd = VECTOR.Dot(displacement, velocity);
            if (elastic) {
                //HACK: If just barely intersecting, skip a frame to prevent bounce oscillation
                if (displacementMagSq > 1e-10 || velocity.magnitude < 0.5f) {
                    float bounce = 1.0f + restitution * maxRest;
                    velocity -= displacement * (bounce * dotProd / displacementMagSq);
                }
            } else {
                bool cancelUp = (maxSinUp <= limitSlope && walking);
                float origVUp = VECTOR.Dot(velocity, gravityDirection);
                velocity -= displacement * (dotProd / displacementMagSq);
                if (cancelUp) {
                    velocity += gravityDirection * (origVUp - VECTOR.Dot(velocity, gravityDirection));
                }
            }
        }

        //Return the result
        return pos<D>;
    }
}
