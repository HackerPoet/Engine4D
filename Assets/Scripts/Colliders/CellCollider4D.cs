using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellCollider4D : Collider4D {
    public Vector4 origin = Vector4.zero;
    public Matrix4x4 basis;
    public static readonly Vector3 a1 = Vector3.zero;
    public Vector3 a2 = Vector3.zero;
    public Vector3 A1 = Vector3.zero;
    public Vector3 A2 = Vector3.zero;
    public Vector3 b1 = Vector3.zero;
    public Vector3 b2 = Vector3.zero;
    public Vector3 B1 = Vector3.zero;
    public Vector3 B2 = Vector3.zero;

    protected override void Awake() {
        base.Awake();
        boundsCheck = true;
    }

    public void MakeCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2) {
        //Get origin and axes
        origin = a1;
        Vector4 w1 = a2 - origin;
        Vector4 w2 = A1 - origin;
        Vector4 w3 = b1 - origin;

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
        this.a2 = CheckFlat(invBasis * (a2 - origin));
        this.A1 = CheckFlat(invBasis * (A1 - origin));
        this.A2 = CheckFlat(invBasis * (A2 - origin));
        this.b1 = CheckFlat(invBasis * (b1 - origin));
        this.b2 = CheckFlat(invBasis * (b2 - origin));
        this.B1 = CheckFlat(invBasis * (B1 - origin));
        this.B2 = CheckFlat(invBasis * (B2 - origin));

        //Create AABB
        ResetBoundingBox();
        AddBoundingPoint(a1);
        AddBoundingPoint(a2);
        AddBoundingPoint(A1);
        AddBoundingPoint(A2);
        AddBoundingPoint(b1);
        AddBoundingPoint(b2);
        AddBoundingPoint(B1);
        AddBoundingPoint(B2);
    }

    private static Vector3 CheckFlat(Vector4 v) {
        Debug.Assert(Mathf.Abs(v.w) < 1e-5f);
        return v;
    }

    public override Vector4 NP(Vector4 localPt) {
        //Project point onto the space, the nearest point will always be here.
        Vector3 pt = basis.transpose * (localPt - origin);

        //Compute closest point on each quad
        bool inside = true;
        Vector3 np1 = QuadNP(pt, a2, b2, a1, b1, ref inside);
        Vector3 np2 = QuadNP(pt, A1, B1, A2, B2, ref inside);
        Vector3 np3 = QuadNP(pt, A1, a1, B1, b1, ref inside);
        Vector3 np4 = QuadNP(pt, a2, A2, b2, B2, ref inside);
        Vector3 np5 = QuadNP(pt, a2, a1, A2, A1, ref inside);
        Vector3 np6 = QuadNP(pt, b1, b2, B1, B2, ref inside);

        //Return early if we're on the inside
        if (inside) {
            return ReturnNP(pt);
        }

        //Calculate the distance to each nearest point
        float distSq1 = (np1 - pt).sqrMagnitude;
        float distSq2 = (np2 - pt).sqrMagnitude;
        float distSq3 = (np3 - pt).sqrMagnitude;
        float distSq4 = (np4 - pt).sqrMagnitude;
        float distSq5 = (np5 - pt).sqrMagnitude;
        float distSq6 = (np6 - pt).sqrMagnitude;

        //Determine the point with the smallest distance and return it
        float minDist = Mathf.Min(Mathf.Min(Mathf.Min(distSq1, distSq2), Mathf.Min(distSq3, distSq4)), Mathf.Min(distSq5, distSq6));
        if (distSq1 == minDist) { return ReturnNP(np1); }
        if (distSq2 == minDist) { return ReturnNP(np2); }
        if (distSq3 == minDist) { return ReturnNP(np3); }
        if (distSq4 == minDist) { return ReturnNP(np4); }
        if (distSq5 == minDist) { return ReturnNP(np5); }
        return ReturnNP(np6);
    }

    private Vector4 ReturnNP(Vector3 v) {
        return basis * v + origin;
    }

    private static Vector3 QuadNP(Vector3 p, Vector3 a, Vector3 b, Vector3 c, Vector3 d, ref bool inside) {
        Vector3 np1 = TriangleNP(p, a, b, c, ref inside);
        Vector3 np2 = TriangleNP(p, c, b, d, ref inside);
        if ((p - np1).sqrMagnitude < (p - np2).sqrMagnitude) {
            return np1;
        } else {
            return np2;
        }
    }

    public static Vector3 TriangleNP(Vector3 p, Vector3 a, Vector3 b, Vector3 c, ref bool inside) {
        a -= c;
        b -= c;
        p -= c;

        //Check if the point is inside the volume
        if (Vector3.Dot(Vector3.Cross(a, b), p) >= 0.0f) {
            inside = false;
        }

        float aa = Vector3.Dot(a, a);
        float ab = Vector3.Dot(a, b);
        float bb = Vector3.Dot(b, b);
        float av = -Vector3.Dot(a, p);
        float bv = -Vector3.Dot(b, p);

        float det = aa * bb - ab * ab;
        float s = ab * bv - bb * av;
        float t = ab * av - aa * bv;

        if (s + t < det) {
            if (s < 0.0f) {
                if (t < 0.0f) {
                    if (av < 0.0f) {
                        s = Mathf.Clamp01(-av / aa);
                        t = 0.0f;
                    } else {
                        s = 0.0f;
                        t = Mathf.Clamp01(-bv / bb);
                    }
                } else {
                    s = 0.0f;
                    t = Mathf.Clamp01(-bv / bb);
                }
            } else if (t < 0.0f) {
                s = Mathf.Clamp01(-av / aa);
                t = 0.0f;
            } else {
                float invDet = 1.0f / det;
                s *= invDet;
                t *= invDet;
            }
        } else {
            if (s < 0.0f) {
                float tmp0 = ab + av;
                float tmp1 = bb + bv;
                if (tmp1 > tmp0) {
                    float numer = tmp1 - tmp0;
                    float denom = aa - 2 * ab + bb;
                    s = Mathf.Clamp01(numer / denom);
                    t = 1.0f - s;
                } else {
                    t = Mathf.Clamp01(-bv / bb);
                    s = 0.0f;
                }
            } else if (t < 0.0f) {
                if (aa + av > ab + bv) {
                    float numer = bb + bv - ab - av;
                    float denom = aa - 2 * ab + bb;
                    s = Mathf.Clamp01(numer / denom);
                    t = 1.0f - s;
                } else {
                    s = Mathf.Clamp01(-bv / bb);
                    t = 0.0f;
                }
            } else {
                float numer = bb + bv - ab - av;
                float denom = aa - 2 * ab + bb;
                s = Mathf.Clamp01(numer / denom);
                t = 1.0f - s;
            }
        }

        return c + a * s + b * t;
    }
}
