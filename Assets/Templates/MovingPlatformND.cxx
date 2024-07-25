#define USE_<D>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformND : MonoBehaviour {
    public const float PLATFORM_FRICTION = 0.75f;

    public Object<D> obj<D>;
    public Collider<D> collider<D>;

    protected bool delayStopBall = false;

    private CameraControl<D> cam<D>;
    private Collider<D>.Hit hit = Collider<D>.Hit.Empty;

    protected virtual void Start() {
        Debug.Assert(obj<D> != null);
        cam<D> = FindObjectOfType<CameraControl<D>>();
        if (collider<D>) { collider<D>.frictionOverride = PLATFORM_FRICTION; }
    }

    public virtual void Movement() { }

    private void FixedUpdate() {
        //Save starting transform, then apply movement
        Transform<D> lastTransform = obj<D>.WorldTransform<D>();
        Movement();

        //Don't move ball or player if this is a reflection
        if (!collider<D>) { return; }

        //Use new transform to compute deltas
        Transform<D> curTransform = obj<D>.WorldTransform<D>();
        MATRIX deltaMatrix = curTransform.matrix * lastTransform.matrix.inverse;
        VECTOR deltaTranslate = curTransform.translation - lastTransform.translation;

        //Check if player is walking on the platform
        hit = Collider<D>.Hit.Empty;
        bool playerOnPlatform = (collider<D>.Collide(cam<D>.colliderPosition<D>, cam<D>.colliderRadius * 1.05f, ref hit));

        //Move the player as well if needed
        if (playerOnPlatform) {
            cam<D>.position<D> = deltaMatrix * (cam<D>.position<D> - lastTransform.translation) + deltaTranslate + lastTransform.translation;
            //Debug.Assert(deltaMatrix.GetColumn(1) == (VECTOR)Vector3.up);
#if USE_5D
            Matrix4x4 mMat = Transform5D.SkipY(deltaMatrix);
            cam<D>.m1 = Isocline.FromMatrix(mMat) * cam<D>.m1;
#else
            Matrix4x4 mMat = Transform4D.XYZWTo(deltaMatrix, 0, 3, 1, 2);
            cam<D>.m1 = mMat.rotation * cam<D>.m1;
#endif
            //Debug.Assert(mMat.determinant > 0.0f);
        }
    }
}
