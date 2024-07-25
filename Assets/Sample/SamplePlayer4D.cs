using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SamplePlayer4D : CameraControl4D {
    public const float JUMP_VELOCITY = 4.0f;

    protected override void Update() {
        //Base update
        base.Update();

        //Find the nearest colliders
        if (isGrounded && InputManager.GetKeyDown(InputManager.KeyBind.Putt)) {
            velocity += gravityDirection * JUMP_VELOCITY;
            position4D += velocity * Time.deltaTime;
            isGrounded = false;
        }
    }
}