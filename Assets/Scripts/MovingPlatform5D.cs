//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
#define USE_5D
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform5D : MonoBehaviour {
    public const float PLATFORM_FRICTION = 0.75f;

    public Object5D obj5D;
    public Collider5D collider5D;

    protected bool delayStopBall = false;

    private CameraControl5D cam5D;
    private Collider5D.Hit hit = Collider5D.Hit.Empty;

    protected virtual void Start() {
        Debug.Assert(obj5D != null);
        cam5D = FindObjectOfType<CameraControl5D>();
        if (collider5D) { collider5D.frictionOverride = PLATFORM_FRICTION; }
    }

    public virtual void Movement() { }

    private void FixedUpdate() {
        //Save starting transform, then apply movement
        Transform5D lastTransform = obj5D.WorldTransform5D();
        Movement();

        //Don't move ball or player if this is a reflection
        if (!collider5D) { return; }

        //Use new transform to compute deltas
        Transform5D curTransform = obj5D.WorldTransform5D();
        Matrix5x5 deltaMatrix = curTransform.matrix * lastTransform.matrix.inverse;
        Vector5 deltaTranslate = curTransform.translation - lastTransform.translation;

        //Check if player is walking on the platform
        hit = Collider5D.Hit.Empty;
        bool playerOnPlatform = (collider5D.Collide(cam5D.colliderPosition5D, cam5D.colliderRadius * 1.05f, ref hit));

        //Move the player as well if needed
        if (playerOnPlatform) {
            cam5D.position5D = deltaMatrix * (cam5D.position5D - lastTransform.translation) + deltaTranslate + lastTransform.translation;
            //Debug.Assert(deltaMatrix.GetColumn(1) == (Vector5)Vector3.up);
#if USE_5D
            Matrix4x4 mMat = Transform5D.SkipY(deltaMatrix);
            cam5D.m1 = Isocline.FromMatrix(mMat) * cam5D.m1;
#else
            Matrix4x4 mMat = Transform4D.XYZWTo(deltaMatrix, 0, 3, 1, 2);
            cam5D.m1 = mMat.rotation * cam5D.m1;
#endif
            //Debug.Assert(mMat.determinant > 0.0f);
        }
    }
}
