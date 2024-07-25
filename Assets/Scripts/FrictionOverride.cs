using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrictionOverride : MonoBehaviour {
    public float frictionOverride = 1.0f;

    private void Awake() {
        Collider4D[] colliders4D = GetComponents<Collider4D>();
        foreach (Collider4D collider in colliders4D) {
            collider.frictionOverride = frictionOverride;
        }
        Collider5D[] colliders5D = GetComponents<Collider5D>();
        foreach (Collider5D collider in colliders5D) {
            collider.frictionOverride = frictionOverride;
        }
    }
}
