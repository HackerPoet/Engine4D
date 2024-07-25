using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Isocline {
    public static readonly Isocline identity = new Isocline(Quaternion.identity, Quaternion.identity);

    public Quaternion qL;
    public Quaternion qR;

    public Isocline(Quaternion qL, Quaternion qR) {
        this.qL = qL;
        this.qR = qR;
    }

    public static Isocline FromMatrix(Matrix4x4 m) {
        Vector4 r0 = m.GetRow(0);
        Vector4 r1 = m.GetRow(1);
        Vector4 r2 = m.GetRow(2);
        Vector4 r3 = m.GetRow(3);
        Quaternion qL = new Quaternion(r3.x - r0.w + r1.z - r2.y,
                                       r3.y - r0.z - r1.w + r2.x,
                                       r3.z + r0.y - r1.x - r2.w,
                                       r3.w + r0.x + r1.y + r2.z).normalized;
        Vector4 vR = LeftIsocline(qL) * r3;
        Quaternion qR = new Quaternion(vR.x, vR.y, vR.z, vR.w);
        return new Isocline(qL, qR);
    }
    public static Isocline FromDual(Quaternion r, Quaternion d) {
        return new Isocline(Quaternion.Inverse(r) * d, r * d);
    }
    public static Isocline Euler(float x, float y, float z) {
        Quaternion q = Quaternion.Euler(x, y, z);
        return new Isocline(Quaternion.Inverse(q), q);
    }
    public static Isocline FromToRotation(Vector4 from, Vector4 to) {
        //TODO: Give actual implementation!
        return FromMatrix(Transform4D.FromToRotation(from, to));
    }

    public static Isocline operator*(Isocline i1, Isocline i2) {
        return new Isocline(i2.qL * i1.qL, i1.qR * i2.qR);
    }
    public static Isocline operator*(Isocline i1, Quaternion q) {
        return new Isocline(Quaternion.Inverse(q) * i1.qL, i1.qR * q);
    }
    public static Vector4 operator*(Isocline i, Vector4 v) {
        return i.matrixL * (i.matrixR * v);
    }

    public Isocline inverse {
        get {
            return new Isocline(Quaternion.Inverse(qL), Quaternion.Inverse(qR));
        }
    }
    public static Isocline Inverse(Isocline i) {
        return i.inverse;
    }

    public static Isocline Slerp(Isocline a, Isocline b, float t) {
        return new Isocline(Quaternion.Slerp(a.qL, b.qL, t), Quaternion.Slerp(a.qR, b.qR, t));
    }

    public static Matrix4x4 LeftIsocline(Quaternion q) {
        Matrix4x4 mat = new Matrix4x4();
        mat[0] = q.w;
        mat[1] = -q.z;
        mat[2] = q.y;
        mat[3] = q.x;
        mat[4] = q.z;
        mat[5] = q.w;
        mat[6] = -q.x;
        mat[7] = q.y;
        mat[8] = -q.y;
        mat[9] = q.x;
        mat[10] = q.w;
        mat[11] = q.z;
        mat[12] = -q.x;
        mat[13] = -q.y;
        mat[14] = -q.z;
        mat[15] = q.w;
        return mat;
    }

    public static Matrix4x4 RightIsocline(Quaternion q) {
        Matrix4x4 mat = new Matrix4x4();
        mat[0] = q.w;
        mat[1] = q.z;
        mat[2] = -q.y;
        mat[3] = q.x;
        mat[4] = -q.z;
        mat[5] = q.w;
        mat[6] = q.x;
        mat[7] = q.y;
        mat[8] = q.y;
        mat[9] = -q.x;
        mat[10] = q.w;
        mat[11] = q.z;
        mat[12] = -q.x;
        mat[13] = -q.y;
        mat[14] = -q.z;
        mat[15] = q.w;
        return mat;
    }

    public Matrix4x4 matrixL {
        get { return LeftIsocline(qL); }
    }

    public Matrix4x4 matrixR {
        get { return RightIsocline(qR); }
    }

    public Matrix4x4 matrix {
        get { return matrixL * matrixR; }
    }

    public override string ToString() {
        return "[" + qL + "|" + qR + "]";
    }
}
