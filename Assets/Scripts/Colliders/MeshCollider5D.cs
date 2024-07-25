using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class MeshCollider5D : Collider5D {
    [System.Serializable]
    public struct Simplex {
        public Vector5 origin;
        public Matrix5x5 basis;
        public MeshCollider4D.Tetrahedron a, b, c, d, e;
        public Simplex(Vector5 va, Vector5 vb, Vector5 vc, Vector5 vd, Vector5 ve) {
            //Get origin and axes
            origin = va;
            Vector5 w1 = vb - va;
            Vector5 w2 = vc - va;
            Vector5 w3 = vd - va;
            Vector5 w4 = ve - va;

            //Gram-Schmidt process
            Vector5 v1 = w1;
            Vector5 v2 = w2 - (Vector5.Dot(v1, w2) / Vector5.Dot(v1, v1)) * v1;
            Vector5 v3 = w3 - (Vector5.Dot(v1, w3) / Vector5.Dot(v1, v1)) * v1
                            - (Vector5.Dot(v2, w3) / Vector5.Dot(v2, v2)) * v2;
            Vector5 v4 = w4 - (Vector5.Dot(v1, w4) / Vector5.Dot(v1, v1)) * v1
                            - (Vector5.Dot(v2, w4) / Vector5.Dot(v2, v2)) * v2
                            - (Vector5.Dot(v3, w4) / Vector5.Dot(v3, v3)) * v3;

            //Normalize the vectors
            v1.Normalize();
            v2.Normalize();
            v3.Normalize();
            v4.Normalize();

            //Create the basis transform
            basis = new Matrix5x5(v1, v2, v3, v4, Transform5D.MakeNormal(v1, v2, v3, v4));
            Debug.Assert(Mathf.Abs(basis.determinant - 1.0f) < 1e-4f, "Flipped collider basis");
            Matrix5x5 invBasis = basis.transpose;

            //Apply basis to all vectors
            Vector4 ta = (Vector4)(invBasis * w1);
            Vector4 tb = (Vector4)(invBasis * w2);
            Vector4 tc = (Vector4)(invBasis * w3);
            Vector4 td = (Vector4)(invBasis * w4);
            Debug.Assert(Vector4.Dot(Transform4D.MakeNormal(ta, tb, tc), td) > 0.0f);
            a = new MeshCollider4D.Tetrahedron(ta, tb, tc, td);
            b = new MeshCollider4D.Tetrahedron(tb, tc, td, Vector4.zero);
            c = new MeshCollider4D.Tetrahedron(tc, td, Vector4.zero, ta);
            d = new MeshCollider4D.Tetrahedron(td, Vector4.zero, ta, tb);
            e = new MeshCollider4D.Tetrahedron(Vector4.zero, ta, tb, tc);
        }
    }

    public Mesh mesh = null;
    public List<Simplex> pentas = new List<Simplex>();

    public static bool IsFlatCell(Vector5 wa1, Vector5 wa2, Vector5 wA1, Vector5 wA2, Vector5 wb1, Vector5 wb2, Vector5 wB1, Vector5 wB2,
                                  Vector5 va1, Vector5 va2, Vector5 vA1, Vector5 vA2, Vector5 vb1, Vector5 vb2, Vector5 vB1, Vector5 vB2) {
        //Get origin and axes
        Vector5 w1 = wa2 - wa1;
        Vector5 w2 = wA1 - wa1;
        Vector5 w3 = wb1 - wa1;
        Vector5 w4 = va1 - wa1;

        //Gram-Schmidt process
        Vector5 v1 = w1;
        Vector5 v2 = w2 - (Vector5.Dot(v1, w2) / Vector5.Dot(v1, v1)) * v1;
        Vector5 v3 = w3 - (Vector5.Dot(v1, w3) / Vector5.Dot(v1, v1)) * v1
                        - (Vector5.Dot(v2, w3) / Vector5.Dot(v2, v2)) * v2;
        Vector5 v4 = w4 - (Vector5.Dot(v1, w4) / Vector5.Dot(v1, v1)) * v1
                        - (Vector5.Dot(v2, w4) / Vector5.Dot(v2, v2)) * v2
                        - (Vector5.Dot(v3, w4) / Vector5.Dot(v3, v3)) * v3;

        //Normalize the vectors
        v1.Normalize();
        v2.Normalize();
        v3.Normalize();
        v4.Normalize();

        //Create the basis transform
        Matrix5x5 basis = new Matrix5x5(v1, v2, v3, v4, Transform5D.MakeNormal(v1, v2, v3, v4));
        Debug.Assert(basis.determinant > 0.0f, "Flipped collider basis");
        Matrix5x5 invBasis = basis.transpose;

        //Apply basis to all vectors
        return Mathf.Abs((invBasis * (wa2 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (wA1 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (wA2 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (wb1 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (wb2 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (wB1 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (wB2 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (va1 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (va2 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (vA1 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (vA2 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (vb1 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (vb2 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (vB1 - wa1)).v) < 1e-5f &&
               Mathf.Abs((invBasis * (vB2 - wa1)).v) < 1e-5f;
    }

    protected override void Awake() {
        base.Awake();
        boundsCheck = true;

        //Check if a mesh is specified
        if (mesh) {
            //Acquire the 5D mesh data
            Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh);
            NativeArray<Mesh5D.Vertex5D> verts = meshData[0].GetVertexData<Mesh5D.Vertex5D>(0);
            int[] indices = mesh.GetIndices(0);
            Debug.Assert(verts.Length % 5 == 0);
            Debug.Assert(indices.Length % 9 == 0);

            //Convert mesh data into tetrahedrons
            for (int i = 0; i < indices.Length; i += 9) {
                Mesh5D.Vertex5D vert5D = verts[indices[i]];
                AddSimplex(vert5D.va5, vert5D.vb5, vert5D.vc5, vert5D.vd5, vert5D.ve5);
            }

            //Free the memory as required
            meshData.Dispose();
        }
    }

    public void AddSimplex(Vector5 va, Vector5 vb, Vector5 vc, Vector5 vd, Vector5 ve) {
        pentas.Add(new Simplex(va, vb, vc, vd, ve));
        AddBoundingPoint(va);
        AddBoundingPoint(vb);
        AddBoundingPoint(vc);
        AddBoundingPoint(vd);
        AddBoundingPoint(ve);
    }

    public void AddCell(Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                        Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11, Vector5 v12, Vector5 v13, Vector5 v14, Vector5 v15) {
        AddSimplex(v0, v1, v2, v4, v8);
        AddSimplex(v8, v4, v12, v13, v14);
        AddSimplex(v2, v8, v10, v11, v14);
        AddSimplex(v4, v2, v6, v7, v14);
        AddSimplex(v8, v1, v9, v11, v13);
        AddSimplex(v1, v4, v5, v7, v13);
        AddSimplex(v2, v1, v3, v7, v11);
        AddSimplex(v7, v11, v13, v14, v15);
        AddSimplex(v2, v1, v4, v8, v14);
        AddSimplex(v2, v1, v8, v11, v14);
        AddSimplex(v4, v1, v7, v13, v14);
        AddSimplex(v7, v1, v11, v13, v14);
        AddSimplex(v1, v4, v8, v13, v14);
        AddSimplex(v1, v2, v4, v7, v14);
        AddSimplex(v1, v8, v11, v13, v14);
        AddSimplex(v1, v2, v7, v11, v14);
    }

    public override Vector5 NP(Vector5 localPt) {
        //Compute the distance to each tetrahedron
        Debug.Assert(pentas.Count > 0);
        float bestDistSq = float.MaxValue;
        Vector5 bestPt = localPt;
        for (int i = 0; i < pentas.Count; ++i) {
            bool inside = true;
            Vector5 np = NPSimplex(localPt, pentas[i], ref inside);
            float distSq = (np - localPt).sqrMagnitude;
            if (distSq < bestDistSq) {
                bestDistSq = distSq;
                bestPt = np;
            }
        }
        return bestPt;
    }

    private static Vector5 NPSimplex(Vector5 localPt, Simplex penta, ref bool inside) {
        //Project point onto the space, the nearest point will always be here.
        Vector4 pt = (Vector4)(penta.basis.transpose * (localPt - penta.origin));

        //Check each 4D "side" tetrahedron
        inside &= Vector4.Dot(penta.a.basis.GetColumn(3), pt - penta.a.origin) > 0.0f;
        inside &= Vector4.Dot(penta.b.basis.GetColumn(3), pt - penta.b.origin) > 0.0f;
        inside &= Vector4.Dot(penta.c.basis.GetColumn(3), pt - penta.c.origin) > 0.0f;
        inside &= Vector4.Dot(penta.d.basis.GetColumn(3), pt - penta.d.origin) > 0.0f;
        inside &= Vector4.Dot(penta.e.basis.GetColumn(3), pt - penta.e.origin) > 0.0f;

        //Only need to calculate NP if we're not already inside
        if (!inside) {
            //Compute closest point on each tetrahedron
            bool insideTetra = true;
            Vector4 np1 = MeshCollider4D.NPTetrahedron(pt, penta.a, ref insideTetra);
            Vector4 np2 = MeshCollider4D.NPTetrahedron(pt, penta.b, ref insideTetra);
            Vector4 np3 = MeshCollider4D.NPTetrahedron(pt, penta.c, ref insideTetra);
            Vector4 np4 = MeshCollider4D.NPTetrahedron(pt, penta.d, ref insideTetra);
            Vector4 np5 = MeshCollider4D.NPTetrahedron(pt, penta.e, ref insideTetra);

            //Calculate the distance to each nearest point
            float distSq1 = (np1 - pt).sqrMagnitude;
            float distSq2 = (np2 - pt).sqrMagnitude;
            float distSq3 = (np3 - pt).sqrMagnitude;
            float distSq4 = (np4 - pt).sqrMagnitude;
            float distSq5 = (np5 - pt).sqrMagnitude;

            //Determine the point with the smallest distance
            float minDist = Mathf.Min(Mathf.Min(distSq1, distSq2), Mathf.Min(Mathf.Min(distSq3, distSq4), distSq5));
            if (distSq1 == minDist) { pt = np1; }
            else if (distSq2 == minDist) { pt = np2; }
            else if (distSq3 == minDist) { pt = np3; }
            else if (distSq4 == minDist) { pt = np4; }
            else { pt = np5; }
        }

        //Project back into 5D to return NP
        return penta.basis * ((Vector5)pt) + penta.origin;
    }
}
