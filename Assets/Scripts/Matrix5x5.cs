using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Matrix5x5 : IEquatable<Matrix5x5> {
    public Vector5 column0;
    public Vector5 column1;
    public Vector5 column2;
    public Vector5 column3;
    public Vector5 column4;

    public Matrix5x5(Vector5 column0, Vector5 column1, Vector5 column2, Vector5 column3, Vector5 column4) {
        this.column0 = column0;
        this.column1 = column1;
        this.column2 = column2;
        this.column3 = column3;
        this.column4 = column4;
    }

    public float this[int index] {
        get { return this[index / 5, index % 5]; }
        set { this[index / 5, index % 5] = value; }
    }
    public float this[int row, int column] {
        get {
            switch (column) {
                default:
                case 0: return column0[row];
                case 1: return column1[row];
                case 2: return column2[row];
                case 3: return column3[row];
                case 4: return column4[row];
            }
        }
        set {
            switch (column) {
                default:
                case 0: column0[row] = value; break;
                case 1: column1[row] = value; break;
                case 2: column2[row] = value; break;
                case 3: column3[row] = value; break;
                case 4: column4[row] = value; break;
            }
        }
    }

    public static Matrix5x5 zero = new Matrix5x5(Vector5.zero, Vector5.zero, Vector5.zero, Vector5.zero, Vector5.zero);
    public static Matrix5x5 identity = new Matrix5x5(new Vector5(1, 0, 0, 0, 0), new Vector5(0, 1, 0, 0, 0), new Vector5(0, 0, 1, 0, 0), new Vector5(0, 0, 0, 1, 0), new Vector5(0, 0, 0, 0, 1));

    public float determinant {
        get {
            return column0.v * new Matrix4x4((Vector4)column1, (Vector4)column2, (Vector4)column3, (Vector4)column4).determinant -
                   column1.v * new Matrix4x4((Vector4)column0, (Vector4)column2, (Vector4)column3, (Vector4)column4).determinant +
                   column2.v * new Matrix4x4((Vector4)column0, (Vector4)column1, (Vector4)column3, (Vector4)column4).determinant -
                   column3.v * new Matrix4x4((Vector4)column0, (Vector4)column1, (Vector4)column2, (Vector4)column4).determinant +
                   column4.v * new Matrix4x4((Vector4)column0, (Vector4)column1, (Vector4)column2, (Vector4)column3).determinant;
        }
    }

    public Matrix5x5 transpose {
        get {
            return new Matrix5x5(new Vector5(column0.x, column1.x, column2.x, column3.x, column4.x),
                                 new Vector5(column0.y, column1.y, column2.y, column3.y, column4.y),
                                 new Vector5(column0.z, column1.z, column2.z, column3.z, column4.z),
                                 new Vector5(column0.w, column1.w, column2.w, column3.w, column4.w),
                                 new Vector5(column0.v, column1.v, column2.v, column3.v, column4.v));
        }
    }

    public Matrix5x5 inverse {
        get {
            Matrix5x5 mt = this;
            Matrix5x5 result = identity;
            for (int h = 0; h < 5; ++h) {
                //Find row with largest value to use as the pivot
                int maxIx = h;
                float maxValue = 0.0f;
                for (int i = h; i < 5; ++i) {
                    float v = Mathf.Abs(mt.GetColumn(i)[h]);
                    if (v > maxValue) {
                        maxIx = i;
                        maxValue = v;
                    }
                }

                //Swap the row to the pivot
                if (h != maxIx) {
                    Vector5 temp = mt.GetColumn(maxIx);
                    mt.SetColumn(maxIx, mt.GetColumn(h));
                    mt.SetColumn(h, temp);
                    temp = result.GetColumn(maxIx);
                    result.SetColumn(maxIx, result.GetColumn(h));
                    result.SetColumn(h, temp);
                }

                //Reduce all rows below the pivot
                Vector5 pivotRow = mt.GetColumn(h);
                Vector5 resultPivotRow = result.GetColumn(h);
                for (int i = h + 1; i < 5; ++i) {
                    Vector5 curRow = mt.GetColumn(i);
                    float f = curRow[h] / pivotRow[h];
                    mt.SetColumn(i, curRow - pivotRow * f);
                    result.SetColumn(i, result.GetColumn(i) - resultPivotRow * f);
                }
            }

            //Reduce row echelon
            for (int h = 4; h >= 0; --h) {
                float pivotVal = mt.GetColumn(h)[h];
                Vector5 pivotRow = mt.GetColumn(h) / pivotVal;
                Vector5 resultPivotRow = result.GetColumn(h) / pivotVal;
                mt.SetColumn(h, pivotRow);
                result.SetColumn(h, resultPivotRow);
                for (int i = h - 1; i >= 0; --i) {
                    float f = mt.GetColumn(i)[h];
                    mt.SetColumn(i, mt.GetColumn(i) - pivotRow * f);
                    result.SetColumn(i, result.GetColumn(i) - resultPivotRow * f);
                }
            }
            return result;
        }
    }

    public static Matrix5x5 Scale(Vector5 vector) {
        return new Matrix5x5(new Vector5(vector.x, 0, 0, 0, 0),
                             new Vector5(0, vector.y, 0, 0, 0),
                             new Vector5(0, 0, vector.z, 0, 0),
                             new Vector5(0, 0, 0, vector.w, 0),
                             new Vector5(0, 0, 0, 0, vector.v));
    }

    public override bool Equals(object other) { return this == (Matrix5x5)other; }
    public bool Equals(Matrix5x5 other) { return this == other; }
    public override int GetHashCode() {
        return column0.GetHashCode() + 17 * (column1.GetHashCode() + 17 * (column2.GetHashCode() + 17 * (column3.GetHashCode() + 17 * column4.GetHashCode())));
    }

    public Vector5 GetColumn(int index) {
        switch (index) {
            default:
            case 0: return column0;
            case 1: return column1;
            case 2: return column2;
            case 3: return column3;
            case 4: return column4;
        }
    }
    public void SetColumn(int index, Vector5 column) {
        switch (index) {
            default:
            case 0: column0 = column; break;
            case 1: column1 = column; break;
            case 2: column2 = column; break;
            case 3: column3 = column; break;
            case 4: column4 = column; break;
        }
    }

    public Vector5 GetRow(int index) {
        switch (index) {
            default:
            case 0: return new Vector5(column0.x, column1.x, column2.x, column3.x, column4.x);
            case 1: return new Vector5(column0.y, column1.y, column2.y, column3.y, column4.y);
            case 2: return new Vector5(column0.z, column1.z, column2.z, column3.z, column4.z);
            case 3: return new Vector5(column0.w, column1.w, column2.w, column3.w, column4.w);
            case 4: return new Vector5(column0.v, column1.v, column2.v, column3.v, column4.v);
        }
    }
    public void SetRow(int index, Vector5 row) {
        switch (index) {
            default:
            case 0: column0.x = row.x; column1.x = row.y; column2.x = row.z; column3.x = row.w; column4.x = row.v; break;
            case 1: column0.y = row.x; column1.y = row.y; column2.y = row.z; column3.y = row.w; column4.y = row.v; break;
            case 2: column0.z = row.x; column1.z = row.y; column2.z = row.z; column3.z = row.w; column4.z = row.v; break;
            case 3: column0.w = row.x; column1.w = row.y; column2.w = row.z; column3.w = row.w; column4.w = row.v; break;
            case 4: column0.v = row.x; column1.v = row.y; column2.v = row.z; column3.v = row.w; column4.v = row.v; break;
        }
    }

    public void ToShaderVars(out Matrix4x4 m, out Vector4 vc, out Vector4 vr, out float vv) {
        m = new Matrix4x4((Vector4)column0, (Vector4)column1, (Vector4)column2, (Vector4)column3);
        vc = (Vector4)column4;
        vr = (Vector4)GetRow(4);
        vv = column4.v;
    }
    public static Matrix5x5 FromShaderVars(Matrix4x4 m, Vector4 vc, Vector4 vr, float vv) {
        Matrix5x5 result = (Matrix5x5)m;
        result.column4 = (Vector5)vc;
        result.SetRow(4, new Vector5(vr.x, vr.y, vr.z, vr.w, vv));
        return result;
    }

    public override string ToString() {
        return "[" + GetRow(0) + "\n " + GetRow(1) + "\n " + GetRow(2) + "\n " + GetRow(3) + "\n " + GetRow(4) + "]";
    }

    public static Vector5 operator*(Matrix5x5 lhs, Vector5 vector) {
        return lhs.column0 * vector.x + lhs.column1 * vector.y + lhs.column2 * vector.z + lhs.column3 * vector.w + lhs.column4 * vector.v;
    }
    public static Matrix5x5 operator*(Matrix5x5 lhs, Matrix5x5 rhs) {
        return new Matrix5x5(lhs * rhs.column0, lhs * rhs.column1, lhs * rhs.column2, lhs * rhs.column3, lhs * rhs.column4);
    }
    public static bool operator==(Matrix5x5 lhs, Matrix5x5 rhs) {
        return lhs.column0 == rhs.column0 && lhs.column1 == rhs.column1 && lhs.column2 == rhs.column2 && lhs.column3 == rhs.column3 && lhs.column4 == rhs.column4;
    }
    public static bool operator!=(Matrix5x5 lhs, Matrix5x5 rhs) {
        return !(lhs == rhs);
    }

    public static explicit operator Matrix4x4(Matrix5x5 v) { return new Matrix4x4((Vector4)v.column0, (Vector4)v.column1, (Vector4)v.column2, (Vector4)v.column3); }
    public static explicit operator Matrix5x5(Matrix4x4 v) { return new Matrix5x5((Vector5)v.GetColumn(0), (Vector5)v.GetColumn(1), (Vector5)v.GetColumn(2), (Vector5)v.GetColumn(3), new Vector5(0,0,0,0,1)); }
}
