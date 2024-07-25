//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
#define USE_4D
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//Important: Object4D should not have it's parent reassigned after creation!
[ExecuteAlways][DisallowMultipleComponent]
public class Object4D : MonoBehaviour {
    public static readonly int ModelMatrixID = Shader.PropertyToID("_ModelMatrix");
    public static readonly int ModelMatrixITID = Shader.PropertyToID("_ModelMatrixIT");
    public static readonly int ModelPositionID = Shader.PropertyToID("_ModelPosition");
#if USE_5D
    public static readonly int ModelMatrixC4ID = Shader.PropertyToID("_ModelMatrix_C4");
    public static readonly int ModelMatrixR4ID = Shader.PropertyToID("_ModelMatrix_R4");
    public static readonly int ModelMatrixVVID = Shader.PropertyToID("_ModelMatrix_VV");
    public static readonly int ModelPositionVID = Shader.PropertyToID("_ModelPosition_V");
#endif

    //Note: Only for initialization, then discarded
    public float positionW = 0.0f;
    public float scaleW = 1.0f;
#if USE_4D
    public Vector3 dualEuler = Vector3.zero;
#elif USE_5D
    public float positionV = 0.0f;
    public float scaleV = 1.0f;
    public Vector3 wRot = Vector3.zero;
    public Vector3 vRot = Vector3.zero;
    public float vwRot = 0.0f;
#endif
    public bool matrixRotationOnly = false;

    private MeshRenderer mr = null;
    private int numMaterials = 0;
    private MaterialPropertyBlock propBlock = null;
    [SerializeField] private Matrix4x4 _localRotation4D;
    private bool isAwake = false;
#if USE_4D
    private Matrix4x4 tempMatrixIT;
#endif

    private Transform transf;
    private Transform4D cachedLocalTransform4D;
    private Object4D parent;

    public void Awake() {
        if (!isAwake) {
            //Cache to remove component lookup in profiling
            transf = transform;
            RefreshMaterials();

            //Initialize the camera matrix
            if (!matrixRotationOnly) {
                _localRotation4D = MakeRotation();
            }
            transf.hasChanged = true;
            parent = FindParent(transf);
            isAwake = true;
        }
    }

    public void RefreshMaterials() {
        mr = GetComponent<MeshRenderer>();
        if (mr) {
            //Cache the material count because there's no way to access it without
            //allocating a sharedMaterial array every frame. Oh Unity...
            numMaterials = mr.sharedMaterials.Length;
#if UNITY_EDITOR
            if (EditorApplication.isPlaying) {
                Debug.Assert(GetComponent<ShadowFilter>(), "Missing shadow filter for mesh renderer on " + name);
                if (mr.sharedMaterial) {
                    Debug.Assert(mr.sharedMaterial.enableInstancing, "Material '" + mr.sharedMaterial.name + "' does not support instancing");
                }
            }
#endif
        }
    }

#if UNITY_EDITOR
    private void Update() {
        if (!EditorApplication.isPlaying) {
            if (!matrixRotationOnly) {
                _localRotation4D = MakeRotation();
            }
            transf.hasChanged = true;
        }
    }
#endif

    public Matrix4x4 MakeRotation() {
#if USE_4D
        Matrix4x4 result = Isocline.FromDual(transf.localRotation, Quaternion.Euler(dualEuler)).matrix;
#elif USE_5D
        Matrix4x4 result = Transform4D.FromQuaternion(transf.localRotation);
        if (wRot.x != 0.0f) result = result * Transform4D.PlaneRotation(wRot.x, 0, 3);
        if (wRot.y != 0.0f) result = result * Transform4D.PlaneRotation(wRot.y, 1, 3);
        if (wRot.z != 0.0f) result = result * Transform4D.PlaneRotation(wRot.z, 2, 3);
        if (vRot.x != 0.0f) result = result * Transform4D.PlaneRotation(vRot.x, 0, 4);
        if (vRot.y != 0.0f) result = result * Transform4D.PlaneRotation(vRot.y, 1, 4);
        if (vRot.z != 0.0f) result = result * Transform4D.PlaneRotation(vRot.z, 2, 4);
        if (vwRot != 0.0f) result = result * Transform4D.PlaneRotation(vwRot, 3, 4);
#endif
        return result;
    }

