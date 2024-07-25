using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// Summary:
//     Representation of five-dimensional vectors.
[System.Serializable]
public struct Vector5 : IEquatable<Vector5> {
    public const float kEpsilon = 1e-5F;
    public float x;
    public float y;
    public float z;
    public float w;
    public float v;

    public Vector5(float x, float y, float z, float w, float v) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
        this.v = v;
    }
    public Vector5(Vector4 v4, float v) {
        this.x = v4.x;
        this.y = v4.y;
        this.z = v4.z;
        this.w = v4.w;
        this.v = v;
    }

    public float this[int index] {
        get {
            switch(index) {
                default:
                case 0: return x;
                case 1: return y;
                case 2: return z;
                case 3: return w;
                case 4: return v;
            }
        }
        set {
            switch (index) {
                default:
                case 0: x = value; break;
                case 1: y = value; break;
                case 2: z = value; break;
                case 3: w = value; break;
                case 4: v = value; break;
            }
        }
    }


    public static readonly Vector5 one = new Vector5(1, 1, 1, 1, 1);
    public static readonly Vector5 zero = new Vector5(0, 0, 0, 0, 0);
    public static Vector5 positiveInfinity = new Vector5(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
    public static Vector5 negativeInfinity = new Vector5(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

    public Vector5 normalized { get { return this / magnitude; } }
    public float magnitude { get { return Mathf.Sqrt(sqrMagnitude); } }
    public float sqrMagnitude { get { return x * x + y * y + z * z + w * w + v * v; } }

    public static float Distance(Vector5 a, Vector5 b) { return (a - b).magnitude; }
    public static float Dot(Vector5 a, Vector5 b) { return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w + a.v * b.v; }
    public static Vector5 Lerp(Vector5 a, Vector5 b, float t) { return LerpUnclamped(a, b, Mathf.Clamp01(t)); }
    public static Vector5 LerpUnclamped(Vector5 a, Vector5 b, float t) { return a - (a - b) * t; }
    public static Vector5 Max(Vector5 lhs, Vector5 rhs) {
        return new Vector5(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z), Mathf.Max(lhs.w, rhs.w), Mathf.Max(lhs.v, rhs.v));
    }
    public static Vector5 Min(Vector5 lhs, Vector5 rhs) {
        return new Vector5(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z), Mathf.Min(lhs.w, rhs.w), Mathf.Min(lhs.v, rhs.v));
    }
    public static Vector5 Project(Vector5 a, Vector5 b) { return b * (Dot(a, b) / b.sqrMagnitude); }
    public static Vector5 Scale(Vector5 a, Vector5 b) { return new Vector5(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w, a.v * b.v); }

    public override bool Equals(object other) { return this == (Vector5)other; }
    public bool Equals(Vector5 other) { return this == other; }
    public override int GetHashCode() {
        return x.GetHashCode() + 23 * (y.GetHashCode() + 23 * (z.GetHashCode() + 23 * (w.GetHashCode() + 23 * v.GetHashCode())));
    }

    public void Normalize() { this = normalized; }
    public void Scale(Vector5 scale) { this = Scale(this, scale); }
    public void Set(float newX, float newY, float newZ, float newW, float newV) { x = newX; y = newY; z = newZ; w = newW; v = newV; }
    public override string ToString() { return "(" + x + ", " + y + ", " + z + ", " + w + ", " + v + ")"; }

    public static Vector5 operator +(Vector5 a, Vector5 b) { return new Vector5(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w, a.v + b.v); }
    public static Vector5 operator -(Vector5 a) { return new Vector5(-a.x, -a.y, -a.z, -a.w, -a.v); }
    public static Vector5 operator -(Vector5 a, Vector5 b) { return new Vector5(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w, a.v - b.v); }
    public static Vector5 operator *(float d, Vector5 a) { return new Vector5(d * a.x, d * a.y, d * a.z, d * a.w, d * a.v); }
    public static Vector5 operator *(Vector5 a, float d) { return new Vector5(d * a.x, d * a.y, d * a.z, d * a.w, d * a.v); }
    public static Vector5 operator /(Vector5 a, float d) { return new Vector5(a.x / d, a.y / d, a.z / d, a.w / d, a.v / d); }
    public static bool operator ==(Vector5 lhs, Vector5 rhs) { return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w && lhs.v == rhs.v; }
    public static bool operator !=(Vector5 lhs, Vector5 rhs) { return !(lhs == rhs); }

    public static explicit operator Vector2(Vector5 v) { return new Vector2(v.x, v.y); }
    public static explicit operator Vector5(Vector2 v) { return new Vector5(v.x, v.y, 0, 0, 0); }
    public static explicit operator Vector3(Vector5 v) { return new Vector3(v.x, v.y, v.z); }
    public static explicit operator Vector5(Vector3 v) { return new Vector5(v.x, v.y, v.z, 0, 0); }
    public static explicit operator Vector4(Vector5 v) { return new Vector4(v.x, v.y, v.z, v.w); }
    public static explicit operator Vector5(Vector4 v) { return new Vector5(v.x, v.y, v.z, v.w, 0); }
}
