//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
#define USE_5D
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Transform5D {
    //Variables
    public Matrix5x5 matrix;
    public Vector5 translation;

    //Constructors
    public Transform5D(Vector5 _translation) {
        matrix = Matrix5x5.identity;
        translation = _translation;
    }
    public Transform5D(Matrix5x5 _matrix) {
        matrix = _matrix;
        translation = Vector5.zero;
    }
    public Transform5D(Matrix5x5 _matrix, Vector5 _translation) {
        matrix = _matrix;
        translation = _translation;
    }
    public Transform5D(Matrix5x5 _rotation, Vector5 _translation, Vector5 _scale) {
        matrix = _rotation * ScaleMatrix(_scale);
        translation = _translation;
    }
    public Transform5D(Transform transform) {
        Matrix4x4 trs = Matrix4x4.TRS(Vector3.zero, transform.localRotation, transform.localScale);
        matrix = Matrix5x5.identity;
        matrix.SetColumn(0, (Vector5)trs.GetColumn(0));
        matrix.SetColumn(1, (Vector5)trs.GetColumn(1));
        matrix.SetColumn(2, (Vector5)trs.GetColumn(2));
        translation = (Vector5)transform.localPosition;
    }

    //Elements
    public static readonly Transform5D identity = new Transform5D(Vector5.zero);
    public static readonly Transform5D zero = new Transform5D(Matrix5x5.zero);

    //Operators
    public static Vector5 operator*(Transform5D a, Vector5 b) {
        return a.matrix * b + a.translation;
    }
    public static Transform5D operator*(Transform5D a, Transform5D b) {
        return new Transform5D(a.matrix * b.matrix, a * b.translation);
    }

    //Print
    public override string ToString() {
        string result = matrix.ToString() + "(";
        for (int i = 0; i < 5; ++i) {
            if (i > 0) { result += ','; }
            result += translation[i];
        }
        return result + ")";
    }

    //Get scaling factor in largest direction
    public float MaxScale() {
        return MaxScale(matrix);
    }

    //Standard functions
    public Transform5D inverse {
        get {
            Matrix5x5 invRotation = matrix.inverse;
            return new Transform5D(invRotation, -(invRotation * translation));
        }
    }

    //####################################################################################################
    //#  STATIC HELPERS
    //####################################################################################################
    public static Matrix5x5 ScaleMatrix(Vector5 scale) {
        Matrix5x5 scaleMatrix = Matrix5x5.identity;
        for (int i = 0; i < 5; ++i) {
            scaleMatrix[i,i] = scale[i];
        }
        return scaleMatrix;
    }

    public static float MaxScale(Matrix5x5 matrix) {
        float maxScaleSq = 0.0f;
        for (int i = 0; i < 5; ++i) {
            maxScaleSq = Mathf.Max(maxScaleSq, matrix.GetColumn(i).sqrMagnitude);
        }
        return Mathf.Sqrt(maxScaleSq);
    }

    public static Matrix5x5 PlaneRotation(float angle, int p1, int p2) {
        float cs = Mathf.Cos(angle * Mathf.Deg2Rad);
        float sn = Mathf.Sin(angle * Mathf.Deg2Rad);
        if (Mathf.Abs(angle) == 90.0f || angle == 180.0f || angle == 0.0f) {
            cs = Mathf.Round(cs); sn = Mathf.Round(sn);
        }
        Matrix5x5 result = Matrix5x5.identity;
        result[p1, p1] = cs;
        result[p2, p2] = cs;
        result[p1, p2] = sn;
        result[p2, p1] = -sn;
        return result;
    }

    public static Matrix5x5 FromQuaternion(Quaternion q) {
        Matrix4x4 r = Matrix4x4.Rotate(q);
        Matrix5x5 matrix = Matrix5x5.identity;
        matrix.SetColumn(0, (Vector5)r.GetColumn(0));
        matrix.SetColumn(1, (Vector5)r.GetColumn(1));
        matrix.SetColumn(2, (Vector5)r.GetColumn(2));
        return matrix;
    }

    public static void MakeOrthoNormal(ref Matrix4x4 matrix) {
        //Get columns
        Vector4 w1 = matrix.GetColumn(0);
        Vector4 w2 = matrix.GetColumn(1);
        Vector4 w3 = matrix.GetColumn(2);
        Vector4 w4 = matrix.GetColumn(3);
    
        //Gram-Schmidt process
        Vector4 v1 = w1;
        Vector4 v2 = w2 - (Vector4.Dot(v1, w2) / Vector4.Dot(v1, v1)) * v1;
        Vector4 v3 = w3 - (Vector4.Dot(v1, w3) / Vector4.Dot(v1, v1)) * v1
                        - (Vector4.Dot(v2, w3) / Vector4.Dot(v2, v2)) * v2;
        Vector4 v4 = w4 - (Vector4.Dot(v1, w4) / Vector4.Dot(v1, v1)) * v1
                        - (Vector4.Dot(v2, w4) / Vector4.Dot(v2, v2)) * v2
                        - (Vector4.Dot(v3, w4) / Vector4.Dot(v3, v3)) * v3;
    
        //Set columns and normalize
        matrix.SetColumn(0, v1.normalized);
        matrix.SetColumn(1, v2.normalized);
        matrix.SetColumn(2, v3.normalized);
        matrix.SetColumn(3, v4.normalized);
    }

    public static void MakeOrthoNormal(ref Matrix5x5 matrix) {
        //Get columns
        Vector5 w1 = matrix.GetColumn(0);
        Vector5 w2 = matrix.GetColumn(1);
        Vector5 w3 = matrix.GetColumn(2);
        Vector5 w4 = matrix.GetColumn(3);
        Vector5 w5 = matrix.GetColumn(4);
        
        //Gram-Schmidt process
        Vector5 v1 = w1;
        Vector5 v2 = w2 - (Vector5.Dot(v1, w2) / Vector5.Dot(v1, v1)) * v1;
        Vector5 v3 = w3 - (Vector5.Dot(v1, w3) / Vector5.Dot(v1, v1)) * v1
                        - (Vector5.Dot(v2, w3) / Vector5.Dot(v2, v2)) * v2;
        Vector5 v4 = w4 - (Vector5.Dot(v1, w4) / Vector5.Dot(v1, v1)) * v1
                        - (Vector5.Dot(v2, w4) / Vector5.Dot(v2, v2)) * v2
                        - (Vector5.Dot(v3, w4) / Vector5.Dot(v3, v3)) * v3;
        Vector5 v5 = w5 - (Vector5.Dot(v1, w5) / Vector5.Dot(v1, v1)) * v1
                        - (Vector5.Dot(v2, w5) / Vector5.Dot(v2, v2)) * v2
                        - (Vector5.Dot(v3, w5) / Vector5.Dot(v3, v3)) * v3
                        - (Vector5.Dot(v4, w5) / Vector5.Dot(v4, v4)) * v4;
        
        //Set columns and normalize
        matrix.SetColumn(0, v1.normalized);
        matrix.SetColumn(1, v2.normalized);
        matrix.SetColumn(2, v3.normalized);
        matrix.SetColumn(3, v4.normalized);
        matrix.SetColumn(4, v5.normalized);
    }

    public static Matrix5x5 FromToRotation(Vector5 from, Vector5 to) {
        from.Normalize();
        to.Normalize();
        Vector5 c = from + to;
        float magSq = c.sqrMagnitude;
        if (magSq < 1e-10f) {
#if USE_4D
            return ScaleMatrix(-Vector5.one);
#else
            Debug.Log("TODO: Fix degenerate FromToRotation for 5D");
            from.x += 0.01f;
            from.w += 0.01f;
            from.v += 0.01f;
            from = (from + new Vector5(0.01f, 0, 0, 0.01f, 0.01f)).normalized;
            c = from + to;
            magSq = c.sqrMagnitude;
            //return Matrix5x5.identity;
#endif
        }
        Matrix5x5 S = MatAdd(Matrix5x5.identity, Outer((-2.0f / magSq) * c, c));
        return MatAdd(S, Outer(-2.0f * to, S * to));
    }

    public static Matrix5x5 CayleyTransform(Matrix5x5 m) {
        Matrix5x5 a = MatAdd(Matrix5x5.identity, MatMul(m, -1.0f));
        Matrix5x5 b = MatAdd(Matrix5x5.identity, m).inverse;
        return a * b;
    }

    public static float SkewSymmetricMagnitude(Matrix5x5 s) {
        float magSq = 0.0f;
        for (int i = 0; i < 5; ++i) {
            for (int j = i + 1; j < 5; ++j) {
                magSq += s[i, j] * s[i, j];
            }
        }
        return Mathf.Sqrt(magSq);
    }

    //NOTE: This formula will only work for dimensions 4 and 5
    public static Vector2 RotationAngles(Matrix5x5 r) {
        float traceR = Trace(r);
        float tn = traceR - (5 - 4);
        float delta = Mathf.Sqrt(Mathf.Max(2 * (Trace(r * r) - traceR) - (tn - 4) * (tn + 2), 0.0f));
        float y1 = Mathf.Clamp(0.25f * (tn - delta), -1.0f, 1.0f);
        float y2 = Mathf.Clamp(0.25f * (tn + delta), -1.0f, 1.0f);
        return new Vector2(Mathf.Acos(y1), Mathf.Acos(y2)) * Mathf.Rad2Deg;
    }

    public static Matrix5x5 MatAdd(Matrix5x5 a, Matrix5x5 b) {
        for (int i = 0; i < 5; ++i) {
            a.SetColumn(i, a.GetColumn(i) + b.GetColumn(i));
        }
        return a;
    }
    public static Matrix5x5 MatSub(Matrix5x5 a, Matrix5x5 b) {
        for (int i = 0; i < 5; ++i) {
            a.SetColumn(i, a.GetColumn(i) - b.GetColumn(i));
        }
        return a;
    }
    public static Matrix5x5 MatMul(Matrix5x5 a, float b) {
        for (int i = 0; i < 5; ++i) {
            a.SetColumn(i, a.GetColumn(i) * b);
        }
        return a;
    }
    public static Matrix5x5 Outer(Vector5 a, Vector5 b) {
        Matrix5x5 result = Matrix5x5.zero;
        for (int i = 0; i < 5; ++i) {
            result.SetColumn(i, a * b[i]);
        }
        return result;
    }

    public static float Norm(Matrix5x5 m) {
        float sumSq = 0.0f;
        for (int i = 0; i < 5; ++i) {
            sumSq += m.GetColumn(i).sqrMagnitude;
        }
        return Mathf.Sqrt(sumSq);
    }

    public static float Trace(Matrix4x4 m) {
        return m.m00 + m.m11 + m.m22 + m.m33;
    }
    public static float Trace(Matrix5x5 m) {
        return m[0,0] + m[1,1] + m[2,2] + m[3,3] + m[4,4];
    }

    public static Matrix5x5 Slerp(Matrix5x5 A, Matrix5x5 B, float t) {
        Matrix5x5 C = A.inverse * B;
        C = CayleyTransform(C);
        float mag = SkewSymmetricMagnitude(C);
        if (mag < 1e-12f) { return A; }
        float mul = (float)Math.Tan(Math.Atan(mag) * t);
        C = CayleyTransform(MatMul(C, mul / mag));
        return A * C;
    }

    public static Matrix5x5 SlerpNear(Matrix5x5 A, Matrix5x5 B, float t) {
        Matrix5x5 m = MatAdd(MatMul(A, 1.0f - t), MatMul(B, t));
        m = OrthoIterate(m);
        m = OrthoIterate(m);
        m = OrthoIterate(m);
        return m;
    }

    public static Matrix5x5 SkipY(Quaternion q) {
        return XYZTo(Matrix4x4.Rotate(q), 0, 2, 3);
    }
    public static Matrix5x5 SkipY(Isocline i) {
        return XYZWTo(i.matrix, 0, 2, 3, 4);
    }
    public static Matrix5x5 SkipY(AxisRotation a) {
        return XYZWTo(a.matrix, 0, 2, 3, 4);
    }
    public static Matrix5x5 XYZTo(Quaternion q, int sendX, int sendY, int sendZ) {
        return XYZTo(Matrix4x4.Rotate(q), sendX, sendY, sendZ);
    }
    public static Matrix5x5 XYZWTo(Isocline i, int sendX, int sendY, int sendZ, int sendW) {
        return XYZWTo(i.matrix, sendX, sendY, sendZ, sendW);
    }
    public static Matrix5x5 XYZWTo(AxisRotation a, int sendX, int sendY, int sendZ, int sendW) {
        return XYZWTo(a.matrix, sendX, sendY, sendZ, sendW);
    }

    public static Matrix5x5 XYZTo(Matrix4x4 m, int sendX, int sendY, int sendZ) {
        Matrix5x5 mC = Matrix5x5.zero;
        mC.SetColumn(sendX, (Vector5)m.GetColumn(0));
        mC.SetColumn(sendY, (Vector5)m.GetColumn(1));
        mC.SetColumn(sendZ, (Vector5)m.GetColumn(2));
        Matrix5x5 mV = Matrix5x5.identity;
        mV.SetRow(sendX, mC.GetRow(0));
        mV.SetRow(sendY, mC.GetRow(1));
        mV.SetRow(sendZ, mC.GetRow(2));
        return mV;
    }

    public static Matrix5x5 XYZWTo(Matrix4x4 m, int sendX, int sendY, int sendZ, int sendW) {
        Matrix5x5 mC = Matrix5x5.zero;
        mC.SetColumn(sendX, (Vector5)m.GetColumn(0));
        mC.SetColumn(sendY, (Vector5)m.GetColumn(1));
        mC.SetColumn(sendZ, (Vector5)m.GetColumn(2));
        mC.SetColumn(sendW, (Vector5)m.GetColumn(3));
        Matrix5x5 mV = Matrix5x5.identity;
        mV.SetRow(sendX, mC.GetRow(0));
        mV.SetRow(sendY, mC.GetRow(1));
        mV.SetRow(sendZ, mC.GetRow(2));
        mV.SetRow(sendW, mC.GetRow(3));
        return mV;
    }

    public static Vector3 SkipY(Vector4 v) {
        return new Vector3(v.x, v.z, v.w);
    }
    public static Vector4 SkipY(Vector5 v) {
        return new Vector4(v.x, v.z, v.w, v.v);
    }
    public static Matrix4x4 SkipY(Matrix5x5 m) {
        return new Matrix4x4(SkipY(m.GetColumn(0)), SkipY(m.GetColumn(2)), SkipY(m.GetColumn(3)), SkipY(m.GetColumn(4)));
    }
    public static Vector4 InsertY(Vector3 v, float y) {
        return new Vector4(v.x, y, v.y, v.z);
    }
    public static Vector5 InsertY(Vector4 v, float y) {
        return new Vector5(v.x, y, v.y, v.z, v.w);
    }

    public static Matrix5x5 Adjugate(Matrix5x5 m) {
        return Cofactor(m).transpose;
    }

    public static Vector4 Sign(Vector4 v) {
        return new Vector4(Mathf.Sign(v.x), Mathf.Sign(v.y), Mathf.Sign(v.z), Mathf.Sign(v.w));
    }
    public static Vector5 Sign(Vector5 v) {
        return new Vector5(Mathf.Sign(v.x), Mathf.Sign(v.y), Mathf.Sign(v.z), Mathf.Sign(v.w), Mathf.Sign(v.v));
    }

    //Project onto a line spanned by a vector
    public static Vector5 Project(Vector5 p, Vector5 ax1) {
        return Vector5.Project(p, ax1);
    }
    //Project onto a plane spanned by 2 vectors
    public static Vector5 Project(Vector5 p, Vector5 ax1, Vector5 ax2) {
        float d11 = Vector5.Dot(ax1, ax1);
        float d12 = Vector5.Dot(ax1, ax2);
        float d22 = Vector5.Dot(ax2, ax2);
        float dp1 = Vector5.Dot(p, ax1);
        float dp2 = Vector5.Dot(p, ax2);
        float d = d11*d22 - d12*d12;
        float t1 = (d22*dp1 - d12*dp2) / d;
        float t2 = (d11*dp2 - d12*dp1) / d;
        return ax1 * t1 + ax2 * t2;
    }
    //Project onto a space spanned by 3 vectors
    public static Vector5 Project(Vector5 p, Vector5 ax1, Vector5 ax2, Vector5 ax3) {
        float d11 = Vector5.Dot(ax1, ax1);
        float d12 = Vector5.Dot(ax1, ax2);
        float d13 = Vector5.Dot(ax1, ax3);
        float d22 = Vector5.Dot(ax2, ax2);
        float d23 = Vector5.Dot(ax2, ax3);
        float d33 = Vector5.Dot(ax3, ax3);
        float dp1 = Vector5.Dot(p, ax1);
        float dp2 = Vector5.Dot(p, ax2);
        float dp3 = Vector5.Dot(p, ax3);
        float a11 = d33 * d22 - d23 * d23;
        float a12 = d13 * d23 - d33 * d12;
        float a13 = d12 * d23 - d13 * d22;
        float a22 = d33 * d11 - d13 * d13;
        float a23 = d12 * d13 - d11 * d23;
        float a33 = d11 * d22 - d12 * d12;
        float d = (d11 * a11) + (d12 * a12) + (d13 * a13);
        float t1 = (a11 * dp1 + a12 * dp2 + a13 * dp3) / d;
        float t2 = (a12 * dp1 + a22 * dp2 + a23 * dp3) / d;
        float t3 = (a13 * dp1 + a23 * dp2 + a33 * dp3) / d;
        return ax1 * t1 + ax2 * t2 + ax3 * t3;
    }

    public static float Angle(Vector5 a, Vector5 b) {
        return Mathf.Rad2Deg * Mathf.Acos(CosAngle(a, b));
    }
    public static float CosAngle(Vector5 a, Vector5 b) {
        return Mathf.Clamp(Vector5.Dot(a, b) / Mathf.Sqrt(a.sqrMagnitude * b.sqrMagnitude), -1.0f, 1.0f);
    }

    public static Vector5 RotateTowards(Vector5 from, Vector5 target, float maxDegrees) {
        float maxRadians = maxDegrees * Mathf.Deg2Rad;
        float minCos = Mathf.Cos(maxRadians);
        float cosAng = Vector5.Dot(from, target);
        if (cosAng >= minCos) { return target; }
        float minSin = Mathf.Sin(maxRadians);
        Vector5 perp = target - from * cosAng;
        float perpMag = perp.magnitude;
#if USE_4D
        if (perpMag <= 1e-6f) {
            perp = new Vector5(from.y, -from.x, from.w, -from.z);
            perpMag = perp.magnitude;
        }
#endif
        return from * minCos + perp * (minSin / perpMag);
    }

    public static Matrix5x5 OrthoIterate(Matrix5x5 m) {
        for (int i = 0; i < 5; ++i) {
            Vector5 v = m.GetColumn(i);
            float mag = v.magnitude;
            if (mag < 1e-8f) { return m; }
            m.SetColumn(i, v / mag);
        }
        Matrix5x5 mt = m.transpose * m;
        Matrix5x5 result = Matrix5x5.zero;
        for (int i = 0; i < 5; ++i) {
            Vector5 sum = m.GetColumn(i);
            for (int j = 0; j < 5; ++j) {
                if (i == j) { continue; }
                sum += m.GetColumn(j) * (-0.5f * mt[i, j]);
            }
            result.SetColumn(i, sum);
        }
        return result;
    }

    //####################################################################################################
    //#  DIMENSION SPECIFIC HELPERS
    //####################################################################################################
    public static Vector4 MakeNormal(Vector4 a, Vector4 b, Vector4 c) {
        return new Vector4(
           -Vector3.Dot(YZW(a), Vector3.Cross(YZW(b), YZW(c))),
            Vector3.Dot(ZWX(a), Vector3.Cross(ZWX(b), ZWX(c))),
           -Vector3.Dot(WXY(a), Vector3.Cross(WXY(b), WXY(c))),
            Vector3.Dot(XYZ(a), Vector3.Cross(XYZ(b), XYZ(c))));
    }
    public static Vector3 YZW(Vector4 v) { return new Vector3(v.y, v.z, v.w); }
    public static Vector3 ZWX(Vector4 v) { return new Vector3(v.z, v.w, v.x); }
    public static Vector3 WXY(Vector4 v) { return new Vector3(v.w, v.x, v.y); }
    public static Vector3 XYZ(Vector4 v) { return new Vector3(v.x, v.y, v.z); }

    public static Vector5 MakeNormal(Vector5 a, Vector5 b, Vector5 c, Vector5 d) {
      return new Vector5(
          -Vector4.Dot(YZWV(a), MakeNormal(YZWV(b), YZWV(c), YZWV(d))),
          -Vector4.Dot(ZWVX(a), MakeNormal(ZWVX(b), ZWVX(c), ZWVX(d))),
          -Vector4.Dot(WVXY(a), MakeNormal(WVXY(b), WVXY(c), WVXY(d))),
          -Vector4.Dot(VXYZ(a), MakeNormal(VXYZ(b), VXYZ(c), VXYZ(d))),
          -Vector4.Dot(XYZW(a), MakeNormal(XYZW(b), XYZW(c), XYZW(d))));
    }
    public static Vector4 YZWV(Vector5 v) { return new Vector4(v.y, v.z, v.w, v.v); }
    public static Vector4 ZWVX(Vector5 v) { return new Vector4(v.z, v.w, v.v, v.x); }
    public static Vector4 WVXY(Vector5 v) { return new Vector4(v.w, v.v, v.x, v.y); }
    public static Vector4 VXYZ(Vector5 v) { return new Vector4(v.v, v.x, v.y, v.z); }
    public static Vector4 XYZW(Vector5 v) { return new Vector4(v.x, v.y, v.z, v.w); }

    public static Matrix4x4 Cofactor(Matrix4x4 m) {
        return new Matrix4x4(-MakeNormal(m.GetColumn(1), m.GetColumn(2), m.GetColumn(3)),
                             MakeNormal(m.GetColumn(2), m.GetColumn(3), m.GetColumn(0)),
                             -MakeNormal(m.GetColumn(3), m.GetColumn(0), m.GetColumn(1)),
                             MakeNormal(m.GetColumn(0), m.GetColumn(1), m.GetColumn(2)));
    }
    public static Matrix5x5 Cofactor(Matrix5x5 m) {
        return new Matrix5x5(MakeNormal(m.GetColumn(1), m.GetColumn(2), m.GetColumn(3), m.GetColumn(4)),
                             MakeNormal(m.GetColumn(2), m.GetColumn(3), m.GetColumn(4), m.GetColumn(0)),
                             MakeNormal(m.GetColumn(3), m.GetColumn(4), m.GetColumn(0), m.GetColumn(1)),
                             MakeNormal(m.GetColumn(4), m.GetColumn(0), m.GetColumn(1), m.GetColumn(2)),
                             MakeNormal(m.GetColumn(0), m.GetColumn(1), m.GetColumn(2), m.GetColumn(3)));
    }
}
