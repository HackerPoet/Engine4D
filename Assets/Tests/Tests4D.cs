using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Tests4D {
    private readonly Matrix4x4 I = Matrix4x4.identity;

    public static void AssertEqual(Vector2 v1, Vector2 v2, float delta) {
        Assert.AreEqual(v1.x, v2.x, delta);
        Assert.AreEqual(v1.y, v2.y, delta);
    }
    public static void AssertEqual(Vector3 v1, Vector3 v2, float delta) {
        Assert.AreEqual(v1.x, v2.x, delta);
        Assert.AreEqual(v1.y, v2.y, delta);
        Assert.AreEqual(v1.z, v2.z, delta);
    }
    public static void AssertEqual(Vector4 v1, Vector4 v2, float delta) {
        Assert.AreEqual(v1.x, v2.x, delta);
        Assert.AreEqual(v1.y, v2.y, delta);
        Assert.AreEqual(v1.z, v2.z, delta);
        Assert.AreEqual(v1.w, v2.w, delta);
    }
    public static void AssertEqual(Quaternion q1, Quaternion q2, float delta) {
        Assert.AreEqual(q1.x, q2.x, delta);
        Assert.AreEqual(q1.y, q2.y, delta);
        Assert.AreEqual(q1.z, q2.z, delta);
        Assert.AreEqual(q1.w, q2.w, delta);
    }
    public static void AssertEqual(Isocline i1, Isocline i2, float delta) {
        AssertEqual(i1.qL, i2.qL, delta);
        AssertEqual(i1.qR, i2.qR, delta);
    }
    public static void AssertEqual(Matrix4x4 m1, Matrix4x4 m2, float delta) {
        AssertEqual(m1.GetColumn(0), m2.GetColumn(0), delta);
        AssertEqual(m1.GetColumn(1), m2.GetColumn(1), delta);
        AssertEqual(m1.GetColumn(2), m2.GetColumn(2), delta);
        AssertEqual(m1.GetColumn(3), m2.GetColumn(3), delta);
    }
    public static void AssertEqual(Transform4D t1, Transform4D t2, float delta) {
        AssertEqual(t1.translation, t1.translation, delta);
        AssertEqual(t1.matrix, t1.matrix, delta);
    }
    public static void DebugPrint(Vector4 v) {
        Debug.Log("(" + v.x + " " + v.y + " " + v.z + " " + v.w + ")");
    }
    public static void DebugPrint(Quaternion q) {
        Debug.Log("(" + q.x + " " + q.y + " " + q.z + " " + q.w + ")");
    }
    public static void DebugPrint(Matrix4x4 m) {
        DebugPrint(m.GetRow(0));
        DebugPrint(m.GetRow(1));
        DebugPrint(m.GetRow(2));
        DebugPrint(m.GetRow(3));
    }

    [Test]
    public void TestMakeNormal() {
        Matrix4x4 I = Matrix4x4.identity;
        Vector4 n = Transform4D.MakeNormal(I.GetColumn(0), I.GetColumn(1), I.GetColumn(2));
        AssertEqual(I.GetColumn(3), n, 1e-6f);

        Vector4 a = new Vector4(0.20f, 0.51f, -0.94f, 0.47f);
        Vector4 b = new Vector4(0.22f, -0.11f, 0.34f, 1.47f);
        Vector4 c = new Vector4(0.72f, -0.5f, 0.02f, -0.70f);
        Vector4 d = Transform4D.MakeNormal(a, b, c);

        Assert.AreEqual(0.0f, Vector4.Dot(d, a), 1e-5f);
        Assert.AreEqual(0.0f, Vector4.Dot(d, b), 1e-5f);
        Assert.AreEqual(0.0f, Vector4.Dot(d, c), 1e-5f);

        Vector4 d2 = Transform4D.MakeNormal(c, a, b);
        Vector4 d3 = Transform4D.MakeNormal(b, c, a);
        AssertEqual(d, d2, 1e-5f);
        AssertEqual(d, d3, 1e-5f);

        Vector4 d4 = Transform4D.MakeNormal(a, c, b);
        Vector4 d5 = Transform4D.MakeNormal(c, b, a);
        Vector4 d6 = Transform4D.MakeNormal(b, a, c);
        AssertEqual(-d, d4, 1e-5f);
        AssertEqual(-d, d5, 1e-5f);
        AssertEqual(-d, d6, 1e-5f);
    }

    [Test]
    public void TestIsocline() {
        Quaternion q1 = Quaternion.Euler(10.0f, 50.0f, -20.0f);
        Quaternion q2 = Quaternion.Euler(-40.0f, 60.0f, 80.0f);
        Quaternion q3 = Quaternion.Euler(70.0f, 0.0f, -10.0f);
        Quaternion q4 = Quaternion.Euler(40.0f, 100.0f, -30.0f);
        Vector4 v = new Vector4(1.0f, 2.0f, -3.0f, 5.0f);
        Isocline i1 = new Isocline(q1, q2);
        Isocline i2 = new Isocline(q3, q4);

        //Test isoclinic matrix commutativity
        Matrix4x4 m12 = i1.matrixL * i1.matrixR;
        Matrix4x4 m21 = i1.matrixR * i1.matrixL;
        AssertEqual(m12, m21, 1e-5f);

        //Test that isoclinic matrices are rotations
        AssertEqual(i1.matrixL.transpose, i1.matrixL.inverse, 1e-5f);
        AssertEqual(i1.matrixR.transpose, i1.matrixR.inverse, 1e-5f);
        AssertEqual(i1.matrix.transpose, i1.matrix.inverse, 1e-5f);

        //Test isocline inverse
        AssertEqual(Isocline.identity, i1.inverse * i1, 1e-5f);
        AssertEqual(Isocline.identity, i1 * i1.inverse, 1e-5f);
        AssertEqual(i1.matrix.inverse, i1.inverse.matrix, 1e-5f);

        //Test isocline linearity
        Matrix4x4 mMat = i1.matrix * i2.matrix;
        Matrix4x4 mMul = (i1 * i2).matrix;
        AssertEqual(mMat, mMul, 1e-5f);

        //Test apply to vector
        AssertEqual(i1.matrix * v, i1 * v, 1e-5f);

        //Test apply xyz rotation
        AssertEqual(i1.matrix * Matrix4x4.Rotate(q3), (i1 * q3).matrix, 1e-5f);

        //Test quaternion equivalence when dual is identity
        AssertEqual(Matrix4x4.Rotate(q1), Isocline.FromDual(q1, Quaternion.identity).matrix, 1e-5f);
        AssertEqual(Matrix4x4.Rotate(q2), Isocline.FromDual(q2, Quaternion.identity).matrix, 1e-5f);

        //Test Euler angles from normal quaternion
        AssertEqual(Matrix4x4.Rotate(Quaternion.Euler(10.0f, 40.0f, 70.0f)), Isocline.Euler(10.0f, 40.0f, 70.0f).matrix, 1e-5f);

        //Test that pure rotation and pure dual are simple-rotations
        Vector2 pureRotAngs = Transform4D.RotationAngles(Isocline.FromDual(q1, Quaternion.identity).matrix);
        Vector2 pureDualAngs = Transform4D.RotationAngles(Isocline.FromDual(Quaternion.identity, q1).matrix);
        Assert.AreEqual(0.0f, pureRotAngs.y, 2e-2f);
        Assert.AreEqual(0.0f, pureDualAngs.y, 2e-2f);

        //Test isocline from matrix
        AssertEqual(i1, Isocline.FromMatrix(i1.matrix), 1e-5f);
        AssertEqual(i2, Isocline.FromMatrix(i2.matrix), 1e-5f);

        //Test that isocline is a double-cover
        Quaternion q1Neg = new Quaternion(-q1.x, -q1.y, -q1.z, -q1.w);
        Quaternion q2Neg = new Quaternion(-q2.x, -q2.y, -q2.z, -q2.w);
        AssertEqual(new Isocline(q1, q2) * v, new Isocline(q1Neg, q2Neg) * v, 1e-5f);
        AssertEqual(new Isocline(q1, q2Neg) * v, new Isocline(q1Neg, q2) * v, 1e-5f);

        //Test Slerp
        Matrix4x4 a = i1.matrix * Transform4D.PlaneRotation(100.0f, 0, 1) * i1.matrix.transpose;
        Isocline ai = Isocline.FromMatrix(a);
        Matrix4x4 a80 = Transform4D.Slerp(Matrix4x4.identity, a, 0.80f);
        Matrix4x4 a50 = Transform4D.Slerp(Matrix4x4.identity, a, 0.50f);
        Matrix4x4 a20 = Transform4D.Slerp(Matrix4x4.identity, a, 0.20f);
        Isocline i80 = Isocline.Slerp(Isocline.identity, ai, 0.80f);
        Isocline i50 = Isocline.Slerp(Isocline.identity, ai, 0.50f);
        Isocline i20 = Isocline.Slerp(Isocline.identity, ai, 0.20f);
        AssertEqual(a80, i80.matrix, 1e-5f);
        AssertEqual(a50, i50.matrix, 1e-5f);
        AssertEqual(a20, i20.matrix, 1e-5f);
    }

    [Test]
    public void TestFromToRotation() {
        //Test From-To rotation
        Vector4 a = new Vector4(1.0f, 2.0f, -3.0f, 5.0f);
        Vector4 b = new Vector4(2.0f, -4.0f, 2.0f, 1.0f);
        Matrix4x4 R = Transform4D.FromToRotation(a, b);
        Assert.AreEqual(1.0f, R.determinant, 1e-5f);
        AssertEqual(R.transpose, R.inverse, 1e-5f);
        AssertEqual(b.normalized, R * a.normalized, 1e-5f);

        //Test degenerate 180 degree case
        a = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        b = new Vector4(0.0f, 0.0f, 0.0f, -1.0f);
        R = Transform4D.FromToRotation(a, b);
        Assert.AreEqual(1.0f, R.determinant, 1e-5f);
        AssertEqual(R.transpose, R.inverse, 1e-5f);
        AssertEqual(b.normalized, R * a.normalized, 1e-5f);
    }

    [Test]
    public void TestCayleyTransform() {
        Quaternion q1 = Quaternion.Euler(70.0f, 0.0f, -10.0f);
        Quaternion q2 = Quaternion.Euler(40.0f, 100.0f, -30.0f);
        Matrix4x4 R = new Isocline(q1, q2).matrix;
        AssertEqual(R.transpose, R.inverse, 1e-5f);

        //Test conversion both ways
        Matrix4x4 A = Transform4D.CayleyTransform(R);
        AssertEqual(Transform4D.MatMul(A, -1.0f), A.transpose, 1e-5f);
        Matrix4x4 R2 = Transform4D.CayleyTransform(A);
        AssertEqual(R, R2, 1e-5f);
    }

    [Test]
    public void TestRotationAngle() {
        Vector4 a = new Vector4(1.0f, 2.0f, -3.0f, 5.0f);
        Vector4 b = new Vector4(2.0f, -4.0f, 2.0f, 1.0f);
        Matrix4x4 R = Transform4D.FromToRotation(a, b);

        //Test with random vectors
        float angleAB = Mathf.Rad2Deg * Mathf.Acos(Vector4.Dot(a, b) / (a.magnitude * b.magnitude));
        AssertEqual(new Vector2(angleAB, 0.0f), Transform4D.RotationAngles(R), 1e-5f);

        //Test with single-plane rotations
        Vector4 right =   new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
        Vector4 up =      new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
        Vector4 forward = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 into =    new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        AssertEqual(new Vector2(0, 0), Transform4D.RotationAngles(Matrix4x4.identity), 1e-5f);
        AssertEqual(new Vector2(90, 0), Transform4D.RotationAngles(Transform4D.FromToRotation(up, right)), 1e-5f);
        AssertEqual(new Vector2(90, 0), Transform4D.RotationAngles(Transform4D.FromToRotation(forward, right)), 1e-5f);
        AssertEqual(new Vector2(90, 0), Transform4D.RotationAngles(Transform4D.FromToRotation(into, up)), 1e-5f);
        AssertEqual(new Vector2(180, 0), Transform4D.RotationAngles(Transform4D.PlaneRotation(180.0f, 0, 1)), 1e-5f);

        //Test with double-rotations
        Matrix4x4 rot55xy = Transform4D.PlaneRotation(55.0f, 0, 1);
        Matrix4x4 rot80zw = Transform4D.PlaneRotation(80.0f, 2, 3);
        Matrix4x4 rot180xz = Transform4D.PlaneRotation(180.0f, 0, 2);
        Matrix4x4 rot15yw = Transform4D.PlaneRotation(15.0f, 1, 3);
        AssertEqual(new Vector2(80, 55), Transform4D.RotationAngles(rot55xy * rot80zw), 1e-5f);
        AssertEqual(new Vector2(80, 55), Transform4D.RotationAngles(R.transpose * rot55xy * rot80zw * R), 2e-2f);
        AssertEqual(new Vector2(180, 15), Transform4D.RotationAngles(rot180xz * rot15yw), 1e-5f);
        AssertEqual(new Vector2(180, 15), Transform4D.RotationAngles(R * rot180xz * rot15yw * R.transpose), 2e-2f);
    }

    [Test]
    public void TestAdjugate() {
        Vector4 a = new Vector4(0.20f, 0.51f, -0.94f, 0.47f);
        Vector4 b = new Vector4(0.22f, -0.11f, 0.34f, 1.47f);
        Vector4 c = new Vector4(0.72f, -0.5f, 0.02f, -0.70f);
        Vector4 d = new Vector4(-1.5f, 0.4f, 0.5f, 0.60f);
        Matrix4x4 m = new Matrix4x4(a, b, c, d);

        AssertEqual(m.inverse, Transform4D.MatMul(Transform4D.Adjugate(m), 1.0f / m.determinant), 1e-5f);
    }

    [Test]
    public void TestSlerp() {
        Quaternion q1 = Quaternion.Euler(10.0f, 50.0f, -20.0f);
        Quaternion q2 = Quaternion.Euler(-40.0f, 60.0f, 80.0f);
        Quaternion q3 = Quaternion.Euler(70.0f, 0.0f, -10.0f);
        Quaternion q4 = Quaternion.Euler(40.0f, 100.0f, -30.0f);
        Matrix4x4 m1 = new Isocline(q1, q2).matrix;
        Matrix4x4 m2 = new Isocline(q3, q4).matrix;

        //Test bounds
        AssertEqual(m1, Transform4D.Slerp(m1, m2, 0.0f), 1e-5f);
        AssertEqual(m2, Transform4D.Slerp(m1, m2, 1.0f), 1e-5f);

        //Make sure it remains a rotation with different interpolation values
        Matrix4x4 mR1 = Transform4D.Slerp(m1, m2, 0.61f);
        Matrix4x4 mR2 = Transform4D.Slerp(m1, m2, 0.23f);
        AssertEqual(Matrix4x4.identity, mR1 * mR1.transpose, 1e-5f);
        AssertEqual(Matrix4x4.identity, mR2 * mR2.transpose, 1e-5f);

        //Make sure single-rotations have continuous angle
        Matrix4x4 a = m1 * Transform4D.PlaneRotation(50.0f, 0, 1) * m1.transpose;
        Matrix4x4 a80 = Transform4D.Slerp(Matrix4x4.identity, a, 0.80f);
        Matrix4x4 a50 = Transform4D.Slerp(Matrix4x4.identity, a, 0.50f);
        Matrix4x4 a10 = Transform4D.Slerp(Matrix4x4.identity, a, 0.10f);
        AssertEqual(new Vector2(40.0f, 0.0f), Transform4D.RotationAngles(a80), 5e-4f);
        AssertEqual(new Vector2(25.0f, 0.0f), Transform4D.RotationAngles(a50), 5e-4f);
        //TODO: Make numerically stable
        //AssertEqual(new Vector2(5.0f, 0.0f), Transform4D.RotationAngles(a10), 5e-4f);
    }

    [Test]
    public void TestProjections() {
        Vector4 a = new Vector4(0.20f, 0.51f, -0.94f, 0.47f);
        Vector4 b = new Vector4(0.22f, -0.11f, 0.34f, 1.47f);
        Vector4 c = new Vector4(0.72f, -0.5f, 0.02f, -0.70f);
        Vector4 d = new Vector4(-1.5f, 0.4f, 0.5f, 0.60f);

        //Line projection
        Vector4 d_on_a = Transform4D.Project(d, a);
        Assert.AreEqual(0.0f, Vector4.Dot(d - d_on_a, a), 1e-6f);
        AssertEqual(d_on_a, Transform4D.Project(d_on_a, a), 1e-6f);

        //Plane projection
        Vector4 d_on_ab = Transform4D.Project(d, a, b);
        Assert.AreEqual(0.0f, Vector4.Dot(d - d_on_ab, a), 1e-6f);
        Assert.AreEqual(0.0f, Vector4.Dot(d - d_on_ab, b), 1e-6f);
        AssertEqual(d_on_ab, Transform4D.Project(d_on_ab, a, b), 1e-6f);

        //Space projection
        Vector4 d_on_abc = Transform4D.Project(d, a, b, c);
        Assert.AreEqual(0.0f, Vector4.Dot(d - d_on_abc, a), 1e-6f);
        Assert.AreEqual(0.0f, Vector4.Dot(d - d_on_abc, b), 1e-6f);
        Assert.AreEqual(0.0f, Vector4.Dot(d - d_on_abc, c), 1e-6f);
        AssertEqual(d_on_abc, Transform4D.Project(d_on_abc, a, b, c), 1e-6f);
        Assert.AreEqual(1.0f, Mathf.Abs(Vector4.Dot((d - d_on_abc).normalized, Transform4D.MakeNormal(a, b, c).normalized)), 1e-6f);
    }

    [Test]
    public void TestAxisRotation() {
        Quaternion qr = Quaternion.Euler(10.0f, 50.0f, 20.0f);
        Quaternion qd = Quaternion.Euler(70.0f, 20.0f, -10.0f);
        Quaternion qdSqr = qd * qd;
        Vector4 vd = new Vector4(qdSqr.x, qdSqr.y, qdSqr.z, qdSqr.w);
        Matrix4x4 m = new AxisRotation(qr, qd).matrix;

        //Make sure this is a proper rotation
        AssertEqual(m.transpose, m.inverse, 1e-5f);

        //Make sure it has the property that it maps the w vector to the axis
        AssertEqual(vd, m * new Vector4(0, 0, 0, 1), 1e-5f);

        //Try to recover the quaternions from the matrix
        AxisRotation a = AxisRotation.FromMatrix(m);
        AssertEqual(qr, a.r, 1e-5f);
        AssertEqual(qd, a.d, 1e-5f);

        //Test the from-to rotation
        Vector4 c = new Vector4(0.30f, -0.21f, 0.68f, 0.64f);
        Vector4 d = new Vector4(0.07f, 0.01f, 0.02f, -0.64f);
        AssertEqual(d.normalized, AxisRotation.FromToRotation(c, d) * c.normalized, 1e-5f);
    }

    [Test]
    public void TestRotateTowards() {
        Vector4 a = new Vector4(0.22f, -0.11f, 0.34f, 1.47f).normalized;
        Vector4 b = new Vector4(0.72f, -0.5f, 0.02f, -0.70f).normalized;

        Vector4 a2 = Transform4D.RotateTowards(a, b, 30.0f);
        Vector4 a3 = Transform4D.RotateTowards(a2, b, 30.0f);
        Vector4 a4 = Transform4D.RotateTowards(a3, b, 30.0f);
        Vector4 a5 = Transform4D.RotateTowards(a4, b, 30.0f);

        Assert.AreEqual(1.0f, a2.magnitude, 1e-6f);
        Assert.AreEqual(30.0f, Transform4D.Angle(a, a2), 1e-4f);
        Assert.AreEqual(1.0f, a3.magnitude, 1e-6f);
        Assert.AreEqual(60.0f, Transform4D.Angle(a, a3), 1e-4f);
        Assert.AreEqual(1.0f, a4.magnitude, 1e-6f);
        Assert.AreEqual(90.0f, Transform4D.Angle(a, a4), 1e-4f);
        Assert.AreEqual(1.0f, a5.magnitude, 1e-6f);
        AssertEqual(b, a5, 1e-6f);

        //Must still work when the vectors are 180 degrees apart, any axis to rotate is fine.
        Vector4 a6 = Transform4D.RotateTowards(a, -a, 90.0f);
        Assert.AreEqual(1.0f, a6.magnitude, 1e-6f);
        Assert.AreEqual(0.0f, Vector4.Dot(a6, a), 1e-6f);
    }

    [Test]
    public void TestOrthoIter() {
        Quaternion q1 = Quaternion.Euler(10.0f, 50.0f, -20.0f);
        Quaternion q2 = Quaternion.Euler(-40.0f, 60.0f, 80.0f);
        Quaternion q3 = Quaternion.Euler(70.0f, 0.0f, -10.0f);
        Quaternion q4 = Quaternion.Euler(40.0f, 100.0f, -30.0f);
        Matrix4x4 m1 = new Isocline(q1, q2).matrix;
        Matrix4x4 m2 = new Isocline(q3, q4).matrix;

        //Make sure the original matrix is orthogonal
        AssertEqual(Matrix4x4.identity, m1 * m1.transpose, 1e-5f);
        AssertEqual(Matrix4x4.identity, m2 * m2.transpose, 1e-5f);

        //Should not change the matrix if it's already orthogonal
        AssertEqual(m1, Transform4D.OrthoIterate(m1), 1e-5f);
        AssertEqual(m2, Transform4D.OrthoIterate(m2), 1e-5f);

        //Create a new matrix that is not orthogonal, make sure it gets there after a few iterations
        Matrix4x4 m3 = Transform4D.MatAdd(m1, m2);
        m3 = Transform4D.OrthoIterate(m3);
        m3 = Transform4D.OrthoIterate(m3);
        m3 = Transform4D.OrthoIterate(m3);
        AssertEqual(Matrix4x4.identity, m3 * m3.transpose, 1e-5f);
    }

    [Test]
    public void TestTransform() {
        Matrix4x4 A = new Isocline(Quaternion.Euler(10.0f, 50.0f, -20.0f), Quaternion.Euler(-40.0f, 60.0f, 80.0f)).matrix;
        Matrix4x4 B = new Isocline(Quaternion.Euler(70.0f, 0.0f, -10.0f), Quaternion.Euler(40.0f, 100.0f, -30.0f)).matrix;
        Vector4 a = new Vector4(0.20f, 0.51f, -0.94f, 0.47f);
        Vector4 b = new Vector4(0.22f, -0.11f, 0.34f, 1.47f);
        Vector4 x = new Vector4(0.72f, -0.5f, 0.02f, -0.70f);
        Transform4D TA = new Transform4D(A, a);
        Transform4D TB = new Transform4D(B, b);

        //Test zero transform
        AssertEqual(Transform4D.zero, Transform4D.zero * TA, 1e-5f);
        AssertEqual(Transform4D.zero, TA * Transform4D.zero, 1e-5f);
        AssertEqual(Vector4.zero, Transform4D.zero * x, 1e-5f);

        //Test identity transform
        AssertEqual(TA, Transform4D.identity * TA, 1e-5f);
        AssertEqual(TA, TA * Transform4D.identity, 1e-5f);
        AssertEqual(x, Transform4D.identity * x, 1e-5f);

        //Test inverse
        AssertEqual(Transform4D.identity, TA * TA.inverse, 1e-5f);
        AssertEqual(Transform4D.identity, TA.inverse * TA, 1e-5f);

        //Test associativity
        AssertEqual(TA * (TB * x), (TA * TB) * x, 1e-5f);
    }
}