//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
#define USE_5D
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Occlusion5D : MonoBehaviour {
    public static readonly int shadowDistID = Shader.PropertyToID("_ShadowDist");
    public static readonly int vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Mesh5D.Vertex5D));
    public Vector5 bCenter = Vector5.zero;
    public float bRadius = -1.0f;

    private ShadowFilter sf;
    private BasicCamera5D cc;
    [System.NonSerialized] public Object5D obj5D;

    private MeshFilter mf = null;
    private MeshRenderer mr = null;
    private Mesh origMesh = null;
    private float shadowDist = 0.0f;
    private int reflectionLayer = 0;

    protected void Awake() {
        reflectionLayer = LayerMask.NameToLayer("Reflection");
        sf = GetComponent<ShadowFilter>();
        mr = GetComponent<MeshRenderer>();
        obj5D = GetComponent<Object5D>();
        cc = FindObjectOfType<BasicCamera5D>();
        Debug.Assert(mr != null);
        Debug.Assert(obj5D != null);

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

    public bool Overlaps(Vector5 worldPt, float r) {
        Transform5D localToWorld5D = obj5D.WorldTransform5D();
        float d2 = (localToWorld5D * bCenter - worldPt).sqrMagnitude;
        r += localToWorld5D.MaxScale() * bRadius;
        return d2 < r * r;
    }

    protected void LateUpdate() {
        //Make sure there's an active camera controller before attempting occlusion updates
        if (!cc) { return; }

        //Get the 4D world transform for the object
        Transform5D localToWorld5D = obj5D.WorldTransform5D();
        Matrix5x5 camTranspose = cc.xrCamMatrix.transpose;
        Vector5 camPos = cc.camPosition5D;

        //Reflect occlusion
        //TODO: Don't hard-code reflection plane
        if (gameObject.layer == reflectionLayer) {
            localToWorld5D.translation.y = 2.0f * (-0.1f) - localToWorld5D.translation.y;
            localToWorld5D.matrix.SetRow(1, -localToWorld5D.matrix.GetRow(1));
        }

        //Bounds check
        float tanFOVY = Mathf.Tan(cc.sliceCam.fieldOfView * Mathf.Deg2Rad * 0.5f);
        float tanFOVX = tanFOVY * cc.sliceCam.aspect;
        CheckOcclusion(camTranspose, camPos,
                       localToWorld5D.matrix, localToWorld5D.translation, localToWorld5D.MaxScale(),
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

    public static void CheckOcclusion(Matrix5x5 camTranspose, Vector5 camPos,
                                      Matrix5x5 objMatrix, Vector5 objVec, float objScale,
                                      Vector5 center, float radius, float shadowDist,
                                      float tanFOVX, float tanFOVY,
                                      out bool disableMesh, out bool disableShadow) {
        //Transform bounding sphere to camera coordinates
        Vector5 worldPt = objMatrix * center + objVec;
        float worldScale = objScale * radius;
        Vector5 camPt = camTranspose * (worldPt - camPos);

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

    public static void ComputeBoundingSphere(Mesh mesh5D, out Vector5 center, out float radius) {
        //Scan through the 4D mesh
        HashSet<Vector5> vertHash = new HashSet<Vector5>();
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh5D)) {
            if (meshData[0].GetVertexBufferStride(0) == vertexSize) {
                NativeArray<Mesh5D.Vertex5D> verts = meshData[0].GetVertexData<Mesh5D.Vertex5D>(0);
                Debug.Assert(verts.Length % 5 == 0);
                for (int i = 0; i < verts.Length; i += 5) {
                    Mesh5D.Vertex5D v = verts[i];
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
                NativeArray<Mesh5D.Shadow5D> verts = meshData[0].GetVertexData<Mesh5D.Shadow5D>(0);
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
        Vector5[] allVerts = new Vector5[vertHash.Count];
        vertHash.CopyTo(allVerts);
        ComputeBoundingSphere(allVerts, out center, out radius);
    }

    //Use "Ritter's Bounding Sphere" algorithm
    public static void ComputeBoundingSphere(Vector5[] allVerts, out Vector5 center, out float radius) {
        Vector5 x = allVerts[0];
        Vector5 y = FarthestFrom(x, allVerts);
        Vector5 z = FarthestFrom(y, allVerts);
        center = (y + z) * 0.5f;
        radius = (y - z).magnitude * 0.5f;
        for (int i = 0; i < allVerts.Length; ++i) {
            Vector5 delta = allVerts[i] - center;
            float dist = delta.magnitude;
            if (dist > radius) {
                float expand = (dist - radius) * 0.5f;
                center += delta.normalized * expand;
                radius += expand;
            }
        }
    }

    private static Vector5 FarthestFrom(Vector5 pt, Vector5[] allVerts) {
        float farthestDistSq = 0.0f;
        Vector5 farthest = pt;
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
