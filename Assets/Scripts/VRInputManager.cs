using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public static class VRInputManager {
    public enum Hand {
        Left = 0,
        Right = 1,
    }

    public enum VRButton {
        Rotate,
        Pause,
        GUISelect,
    };

    public static Dictionary<VRButton, string> keyMap = new() {
        { VRButton.Rotate, "LeftTrigger" },
        { VRButton.GUISelect, "RightTrigger" },
        { VRButton.Pause, "RightButton1" },
    };

    public delegate void ControllerInitializedCallback(Hand hand, string name);

    private static InputFeatureUsage<Vector3> devicePosition = new InputFeatureUsage<Vector3>("DevicePosition");
    private static InputFeatureUsage<Quaternion> deviceRotation = new InputFeatureUsage<Quaternion>("DeviceRotation");

    private static UnityEngine.InputSystem.InputDevice[] controllers = new UnityEngine.InputSystem.InputDevice[2];
    private static UnityEngine.XR.InputDevice[] xrControllers = new UnityEngine.XR.InputDevice[2];
    private static InputActionAsset inputActionAsset = null;
    private static InputActionMap inputActionMap = null;

    private static void InitializeVRControllers() {
        InputSystem.onDeviceChange += (device, change) => {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected) {
                controllers[(int)GetHandIx(device)] = device;
            } else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected) {
                controllers[(int)GetHandIx(device)] = null;
            }
        };
    }

    private static Hand GetHandIx(UnityEngine.InputSystem.InputDevice device) {
        foreach (string usage in device.usages) {
            if (usage.Contains(UnityEngine.InputSystem.CommonUsages.LeftHand)) {
                return Hand.Left;
            } else if (usage.Contains(UnityEngine.InputSystem.CommonUsages.RightHand)) {
                return Hand.Right;
            }
        }
        return Hand.Left;
    }

    private static InputActionMap GetInputActionMap() {
        if (inputActionAsset == null || !inputActionAsset.enabled) {
            InitializeVRControllers();
            inputActionAsset = Resources.Load<InputActionAsset>("Controls");
            inputActionAsset.Enable();
            inputActionMap = null;
        }
        if (inputActionMap == null) {
            inputActionMap = inputActionAsset.FindActionMap("Gameplay");
        }
        return inputActionMap;
    }

    public static bool GetKeyDown(VRButton key, bool overridePause = false) {
        if (!UnityEngine.XR.XRSettings.enabled) { return false; }
        if ((InputManager.isPaused || !InputManager.isFocused) && !overridePause) {
            return false;
        //TODO: Handle tutorials for VR
        //} else if (tutorialLevel != TutLock.Invalid &&
        //           TutorialLockKeys.ContainsKey(key) &&
        //           TutorialLockKeys[key] > tutorialLevel) {
        //    return false;
        } else if (keyMap.ContainsKey(key)) {
            return GetKeyDown(keyMap[key]);
        } else {
            LogReport.Error("Key is not bound: " + key);
            return false;
        }
    }
    public static bool GetKeyDown(string key) {
        if (key.Length == 0) { return false; }
        InputAction action = GetInputActionMap().FindAction(key);
        if (action == null) {
            return false;
        } else {
            return action.triggered;
        }
    }

    public static bool GetKey(VRButton key, bool overridePause = false) {
        if (!UnityEngine.XR.XRSettings.enabled) { return false; }
        if ((InputManager.isPaused || !InputManager.isFocused) && !overridePause) {
            return false;
            //TODO: Handle tutorials for VR
            //} else if (tutorialLevel != TutLock.Invalid &&
            //           TutorialLockKeys.ContainsKey(key) &&
            //           TutorialLockKeys[key] > tutorialLevel) {
            //    return false;
        } else if (keyMap.ContainsKey(key)) {
            return GetKey(keyMap[key]);
        } else {
            LogReport.Error("Key is not bound: " + key);
            return false;
        }
    }
    public static bool GetKey(string key) {
        if (key.Length == 0) { return false; }
        InputAction action = GetInputActionMap().FindAction(key);
        if (action == null) {
            return false;
        } else {
            return (action.phase == InputActionPhase.Started || action.phase == InputActionPhase.Performed);
        }
    }

    public static bool HandOrientation(Hand hand, out Quaternion handOrientation) {
        handOrientation = Quaternion.identity;
        UnityEngine.XR.InputDevice device = xrControllers[(int)hand];
        if (device == null || !device.isValid) { return false; }
        return device.TryGetFeatureValue(deviceRotation, out handOrientation);
    }
    public static bool HandPosition(Hand hand, out Vector3 handPosition) {
        handPosition = Vector3.zero;
        UnityEngine.XR.InputDevice device = xrControllers[(int)hand];
        if (device == null || !device.isValid) { return false; }
        return device.TryGetFeatureValue(devicePosition, out handPosition) && (handPosition != Vector3.zero);
    }

    public static bool CheckInitializeHand(Hand hand, ControllerInitializedCallback callback) {
        //Check if hand is already valid
        if (xrControllers[(int)hand].isValid) { return true; }

        //Otherwise, try to initialize hand
        InputDeviceCharacteristics idc = (hand == Hand.Left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right);
        InitializeHand(idc, ref xrControllers[(int)hand], callback);
        return xrControllers[(int)hand].isValid;
    }

    private static void InitializeHand(InputDeviceCharacteristics hand, ref UnityEngine.XR.InputDevice device, ControllerInitializedCallback callback) {
        var inputDevices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(hand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice, inputDevices);
        if (inputDevices.Count > 0) {
            if (inputDevices[0].isValid && device.name != inputDevices[0].name) {
                //Only need the callback for new, valid controllers
                bool isLeft = (hand == InputDeviceCharacteristics.Left);
                Debug.Log("Detected " + (isLeft ? "Left" : "Right") + " XR Controller: " + inputDevices[0].name);
                callback(isLeft ? Hand.Left : Hand.Right, inputDevices[0].name);
            }
            device = inputDevices[0];
        }
    }
}
