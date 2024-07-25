#define USE_<D>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//Important: ObjectND should not have it's parent reassigned after creation!
[ExecuteAlways][DisallowMultipleComponent]
public class ObjectND : MonoBehaviour {
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
    [SerializeField] private MATRIX _localRotation<D>;
    private bool isAwake = false;
#if USE_4D
    private Matrix4x4 tempMatrixIT;
#endif

    private Transform transf;
    private Transform<D> cachedLocalTransform<D>;
    private Object<D> parent;

    public void Awake() {
        if (!isAwake) {
            //Cache to remove component lookup in profiling
            transf = transform;
            RefreshMaterials();

            //Initialize the camera matrix
            if (!matrixRotationOnly) {
                _localRotation<D> = MakeRotation();
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
                _localRotation<D> = MakeRotation();
            }
            transf.hasChanged = true;
        }
    }
#endif

    public MATRIX MakeRotation() {
#if USE_4D
        MATRIX result = Isocline.FromDual(transf.localRotation, Quaternion.Euler(dualEuler)).matrix;
#elif USE_5D
        MATRIX result = Transform<D>.FromQuaternion(transf.localRotation);
        if (wRot.x != 0.0f) result = result * Transform<D>.PlaneRotation(wRot.x, 0, 3);
        if (wRot.y != 0.0f) result = result * Transform<D>.PlaneRotation(wRot.y, 1, 3);
        if (wRot.z != 0.0f) result = result * Transform<D>.PlaneRotation(wRot.z, 2, 3);
        if (vRot.x != 0.0f) result = result * Transform<D>.PlaneRotation(vRot.x, 0, 4);
        if (vRot.y != 0.0f) result = result * Transform<D>.PlaneRotation(vRot.y, 1, 4);
        if (vRot.z != 0.0f) result = result * Transform<D>.PlaneRotation(vRot.z, 2, 4);
        if (vwRot != 0.0f) result = result * Transform<D>.PlaneRotation(vwRot, 3, 4);
#endif
        return result;
    }

    void LateUpdate() {
        //Only need to update mesh property blocks if rendering
        if (!mr) { return; }

        //Get the world transformation
        Transform<D> transform<D> = WorldTransform<D>();

        //Make sure property block is created
        if (propBlock == null) {
            propBlock = new MaterialPropertyBlock();
        }

#if USE_5D
        //HACK: Add a small epsilon since there's a weird edge case I hope to investigate later.
        VECTOR translation = transform<D>.translation;
        translation.v += 1e-6f;

        //Break up 5x5 into pieces the GPU can handle
        transform<D>.matrix.ToShaderVars(out Matrix4x4 mat, out Vector4 matC4, out Vector4 matR4, out float matVV);
#endif

        //Apply model matrix parameters
        for (int i = 0; i < numMaterials; ++i) {
            mr.GetPropertyBlock(propBlock, i);
#if USE_4D
            if (i == 0) { tempMatrixIT = transform<D>.matrix.inverse.transpose; }
            propBlock.SetMatrix(ModelMatrixID, transform<D>.matrix);
            propBlock.SetMatrix(ModelMatrixITID, tempMatrixIT);
            propBlock.SetVector(ModelPositionID, transform<D>.translation);
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

    public VECTOR localPosition<D> {
        get {
            VECTOR p = (VECTOR)transf.localPosition;
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

    public VECTOR worldPosition<D> {
        get {
            return WorldTransform<D>().translation;
        }
    }

    //Object<D> must be awake for localRotation to be valid
    public MATRIX localRotation<D> {
        get {
            Awake();
            return _localRotation<D>;
        }
        set {
            Awake();
            _localRotation<D> = value;
            Transform<D>.MakeOrthoNormal(ref _localRotation<D>);
            transf.hasChanged = true;
        }
    }

    public VECTOR localScale<D> {
        get {
            VECTOR s = (VECTOR)transf.localScale;
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

    public Transform<D> LocalTransform<D>() {
        if (transf.hasChanged) {
            cachedLocalTransform<D> = new Transform<D>(localRotation<D>, localPosition<D>, localScale<D>);
            transf.hasChanged = false;
        }
        return cachedLocalTransform<D>;
    }

    public Transform<D> WorldTransform<D>() {
        //Start with the local transformation
        Transform<D> worldTransform<D> = LocalTransform<D>();

        //Apply parent transforms to get world transformation
        Object<D> iterParent = parent;
        while (iterParent != null) {
            worldTransform<D> = iterParent.LocalTransform<D>() * worldTransform<D>;
            iterParent = iterParent.parent;
        }

        //Return the result
        return worldTransform<D>;
    }

    private static Object<D> FindParent(Transform self) {
        Transform parent = self.parent;
        if (parent == null) { return null; }
        Object<D> parent<D> = parent.GetComponent<Object<D>>();
        if (parent<D>) { return parent<D>; }
        return FindParent(parent);
    }

    public ObjectND GetParent() {
        Awake();
        return parent;
    }
}
