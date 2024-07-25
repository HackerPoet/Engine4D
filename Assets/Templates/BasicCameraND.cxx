#define USE_<D>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicCameraND : Physical<D> {
    public static readonly int minCheckerID = Shader.PropertyToID("_MinChecker");
    public static readonly int ditherDistID = Shader.PropertyToID("_DitherDist");
    public static readonly int ditherRadiusID = Shader.PropertyToID("_DitherRadius");
    public static readonly int camPositionID = Shader.PropertyToID("_CamPosition");
    public static readonly int camMatrixID = Shader.PropertyToID("_CamMatrix");
#if USE_5D
    public static readonly int camPositionVID = Shader.PropertyToID("_CamPosition_V");
    public static readonly int camMatrixC4ID = Shader.PropertyToID("_CamMatrix_C4");
    public static readonly int camMatrixR4ID = Shader.PropertyToID("_CamMatrix_R4");
    public static readonly int camMatrixVVID = Shader.PropertyToID("_CamMatrix_VV");
#endif
    public static Color noSliceBackgroundColor = Color.black;
    public static Color sliceBackgroundColor = Color.white;
    public static bool useSkybox = true;

    public Shader shadowShader;

    [System.NonSerialized] public MATRIX camMatrix = MATRIX.identity;
    [System.NonSerialized] public VECTOR position<D> = VECTOR.zero;
    [System.NonSerialized] public Camera sliceCam = null;
    [System.NonSerialized] public Camera shadowCam = null;
    [System.NonSerialized] public Camera overlayCam = null;

    protected float ditherDist = 0.0f;
    protected float ditherRadius = 0.0f;

    private int sliceCullingMask;
    private int defaultLayer;
    private int transparentFXLayer;
    private int alwaysOnLayer;
    private int particleLayer;

    private MATRIX xrCamCache = MATRIX.identity;

    protected virtual void Awake() {
        position<D> = (VECTOR)transform.position;
        transform.position = Vector3.zero;
        ShadowFilter.useWireframe = false;
        ShadowFilter.UpdateGlobalOpacity();

        //Find all the cameras
        sliceCam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        shadowCam = GameObject.FindGameObjectWithTag("ShadowCamera")?.GetComponent<Camera>();
        overlayCam = GameObject.FindGameObjectWithTag("OverlayCamera")?.GetComponent<Camera>();

        //Get all camera mask layers
        sliceCullingMask = sliceCam.cullingMask;
        defaultLayer = LayerMask.GetMask("Default");
        transparentFXLayer = LayerMask.GetMask("TransparentFX");
        alwaysOnLayer = LayerMask.GetMask("AlwaysOn");
        particleLayer = LayerMask.GetMask("Particle");
    }

    protected virtual void Start() {
        Texture2D lutTexture = Resources.Load<Texture2D>("LUT<D>");
        Shader.SetGlobalTexture("_LUT", lutTexture);
        Debug.Assert(lutTexture != null);

        Texture3D noiseTexture = Resources.Load<Texture3D>("Noise");
        Shader.SetGlobalTexture("_NOISE", noiseTexture);
        Debug.Assert(noiseTexture != null);

        Shader.SetGlobalFloat(minCheckerID, 0.0f);
        Shader.SetGlobalFloat(ditherDistID, 0.0f);

#if USE_5D
        Shader.EnableKeyword("IS_5D");
#else
        Shader.DisableKeyword("IS_5D");
#endif

        Shader.DisableKeyword("IS_EDITOR");
        Shader.DisableKeyword("IS_EDITOR_V");
        if (shadowCam) {
            shadowCam.SetReplacementShader(shadowShader, "RenderType");
        }
    }

    public virtual void Reset() {
        position<D> = VECTOR.zero;
        camMatrix = MATRIX.identity;
        ditherDist = 0.0f;
        ditherRadius = 0.0f;
    }

    private void LateUpdate() {
        //Update dithering
        Shader.SetGlobalFloat(ditherDistID, ditherDist);
        Shader.SetGlobalFloat(ditherRadiusID, ditherRadius);

        //Get the view matrix
        MATRIX viewMatrix = camMatrix.transpose;
        viewMatrix.SetRow(2, -viewMatrix.GetRow(2));

#if USE_4D
        Shader.SetGlobalVector(camPositionID, -camPosition<D>);
        Shader.SetGlobalMatrix(camMatrixID, viewMatrix);
#elif USE_5D
        //Offset camera position slightly to prevent faces slicing exactly against camera plane
        Vector5 camPos = -camPosition5D;
        camPos.w += 1e-4f;
        camPos.v -= 1e-4f;

        Shader.SetGlobalVector(camPositionID, (Vector4)camPos);
        Shader.SetGlobalFloat(camPositionVID, camPos.v);

        viewMatrix.ToShaderVars(out Matrix4x4 mat, out Vector4 matC4, out Vector4 matR4, out float matVV);
        Shader.SetGlobalMatrix(camMatrixID, mat);
        Shader.SetGlobalVector(camMatrixC4ID, matC4);
        Shader.SetGlobalVector(camMatrixR4ID, matR4);
        Shader.SetGlobalFloat(camMatrixVVID, matVV);
#endif
    }
    
    public virtual VECTOR camPosition<D> {
        get { return position<D>; }
        set { position<D> = value; }
    }
    
    public void UpdateCameraMask(int shadowMode, bool sliceEnabled) {
        Debug.Assert(sliceCullingMask != 0, "Awake() must be called before UpdateCameraMask");
        if (shadowMode == 0) {
            shadowCam.enabled = false;
        } else if (shadowMode == 1) {
            ShadowFilter.useWireframe = false;
            ShadowFilter.UpdateGlobalOpacity();
            shadowCam.enabled = true;
        } else if (shadowMode == 2) {
            ShadowFilter.useWireframe = true;
            ShadowFilter.UpdateGlobalOpacity();
            shadowCam.enabled = true;
        }
        int mask = (sliceEnabled ? sliceCullingMask : alwaysOnLayer);
        if (shadowCam.enabled) {
            mask |= transparentFXLayer;
        } else {
            mask &= ~transparentFXLayer;
        }
        sliceCam.cullingMask = mask;
        sliceCam.backgroundColor = (sliceEnabled ? sliceBackgroundColor : noSliceBackgroundColor);
        sliceCam.clearFlags = (sliceEnabled && useSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor);
        overlayCam.cullingMask = (sliceEnabled ? particleLayer : 0);
    }

    public void UpdateFOV(float degrees) {
        if (sliceCam) { sliceCam.fieldOfView = degrees; }
        if (shadowCam) { shadowCam.fieldOfView = degrees; }
        if (overlayCam) { overlayCam.fieldOfView = degrees; }
    }

    public MATRIX xrCamMatrix {
        get {
            if (sliceCam.transform.hasChanged) {
                xrCamCache = Transform<D>.FromQuaternion(sliceCam.transform.localRotation);
                sliceCam.transform.hasChanged = false;
            }
            return camMatrix * xrCamCache;
        }
    }

    public bool SliceEnabled() {
        return (sliceCam.cullingMask & defaultLayer) != 0;
    }
}
