using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildColliders : MonoBehaviour {
    public bool is5D = false;
    void Start() {
        if (is5D) {
            Collider5D.UpdateColliders();
        } else {
            Collider4D.UpdateColliders();
        }
    }
}
