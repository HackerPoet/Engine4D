using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//This component can be added to every scene so that there will be a single global instance
//of a gameobject across scenes no matter which one you start with.
[DefaultExecutionOrder(-99)]
public class Singleton : MonoBehaviour {
    public GameObject singletonPrefab;

    private static GameObject instance = null;

    void Awake() {
        if (instance != null) {
            DestroyImmediate(gameObject);
        } else {
            instance = Instantiate(singletonPrefab, null);
            DontDestroyOnLoad(instance);
        }
    }

    public static void RestartGame() {
        //NOTE: DestroyImmediate caused a crash when unlocking all features
        //      using menu navigation (as opposed to a mouse click).
        Destroy(instance);
        instance = null;
        SceneManager.LoadScene(0);
    }
}
