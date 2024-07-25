//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
#define USE_4D
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform4D : MonoBehaviour {
    public const float PLATFORM_FRICTION = 0.75f;

    public Object4D obj4D;
    public Collider4D collider4D;

    protected bool delayStopBall = false;

    private CameraControl4D cam4D;
    private Collider4D.Hit hit = Collider4D.Hit.Empty;

    protected virtual void Start() {
        Debug.Assert(obj4D != null);
        cam4D = FindObjectOfType<CameraControl4D>();
        if (collider4D) { collider4D.frictionOverride = PLATFORM_FRICTION; }
    }

    public virtual void Movement() { }

    private void FixedUpdate() {
        //Save starting transform, then apply movement
        Transform4D lastTransform = obj4D.WorldTransform4D();
        Movement();

        //Don't move ball or player if this is a reflection
        if (!collider4D) { return; }

        //Use new transform to compute deltas
        Transform4D curTransform = obj4D.WorldTransform4D();
        Matrix4x4 deltaMatrix = curTransform.matrix * lastTransform.matrix.inverse;
        Vector4 deltaTranslate = curTransform.translation - lastTransform.translation;

        //Check if player is walking on the platform
        hit = Collider4D.Hit.Empty;
        bool playerOnPlatform = (collider4D.Collide(cam4D.colliderPosition4D, cam4D.colliderRadius * 1.05f, ref hit));

        //Move the player as well if needed
        if (playerOnPlatform) {
            cam4D.position4D = deltaMatrix * (cam4D.position4D - lastTransform.translation) + deltaTranslate + lastTransform.translation;
            //Debug.Assert(deltaMatrix.GetColumn(1) == (Vector4)Vector3.up);
#if USE_5D
            Matrix4x4 mMat = Transform5D.SkipY(deltaMatrix);
            cam4D.m1 = Isocline.FromMatrix(mMat) * cam4D.m1;
#else
            Matrix4x4 mMat = Transform4D.XYZWTo(deltaMatrix, 0, 3, 1, 2);
            cam4D.m1 = mMat.rotation * cam4D.m1;
#endif
            //Debug.Assert(mMat.determinant > 0.0f);
        }
    }
}