    void LateUpdate() {
        //Only need to update mesh property blocks if rendering
        if (!mr) { return; }

        //Get the world transformation
        Transform4D transform4D = WorldTransform4D();

        //Make sure property block is created
        if (propBlock == null) {
            propBlock = new MaterialPropertyBlock();
        }

#if USE_5D
        //HACK: Add a small epsilon since there's a weird edge case I hope to investigate later.
        Vector4 translation = transform4D.translation;
        translation.v += 1e-6f;

        //Break up 5x5 into pieces the GPU can handle
        transform4D.matrix.ToShaderVars(out Matrix4x4 mat, out Vector4 matC4, out Vector4 matR4, out float matVV);
#endif

        //Apply model matrix parameters
        for (int i = 0; i < numMaterials; ++i) {
            mr.GetPropertyBlock(propBlock, i);
#if USE_4D
            if (i == 0) { tempMatrixIT = transform4D.matrix.inverse.transpose; }
            propBlock.SetMatrix(ModelMatrixID, transform4D.matrix);
            propBlock.SetMatrix(ModelMatrixITID, tempMatrixIT);
            propBlock.SetVector(ModelPositionID, transform4D.translation);
#elif USE_5D
            propBlock.SetVector(ModelPositionID, (Vector4)translation);
            propBlock.SetFloat(ModelPositionVID, translation.v);
            propBlock.SetMatrix(ModelMatrixID, mat);
            propBlock.SetVector(ModelMatrixC4ID, matC4);
            propBlock.SetVector(ModelMatrixR4ID, matR4);
            propBlock.SetFloat(ModelMatrixVVID, matVV);
#endif
            mr.SetPropertyBlock(propBlock, i);
        }
    }

    public Vector4 localPosition4D {
        get {
            Vector4 p = (Vector4)transf.localPosition;
            p.w = positionW;
#if USE_5D
            p.v = positionV;
#endif
            return p;
        }
        set {
            transf.localPosition = (Vector3)value;
            transf.hasChanged = true;
            positionW = value.w;
#if USE_5D
            positionV = value.v;
#endif
        }
    }

    public Vector4 worldPosition4D {
        get {
            return WorldTransform4D().translation;
        }
    }

    //Object4D must be awake for localRotation to be valid
    public Matrix4x4 localRotation4D {
        get {
            Awake();
            return _localRotation4D;
        }
        set {
            Awake();
            _localRotation4D = value;
            Transform4D.MakeOrthoNormal(ref _localRotation4D);
            transf.hasChanged = true;
        }
    }

    public Vector4 localScale4D {
        get {
            Vector4 s = (Vector4)transf.localScale;
            s.w = scaleW;
#if USE_5D
            s.v = scaleV;
#endif
            return s;
        }
        set {
            transf.localScale = (Vector3)value;
            transf.hasChanged = true;
            scaleW = value.w;
#if USE_5D
            scaleV = value.v;
#endif
        }
    }

    public Transform4D LocalTransform4D() {
        if (transf.hasChanged) {
            cachedLocalTransform4D = new Transform4D(localRotation4D, localPosition4D, localScale4D);
            transf.hasChanged = false;
        }
        return cachedLocalTransform4D;
    }

    public Transform4D WorldTransform4D() {
        //Start with the local transformation
        Transform4D worldTransform4D = LocalTransform4D();

        //Apply parent transforms to get world transformation
        Object4D iterParent = parent;
        while (iterParent != null) {
            worldTransform4D = iterParent.LocalTransform4D() * worldTransform4D;
            iterParent = iterParent.parent;
        }

        //Return the result
        return worldTransform4D;
    }

    private static Object4D FindParent(Transform self) {
        Transform parent = self.parent;
        if (parent == null) { return null; }
        Object4D parent4D = parent.GetComponent<Object4D>();
        if (parent4D) { return parent4D; }
        return FindParent(parent);
    }

    public Object4D GetParent() {
        Awake();
        return parent;
    }
}
