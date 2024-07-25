using UnityEngine;

public static class PseudoRandom {
    public static uint _seed = 0;
    public static float Float() {
        unchecked {
            _seed += 5381;
            _seed = (_seed << 5) ^ (_seed >> 3);
            return _seed / (float)uint.MaxValue;
        }
    }
    public static Vector2 Uniform2D() {
        return new Vector2(Float(), Float());
    }
    public static Vector3 Uniform3D() {
        return new Vector3(Float(), Float(), Float());
    }
    public static Vector4 Uniform4D() {
        return new Vector4(Float(), Float(), Float(), Float());
    }
    public static Vector5 Uniform5D() {
        return new Vector5(Float(), Float(), Float(), Float(), Float());
    }
    public static float Range(float min, float max) {
        return min + Float() * (max - min);
    }
    public static float Normal() {
        float u1 = Mathf.Max(Float(), float.Epsilon);
        float u2 = 2.0f * Mathf.PI * Float();
        return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(u2);
    }
    public static void NormalPair(out float n1, out float n2) {
        float u1 = Mathf.Max(Float(), float.Epsilon);
        float u2 = 2.0f * Mathf.PI * Float();
        float m = Mathf.Sqrt(-2.0f * Mathf.Log(u1));
        n1 = m * Mathf.Cos(u2);
        n2 = m * Mathf.Sin(u2);
    }
    public static Vector2 Normal2D() {
        NormalPair(out float x, out float y);
        return new Vector2(x, y);
    }
    public static Vector3 Normal3D() {
        NormalPair(out float x, out float y);
        return new Vector3(x, y, Normal());
    }
    public static Vector4 Normal4D() {
        NormalPair(out float x, out float y);
        NormalPair(out float z, out float w);
        return new Vector4(x, y, z, w);
    }
    public static Vector5 Normal5D() {
        NormalPair(out float x, out float y);
        NormalPair(out float z, out float w);
        return new Vector5(x, y, z, w, Normal());
    }
    public static Vector2 Sphere2D() {
        return Normal2D().normalized;
    }
    public static Vector3 Sphere3D() {
        return Normal3D().normalized;
    }
    public static Vector4 Sphere4D() {
        return Normal4D().normalized;
    }
    public static Vector5 Sphere5D() {
        return Normal5D().normalized;
    }
    public static Vector2 Ball2D() {
        return Sphere2D() * Mathf.Pow(Float(), 1.0f / 2.0f);
    }
    public static Vector3 Ball3D() {
        return Sphere3D() * Mathf.Pow(Float(), 1.0f / 3.0f);
    }
    public static Vector4 Ball4D() {
        return Sphere4D() * Mathf.Pow(Float(), 1.0f / 4.0f);
    }
    public static Vector5 Ball5D() {
        return Sphere5D() * Mathf.Pow(Float(), 1.0f / 5.0f);
    }
    public static Vector2 Ball2D(float minR, float maxR) {
        minR = Mathf.Pow(minR, 2.0f);
        maxR = Mathf.Pow(maxR, 2.0f);
        return Sphere2D() * Mathf.Pow(Range(minR, maxR), 1.0f / 2.0f);
    }
    public static Vector3 Ball3D(float minR, float maxR) {
        minR = Mathf.Pow(minR, 3.0f);
        maxR = Mathf.Pow(maxR, 3.0f);
        return Sphere3D() * Mathf.Pow(Range(minR, maxR), 1.0f / 3.0f);
    }
    public static Vector4 Ball4D(float minR, float maxR) {
        minR = Mathf.Pow(minR, 4.0f);
        maxR = Mathf.Pow(maxR, 4.0f);
        return Sphere4D() * Mathf.Pow(Range(minR, maxR), 1.0f / 4.0f);
    }
    public static Vector5 Ball5D(float minR, float maxR) {
        minR = Mathf.Pow(minR, 5.0f);
        maxR = Mathf.Pow(maxR, 5.0f);
        return Sphere5D() * Mathf.Pow(Range(minR, maxR), 1.0f / 5.0f);
    }
    public static Quaternion Rotation3D() {
        Vector4 v = Sphere4D();
        return new Quaternion(v.x, v.y, v.z, v.w);
    }
    public static Isocline Rotation4D() {
        return new Isocline(Rotation3D(), Rotation3D());
    }
    public static Matrix4x4 RotationMatrix4D() {
        return Rotation4D().matrix;
    }
    public static Matrix5x5 RotationMatrix5D() {
        return Transform5D.FromToRotation(new Vector5(0,0,0,0,1), Sphere5D()) * (Matrix5x5)RotationMatrix4D();
    }
}
