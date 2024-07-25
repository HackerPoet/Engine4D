using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinDisk4D : MovingPlatform4D {
    public float SPIN_RATE = 20.0f; //Degrees per second

    private float t = 0.0f;

    public override void Movement() {
        t = (t + SPIN_RATE * Time.fixedDeltaTime) % 360.0f;
        obj4D.localRotation4D = Transform4D.PlaneRotation(t, 0, 2);
    }
}
