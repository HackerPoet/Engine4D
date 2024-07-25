using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public class GenerateGroups {
    private static readonly int[][] A4 = new int[][] {
        new int[] {0, 1, 2, 3, 4 },
        new int[] {0, 1, 3, 2, 4 },
        new int[] {0, 2, 1, 3, 4 },
        new int[] {0, 2, 3, 1, 4 },
        new int[] {0, 3, 1, 2, 4 },
        new int[] {0, 3, 2, 1, 4 },
        new int[] {1, 0, 2, 3, 4 },
        new int[] {1, 0, 3, 2, 4 },
        new int[] {1, 2, 0, 3, 4 },
        new int[] {1, 2, 3, 0, 4 },
        new int[] {1, 3, 0, 2, 4 },
        new int[] {1, 3, 2, 0, 4 },
        new int[] {2, 1, 0, 3, 4 },
        new int[] {2, 1, 3, 0, 4 },
        new int[] {2, 0, 1, 3, 4 },
        new int[] {2, 0, 3, 1, 4 },
        new int[] {2, 3, 1, 0, 4 },
        new int[] {2, 3, 0, 1, 4 },
        new int[] {3, 1, 2, 0, 4 },
        new int[] {3, 1, 0, 2, 4 },
        new int[] {3, 2, 1, 0, 4 },
        new int[] {3, 2, 0, 1, 4 },
        new int[] {3, 0, 1, 2, 4 },
        new int[] {3, 0, 2, 1, 4 },
    };
    private static readonly int[][] A5 = new int[][] {
        new int[] {0, 1, 2, 4, 3 },
        new int[] {0, 1, 3, 4, 2 },
        new int[] {0, 2, 1, 4, 3 },
        new int[] {0, 2, 3, 4, 1 },
        new int[] {0, 3, 1, 4, 2 },
        new int[] {0, 3, 2, 4, 1 },
        new int[] {1, 0, 2, 4, 3 },
        new int[] {1, 0, 3, 4, 2 },
        new int[] {1, 2, 0, 4, 3 },
        new int[] {1, 2, 3, 4, 0 },
        new int[] {1, 3, 0, 4, 2 },
        new int[] {1, 3, 2, 4, 0 },
        new int[] {2, 1, 0, 4, 3 },
        new int[] {2, 1, 3, 4, 0 },
        new int[] {2, 0, 1, 4, 3 },
        new int[] {2, 0, 3, 4, 1 },
        new int[] {2, 3, 1, 4, 0 },
        new int[] {2, 3, 0, 4, 1 },
        new int[] {3, 1, 2, 4, 0 },
        new int[] {3, 1, 0, 4, 2 },
        new int[] {3, 2, 1, 4, 0 },
        new int[] {3, 2, 0, 4, 1 },
        new int[] {3, 0, 1, 4, 2 },
        new int[] {3, 0, 2, 4, 1 },
        new int[] {0, 1, 4, 2, 3 },
        new int[] {0, 1, 4, 3, 2 },
        new int[] {0, 2, 4, 1, 3 },
        new int[] {0, 2, 4, 3, 1 },
        new int[] {0, 3, 4, 1, 2 },
        new int[] {0, 3, 4, 2, 1 },
        new int[] {1, 0, 4, 2, 3 },
        new int[] {1, 0, 4, 3, 2 },
        new int[] {1, 2, 4, 0, 3 },
        new int[] {1, 2, 4, 3, 0 },
        new int[] {1, 3, 4, 0, 2 },
        new int[] {1, 3, 4, 2, 0 },
        new int[] {2, 1, 4, 0, 3 },
        new int[] {2, 1, 4, 3, 0 },
        new int[] {2, 0, 4, 1, 3 },
        new int[] {2, 0, 4, 3, 1 },
        new int[] {2, 3, 4, 1, 0 },
        new int[] {2, 3, 4, 0, 1 },
        new int[] {3, 1, 4, 2, 0 },
        new int[] {3, 1, 4, 0, 2 },
        new int[] {3, 2, 4, 1, 0 },
        new int[] {3, 2, 4, 0, 1 },
        new int[] {3, 0, 4, 1, 2 },
        new int[] {3, 0, 4, 2, 1 },
        new int[] {0, 4, 1, 2, 3 },
        new int[] {0, 4, 1, 3, 2 },
        new int[] {0, 4, 2, 1, 3 },
        new int[] {0, 4, 2, 3, 1 },
        new int[] {0, 4, 3, 1, 2 },
        new int[] {0, 4, 3, 2, 1 },
        new int[] {1, 4, 0, 2, 3 },
        new int[] {1, 4, 0, 3, 2 },
        new int[] {1, 4, 2, 0, 3 },
        new int[] {1, 4, 2, 3, 0 },
        new int[] {1, 4, 3, 0, 2 },
        new int[] {1, 4, 3, 2, 0 },
        new int[] {2, 4, 1, 0, 3 },
        new int[] {2, 4, 1, 3, 0 },
        new int[] {2, 4, 0, 1, 3 },
        new int[] {2, 4, 0, 3, 1 },
        new int[] {2, 4, 3, 1, 0 },
        new int[] {2, 4, 3, 0, 1 },
        new int[] {3, 4, 1, 2, 0 },
        new int[] {3, 4, 1, 0, 2 },
        new int[] {3, 4, 2, 1, 0 },
        new int[] {3, 4, 2, 0, 1 },
        new int[] {3, 4, 0, 1, 2 },
        new int[] {3, 4, 0, 2, 1 },
        new int[] {4, 0, 1, 2, 3 },
        new int[] {4, 0, 1, 3, 2 },
        new int[] {4, 0, 2, 1, 3 },
        new int[] {4, 0, 2, 3, 1 },
        new int[] {4, 0, 3, 1, 2 },
        new int[] {4, 0, 3, 2, 1 },
        new int[] {4, 1, 0, 2, 3 },
        new int[] {4, 1, 0, 3, 2 },
        new int[] {4, 1, 2, 0, 3 },
        new int[] {4, 1, 2, 3, 0 },
        new int[] {4, 1, 3, 0, 2 },
        new int[] {4, 1, 3, 2, 0 },
        new int[] {4, 2, 1, 0, 3 },
        new int[] {4, 2, 1, 3, 0 },
        new int[] {4, 2, 0, 1, 3 },
        new int[] {4, 2, 0, 3, 1 },
        new int[] {4, 2, 3, 1, 0 },
        new int[] {4, 2, 3, 0, 1 },
        new int[] {4, 3, 1, 2, 0 },
        new int[] {4, 3, 1, 0, 2 },
        new int[] {4, 3, 2, 1, 0 },
        new int[] {4, 3, 2, 0, 1 },
        new int[] {4, 3, 0, 1, 2 },
        new int[] {4, 3, 0, 2, 1 },
    };
    private static readonly bool[] isDemi = new bool[] {
        true,  //00000
        false, //00001
        false, //00010
        true,  //00011
        false, //00100
        true,  //00101
        true,  //00110
        false, //00111
        false, //01000
        true,  //01001
        true,  //01010
        false, //01011
        true,  //01100
        false, //01101
        false, //01110
        true,  //01111
        false, //10000
        true,  //10001
        true,  //10010
        false, //10011
        true,  //10100
        false, //10101
        false, //10110
        true,  //10111
        true,  //11000
        false, //11001
        false, //11010
        true,  //11011
        false, //11100
        true,  //11101
        true,  //11110
        false, //11111
    };
    private static Vector4[] Unit5Cell = new Vector4[] {
        new Vector4(1, 1, 1, -1.0f / Mathf.Sqrt(5)),
        new Vector4(1, -1, -1, -1.0f / Mathf.Sqrt(5)),
        new Vector4(-1, 1, -1, -1.0f / Mathf.Sqrt(5)),
        new Vector4(-1, -1, 1, -1.0f / Mathf.Sqrt(5)),
        new Vector4(0, 0, 0, 4.0f / Mathf.Sqrt(5)),
    };
    private static readonly List<Matrix4x4> IdentityGroup = new() { Matrix4x4.identity };
    private static readonly List<Matrix4x4> PointReflectGroup = new() { Matrix4x4.identity, Transform4D.ScaleMatrix(-Vector4.one) };
    private static readonly List<Matrix4x4> ReflectXGroup = new() { Matrix4x4.identity, Transform4D.ScaleMatrix(new Vector4(-1,1,1,1)) };

    private static readonly List<Matrix5x5> IdentityGroup5 = new() { Matrix5x5.identity };
    private static readonly List<Matrix5x5> PointReflectGroup5 = new() { Matrix5x5.identity, Transform5D.ScaleMatrix(-Vector5.one) };
    private static readonly List<Matrix5x5> ReflectXGroup5 = new() { Matrix5x5.identity, Transform5D.ScaleMatrix(new Vector5(-1, 1, 1, 1, 1)) };

    [MenuItem("4D/Generate Groups")]
    public static void GenerateGroupsMenu() {
        //Trivial groups
        SaveGroup(IdentityGroup, "Identity");
        SaveGroup(PointReflectGroup, "PointReflect");
        SaveGroup(ReflectXGroup, "ReflectX");

        //Polytope groups
        List<Matrix4x4> Chiral5CellGroup = Generate5CellGroup(true);
        List<Matrix4x4> Full5CellGroup = Generate5CellGroup(false);
        List<Matrix4x4> ChiralDemiTesseractGroup = GenerateTesseractGroup(true, true);
        List<Matrix4x4> FullDemiTesseractGroup = GenerateTesseractGroup(false, true);
        SaveGroup(Chiral5CellGroup, "Chiral5Cell");
        SaveGroup(Full5CellGroup, "Full5Cell");
        SaveGroup(ChiralDemiTesseractGroup, "ChiralDemiTesseract");
        SaveGroup(FullDemiTesseractGroup, "FullDemiTesseract");
        SaveGroup(GenerateTesseractGroup(true, false), "ChiralTesseract");
        SaveGroup(Product(FullDemiTesseractGroup, ReflectXGroup), "FullTesseract");
        SaveGroup(Generate600CellGroup(true), "Chiral600Cell");
        SaveGroup(Generate600CellGroup(false), "Full600Cell");
        SaveGroup(Generate600CellGroup(true, true), "Chiral120Cell");
        SaveGroup(Generate600CellGroup(false, true), "Full120Cell");

        //Products
        SaveGroup(Product(Chiral5CellGroup, PointReflectGroup), "ChiralDual5Cell");
        SaveGroup(Product(Full5CellGroup, PointReflectGroup), "FullDual5Cell");

        //Trivial 5D groups
        SaveGroup(IdentityGroup5, "Identity5");
        SaveGroup(PointReflectGroup5, "PointReflect5");
        SaveGroup(ReflectXGroup5, "ReflectX5");

        //Polytope 5D Groups
        List<Matrix5x5> ChiralDemiPenteractGroup = GeneratePenteractGroup(true, true);
        List<Matrix5x5> FullDemiPenteractGroup = GeneratePenteractGroup(false, true);
        SaveGroup(ChiralDemiPenteractGroup, "ChiralDemiPenteract");
        SaveGroup(FullDemiPenteractGroup, "FullDemiPenteract");
        Debug.Log("Done");
    }

    public static List<Matrix4x4> Generate5CellGroup(bool chiral) {
        //Align every 'face' to the default one
        List<Matrix4x4> group = new();
        for (int i = 0; i < 5; ++i) {
            Vector4[] exCell = (Vector4[])Unit5Cell.Clone();
            exCell[i] = exCell[4];
            group.AddRange(TetrahedralRotations(exCell, Unit5Cell, chiral));
        }
        return group;
    }

    public static List<Matrix4x4> GenerateTesseractGroup(bool chiral, bool demi) {
        List<Matrix4x4> group = new();
        for (int b = 0; b < 16; ++b) {
            if (demi && !isDemi[b]) { continue; }
            foreach (int[] p in A4) {
                Matrix4x4 m = Matrix4x4.zero;
                for (int i = 0; i < 4; ++i) {
                    m[i, p[i]] = (((b >> i) & 1) == 0 ? 1.0f : -1.0f);
                }
                TestRotation(m);
                if (chiral && m.determinant < 0.0f) { continue; }
                group.Add(m);
            }
        }
        return group;
    }

    public static List<Matrix5x5> GeneratePenteractGroup(bool chiral, bool demi) {
        List<Matrix5x5> group = new();
        for (int b = 0; b < 32; ++b) {
            if (demi && !isDemi[b]) { continue; }
            foreach (int[] p in A5) {
                Matrix5x5 m = Matrix5x5.zero;
                for (int i = 0; i < 5; ++i) {
                    m[i, p[i]] = (((b >> i) & 1) == 0 ? 1.0f : -1.0f);
                }
                TestRotation(m);
                if (chiral && m.determinant < 0.0f) { continue; }
                group.Add(m);
            }
        }
        return group;
    }

    public static List<Matrix4x4> Generate600CellGroup(bool chiral, bool singleColor = false) {
        //Load the 600cell vertices
        List<Vector4[]> cells = new();
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/600cell_colored.mesh");
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D)) {
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(verts.Length % 4 == 0);
            Debug.Assert(mesh4D.subMeshCount == 1);
            int[] indices = mesh4D.GetIndices(0);
            Debug.Assert(indices.Length % 6 == 0);
            for (int i = 0; i < indices.Length; i += 6) {
                Mesh4D.Vertex4D cell = verts[indices[i]];
                if (singleColor && cell.ao != 0) { continue; }
                cells.Add(new Vector4[] { cell.va, cell.vb, cell.vc, cell.vd });
            }
        }

        //Align every 'face' to the default one
        List<Matrix4x4> group = new();
        for (int i = 0; i < cells.Count; ++i) {
            group.AddRange(TetrahedralRotations(cells[i], cells[0], chiral));
        }
        return group;
    }

    public static List<Matrix4x4> TetrahedralRotations(Vector4[] exCell, Vector4[] cell, bool chiral) {
        Debug.Assert(cell.Length >= 4 && exCell.Length >= 4);
        Matrix4x4 a = new Matrix4x4(cell[0], cell[1], cell[2], cell[3]);
        List<Matrix4x4> group = new();
        foreach (int[] p in A4) {
            Matrix4x4 b = new Matrix4x4(exCell[p[0]], exCell[p[1]], exCell[p[2]], exCell[p[3]]);
            Matrix4x4 rot = a * b.inverse;
            TestRotation(rot);
            if (!chiral || rot.determinant > 0.0f) {
                group.Add(rot);
            }
        }
        return group;
    }

    private static void SaveGroup(List<Matrix4x4> group, string name) {
        string path = "Assets/Editor/Groups/" + name + ".bin";
        using (FileStream fs = File.Open(path, FileMode.OpenOrCreate)) {
            BinaryWriter writer = new BinaryWriter(fs);
            writer.Write(group.Count);
            foreach (Matrix4x4 m in group) {
                for (int i = 0; i < 4; ++i) {
                    for (int j = 0; j < 4; ++j) {
                        writer.Write(m[i,j]);
                    }
                }
            }
        }
    }

    private static void SaveGroup(List<Matrix5x5> group, string name) {
        string path = "Assets/Editor/Groups/" + name + ".bin";
        using (FileStream fs = File.Open(path, FileMode.OpenOrCreate)) {
            BinaryWriter writer = new BinaryWriter(fs);
            writer.Write(group.Count);
            foreach (Matrix5x5 m in group) {
                for (int i = 0; i < 5; ++i) {
                    for (int j = 0; j < 5; ++j) {
                        writer.Write(m[i, j]);
                    }
                }
            }
        }
    }

    public static List<Matrix4x4> LoadGroup(string name) {
        string path = "Assets/Editor/Groups/" + name + ".bin";
        List<Matrix4x4> rots = new();
        using (FileStream fs = File.Open(path, FileMode.Open)) {
            BinaryReader reader = new BinaryReader(fs);
            int numRots = reader.ReadInt32();
            for (int n = 0; n < numRots; ++n) {
                Matrix4x4 rot = Matrix4x4.identity;
                for (int i = 0; i < 4; ++i) {
                    for (int j = 0; j < 4; ++j) {
                        rot[i, j] = reader.ReadSingle();
                    }
                }
                rots.Add(rot);
            }
        }
        return rots;
    }

    public static List<Matrix5x5> LoadGroup5D(string name) {
        string path = "Assets/Editor/Groups/" + name + ".bin";
        List<Matrix5x5> rots = new();
        using (FileStream fs = File.Open(path, FileMode.Open)) {
            BinaryReader reader = new BinaryReader(fs);
            int numRots = reader.ReadInt32();
            for (int n = 0; n < numRots; ++n) {
                Matrix5x5 rot = Matrix5x5.identity;
                for (int i = 0; i < 5; ++i) {
                    for (int j = 0; j < 5; ++j) {
                        rot[i, j] = reader.ReadSingle();
                    }
                }
                rots.Add(rot);
            }
        }
        return rots;
    }

    private static void TestRotation(Matrix4x4 rot) {
        Matrix4x4 rotTest = rot.transpose * rot;
        Debug.Assert(Vector4.Distance(rotTest.GetColumn(0), new Vector4(1, 0, 0, 0)) < 1e-3f, rotTest.GetColumn(0));
        Debug.Assert(Vector4.Distance(rotTest.GetColumn(1), new Vector4(0, 1, 0, 0)) < 1e-3f, rotTest.GetColumn(1));
        Debug.Assert(Vector4.Distance(rotTest.GetColumn(2), new Vector4(0, 0, 1, 0)) < 1e-3f, rotTest.GetColumn(2));
        Debug.Assert(Vector4.Distance(rotTest.GetColumn(3), new Vector4(0, 0, 0, 1)) < 1e-3f, rotTest.GetColumn(3));
    }

    private static void TestRotation(Matrix5x5 rot) {
        Matrix5x5 rotTest = rot.transpose * rot;
        Debug.Assert(Vector5.Distance(rotTest.GetColumn(0), new Vector5(1, 0, 0, 0, 0)) < 1e-3f, rotTest.GetColumn(0));
        Debug.Assert(Vector5.Distance(rotTest.GetColumn(1), new Vector5(0, 1, 0, 0, 0)) < 1e-3f, rotTest.GetColumn(1));
        Debug.Assert(Vector5.Distance(rotTest.GetColumn(2), new Vector5(0, 0, 1, 0, 0)) < 1e-3f, rotTest.GetColumn(2));
        Debug.Assert(Vector5.Distance(rotTest.GetColumn(3), new Vector5(0, 0, 0, 1, 0)) < 1e-3f, rotTest.GetColumn(3));
        Debug.Assert(Vector5.Distance(rotTest.GetColumn(4), new Vector5(0, 0, 0, 0, 1)) < 1e-3f, rotTest.GetColumn(4));
    }

    private static List<Matrix4x4> Product(List<Matrix4x4> group1, List<Matrix4x4> group2) {
        List<Matrix4x4> result = new();
        for (int i = 0; i < group1.Count; ++i) {
            for (int j = 0; j < group2.Count; ++j) {
                result.Add(group1[i] * group2[j]);
            }
        }
        return result;
    }

    private static List<Matrix5x5> Product(List<Matrix5x5> group1, List<Matrix5x5> group2) {
        List<Matrix5x5> result = new();
        for (int i = 0; i < group1.Count; ++i) {
            for (int j = 0; j < group2.Count; ++j) {
                result.Add(group1[i] * group2[j]);
            }
        }
        return result;
    }

    private static float MatrixDiff(Matrix4x4 a, Matrix4x4 b) {
        return (a.GetColumn(0) - b.GetColumn(0)).sqrMagnitude +
               (a.GetColumn(1) - b.GetColumn(1)).sqrMagnitude +
               (a.GetColumn(2) - b.GetColumn(2)).sqrMagnitude +
               (a.GetColumn(3) - b.GetColumn(3)).sqrMagnitude;
    }

    private static float MatrixDiff(Matrix5x5 a, Matrix5x5 b) {
        return (a.GetColumn(0) - b.GetColumn(0)).sqrMagnitude +
               (a.GetColumn(1) - b.GetColumn(1)).sqrMagnitude +
               (a.GetColumn(2) - b.GetColumn(2)).sqrMagnitude +
               (a.GetColumn(3) - b.GetColumn(3)).sqrMagnitude +
               (a.GetColumn(4) - b.GetColumn(4)).sqrMagnitude;
    }
}