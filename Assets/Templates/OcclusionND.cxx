#define USE_<D>
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class OcclusionND : MonoBehaviour {
    public static readonly int shadowDistID = Shader.PropertyToID("_ShadowDist");
    public static readonly int vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Mesh<D>.Vertex<D>));
    public VECTOR bCenter = VECTOR.zero;
    public float bRadius = -1.0f;

    private ShadowFilter sf;
    private BasicCamera<D> cc;
    [System.NonSerialized] public Object<D> obj<D>;

    private MeshFilter mf = null;
    private MeshRenderer mr = null;
    private Mesh origMesh = null;
    private float shadowDist = 0.0f;
    private int reflectionLayer = 0;

    protected void Awake() {
        reflectionLayer = LayerMask.NameToLayer("Reflection");
        sf = GetComponent<ShadowFilter>();
        mr = GetComponent<MeshRenderer>();
        obj<D> = GetComponent<Object<D>>();
        cc = FindObjectOfType<BasicCamera<D>>();
        Debug.Assert(mr != null);
        Debug.Assert(obj<D> != null);

        //Normally, ShadowFilter manages mesh nullification.
        //If it doesn't exist, manage it here instead.
        if (!sf) {
            mf = GetComponent<MeshFilter>();
            origMesh = mf.sharedMesh;
        }

        //Get the maximum shadow distance of the materials
        shadowDist = 0.0f;
        foreach (Material sharedMaterial in mr.sharedMaterials) {
            if (sharedMaterial == null) { continue; }
            shadowDist = Mathf.Max(shadowDist, sharedMaterial.GetFloat(shadowDistID));
        }
    }

    public bool Overlaps(VECTOR worldPt, float r) {
        Transform<D> localToWorld<D> = obj<D>.WorldTransform<D>();
        float d2 = (localToWorld<D> * bCenter - worldPt).sqrMagnitude;
        r += localToWorld<D>.MaxScale() * bRadius;
        return d2 < r * r;
    }

    protected void LateUpdate() {
        //Make sure there's an active camera controller before attempting occlusion updates
        if (!cc) { return; }

        //Get the 4D world transform for the object
        Transform<D> localToWorld<D> = obj<D>.WorldTransform<D>();
        MATRIX camTranspose = cc.xrCamMatrix.transpose;
        VECTOR camPos = cc.camPosition<D>;

        //Reflect occlusion
        //TODO: Don't hard-code reflection plane
        if (gameObject.layer == reflectionLayer) {
            localToWorld<D>.translation.y = 2.0f * (-0.1f) - localToWorld<D>.translation.y;
            localToWorld<D>.matrix.SetRow(1, -localToWorld<D>.matrix.GetRow(1));
        }

        //Bounds check
        float tanFOVY = Mathf.Tan(cc.sliceCam.fieldOfView * Mathf.Deg2Rad * 0.5f);
        float tanFOVX = tanFOVY * cc.sliceCam.aspect;
        CheckOcclusion(camTranspose, camPos,
                       localToWorld<D>.matrix, localToWorld<D>.translation, localToWorld<D>.MaxScale(),
                       bCenter, bRadius, shadowDist,
                       tanFOVX, tanFOVY,
                       out bool disableMesh, out bool disableShadow);

        //Enable or disable the mesh
        if (origMesh) {
            mf.sharedMesh = (disableMesh ? null : origMesh);
        } else {
            sf.disableMesh = disableMesh;
            sf.disableShadow = disableShadow;
        }
    }

    public static void CheckOcclusion(MATRIX camTranspose, VECTOR camPos,
                                      MATRIX objMatrix, VECTOR objVec, float objScale,
                                      VECTOR center, float radius, float shadowDist,
                                      float tanFOVX, float tanFOVY,
                                      out bool disableMesh, out bool disableShadow) {
        //Transform bounding sphere to camera coordinates
        VECTOR worldPt = objMatrix * center + objVec;
        float worldScale = objScale * radius;
        VECTOR camPt = camTranspose * (worldPt - camPos);

        //Check culling in the WV directions
        float planeDistSq = camPt.w * camPt.w;
#if USE_5D
        planeDistSq += camPt.v * camPt.v;
#endif
        disableMesh = (planeDistSq > worldScale * worldScale);
        disableShadow = (planeDistSq > (worldScale + shadowDist) * (worldScale + shadowDist));
        if (disableShadow) { return; }

        //Check culling in the XYZ directions
        bool frustrumXYZ = camPt.z + worldScale < 0.0 ||
                           Mathf.Abs(camPt.x) - tanFOVX * camPt.z > worldScale * Mathf.Sqrt(tanFOVX * tanFOVX + 1.0f) ||
                           Mathf.Abs(camPt.y) - tanFOVY * camPt.z > worldScale * Mathf.Sqrt(tanFOVY * tanFOVY + 1.0f);
        disableMesh |= frustrumXYZ;
        disableShadow |= frustrumXYZ;
    }

    public void Reset() {
        //Check if there's already a mesh filter attached
        MeshFilter mf = GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) { return; }
        ComputeBoundingSphere(mf.sharedMesh, out bCenter, out bRadius);
    }

    public static void ComputeBoundingSphere(Mesh mesh<D>, out VECTOR center, out float radius) {
        //Scan through the 4D mesh
        HashSet<VECTOR> vertHash = new HashSet<VECTOR>();
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh<D>)) {
            if (meshData[0].GetVertexBufferStride(0) == vertexSize) {
                NativeArray<Mesh<D>.Vertex<D>> verts = meshData[0].GetVertexData<Mesh<D>.Vertex<D>>(0);
                Debug.Assert(verts.Length % DIMS == 0);
                for (int i = 0; i < verts.Length; i += DIMS) {
                    Mesh<D>.Vertex<D> v = verts[i];
#if USE_5D
                    vertHash.Add(v.va5);
                    vertHash.Add(v.vb5);
                    vertHash.Add(v.vc5);
                    vertHash.Add(v.vd5);
                    vertHash.Add(v.ve5);
#else
                    vertHash.Add(v.va);
                    vertHash.Add(v.vb);
                    vertHash.Add(v.vc);
                    vertHash.Add(v.vd);
#endif
                }
            } else {
                NativeArray<Mesh<D>.Shadow<D>> verts = meshData[0].GetVertexData<Mesh<D>.Shadow<D>>(0);
                for (int i = 0; i < verts.Length; i++) {
#if USE_5D
                    vertHash.Add(verts[i].vertex5);
#else
                    vertHash.Add(verts[i].vertex);
#endif
                }
            }
        }
        Debug.Assert(vertHash.Count >= 3, "Too few points for a mesh");
        VECTOR[] allVerts = new VECTOR[vertHash.Count];
        vertHash.CopyTo(allVerts);
        ComputeBoundingSphere(allVerts, out center, out radius);
    }

    //Use "Ritter's Bounding Sphere" algorithm
    public static void ComputeBoundingSphere(VECTOR[] allVerts, out VECTOR center, out float radius) {
        VECTOR x = allVerts[0];
        VECTOR y = FarthestFrom(x, allVerts);
        VECTOR z = FarthestFrom(y, allVerts);
        center = (y + z) * 0.5f;
        radius = (y - z).magnitude * 0.5f;
        for (int i = 0; i < allVerts.Length; ++i) {
            VECTOR delta = allVerts[i] - center;
            float dist = delta.magnitude;
            if (dist > radius) {
                float expand = (dist - radius) * 0.5f;
                center += delta.normalized * expand;
                radius += expand;
            }
        }
    }

    private static VECTOR FarthestFrom(VECTOR pt, VECTOR[] allVerts) {
        float farthestDistSq = 0.0f;
        VECTOR farthest = pt;
        for (int i = 0; i < allVerts.Length; ++i) {
            float distSq = (allVerts[i] - pt).sqrMagnitude;
            if (distSq > farthestDistSq) {
                farthestDistSq = distSq;
                farthest = allVerts[i];
            }
        }
        return farthest;
    }
}
