using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OFFParser {
    public enum OFFMode {
        NONE,
        VERTEX,
        FACE,
        CELL,
        TERA,
    }

    public struct Line2D {
        public Vector2 a;
        public Vector2 b;
        public float aoA;
        public float aoB;
        public Line2D Reversed() { return new Line2D() { a = b, b = a, aoA = aoB, aoB = aoA }; }
    }

    public static List<Line2D> SortLoop2D(List<Line2D> lines, bool reverse = false) {
        int numChecked = 1;
        int curIx = 0;
        bool foundLine = true;
        if (reverse) { lines[curIx] = lines[curIx].Reversed(); }
        while (numChecked < lines.Count && foundLine) {
            foundLine = false;
            for (int i = 0; i < lines.Count; ++i) {
                if (i == curIx) continue;
                if (lines[i].a == lines[curIx].b) {
                    curIx = i;
                    foundLine = true;
                    break;
                } else if (lines[i].b == lines[curIx].b) {
                    lines[i] = lines[i].Reversed();
                    curIx = i;
                    foundLine = true;
                    break;
                }
            }
            if (!foundLine) {
                Debug.LogError("Could not sort line loop");
            }
            numChecked += 1;
        }
        return lines;
    }

    public static List<Line2D> LoadOBJ2D(string fname) {
        StreamReader reader = new StreamReader("Assets/Editor/Surface/" + fname + ".obj");
        string fileContents = reader.ReadToEnd();
        string[] fileLines = fileContents.Replace("\r", "").Split('\n');
        reader.Close();

        //Read through the file
        List<Vector2> verticies = new();
        List<float> ao = new();
        List<Line2D> lines = new();
        foreach (string line in fileLines) {
            //Parse headers
            if (line.Length == 0) { continue; }
            string[] data = line.Split(' ');
            if (data[0] == "v") {
                float x = float.Parse(data[1]);
                float y = float.Parse(data[2]);
                float z = float.Parse(data[3]);
                Debug.Assert(y == 0.0f);
                verticies.Add(new Vector2(x, z));
            } else if (data[0] == "vt") {
                float y = float.Parse(data[2]);
                ao.Add(y);
            } else if (data[0] == "l") {
                int ix1 = int.Parse(data[1]) - 1;
                int ix2 = int.Parse(data[2]) - 1;
                lines.Add(new Line2D() {
                    a = verticies[ix1],
                    b = verticies[ix2],
                    aoA = (ix1 < ao.Count ? ao[ix1] : 0.0f),
                    aoB = (ix2 < ao.Count ? ao[ix2] : 0.0f),
                });
            }
        }
        return lines;
    }

    public static void SaveOBJ2D(string fname, List<Line2D> lines) {
        StreamWriter writer = new StreamWriter("Assets/Editor/Surface/" + fname + ".obj");
        foreach (Line2D line in lines) {
            writer.WriteLine("v " + line.a.x + " 0 " + line.a.y);
            writer.WriteLine("v " + line.b.x + " 0 " + line.b.y);
        }
        foreach (Line2D line in lines) {
            writer.WriteLine("vt " + line.aoA + " " + line.aoA);
            writer.WriteLine("vt " + line.aoB + " " + line.aoB);
        }
        for (int i = 0; i < lines.Count; ++i) {
            writer.WriteLine("l " + (2*i + 1) + " " + (2*i + 2));
        }
        writer.Close();
    }

    public static Mesh4DBuilder LoadOFF4D(string fname, bool normalize=true) {
        //Read contents of file
        StreamReader reader = new StreamReader("Assets/Editor/OFF/" + fname + ".off");
        string fileContents = reader.ReadToEnd();
        string[] fileLines = fileContents.Replace("\r", "").Split('\n');
        reader.Close();

        //Allocate structures
        Mesh4D mesh4D = new Mesh4D();
        List<Vector4> verticies = new List<Vector4>();
        List<int[]> faces = new List<int[]>();

        //Read through the file
        OFFMode mode = OFFMode.NONE;
        foreach (string line in fileLines) {
            //Parse headers
            if (line.Length == 0) { continue; }
            if (line == "# Vertices") {
                mode = OFFMode.VERTEX; continue;
            } else if (line == "# Faces") {
                mode = OFFMode.FACE; continue;
            } else if (line == "# Cells") {
                mode = OFFMode.CELL; continue;
            }

            //Parse data
            string[] data = line.Split(' ');
            if (mode == OFFMode.VERTEX) {
                Vector4 v = new Vector4(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));
                if (normalize) { v.Normalize(); }
                verticies.Add(v);
            } else if (mode == OFFMode.FACE) {
                Debug.Assert(data[0] == "3", "Mesh must be triangulated");
                int[] face = new int[3] { int.Parse(data[1]), int.Parse(data[2]), int.Parse(data[3]) };
                faces.Add(face);
                Vector4 a = verticies[face[0]];
                Vector4 b = verticies[face[1]];
                Vector4 c = verticies[face[2]];
                mesh4D.AddTriangleShadow(a, b, c);
            } else if (mode == OFFMode.CELL) {
                if (data[0] == "4") { //Tetrahedron
                    int[] face1 = faces[int.Parse(data[1])];
                    int[] face2 = faces[int.Parse(data[2])];
                    int oppIx = GetUncommon(face1, face2);
                    Vector4 a = verticies[face1[0]];
                    Vector4 b = verticies[face1[1]];
                    Vector4 c = verticies[face1[2]];
                    Vector4 d = verticies[oppIx];
                    mesh4D.AddTetrahedronNormal(d.normalized, a, b, c, d);
                } else if (data[0] == "6") { //Tetrahedral bipyramid
                    HashSet<int> vertexSet = new();
                    vertexSet.UnionWith(faces[int.Parse(data[1])]);
                    vertexSet.UnionWith(faces[int.Parse(data[2])]);
                    vertexSet.UnionWith(faces[int.Parse(data[3])]);
                    vertexSet.UnionWith(faces[int.Parse(data[4])]);
                    List<int> allVertices = new(vertexSet);
                    Debug.Assert(allVertices.Count == 5);
                    int[] vertexCounts = new int[5] { 0, 0, 0, 0, 0 };
                    for (int i = 0; i < 6; ++i) {
                        int[] face = faces[int.Parse(data[i+1])];
                        foreach (int vertIx in face) {
                            vertexCounts[allVertices.IndexOf(vertIx)] += 1;
                        }
                    }
                    List<Vector4> sideVerts = new();
                    for (int i = 0; i < 5; ++i) {
                        if (vertexCounts[i] == 4) {
                            sideVerts.Add(verticies[allVertices[i]]);
                        }
                    }
                    Debug.Assert(sideVerts.Count == 3);
                    for (int i = 0; i < 5; ++i) {
                        if (vertexCounts[i] == 3) {
                            Vector4 a = verticies[allVertices[i]];
                            Vector4 b = sideVerts[0];
                            Vector4 c = sideVerts[1];
                            Vector4 d = sideVerts[2];
                            Vector4 sum = a + b + c + d;
                            mesh4D.AddTetrahedronNormal(sum, a, b, c, d);
                        }
                    }
                } else if (data[0] == "8") { //Uniform Octahedron
                    HashSet<int> vertexSet = new();
                    vertexSet.UnionWith(faces[int.Parse(data[1])]);
                    vertexSet.UnionWith(faces[int.Parse(data[2])]);
                    vertexSet.UnionWith(faces[int.Parse(data[3])]);
                    vertexSet.UnionWith(faces[int.Parse(data[4])]);
                    vertexSet.UnionWith(faces[int.Parse(data[5])]);
                    List<int> allVertices = new(vertexSet);
                    Debug.Assert(allVertices.Count == 6);
                    Vector4 a = verticies[allVertices[0]];
                    int farIx = GetFarthestIx(0, allVertices, verticies);
                    int temp = allVertices[1];
                    allVertices[1] = allVertices[farIx];
                    allVertices[farIx] = temp;
                    Vector4 b = verticies[allVertices[1]];
                    Vector4 c = verticies[allVertices[2]];
                    farIx = GetFarthestIx(2, allVertices, verticies);
                    temp = allVertices[3];
                    allVertices[3] = allVertices[farIx];
                    allVertices[farIx] = temp;
                    Vector4 d = verticies[allVertices[3]];
                    Vector4 e = verticies[allVertices[4]];
                    Vector4 f = verticies[allVertices[5]];
                    Vector4 sum = a + b + c + d + e + f;
                    mesh4D.AddTetrahedronNormal(sum, c, e, a, b);
                    mesh4D.AddTetrahedronNormal(sum, e, d, a, b);
                    mesh4D.AddTetrahedronNormal(sum, d, f, a, b);
                    mesh4D.AddTetrahedronNormal(sum, f, c, a, b);
                } else {
                    Debug.LogError("Cells of size " + data[0] + " not supported");
                }
            }
        }

        return new Mesh4DBuilder(mesh4D);
    }

    public static Mesh5DBuilder LoadOFF5D(string fname) {
        //Read contents of file
        StreamReader reader = new StreamReader("Assets/Editor/OFF/" + fname + ".off");
        string fileContents = reader.ReadToEnd();
        string[] fileLines = fileContents.Replace("\r", "").Split('\n');
        reader.Close();

        //Allocate structures
        Mesh5D mesh5D = new Mesh5D();
        List<Vector5> verticies = new List<Vector5>();
        List<int[]> faces = new List<int[]>();
        List<int[]> tetra = new List<int[]>();

        //Read through the file
        OFFMode mode = OFFMode.NONE;
        foreach (string line in fileLines) {
            //Parse headers
            if (line.Length == 0) { continue; }
            if (line == "# Vertices") {
                mode = OFFMode.VERTEX; continue;
            } else if (line == "# Faces") {
                mode = OFFMode.FACE; continue;
            } else if (line == "# Cells") {
                mode = OFFMode.CELL; continue;
            } else if (line == "# Tera") {
                mode = OFFMode.TERA; continue;
            }

            //Parse data
            string[] data = line.Split(' ');
            if (mode == OFFMode.VERTEX) {
                Vector5 v = new Vector5(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]), float.Parse(data[4]));
                verticies.Add(v.normalized);
            } else if (mode == OFFMode.FACE) {
                Debug.Assert(data[0] == "3", "Mesh must be triangulated");
                int[] face = new int[3] { int.Parse(data[1]), int.Parse(data[2]), int.Parse(data[3]) };
                faces.Add(face);
                Vector5 a = verticies[face[0]];
                Vector5 b = verticies[face[1]];
                Vector5 c = verticies[face[2]];
                mesh5D.AddTriangleShadow(a, b, c);
            } else if (mode == OFFMode.CELL) {
                Debug.Assert(data[0] == "4", "Mesh must be tetrahedralized");
                tetra.Add(new int[4] { int.Parse(data[1]), int.Parse(data[2]), int.Parse(data[3]), int.Parse(data[4]) });
            } else if (mode == OFFMode.TERA) {
                Debug.Assert(data[0] == "5", "Mesh must be simplexed");
                int[] tetra1 = tetra[int.Parse(data[1])];
                int[] tetra2 = tetra[int.Parse(data[2])];
                //Get first tetra vertices
                int[] tetra1face1 = faces[tetra1[0]];
                int[] tetra1face2 = faces[tetra1[1]];
                int tetra1opp = GetUncommon(tetra1face1, tetra1face2);
                //Get second tetra vertices
                int[] tetra2face1 = faces[tetra2[0]];
                int[] tetra2face2 = faces[tetra2[1]];
                int tetra2opp = GetUncommon(tetra2face1, tetra2face2);
                //Combine the tetras
                int oppVertIx = GetUncommon(new int[4] { tetra1face1[0], tetra1face1[1], tetra1face1[2], tetra1opp },
                                            new int[4] { tetra2face1[0], tetra2face1[1], tetra2face1[2], tetra2opp });
                //Save simplex
                Vector5 a = verticies[tetra1face1[0]];
                Vector5 b = verticies[tetra1face1[1]];
                Vector5 c = verticies[tetra1face1[2]];
                Vector5 d = verticies[tetra1opp];
                Vector5 e = verticies[oppVertIx];
                if (new Matrix5x5(a, b, c, d, e).determinant > 0) {
                    mesh5D.AddSimplex(a, b, c, d, e);
                } else {
                    mesh5D.AddSimplex(b, a, c, d, e);
                }
            }
        }
        return new Mesh5DBuilder(mesh5D);
    }

    public static void ParseCenters4D(string fname, List<Vector4> verts, List<Vector4> faces, List<Vector4> cells) {
        //Load OFF file
        StreamReader reader = new StreamReader("Assets/Editor/OFF/" + fname + ".off");
        string fileContents = reader.ReadToEnd();
        string[] fileLines = fileContents.Replace("\r", "").Split('\n');
        reader.Close();

        //Read only vertices from file
        OFFMode mode = OFFMode.NONE;
        verts.Clear();
        faces.Clear();
        cells.Clear();
        foreach (string line in fileLines) {
            //Parse headers
            if (line.Length == 0) { continue; }
            if (line == "# Vertices") {
                mode = OFFMode.VERTEX; continue;
            } else if (line == "# Faces") {
                mode = OFFMode.FACE; continue;
            } else if (line == "# Cells") {
                mode = OFFMode.CELL; continue;
            } else if (line == "# Tera") {
                mode = OFFMode.TERA; continue;
            }

            //Parse data
            string[] data = line.Split(' ');
            if (mode == OFFMode.VERTEX) {
                Vector4 v = new Vector4(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));
                verts.Add(v.normalized);
            } else if (mode == OFFMode.FACE) {
                Debug.Assert(data[0] == "3", "Mesh must be triangulated");
                int[] face = new int[3] { int.Parse(data[1]), int.Parse(data[2]), int.Parse(data[3]) };
                Vector4 a = verts[face[0]];
                Vector4 b = verts[face[1]];
                Vector4 c = verts[face[2]];
                faces.Add((a + b + c) / 3.0f);
            } else if (mode == OFFMode.CELL) {
                Debug.Assert(data[0] == "4", "Mesh must be tetrahedralized");
                int[] tetra = new int[4] { int.Parse(data[1]), int.Parse(data[2]), int.Parse(data[3]), int.Parse(data[4]) };
                Vector4 a = faces[tetra[0]];
                Vector4 b = faces[tetra[1]];
                Vector4 c = faces[tetra[2]];
                Vector4 d = faces[tetra[2]];
                cells.Add((a + b + c + d) / 4.0f);
            }
        }
    }

    private static int GetUncommon(int[] v1, int[] v2) {
        for (int i = 0; i < v2.Length; ++i) {
            bool inCommon = false;
            for (int j = 0; j < v1.Length; ++j) {
                if (v2[i] == v1[j]) {
                    inCommon = true;
                    break;
                }
            }
            if (!inCommon) {
                return v2[i];
            }
        }
        Debug.Assert(false);
        return -1;
    }

    private static int GetFarthestIx(int fromIx, List<int> vixs, List<Vector4> verts) {
        Vector4 v = verts[vixs[fromIx]];
        int farthestIx = fromIx;
        float farthestDistSq = 0.0f;
        for (int i = 0; i < vixs.Count; ++i) {
            float distSq = (verts[vixs[i]] - v).sqrMagnitude;
            if (distSq > farthestDistSq) {
                farthestIx = i;
                farthestDistSq = distSq;
            }
        }
        return farthestIx;
    }
}
