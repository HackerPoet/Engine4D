using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputManager {
    //Game preferences
    public static float CAM_SMOOTHING = 0.05f; //Seconds to half the speed
    public static float LOOK_SENSITIVITY = 1.0f;
    public static float PUTT_SENSITIVITY = 1.0f;
    public static float DEADZONE = 0.2f;

    public enum GUIDirection {
        None, Left, Right, Up, Down
    }

    public const KeyCode CustomKeyStart   = (KeyCode)1000; //Make sure this is equal to the first
    public const KeyCode ButtonNorth      = (KeyCode)1000;
    public const KeyCode ButtonEast       = (KeyCode)1001;
    public const KeyCode ButtonWest       = (KeyCode)1002;
    public const KeyCode ButtonSouth      = (KeyCode)1003;
    public const KeyCode LeftShoulder     = (KeyCode)1004;
    public const KeyCode RightShoulder    = (KeyCode)1005;
    public const KeyCode StartButton      = (KeyCode)1006;
    public const KeyCode SelectButton     = (KeyCode)1007;
    public const KeyCode RightStickButton = (KeyCode)1008;
    public const KeyCode LeftStickButton  = (KeyCode)1009;
    public const KeyCode CustomKeyEnd     = (KeyCode)1010; //Make sure this is 1 more than the last

    public enum KeyBind {
        Left = 0,
        Right = 1,
        Forward = 2,
        Backward = 3,
        Kata = 4,
        Ana = 5,
        Sursum = 6,
        Deorsum = 7,
        Look4D = 8,
        Look5D = 9,
        LookSpin = 10,
        Putt = 11,
        Reset = 12,
        VolumeView = 13,
        SeekHole = 14,
        SeekBall = 15,
        SeekCrystal = 16,
        Pause = 17,
        ShadowToggle = 18,
        SliceToggle = 19,
        Run = 20,
        GUISelect = 30,
        GUIBack = 31,
        GUILeft = 32,
        GUIRight = 33,
        GUIUp = 34,
        GUIDown = 35,
        EditorRotate = 40,
    }

    public enum TutLock {
        Invalid = -1,
        Walk3DOnly = 0,
        CanPutt = 1,
        Locked4D = 1,
        Use4D = 2,
        UseVolume = 3,
        ToggleSlice = 4,
        ToggleShadow = 5,
        UseSeek = 6,
        ResetBall = 7,
        All4D = 7,
        Locked5D = 8,
        Use5D = 9,
        Spin5D = 10,
        All5D = 10,
        All = 10,
    }

    public static readonly Dictionary<KeyBind, KeyCode> DefaultKeyboardMap = new() {
        { KeyBind.Left, KeyCode.A },
        { KeyBind.Right, KeyCode.D },
        { KeyBind.Forward, KeyCode.W },
        { KeyBind.Backward, KeyCode.S },
        { KeyBind.Run, KeyCode.LeftShift },
        { KeyBind.Ana, KeyCode.Q },
        { KeyBind.Kata, KeyCode.E },
        { KeyBind.Sursum, KeyCode.Z },
        { KeyBind.Deorsum, KeyCode.X },
        { KeyBind.Look4D, KeyCode.Mouse0 },
        { KeyBind.Look5D, KeyCode.Mouse1 },
        { KeyBind.LookSpin, KeyCode.Mouse2 },
        { KeyBind.Putt, KeyCode.Space },
        { KeyBind.Reset, KeyCode.R },
        { KeyBind.VolumeView, KeyCode.V },
        { KeyBind.SeekHole, KeyCode.H },
        { KeyBind.SeekBall, KeyCode.B },
        { KeyBind.SeekCrystal, KeyCode.C },
#if UNITY_EDITOR
        { KeyBind.Pause, KeyCode.BackQuote },
        { KeyBind.GUIBack, KeyCode.BackQuote },
#else
        { KeyBind.Pause, KeyCode.Escape },
        { KeyBind.GUIBack, KeyCode.Escape },
#endif
        { KeyBind.ShadowToggle, KeyCode.Alpha2 },
        { KeyBind.SliceToggle, KeyCode.Alpha1 },
        { KeyBind.GUISelect, KeyCode.Return },
        { KeyBind.GUILeft, KeyCode.LeftArrow },
        { KeyBind.GUIRight, KeyCode.RightArrow },
        { KeyBind.GUIUp, KeyCode.UpArrow },
        { KeyBind.GUIDown, KeyCode.DownArrow },
        { KeyBind.EditorRotate, KeyCode.Mouse2 },
    };
    public static readonly Dictionary<KeyBind, KeyCode> DefaultGamepadMap = new() {
        { KeyBind.Left, KeyCode.None },
        { KeyBind.Right, KeyCode.None },
        { KeyBind.Forward, KeyCode.None },
        { KeyBind.Backward, KeyCode.None },
        { KeyBind.Run, KeyCode.None },
        { KeyBind.Ana, KeyCode.None },
        { KeyBind.Kata, KeyCode.None },
        { KeyBind.Sursum, KeyCode.None },
        { KeyBind.Deorsum, KeyCode.None },
        { KeyBind.Look4D, LeftShoulder },
        { KeyBind.Look5D, RightShoulder },
        { KeyBind.LookSpin, KeyCode.None },
        { KeyBind.Putt, ButtonSouth },
        { KeyBind.Reset, ButtonEast },
        { KeyBind.VolumeView, ButtonWest },
        { KeyBind.SeekHole, ButtonNorth },
        { KeyBind.SeekBall, KeyCode.None },
        { KeyBind.SeekCrystal, KeyCode.None },
        { KeyBind.Pause, StartButton },
        { KeyBind.GUIBack, ButtonEast },
        { KeyBind.ShadowToggle, RightStickButton },
        { KeyBind.SliceToggle, LeftStickButton },
        { KeyBind.GUISelect, ButtonSouth },
        { KeyBind.GUILeft, KeyCode.None },
        { KeyBind.GUIRight, KeyCode.None },
        { KeyBind.GUIUp, KeyCode.None },
        { KeyBind.GUIDown, KeyCode.None },
        { KeyBind.EditorRotate, RightShoulder },
    };

    public enum AxisBind {
        LookHorizontal = 0,
        LookVertical = 1,
        MoveLeftRight = 2,
        MoveForwardBack = 3,
        MoveAnaKata = 4,
        MoveSursumDeorsum = 5,
        Zoom = 6,
        GUILeftRight = 7,
        GUIUpDown = 8,
        PuttAxis = 9,
    }
    public static readonly Dictionary<AxisBind, string> DefaultKeyboardAxisMap = new() {
        { AxisBind.LookHorizontal, "MouseX" },
        { AxisBind.LookVertical, "MouseY" },
        { AxisBind.MoveLeftRight, "" },
        { AxisBind.MoveForwardBack, "" },
        { AxisBind.MoveAnaKata, "" },
        { AxisBind.MoveSursumDeorsum, "" },
        { AxisBind.Zoom, "MouseScrollWheel" },
        { AxisBind.GUILeftRight, "" },
        { AxisBind.GUIUpDown, "" },
        { AxisBind.PuttAxis, "MouseY" },
    };
    public static readonly Dictionary<AxisBind, string> DefaultGamepadAxisMap = new() {
        { AxisBind.LookHorizontal, "RightStickX" },
        { AxisBind.LookVertical, "RightStickY" },
        { AxisBind.MoveLeftRight, "LeftStickX" },
        { AxisBind.MoveForwardBack, "LeftStickY" },
        { AxisBind.MoveAnaKata, "DpadY" },
        { AxisBind.MoveSursumDeorsum, "DpadX" },
        { AxisBind.Zoom, "Trigger" },
        { AxisBind.GUILeftRight, "LeftStickX" },
        { AxisBind.GUIUpDown, "LeftStickY" },
        { AxisBind.PuttAxis, "LeftStickY" },
    };
    public static readonly HashSet<AxisBind> DefaultGamepadAxisInvertSet = new() {
    };

    private static readonly KeyCode[] allKeyCodes;
    private static readonly string[] allAxes = new[] {
        "MouseX",
        "MouseY",
        "MouseScrollWheel",
        "RightStickX",
        "RightStickY",
        "LeftStickX",
        "LeftStickY",
        "DpadX",
        "DpadY",
        "Trigger",
    };

    //Lock certain keys during tutorial phases
    public static TutLock tutorialLevel = TutLock.Invalid;
    private static readonly Dictionary<KeyBind, TutLock> TutorialLockKeys = new() {
        { KeyBind.Putt, TutLock.CanPutt },
        { KeyBind.Ana, TutLock.Use4D },
        { KeyBind.Kata, TutLock.Use4D },
        { KeyBind.Look4D, TutLock.Use4D },
        { KeyBind.VolumeView, TutLock.UseVolume },
        { KeyBind.SliceToggle, TutLock.ToggleSlice },
        { KeyBind.ShadowToggle, TutLock.ToggleShadow },
        { KeyBind.SeekHole, TutLock.UseSeek },
        { KeyBind.SeekBall, TutLock.UseSeek },
        { KeyBind.Sursum, TutLock.Use5D },
        { KeyBind.Deorsum, TutLock.Use5D },
        { KeyBind.Look5D, TutLock.Use5D },
        { KeyBind.LookSpin, TutLock.Spin5D },
    };
    private static readonly Dictionary<AxisBind, TutLock> TutorialLockAxis = new() {
        { AxisBind.MoveAnaKata, TutLock.Use4D },
        { AxisBind.MoveSursumDeorsum, TutLock.Use5D },
    };

    //Static initializer
    static InputManager() {
        int numCustom = CustomKeyEnd - CustomKeyStart;
        KeyCode[] enumValues = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        allKeyCodes = new KeyCode[numCustom + enumValues.Length];
        Array.Copy(enumValues, 0, allKeyCodes, numCustom, enumValues.Length);
        for (int i = 0; i < numCustom; ++i) {
            allKeyCodes[i] = CustomKeyStart + i;
        }
    }

    //Singleton to hold the input action asset
    public static bool isPaused = false;
    public static bool isFocused = true;
    public static Dictionary<KeyBind, KeyCode> keyMap = new(DefaultKeyboardMap);
    public static Dictionary<KeyBind, KeyCode> gamepadKeyMap = new(DefaultGamepadMap);
    public static Dictionary<AxisBind, string> axisMap = new(DefaultKeyboardAxisMap);
    public static Dictionary<AxisBind, string> gamepadAxisMap = new(DefaultGamepadAxisMap);
    public static HashSet<AxisBind> axisInvertSet = new();
    public static HashSet<AxisBind> gamepadAxisInvertSet = new(DefaultGamepadAxisInvertSet);

    public static void RestoreDefaults() {
        keyMap = new(DefaultKeyboardMap);
        gamepadKeyMap = new(DefaultGamepadMap);
        axisMap = new(DefaultKeyboardAxisMap);
        gamepadAxisMap = new(DefaultGamepadAxisMap);
        axisInvertSet = new();
        gamepadAxisInvertSet = new(DefaultGamepadAxisInvertSet);
    }

    public static void RebindKey(KeyBind key, KeyCode keyCode, bool gamepad = false) {
        if (gamepad) {
            gamepadKeyMap[key] = keyCode;
        } else {
            keyMap[key] = keyCode;
        }
    }
    public static void RebindAxis(AxisBind axis, string axisCode, bool gamepad = false) {
        if (gamepad) {
            gamepadAxisMap[axis] = axisCode;
        } else {
            axisMap[axis] = axisCode;
        }
    }

    public static bool GetKey(KeyBind key, bool overridePause = false) {
        if ((isPaused || !isFocused) && !overridePause) {
            return false;
        } else if (tutorialLevel != TutLock.Invalid &&
                   TutorialLockKeys.ContainsKey(key) &&
                   TutorialLockKeys[key] > tutorialLevel) {
            return false;
        } else {
            if (UnityEngine.XR.XRSettings.enabled) {
                return false;
            } else if (keyMap.ContainsKey(key)) {
                return GetKey(keyMap[key]) || GetKey(gamepadKeyMap[key]);
            } else {
                LogReport.Error("Key is not bound: " + key);
                return false;
            }
        }
    }

    public static bool GetKey(KeyCode keyCode) {
        if (keyCode >= CustomKeyStart) {
            return GetGamepadButton(keyCode, true);
        } else {
            return Input.GetKey(keyCode);
        }
    }

    public static bool GetKeyDown(KeyBind key, bool overridePause = false) {
        if ((isPaused || !isFocused) && !overridePause) {
            return false;
        } else if (tutorialLevel != TutLock.Invalid &&
                   TutorialLockKeys.ContainsKey(key) &&
                   TutorialLockKeys[key] > tutorialLevel) {
            return false;
        } else {
            if (UnityEngine.XR.XRSettings.enabled) {
                return false;
            } else if (keyMap.ContainsKey(key)) {
                return GetKeyDown(keyMap[key]) || GetKeyDown(gamepadKeyMap[key]);
            } else {
                LogReport.Error("Key is not bound: " + key);
                return false;
            }
        }
    }

    public static bool GetKeyDown(KeyCode keyCode) {
        if (keyCode >= CustomKeyStart) {
            return GetGamepadButton(keyCode, false);
        } else {
            return Input.GetKeyDown(keyCode);
        }
    }

    public static float GetAxis(AxisBind axis, bool overridePause = false) {
        if ((isPaused || !isFocused) && !overridePause) {
            return 0.0f;
        } else if (tutorialLevel != TutLock.Invalid &&
                   TutorialLockAxis.ContainsKey(axis) &&
                   TutorialLockAxis[axis] > tutorialLevel) {
            return 0.0f;
        } else if (UnityEngine.XR.XRSettings.enabled) {
            return 0.0f;
        } else if (axisMap.ContainsKey(axis)) {
            float val = GetRawAxis(axis, axisMap[axis], axisInvertSet.Contains(axis));
            if (val != 0.0f) { return val; }
            return GetRawAxis(axis, gamepadAxisMap[axis], gamepadAxisInvertSet.Contains(axis));
        } else {
            return 0.0f;
        }
    }
    private static float GetRawAxis(AxisBind axis, string axisName, bool invert) {
        if (axisName.Length == 0) { return 0.0f; }
        float rawValue = GetAxisRaw(axisName, out bool isController);
        if (invert) { rawValue = -rawValue; }
        if (axis == AxisBind.LookHorizontal || axis == AxisBind.LookVertical) {
            rawValue *= LOOK_SENSITIVITY;
            if (isController) { rawValue *= 2.5f; }
        } else if (axis == AxisBind.PuttAxis) {
            rawValue *= PUTT_SENSITIVITY;
        }
        return rawValue;
    }
    private static float GetAxisRaw(string axisName, out bool isController) {
        isController = !axisName.StartsWith("Mouse");
        if (isController) {
            return AddDeadzone(GetGamepadAxis(axisName));
        } else {
            return Input.GetAxisRaw(axisName);
        }
    }

    public static KeyCode GetAnyKeyDown() {
        foreach (KeyCode key in allKeyCodes) {
            if (GetKeyDown(key)) {
                return key;
            }
        }
        return KeyCode.None;
    }

    public static string GetAnyAxis() {
        foreach (string axis in allAxes) {
            if (Mathf.Abs(GetAxisRaw(axis, out bool _)) > 0.5f) {
                return axis;
            }
        }
        return "";
    }

    public static void HideCursor(bool hide) {
        if (hide) {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        } else {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public static float AddDeadzone(float value) {
        return (Mathf.Max(0.0f, value - DEADZONE) + Mathf.Min(0.0f, value + DEADZONE)) / (1.0f - DEADZONE);
    }

    public static GUIDirection GetGUIDirection() {
        float x = GetAxis(AxisBind.GUILeftRight, true);
        if (GetKeyDown(KeyBind.GUILeft, true) || x < -0.5f) {
            return GUIDirection.Left;
        } else if (GetKeyDown(KeyBind.GUIRight, true) || x > 0.5f) {
            return GUIDirection.Right;
        }
        float y = -GetAxis(AxisBind.GUIUpDown, true);
        if (GetKeyDown(KeyBind.GUIUp, true) || y < -0.5f) {
            return GUIDirection.Up;
        } else if (GetKeyDown(KeyBind.GUIDown, true) || y > 0.5f) {
            return GUIDirection.Down;
        }
        return GUIDirection.None;
    }

    public static void InvertAxis(AxisBind axisBind, bool gamepad) {
        var invertSet = (gamepad ? gamepadAxisInvertSet : axisInvertSet);
        if (invertSet.Contains(axisBind)) {
            invertSet.Remove(axisBind);
        } else {
            invertSet.Add(axisBind);
        }
    }

    private static float GetGamepadAxis(string axisName) {
        //Make sure gamepad is connected
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) { return 0.0f; }

        //Get the corresponding axis
        switch (axisName) {
            case "RightStickX":
                return gamepad.rightStick.x.ReadValue();
            case "RightStickY":
                return gamepad.rightStick.y.ReadValue();
            case "LeftStickX":
                return gamepad.leftStick.x.ReadValue();
            case "LeftStickY":
                return gamepad.leftStick.y.ReadValue();
            case "DpadX":
                return gamepad.dpad.x.ReadValue();
            case "DpadY":
                return gamepad.dpad.y.ReadValue();
            case "Trigger":
                return gamepad.leftTrigger.ReadValue() - gamepad.rightTrigger.ReadValue();
            default:
                LogReport.Error("Invalid axis: " + axisName);
                return 0.0f;
        }
    }

    private static bool GetGamepadButton(KeyCode key, bool held) {
        //Make sure gamepad is connected
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) { return false; }

        //Get the corresponding button
        switch (key) {
            case ButtonNorth:
                return (held ? gamepad.buttonNorth.isPressed : gamepad.buttonNorth.wasPressedThisFrame);
            case ButtonEast:
                return (held ? gamepad.buttonEast.isPressed : gamepad.buttonEast.wasPressedThisFrame);
            case ButtonWest:
                return (held ? gamepad.buttonWest.isPressed : gamepad.buttonWest.wasPressedThisFrame);
            case ButtonSouth:
                return (held ? gamepad.buttonSouth.isPressed : gamepad.buttonSouth.wasPressedThisFrame);
            case LeftShoulder:
                return (held ? gamepad.leftShoulder.isPressed : gamepad.leftShoulder.wasPressedThisFrame);
            case RightShoulder:
                return (held ? gamepad.rightShoulder.isPressed : gamepad.rightShoulder.wasPressedThisFrame);
            case StartButton:
                return (held ? gamepad.startButton.isPressed : gamepad.startButton.wasPressedThisFrame);
            case SelectButton:
                return (held ? gamepad.selectButton.isPressed : gamepad.selectButton.wasPressedThisFrame);
            case RightStickButton:
                return (held ? gamepad.rightStickButton.isPressed : gamepad.rightStickButton.wasPressedThisFrame);
            case LeftStickButton:
                return (held ? gamepad.leftStickButton.isPressed : gamepad.leftStickButton.wasPressedThisFrame);
            default:
                LogReport.Error("Invalid button: " + key);
                return false;
        }
    }
}
