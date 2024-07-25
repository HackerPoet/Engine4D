using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class MeshCollider4D : Collider4D {
    [System.Serializable]
    public struct Tetrahedron {
        public Vector4 origin;
        public Matrix4x4 basis;
        public Vector3 a, b, c;
        public Tetrahedron(Vector4 va, Vector4 vb, Vector4 vc, Vector4 vd) {
            //Get origin and axes
            origin = va;
            Vector4 w1 = vb - va;
            Vector4 w2 = vc - va;
            Vector4 w3 = vd - va;

            //Gram-Schmidt process
            Vector4 v1 = w1;
            Vector4 v2 = w2 - (Vector4.Dot(v1, w2) / Vector4.Dot(v1, v1)) * v1;
            Vector4 v3 = w3 - (Vector4.Dot(v1, w3) / Vector4.Dot(v1, v1)) * v1
                            - (Vector4.Dot(v2, w3) / Vector4.Dot(v2, v2)) * v2;

            //Normalize the vectors
            v1.Normalize();
            v2.Normalize();
            v3.Normalize();

            //Create the basis transform
            basis = new Matrix4x4(v1, v2, v3, Transform4D.MakeNormal(v1, v2, v3));
            Debug.Assert(basis.determinant > 0.0f, "Flipped collider basis");
            Matrix4x4 invBasis = basis.transpose;

            //Apply basis to all vectors
            a = invBasis * w1;
            b = invBasis * w2;
            c = invBasis * w3;
            Debug.Assert(Vector3.Dot(Vector3.Cross(a, b), c) > 0.0f);
        }
    }

    public Mesh mesh = null;
    public List<Tetrahedron> tetras = new List<Tetrahedron>();

    public static bool IsFlatCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2) {
        //Get origin and axes
        Vector4 w1 = a2 - a1;
        Vector4 w2 = A1 - a1;
        Vector4 w3 = b1 - a1;

        //Gram-Schmidt process
        Vector4 v1 = w1;
        Vector4 v2 = w2 - (Vector4.Dot(v1, w2) / Vector4.Dot(v1, v1)) * v1;
        Vector4 v3 = w3 - (Vector4.Dot(v1, w3) / Vector4.Dot(v1, v1)) * v1
                        - (Vector4.Dot(v2, w3) / Vector4.Dot(v2, v2)) * v2;

        //Normalize the vectors
        v1.Normalize();
        v2.Normalize();
        v3.Normalize();

        //Create the basis transform
        Matrix4x4 basis = new Matrix4x4(v1, v2, v3, Transform4D.MakeNormal(v1, v2, v3));
        Debug.Assert(basis.determinant > 0.0f, "Flipped collider basis");
        Matrix4x4 invBasis = basis.transpose;

        //Apply basis to all vectors
        return Mathf.Abs((invBasis * (a2 - a1)).w) < 1e-5f &&
               Mathf.Abs((invBasis * (A1 - a1)).w) < 1e-5f &&
               Mathf.Abs((invBasis * (A2 - a1)).w) < 1e-5f &&
               Mathf.Abs((invBasis * (b1 - a1)).w) < 1e-5f &&
               Mathf.Abs((invBasis * (b2 - a1)).w) < 1e-5f &&
               Mathf.Abs((invBasis * (B1 - a1)).w) < 1e-5f &&
               Mathf.Abs((invBasis * (B2 - a1)).w) < 1e-5f;
    }

    protected override void Awake() {
        base.Awake();
        boundsCheck = true;

        //Check if a mesh is specified
        if (mesh) {
            //Acquire the 4D mesh data
            Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh);
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            int[] indices = mesh.GetIndices(0);
            Debug.Assert(verts.Length % 4 == 0);
            Debug.Assert(indices.Length % 6 == 0);

            //Convert mesh data into tetrahedrons
            for (int i = 0; i < indices.Length; i += 6) {
                Mesh4D.Vertex4D vert4D = verts[indices[i]];
                AddTetra(vert4D.va, vert4D.vb, vert4D.vc, vert4D.vd);
            }

            //Free the memory as required
            meshData.Dispose();
        }
    }

    public void AddTetra(Vector4 va, Vector4 vb, Vector4 vc, Vector4 vd) {
        tetras.Add(new Tetrahedron(va, vb, vc, vd));
        AddBoundingPoint(va);
        AddBoundingPoint(vb);
        AddBoundingPoint(vc);
        AddBoundingPoint(vd);
    }

    public void AddCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2) {
        AddTetra(A2, a1, B1, b2);
        AddTetra(B1, a1, A2, A1);
        AddTetra(A2, a1, b2, a2);
        AddTetra(A2, b2, B1, B2);
        AddTetra(b2, a1, B1, b1);
    }

    public void AddHalfCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2) {
        AddTetra(a1, A2, b2, a2);
        AddTetra(a1, A1, b1, A2);
        AddTetra(b1, b2, a1, A2);
    }

    public override Vector4 NP(Vector4 localPt) {
        //Compute the distance to each tetrahedron
        Debug.Assert(tetras.Count > 0);
        float bestDistSq = float.MaxValue;
        Vector4 bestPt = localPt;
        for (int i = 0; i < tetras.Count; ++i) {
            bool inside = true;
            Vector4 np = NPTetrahedron(localPt, tetras[i], ref inside);
            float distSq = (np - localPt).sqrMagnitude;
            if (distSq < bestDistSq) {
                bestDistSq = distSq;
                bestPt = np;
            }
        }
        return bestPt;
    }

    public static Vector4 NPTetrahedron(Vector4 localPt, Tetrahedron tetra, ref bool inside) {
        //Project point onto the space, the nearest point will always be here.
        Vector3 pt = tetra.basis.transpose * (localPt - tetra.origin);

        //Compute closest point on each triangle
        Vector3 np1 = CellCollider4D.TriangleNP(pt, tetra.a, tetra.b, tetra.c, ref inside);
        Vector3 np2 = CellCollider4D.TriangleNP(pt, Vector3.zero, tetra.b, tetra.a, ref inside);
        Vector3 np3 = CellCollider4D.TriangleNP(pt, Vector3.zero, tetra.c, tetra.b, ref inside);
        Vector3 np4 = CellCollider4D.TriangleNP(pt, Vector3.zero, tetra.a, tetra.c, ref inside);

        //Only need to calculate NP if we're not already inside
        if (!inside) {
            //Calculate the distance to each nearest point
            float distSq1 = (np1 - pt).sqrMagnitude;
            float distSq2 = (np2 - pt).sqrMagnitude;
            float distSq3 = (np3 - pt).sqrMagnitude;
            float distSq4 = (np4 - pt).sqrMagnitude;

            //Determine the point with the smallest distance
            float minDist = Mathf.Min(Mathf.Min(distSq1, distSq2), Mathf.Min(distSq3, distSq4));
            if (distSq1 == minDist) { pt = np1; }
            else if (distSq2 == minDist) { pt = np2; }
            else if (distSq3 == minDist) { pt = np3; }
            else { pt = np4; }
        }

        //Project back into 4D to return NP
        return tetra.basis * pt + tetra.origin;
    }
}
