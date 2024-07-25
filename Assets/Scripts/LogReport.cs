using System;
using UnityEngine;

public static class LogReport {
    //This is a replacement for Debug.LogError().
    //It allows the error message to show up in Unity's cloud exception reports.
    public static void Error(string errorMsg) {
        Debug.LogException(new Exception(errorMsg));
    }
}
