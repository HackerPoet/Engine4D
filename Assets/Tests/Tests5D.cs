using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Tests5D {
    private readonly Matrix4x4 I = Matrix4x4.identity;

    public static void AssertEqual(Vector2 v1, Vector2 v2, float delta) {
        Assert.AreEqual(v1.x, v2.x, delta);
        Assert.AreEqual(v1.y, v2.y, delta);
    }
    public static void AssertEqual(Vector5 v1, Vector5 v2, float delta) {
        Assert.AreEqual(v1.x, v2.x, delta);
        Assert.AreEqual(v1.y, v2.y, delta);
        Assert.AreEqual(v1.z, v2.z, delta);
        Assert.AreEqual(v1.w, v2.w, delta);
        Assert.AreEqual(v1.v, v2.v, delta);
    }
    public static void AssertEqual(Matrix5x5 m1, Matrix5x5 m2, float delta) {
        AssertEqual(m1.column0, m2.column0, delta);
        AssertEqual(m1.column1, m2.column1, delta);
        AssertEqual(m1.column2, m2.column2, delta);
        AssertEqual(m1.column3, m2.column3, delta);
        AssertEqual(m1.column4, m2.column4, delta);
    }

    [Test]
    public void TestZeroAndIdentity() {
        //Test Zero matrix
        AssertEqual(Matrix5x5.zero.column0, Vector5.zero, 0.0f);
        AssertEqual(Matrix5x5.zero.column1, Vector5.zero, 0.0f);
        AssertEqual(Matrix5x5.zero.column2, Vector5.zero, 0.0f);
        AssertEqual(Matrix5x5.zero.column3, Vector5.zero, 0.0f);
        AssertEqual(Matrix5x5.zero.column4, Vector5.zero, 0.0f);

        //Test Identity matrix
        AssertEqual(Matrix5x5.identity.column0, new Vector5(1, 0, 0, 0, 0), 0.0f);
        AssertEqual(Matrix5x5.identity.column1, new Vector5(0, 1, 0, 0, 0), 0.0f);
        AssertEqual(Matrix5x5.identity.column2, new Vector5(0, 0, 1, 0, 0), 0.0f);
        AssertEqual(Matrix5x5.identity.column3, new Vector5(0, 0, 0, 1, 0), 0.0f);
        AssertEqual(Matrix5x5.identity.column4, new Vector5(0, 0, 0, 0, 1), 0.0f);

        //Test equality on trivial types
        Assert.True(Matrix5x5.zero == Matrix5x5.zero * Matrix5x5.zero);
        Assert.True(Matrix5x5.zero == Matrix5x5.zero * Matrix5x5.identity);
        Assert.True(Matrix5x5.identity == Matrix5x5.identity * Matrix5x5.identity);
        Assert.True(Matrix5x5.identity == Matrix5x5.identity.transpose);
        Assert.True(Matrix5x5.zero != Matrix5x5.identity);
    }

    [Test]
    public void TestDeterminantAndTranspose() {
        Vector5 a = new Vector5(-9, -7, -4, -4, 9);
        Vector5 b = new Vector5(7, 1, -9, -7, 8);
        Vector5 c = new Vector5(-9, -2, -5, 9, 6);
        Vector5 d = new Vector5(-6, 9, -3, -9, 4);
        Vector5 e = new Vector5(-2, 0, -7, 8, 8);
        Matrix5x5 m = new Matrix5x5(a, b, c, d, e);

        Assert.AreEqual(0.0f, Matrix5x5.zero.determinant, 0.0f);
        Assert.AreEqual(1.0f, Matrix5x5.identity.determinant, 0.0f);

        Assert.AreEqual(55647.0f, m.determinant, 1e-5f);
        Assert.AreEqual(55647.0f, m.transpose.determinant, 1e-5f);
        Assert.AreEqual(55647.0f * 55647.0f, (m * m).determinant, 1e-5f);
        Assert.AreEqual(1.0f / 55647.0f, m.inverse.determinant, 1e-5f);

        AssertEqual(m, m.transpose.transpose, 0.0f);
    }

    [Test]
    public void TestInverse() {
        Vector5 a = new Vector5(-9, -7, -4, -4, 9);
        Vector5 b = new Vector5(7, 1, -9, -7, 8);
        Vector5 c = new Vector5(-9, -2, -5, 9, 6);
        Vector5 d = new Vector5(-6, 9, -3, -9, 4);
        Vector5 e = new Vector5(-2, 0, -7, 8, 8);
        Matrix5x5 m = new Matrix5x5(a, b, c, d, e);

        AssertEqual(Matrix5x5.identity, Matrix5x5.identity.inverse, 1e-5f);
        AssertEqual(Matrix5x5.identity, m * m.inverse, 1e-5f);
        AssertEqual(Matrix5x5.identity, m.inverse * m, 1e-5f);
        AssertEqual(m.inverse.transpose, m.transpose.inverse, 1e-5f);
    }


    [Test]
    public void TestFromToRotation() {
        //Test From-To rotation
        Vector5 a = new Vector5(1.0f, 2.0f, -3.0f, 5.0f, 0.5f);
        Vector5 b = new Vector5(2.0f, -4.0f, 2.0f, 1.0f, 3.0f);
        Matrix5x5 R = Transform5D.FromToRotation(a, b);
        Assert.AreEqual(1.0f, R.determinant, 1e-5f);
        AssertEqual(R.transpose, R.inverse, 1e-5f);
        AssertEqual(b.normalized, R * a.normalized, 1e-5f);

        //TODO: Test degenerate 180 degree case
        //a = new Vector5(0.0f, 0.0f, 0.0f, 0.0f, 1.0f);
        //b = new Vector5(0.0f, 0.0f, 0.0f, 0.0f, -1.0f);
        //R = Transform5D.FromToRotation(a, b);
        //Assert.AreEqual(1.0f, R.determinant, 1e-5f);
        //AssertEqual(R.transpose, R.inverse, 1e-5f);
        //AssertEqual(b.normalized, R * a.normalized, 1e-5f);
    }

    [Test]
    public void TestCayleyTransform() {
        Vector5 a = new Vector5(1.0f, 2.0f, -3.0f, 5.0f, 0.5f);
        Vector5 b = new Vector5(2.0f, -4.0f, 2.0f, 1.0f, 3.0f);
        Matrix5x5 R = Transform5D.FromToRotation(a, b);
        AssertEqual(R.transpose, R.inverse, 1e-5f);

        //Test conversion both ways
        Matrix5x5 A = Transform5D.CayleyTransform(R);
        AssertEqual(Transform5D.MatMul(A, -1.0f), A.transpose, 1e-5f);
        Matrix5x5 R2 = Transform5D.CayleyTransform(A);
        AssertEqual(R, R2, 1e-5f);
    }

    [Test]
    public void TestRotationAngle() {
        Vector5 a = new Vector5(1.0f, 2.0f, -3.0f, 5.0f, 0.5f);
        Vector5 b = new Vector5(2.0f, -4.0f, 2.0f, 1.0f, 3.0f);
        Matrix5x5 R = Transform5D.FromToRotation(a, b);

        //Test with random vectors
        float angleAB = Mathf.Rad2Deg * Mathf.Acos(Vector5.Dot(a, b) / (a.magnitude * b.magnitude));
        AssertEqual(new Vector2(angleAB, 0.0f), Transform5D.RotationAngles(R), 1e-5f);

        //Test with single-plane rotations
        Vector5 right =   new Vector5(1,0,0,0,0);
        Vector5 up =      new Vector5(0,1,0,0,0);
        Vector5 forward = new Vector5(0,0,1,0,0);
        Vector5 into =    new Vector5(0,0,0,1,0);
        Vector5 into2 =   new Vector5(0,0,0,0,1);
        AssertEqual(new Vector2(0, 0), Transform5D.RotationAngles(Matrix5x5.identity), 1e-5f);
        AssertEqual(new Vector2(90, 0), Transform5D.RotationAngles(Transform5D.FromToRotation(up, right)), 1e-5f);
        AssertEqual(new Vector2(90, 0), Transform5D.RotationAngles(Transform5D.FromToRotation(forward, right)), 1e-5f);
        AssertEqual(new Vector2(90, 0), Transform5D.RotationAngles(Transform5D.FromToRotation(into, up)), 1e-5f);
        AssertEqual(new Vector2(180, 0), Transform5D.RotationAngles(Transform5D.PlaneRotation(180.0f, 0, 1)), 1e-5f);

        //Test with double-rotations
        Matrix5x5 rot55xy = Transform5D.PlaneRotation(55.0f, 0, 1);
        Matrix5x5 rot80zw = Transform5D.PlaneRotation(80.0f, 2, 3);
        Matrix5x5 rot180xz = Transform5D.PlaneRotation(180.0f, 0, 2);
        Matrix5x5 rot15yv = Transform5D.PlaneRotation(15.0f, 1, 4);
        AssertEqual(new Vector2(80, 55), Transform5D.RotationAngles(rot55xy * rot80zw), 1e-5f);
        AssertEqual(new Vector2(80, 55), Transform5D.RotationAngles(R.transpose * rot55xy * rot80zw * R), 2e-2f);
        AssertEqual(new Vector2(180, 15), Transform5D.RotationAngles(rot180xz * rot15yv), 1e-5f);
        AssertEqual(new Vector2(180, 15), Transform5D.RotationAngles(R * rot180xz * rot15yv * R.transpose), 2e-2f);
    }

    [Test]
    public void TestSlerp() {
        Vector5 a = new Vector5(1.0f, 2.0f, -3.0f, 5.0f, 0.5f);
        Vector5 b = new Vector5(2.0f, -4.0f, 2.0f, 1.0f, 3.0f);
        Vector5 c = new Vector5(0.0f, 1.0f, 2.0f, -3.0f, -4.0f);
        Matrix5x5 m1 = Transform5D.FromToRotation(a, b);
        Matrix5x5 m2 = Transform5D.FromToRotation(a, c);

        //Test bounds
        AssertEqual(m1, Transform5D.Slerp(m1, m2, 0.0f), 1e-5f);
        AssertEqual(m2, Transform5D.Slerp(m1, m2, 1.0f), 1e-5f);

        //Make sure it remains a rotation with different interpolation values
        Matrix5x5 mR1 = Transform5D.Slerp(m1, m2, 0.61f);
        Matrix5x5 mR2 = Transform5D.Slerp(m1, m2, 0.23f);
        AssertEqual(Matrix5x5.identity, mR1 * mR1.transpose, 1e-5f);
        AssertEqual(Matrix5x5.identity, mR2 * mR2.transpose, 1e-5f);

        //Make sure single-rotations have continuous angle
        Matrix5x5 am = m1 * Transform5D.PlaneRotation(50.0f, 0, 1) * m1.transpose;
        Matrix5x5 a80 = Transform5D.Slerp(Matrix5x5.identity, am, 0.80f);
        Matrix5x5 a50 = Transform5D.Slerp(Matrix5x5.identity, am, 0.50f);
        Matrix5x5 a10 = Transform5D.Slerp(Matrix5x5.identity, am, 0.01f);
        AssertEqual(new Vector2(50.0f, 0.0f), Transform5D.RotationAngles(am), 5e-4f);
        AssertEqual(new Vector2(40.0f, 0.0f), Transform5D.RotationAngles(a80), 5e-4f);
        AssertEqual(new Vector2(25.0f, 0.0f), Transform5D.RotationAngles(a50), 5e-4f);
        //TODO: Fix numerical stability here later
        //AssertEqual(new Vector2(0.5f, 0.0f), Transform5D.RotationAngles(a10), 2e-2f);
    }

    [Test]
    public void TestMakeNormal() {
        Matrix5x5 I = Matrix5x5.identity;
        Vector5 n = Transform5D.MakeNormal(I.GetColumn(0), I.GetColumn(1), I.GetColumn(2), I.GetColumn(3));
        AssertEqual(I.GetColumn(4), n, 1e-6f);

        Vector5 a = new Vector5(0.20f, 0.51f, -0.94f, 0.47f, 0.4f);
        Vector5 b = new Vector5(0.22f, -0.11f, 0.34f, 1.47f, 0.1f);
        Vector5 c = new Vector5(0.72f, -0.5f, 0.02f, -0.70f, -0.6f);
        Vector5 d = new Vector5(-0.5f, 0.2f, 0.5f, -0.15f, 0.9f);
        Vector5 e = Transform5D.MakeNormal(a, b, c, d);

        Assert.AreEqual(0.0f, Vector5.Dot(e, a), 1e-5f);
        Assert.AreEqual(0.0f, Vector5.Dot(e, b), 1e-5f);
        Assert.AreEqual(0.0f, Vector5.Dot(e, c), 1e-5f);
        Assert.AreEqual(0.0f, Vector5.Dot(e, d), 1e-5f);

        //Flip parity with a shift either direction
        AssertEqual(-e, Transform5D.MakeNormal(d, a, b, c), 1e-5f);
        AssertEqual(-e, Transform5D.MakeNormal(b, c, d, a), 1e-5f);
        //Flip parity by a single swap
        AssertEqual(-e, Transform5D.MakeNormal(a, b, d, c), 1e-5f);
        AssertEqual(-e, Transform5D.MakeNormal(a, c, b, d), 1e-5f);
        AssertEqual(-e, Transform5D.MakeNormal(b, a, c, d), 1e-5f);
        //Keep parity by reverse or double swap
        AssertEqual(e, Transform5D.MakeNormal(d, c, b, a), 1e-5f);
        AssertEqual(e, Transform5D.MakeNormal(b, a, d, c), 1e-5f);
    }

    [Test]
    public void TestAdjugate() {
        Vector5 a = new Vector5(0.20f, 0.51f, -0.94f, 0.47f, 1.1f);
        Vector5 b = new Vector5(0.22f, -0.11f, 0.34f, 1.47f, 0.4f);
        Vector5 c = new Vector5(0.72f, -0.5f, 0.02f, -0.70f, -1.0f);
        Vector5 d = new Vector5(-1.5f, 0.4f, 0.5f, 0.60f, -0.2f);
        Vector5 e = new Vector5(-0.5f, 0.8f, -0.3f, 1.1f, 0.25f);
        Matrix5x5 m = new Matrix5x5(a, b, c, d, e);

        AssertEqual(m.inverse, Transform5D.MatMul(Transform5D.Adjugate(m), 1.0f / m.determinant), 1e-5f);
    }
}