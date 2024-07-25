using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompassContainer : MonoBehaviour {
    public static readonly int ColorID = Shader.PropertyToID("_Color");
    public static readonly int TextColorID = Shader.PropertyToID("_FaceColor");
    public static readonly int CMatrixID = Shader.PropertyToID("_CMatrix");
    public const string directionNames = "EWNSKA$D";
    public const float H = 1.01f;
    public static readonly Color[] axisColors = new Color[] {
        new Color(1.0f, 0.0f, 0.0f),
        new Color(0.0f, 1.0f, 0.0f),
        new Color(0.0f, 0.0f, 1.0f),
        new Color(1.0f, 1.0f, 0.0f),
        new Color(1.0f, 0.0f, 1.0f),
        new Color(0.0f, 1.0f, 1.0f),
    };
    public enum CompassType {
        Isoclinic = 0,
        Stereographic = 1,
        Disabled = 2,
    }

    public static CompassType compassType = CompassType.Isoclinic;
    public static bool useVolumeLine = false;

    public bool is5D = false;
    public GameObject compass1;
    public GameObject compass2;
    public GameObject compass3;
    public GameObject compassSP;
    public GameObject textSPPrefab;
    public GameObject dotSPPrefab;
    public Image volumeLine;

    [System.NonSerialized] public Quaternion lastOrientation4D = Quaternion.identity;
    [System.NonSerialized] public Isocline lastOrientation5D = Isocline.identity;

    private struct CompassSPObj {
        public CompassSPObj(Vector4 direction, Color color, TMP_Text text) {
            this.direction = direction;
            this.color = color;
            this.text = text;
            mr = text.GetComponent<MeshRenderer>();
            Debug.Assert(mr != null);
        }
        public Vector4 direction;
        public Color color;
        public TMP_Text text;
        public MeshRenderer mr;
    }

    public Mesh dotMesh;
    public Material dotMaterial;
    private int numDots;
    private Camera uiCam;
    private GameObject volumeLineObj;

    private List<CompassSPObj> spObjs = new();
    private MaterialPropertyBlock dotMPB;
    private MaterialPropertyBlock textMPB;
    private bool hide5D = false;

    private Matrix4x4[] propParent;
    private Matrix4x4[] propRotation;
    private Vector4[] propColor;
    private Matrix4x4[] propStartingRot;

    protected void Awake() {
        InitializeCompassSP();
        UpdateStyle();
        dotMPB = new MaterialPropertyBlock();
        textMPB = new MaterialPropertyBlock();
        uiCam = GameObject.FindGameObjectWithTag("UICamera")?.GetComponent<Camera>();
        Debug.Assert(uiCam != null);

        if (UnityEngine.XR.XRSettings.enabled) {
            //Disable all children in VR
            foreach (Transform child in transform) {
                child.gameObject.SetActive(false);
            }
        } else {
            //Move the volume line to a separate, non-scaled canvas
            if (volumeLine) {
                volumeLineObj = new GameObject("VolumeLineObj");
                volumeLineObj.layer = volumeLine.gameObject.layer;
                volumeLineObj.transform.parent = transform.parent.parent;
                Canvas origCanvas = transform.parent.GetComponent<Canvas>();
                Canvas canvas = volumeLineObj.AddComponent<Canvas>();
                canvas.renderMode = origCanvas.renderMode;
                canvas.worldCamera = origCanvas.worldCamera;
                canvas.planeDistance = origCanvas.planeDistance;
                volumeLine.transform.SetParent(volumeLineObj.transform, false);
            }
        }
    }

    protected void OnDestroy() {
        //Clean up volume line when destroyed
        if (volumeLineObj) { Destroy(volumeLineObj); }
    }

    public void SetRotations(Quaternion tilt, Quaternion orientation) {
        lastOrientation4D = orientation;
        compass1.transform.localRotation = Quaternion.Inverse(tilt);
        compass2.transform.localRotation = Quaternion.Inverse(orientation);
        SetRotationsSP(Quaternion.Inverse(orientation));
    }

    public void SetRotations(Quaternion tilt, Isocline orientation) {
        lastOrientation5D = orientation;
        compass1.transform.localRotation = Quaternion.Inverse(tilt);
        compass2.transform.localRotation = orientation.qL;
        compass3.transform.localRotation = Quaternion.Inverse(orientation.qR);
        SetRotationsSP(Isocline.Inverse(orientation));
    }

    public void UpdateVolume(float volume) {
        if (volumeLine) {
            Color color = volumeLine.color;
            color.a = 0.5f * (1.0f - Mathf.Clamp01(10.0f * (1.0f - volume)));
            volumeLine.color = color;
            volumeLine.gameObject.SetActive(useVolumeLine);
        }
    }

    private void InitializeCompassSP() {
        int compassDims = (is5D ? 4 : 3);
        for (int i = 0; i < compassDims; ++i) {
            Vector4 direction = Vector4.zero;
            Color color = Color.black;

            //Add first pole in this dimension
            direction[i] = 1.0f;
            GameObject text1Obj = Instantiate(textSPPrefab, compassSP.transform);
            TMP_Text text1 = text1Obj.GetComponent<TMP_Text>();
            text1.text = directionNames[i * 2].ToString();
            spObjs.Add(new CompassSPObj(direction, color, text1));

            //Add second pole in this dimension
            direction[i] = -1.0f;
            GameObject textObj2 = Instantiate(textSPPrefab, compassSP.transform);
            TMP_Text text2 = textObj2.GetComponent<TMP_Text>();
            text2.text = directionNames[i * 2 + 1].ToString();
            spObjs.Add(new CompassSPObj(direction, color, text2));
        }

        //Initialize prop arrays
        numDots = 8 * compassDims * (compassDims - 1) / 2;
        propParent = new Matrix4x4[numDots];
        propRotation = new Matrix4x4[numDots];
        propColor = new Vector4[numDots];
        propStartingRot = new Matrix4x4[numDots];

        //Add intermediates
        int axisIx = 0;
        int dotIx = 0;
        for (int i = 0; i < compassDims; ++i) {
            for (int j = 0; j < i; ++j) {
                Color color = axisColors[axisIx++];
                AddLines(i, j, color, ref dotIx);
            }
        }
        Debug.Assert(numDots == dotIx);
    }

    private void AddLines(int d1, int d2, Color color, ref int dotIx) {
        Matrix4x4 planeRot = Transform4D.PlaneRotation(90.0f, d1, d2);
        if (d1 == 3 && d2 == 1) {
            planeRot = Transform4D.PlaneRotation(90.0f, 2, 3) * Transform4D.PlaneRotation(90.0f, 0, 1);
        }
        for (int i = 0; i < 4; ++i) {
            propStartingRot[dotIx] = planeRot * Transform4D.PlaneRotation(45.0f + i*90.0f, 0, 2);
            propColor[dotIx] = color;
            dotIx += 1;
            propStartingRot[dotIx] = planeRot * Transform4D.PlaneRotation(45.0f + i*90.0f, 0, 2) * Transform4D.PlaneRotation(90.0f, 1, 3);
            propColor[dotIx] = color;
            dotIx += 1;
        }
    }

    private void SetRotationsSP(Quaternion orientation) {
        if (!compassSP.activeInHierarchy) { return; }
        Matrix4x4 mat = Matrix4x4.Rotate(orientation);
        mat.SetRow(2, -mat.GetRow(2));
        for (int i = 0; i < propStartingRot.Length; ++i) {
            propRotation[i] = mat * propStartingRot[i];
        }
        dotMPB.SetMatrixArray(CMatrixID, propRotation);
        dotMPB.SetVectorArray(ColorID, propColor);
        for (int i = 0; i < spObjs.Count; ++i) {
            Vector4 v = orientation * spObjs[i].direction;
            v.z = -v.z;
            SetRotationSP(spObjs[i], v);
        }
    }
    private void SetRotationsSP(Isocline orientation) {
        if (!compassSP.activeInHierarchy) { return; }
        Matrix4x4 mat = orientation.matrix;
        mat.SetRow(2, -mat.GetRow(2));
        for (int i = 0; i < propStartingRot.Length; ++i) {
            propRotation[i] = mat * propStartingRot[i];
        }
        dotMPB.SetMatrixArray(CMatrixID, propRotation);
        dotMPB.SetVectorArray(ColorID, propColor);
        for (int i = 0; i < spObjs.Count; ++i) {
            Vector4 v = orientation * spObjs[i].direction;
            v.z = -v.z;
            SetRotationSP(spObjs[i], v);
        }
    }

    private void SetRotationSP(CompassSPObj spObj, Vector4 direction) {
        MeshRenderer mr = spObj.mr;
        float a = (H + 1.0f) / (H + direction.y);
        direction *= a;
        Vector3 flatDirection = new Vector3(direction.x, direction.z, direction.w);
        mr.gameObject.transform.localPosition = 0.45f * flatDirection;
        //obj.transform.localScale = Vector3.one * (0.08f * (1.0f - flatDirection.magnitude * 0.25f));
        mr.gameObject.SetActive(a > 0.0f && a < 2.9f);
        mr.GetPropertyBlock(textMPB);
        spObj.color.a = Mathf.Max(1.0f - Mathf.Abs(direction.w) * 0.5f, 0.0f);
        textMPB.SetColor(TextColorID, spObj.color);
        mr.SetPropertyBlock(textMPB);
    }

    public void UpdateStyle() {
        switch (compassType) {
            case CompassType.Isoclinic:
                compass1.transform.parent.gameObject.SetActive(true);
                compass2.transform.parent.gameObject.SetActive(true);
                compass3.transform.parent.gameObject.SetActive(is5D && !hide5D);
                compassSP.transform.parent.gameObject.SetActive(false);
                break;
            case CompassType.Stereographic:
                compass1.transform.parent.gameObject.SetActive(true);
                compass2.transform.parent.gameObject.SetActive(false);
                compass3.transform.parent.gameObject.SetActive(false);
                compassSP.transform.parent.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                break;
            case CompassType.Disabled: default:
                compass1.transform.parent.gameObject.SetActive(false);
                compass2.transform.parent.gameObject.SetActive(false);
                compass3.transform.parent.gameObject.SetActive(false);
                compassSP.transform.parent.gameObject.SetActive(false);
                break;
        }
    }

    public void LateUpdate() {
        if (compassSP.transform.parent.gameObject.activeSelf) {
            int num = (is5D && hide5D) ? (numDots / 2) : numDots;
            Array.Fill(propParent, compassSP.transform.localToWorldMatrix);
            Graphics.DrawMeshInstanced(dotMesh, 0, dotMaterial, propParent, num, dotMPB,
                UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer, uiCam);
        }
    }

    public void Hide5D(bool hide) {
        hide5D = hide;
        foreach (CompassSPObj spObj in spObjs) {
            if (spObj.text.text == "$" || spObj.text.text == "D") {
                spObj.mr.enabled = !hide;
            }
        }
        UpdateStyle();
    }
}
