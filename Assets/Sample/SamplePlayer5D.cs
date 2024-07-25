using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SamplePlayer5D : CameraControl5D {
    public const float JUMP_VELOCITY = 4.0f;

    protected override void Update() {
        //Base update
        base.Update();

        //Find the nearest colliders
        if (isGrounded && InputManager.GetKeyDown(InputManager.KeyBind.Putt)) {
            velocity += gravityDirection * JUMP_VELOCITY;
            position5D += velocity * Time.deltaTime;
            isGrounded = false;
        }
    }
}