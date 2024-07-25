using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AxisRotation {
    public static readonly AxisRotation identity = new AxisRotation(Quaternion.identity, Quaternion.identity);

    public Quaternion r;
    public Quaternion d;

    public AxisRotation(Quaternion r, Quaternion d) {
        this.r = r;
        this.d = d;
    }

    public static AxisRotation FromMatrix(Matrix4x4 m) {
        Vector4 mc3 = m.GetColumn(3);
        Quaternion d = new Quaternion(mc3.x, mc3.y, mc3.z, mc3.w);
        Matrix4x4 ma = Isocline.LeftIsocline(d) * m;
        ma.m03 = 0.0f;
        ma.m13 = 0.0f;
        ma.m23 = 0.0f;
        ma.m33 = 1.0f;
        ma.m32 = 0.0f;
        ma.m31 = 0.0f;
        ma.m30 = 0.0f;
        return new AxisRotation(ma.rotation, QSqrt(d));
    }
    public static AxisRotation FromToRotation(Vector4 from, Vector4 to) {
        //TODO: Give actual implementation!
        return FromMatrix(Transform4D.FromToRotation(from, to));
    }

    public static AxisRotation operator *(AxisRotation a1, AxisRotation a2) {
        //TODO: Give actual implementation!
        return FromMatrix(a1.matrix * a2.matrix);
    }
    public static AxisRotation operator *(AxisRotation a, Quaternion q) {
        return new AxisRotation(a.r * q, a.d);
    }
    public static Vector4 operator *(AxisRotation a, Vector4 v) {
        //TODO: Give actual implementation!
        return a.matrix * v;
    }

    public AxisRotation inverse {
        get {
            //TODO: Give actual implementation!
            return FromMatrix(matrix.inverse);
        }
    }
    public static AxisRotation Inverse(AxisRotation i) {
        return i.inverse;
    }

    public static AxisRotation Slerp(AxisRotation a, AxisRotation b, float t) {
        //TODO: Validate that this works well enough
        return new AxisRotation(Quaternion.Slerp(a.r, b.r, t), Quaternion.Slerp(a.d, b.d, t));
    }

    public Matrix4x4 matrix {
        get {
            Matrix4x4 a = Matrix4x4.Rotate(r);
            Matrix4x4 b = Isocline.LeftIsocline(d * d).transpose;
            return b * a;
        }
    }

    public static Quaternion QSqrt(Quaternion a) {
        if (a.w <= -0.99f) {
            return new Quaternion(a.x, a.y, a.z + 0.01f, a.w + 1.0f).normalized;
        } else {
            return new Quaternion(a.x, a.y, a.z, a.w + 1.0f).normalized;
        }
    }
}
