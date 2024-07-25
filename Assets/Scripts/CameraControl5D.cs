//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
#define USE_5D
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl5D : BasicCamera5D {
    public const float MOVE_SPEED = 60.0f;
    public const float JUMP_SPEED = 6.0f;
    public const float PLAYER_RADIUS = 0.3f;
    public const float CAM_HEIGHT = 1.62f;
    public const float VOLUME_TIME = 0.75f;
    public const float GRAVITY_RATE = 90.0f; //Degrees / Sec
    public const float GRAVITY_SMOOTH = 0.25f;
    public const float ZOOM_RATE = 1.1f;
    public const float ZOOM_MAX = 8.0f;
    public const float ZOOM_MIN = 0.3f;
    public const float ZOOM_SMOOTH = 0.05f;
    public static int PROJECTION_MODE = 0;

    public CompassContainer compass;
    public float lookYZ = 0.0f;

    [System.NonSerialized] public Quaternion m0Quaternion = Quaternion.identity;
    [System.NonSerialized] public Isocline m1 = Isocline.identity;
    [System.NonSerialized] public bool locked = false;
    [System.NonSerialized] public bool lockViews = false; //Locks volume, shadow, and slice views
    [System.NonSerialized] public bool volumeMode = false;

    private Vector5 smoothGravityDirection = (Vector5)Vector3.up;
    private Vector5 intermediateGravityDirection = (Vector5)Vector3.up;
    protected Matrix5x5 gravityMatrix = Matrix5x5.identity;
    protected bool fastGravity = false;
    private float volumeInterp = 0.0f;
    protected float volumeSmooth = 0.0f;
    protected float volumeStartYZ = 0.0f;
    protected float smoothAngX = 0.0f;
    protected float smoothAngY = 0.0f;
    protected float zoom = 1.0f;
    protected float targetZoom = 1.0f;
    protected float volumeHeight = 0.05f;
    protected float runMultiplier = 2.0f;
    private bool oddFrame = true;

    [System.NonSerialized] public bool sliceEnabled = true;
    [System.NonSerialized] public int shadowMode = 1;

    private Quaternion vrLastOrientation = Quaternion.identity;
    private Vector3 vrLastPosition = Vector3.zero;
    private Isocline vrM1Unfiltered = Isocline.identity;
    public static Quaternion vrCompassShift = Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, 0, 180);

    protected override void Start() {
        base.Start();
        InputManager.HideCursor(true);
        colliderRadius = PLAYER_RADIUS;
    }

    public override void Reset() {
        base.Reset();
        lookYZ = 0.0f;
        m0Quaternion = Quaternion.identity;
        m1 = Isocline.identity;
        locked = false;
        smoothAngX = 0.0f;
        smoothAngY = 0.0f;
        volumeMode = false;
        volumeInterp = 0.0f;
        volumeSmooth = 0.0f;
        volumeStartYZ = 0.0f;
        smoothGravityDirection = (Vector5)Vector3.up;
        intermediateGravityDirection = (Vector5)Vector3.up;
        gravityMatrix = Matrix5x5.identity;
        fastGravity = false;
        zoom = 1.0f;
        targetZoom = 1.0f;
    }

    public virtual void StartGame() {}

    public float CamHeight() {
        //In VR, camera height is built-in, don't need to add anything extra.
        float headHeight = (UnityEngine.XR.XRSettings.enabled ? 0.0f : CAM_HEIGHT);
        return Mathf.Lerp(headHeight, volumeHeight, volumeSmooth);
    }

    public bool IsVolumeTransition() {
        return (volumeInterp != 0.0f && volumeInterp != 1.0f);
    }

    public void HandleLooking() {
        //Apply looking
        float mouseSmooth = Mathf.Pow(2.0f, -Time.deltaTime / InputManager.CAM_SMOOTHING);
        float angX = InputManager.GetAxis(InputManager.AxisBind.LookHorizontal);
        float angY = InputManager.GetAxis(InputManager.AxisBind.LookVertical);
        if (Time.deltaTime == 0.0f) {
            smoothAngX = 0.0f;
            smoothAngY = 0.0f;
        } else {
            smoothAngX = smoothAngX * mouseSmooth + angX * (1.0f - mouseSmooth);
            smoothAngY = smoothAngY * mouseSmooth + angY * (1.0f - mouseSmooth);
        }

        //Update rotations
        if (UnityEngine.XR.XRSettings.enabled) {
            if (VRInputManager.GetKeyDown(VRInputManager.VRButton.Rotate)) {
                VRInputManager.HandOrientation(VRInputManager.Hand.Left, out vrLastOrientation);
                vrM1Unfiltered = m1;
                vrLastOrientation = vrCompassShift * vrLastOrientation;
            }
            if (VRInputManager.GetKey(VRInputManager.VRButton.Rotate)) {
                if (volumeMode) {
                    //TODO: Handle volume mode
                } else {
                    if (VRInputManager.HandOrientation(VRInputManager.Hand.Left, out Quaternion newOrientation)) {
                        newOrientation = vrCompassShift * newOrientation;
#if USE_5D
                        //TODO: Handle 5D
#else
                        vrM1Unfiltered = vrM1Unfiltered * vrLastOrientation * Quaternion.Inverse(newOrientation);
                        m1 = Quaternion.Slerp(vrM1Unfiltered, m1, Mathf.Pow(2.0f, -8.0f * Time.deltaTime));
#endif
                        vrLastOrientation = newOrientation;
                    } else {
                        vrLastOrientation = Quaternion.identity;
                    }
                }
            }
        } else {
            if (InputManager.GetKey(InputManager.KeyBind.Look4D)) {
                if (volumeMode) {
                    m1 = m1 * Quaternion.Euler(0.0f, smoothAngX, 0.0f);
                } else {
                    m1 = m1 * Quaternion.Euler(-smoothAngY, smoothAngX, 0.0f);
                }
#if USE_5D
            } else if (InputManager.GetKey(InputManager.KeyBind.Look5D)) {
                Quaternion q = Quaternion.Euler(-smoothAngX, -smoothAngY, 0.0f);
                m1 = m1 * new Isocline(q, q);
            } else if (InputManager.GetKey(InputManager.KeyBind.LookSpin)) {
                Quaternion q = Quaternion.Euler(0.0f, 0.0f, smoothAngX);
                m1 = m1 * new Isocline(q, q);
#endif
            } else {
                if (volumeMode) {
                    m1 = m1 * Quaternion.Euler(-smoothAngY, 0.0f, -smoothAngX);
                } else {
                    m1 = m1 * Quaternion.Euler(0.0f, 0.0f, -smoothAngX);
                    lookYZ += smoothAngY;
                    lookYZ = Mathf.Clamp(lookYZ, -89.0f, 89.0f);
                }
            }
        }
    }

    public Vector5 HandleMoving() {
        //Get movement force and jump
        Vector5 accel = Vector5.zero;
        accel.x = InputManager.GetAxis(InputManager.AxisBind.MoveLeftRight);
        if (InputManager.GetKey(InputManager.KeyBind.Left)) {
            accel.x = -1.0f;
        }
        if (InputManager.GetKey(InputManager.KeyBind.Right)) {
            accel.x = 1.0f;
        }
        accel.z = InputManager.GetAxis(InputManager.AxisBind.MoveForwardBack);
        if (InputManager.GetKey(InputManager.KeyBind.Backward)) {
            accel.z = -1.0f;
        }
        if (InputManager.GetKey(InputManager.KeyBind.Forward)) {
            accel.z = 1.0f;
        }
        accel.w = InputManager.GetAxis(InputManager.AxisBind.MoveAnaKata);
        if (InputManager.GetKey(InputManager.KeyBind.Kata)) {
            accel.w = -1.0f;
        }
        if (InputManager.GetKey(InputManager.KeyBind.Ana)) {
            accel.w = 1.0f;
        }
        accel.y = volumeSmooth * accel.w;
        accel.w = (volumeSmooth - 1.0f) * accel.w;
#if USE_5D
        accel.v = InputManager.GetAxis(InputManager.AxisBind.MoveSursumDeorsum);
        if (InputManager.GetKey(InputManager.KeyBind.Sursum)) {
            accel.v = -1.0f;
        }
        if (InputManager.GetKey(InputManager.KeyBind.Deorsum)) {
            accel.v = 1.0f;
        }
#endif
        if (accel != Vector5.zero) {
            accel = camMatrix * accel;
            float mag = Mathf.Min(accel.magnitude, 1.0f);
            if (useGravity) {
                accel -= smoothGravityDirection * Vector5.Dot(smoothGravityDirection, accel);
            }
            accel = accel.normalized * mag;
            if (InputManager.GetKey(InputManager.KeyBind.Run)) {
                accel *= runMultiplier;
            }
        }

        return accel;
    }

    protected virtual void Update() {
        //Update gravity matrix
        float maxAngle = Time.deltaTime * GRAVITY_RATE;
        float gravitySmooth = Mathf.Pow(2.0f, -Time.deltaTime / GRAVITY_SMOOTH);
        float angle = Transform5D.Angle(gravityDirection, smoothGravityDirection);
        if (fastGravity || angle > 60.0f) {
            intermediateGravityDirection = gravityDirection;
        } else {
            intermediateGravityDirection = Transform5D.RotateTowards(intermediateGravityDirection, gravityDirection, maxAngle);
            angle = Transform5D.Angle(intermediateGravityDirection, smoothGravityDirection);
        }
        smoothGravityDirection = Transform5D.RotateTowards(intermediateGravityDirection, smoothGravityDirection, gravitySmooth * angle);
        gravityMatrix = Transform5D.OrthoIterate(Transform5D.FromToRotation(gravityMatrix.GetColumn(1), smoothGravityDirection) * gravityMatrix);

        //Check if shadows should be enabled/disabled
        if (!lockViews && InputManager.GetKeyDown(InputManager.KeyBind.ShadowToggle)) {
            shadowMode = (shadowMode + 1) % 3;
            UpdateCameraMask();
        }
        if (!lockViews && InputManager.GetKeyDown(InputManager.KeyBind.SliceToggle)) {
            sliceEnabled = !sliceEnabled;
            UpdateCameraMask();
        }

        //Check for volume mode change
        if (!lockViews && InputManager.GetKeyDown(InputManager.KeyBind.VolumeView)) {
            if (!volumeMode) { volumeStartYZ = lookYZ; }
            volumeMode = !volumeMode;
        }

        //Interpolate volume change
        volumeInterp = Mathf.Clamp01(volumeInterp + (volumeMode ? Time.deltaTime : -Time.deltaTime) / VOLUME_TIME);
        volumeSmooth = Mathf.SmoothStep(0.0f, 1.0f, volumeInterp);
        Shader.SetGlobalFloat(minCheckerID, volumeSmooth * 0.125f);
        if (volumeMode) {
            lookYZ = Mathf.Lerp(volumeStartYZ, 0.0f, volumeSmooth);
        }

        //Handle camera and player inputs or seek
        if (!locked) {
            HandleLooking();
            velocity += HandleMoving() * (Time.deltaTime * MOVE_SPEED);
        }

        //Update compasses
        if (compass) {
            compass.SetRotations(m0Quaternion, m1);
        }

        //Disable overriding up-down look in VR
        if (UnityEngine.XR.XRSettings.enabled) {
            lookYZ = 0.0f;
        }

        //Create the camera matrix
        camMatrix = CreateCamMatrix(m1, lookYZ);

        //Update the m0 quaternion
        m0Quaternion = Quaternion.Slerp(Quaternion.Euler(-lookYZ, 0.0f, 0.0f), Quaternion.Euler(0.0f, 0.0f, 90.0f), volumeSmooth);
    }

    protected virtual void FixedUpdate() {
        //Handle the physics for the player
        //NOTE: Player doesn't move quickly, it's okay to update every other frame.
        oddFrame = !oddFrame;
        if (!locked && oddFrame) {
            //Double the time-step since this only happens every other frame.
            colliderPosition5D = UpdatePhysics(colliderPosition5D, 2.0f * Time.fixedDeltaTime);
        }
    }

    protected virtual void UpdateZoom() {
        targetZoom *= Mathf.Pow(ZOOM_RATE, -InputManager.GetAxis(InputManager.AxisBind.Zoom));
        targetZoom = Mathf.Clamp(targetZoom, ZOOM_MIN, ZOOM_MAX);
        float zoomSmooth = Mathf.Pow(2.0f, -Time.deltaTime / ZOOM_SMOOTH);
        zoom = zoom * zoomSmooth + targetZoom * (1.0f - zoomSmooth);
    }

    public override Vector5 camPosition5D {
        get {
            Vector5 result = position5D;
            result += smoothGravityDirection * CamHeight();
            return result;
        }
        set {
            Vector5 result = value;
            result -= smoothGravityDirection * CamHeight();
            position5D = result;
        }
    }

    public Vector5 colliderPosition5D {
        get {
            Vector5 result = position5D;
            result += smoothGravityDirection * colliderRadius;
            return result;
        }
        set {
            Vector5 result = value;
            result -= smoothGravityDirection * colliderRadius;
            position5D = result;
        }
    }

    public Matrix5x5 CreateCamMatrix(Isocline m1Rot, float yz) {
        //Up-Forward
        Matrix5x5 mainRot = Transform5D.Slerp(Transform5D.PlaneRotation(yz, 1, 2), Transform5D.PlaneRotation(90.0f, 1, 3), volumeSmooth);

        //Combine with secondary rotation
        return gravityMatrix * Transform5D.SkipY(m1Rot) * mainRot;
    }

    public void UpdateCameraMask() {
        //NOTE: Using the static variable from CameraControl4D intentionally so it affects both
        if (!sliceEnabled && shadowMode == 0) { shadowMode = 1; }
        if (CameraControl4D.PROJECTION_MODE != 0 && shadowMode == 1) { shadowMode = 2; }
        UpdateCameraMask(shadowMode, sliceEnabled);
    }
}
