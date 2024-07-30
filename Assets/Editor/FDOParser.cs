using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FDOParser {
    enum TFormat {
        V,
        CoV,
        V_Vn,
        V_Co_Vn,
    }

    public static Mesh4DBuilder Load4DO(string fname) {
        //Read contents of file
        StreamReader reader = new StreamReader("Assets/Editor/4DO/" + fname + ".4do");
        string fileContents = reader.ReadToEnd();
        string[] fileLines = fileContents.Replace("\r", "").Split('\n');
        reader.Close();

        //Allocate structures
        Mesh4D mesh4D = new Mesh4D();
        List<Vector4> verticies = new List<Vector4>();

        //Read through the file
        TFormat tFormat = TFormat.V;
        foreach (string line in fileLines) {
            //Parse headers
            if (line.Length == 0) { continue; }
            if (line == "tformat v") {
                tFormat = TFormat.V;
            } else if (line == "tformat co v") {
                tFormat = TFormat.CoV;
            } else if (line == "tformat v/vn") {
                tFormat = TFormat.V_Vn;
            } else if (line == "tformat v/co/vn") {
                tFormat = TFormat.V_Co_Vn;
            } else if (line.StartsWith("tformat")) {
                Debug.LogError("Unsupported tformat: " + line);
                return null;
            }

            if (line.StartsWith("v ")) {
                string[] data = line.Split(' ');
                Vector4 v = new Vector4(float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]), float.Parse(data[4]));
                verticies.Add(v);
            } else if (line.StartsWith("t ")) {
                string[] data = line.Split(' ');
                int iA = 0;
                int iB = 0;
                int iC = 0;
                int iD = 0;
                if (tFormat == TFormat.V) {
                    Debug.Assert(data.Length == 5, "Wrong number of arguments to tet.");
                    iA = int.Parse(data[1]);
                    iB = int.Parse(data[2]);
                    iC = int.Parse(data[3]);
                    iD = int.Parse(data[4]);
                } else if (tFormat == TFormat.CoV) {
                    Debug.Assert(data.Length == 6, "Wrong number of arguments to tet.");
                    iA = int.Parse(data[2]);
                    iB = int.Parse(data[3]);
                    iC = int.Parse(data[4]);
                    iD = int.Parse(data[5]);
                } else if (tFormat == TFormat.V_Vn || tFormat == TFormat.V_Co_Vn) {
                    Debug.Assert(data.Length == 5, "Wrong number of arguments to tet.");
                    iA = int.Parse(data[1].Split('/')[0]);
                    iB = int.Parse(data[2].Split('/')[0]);
                    iC = int.Parse(data[3].Split('/')[0]);
                    iD = int.Parse(data[4].Split('/')[0]);
                } else {
                    //TODO: Add more TFormats
                }
                Vector4 a = verticies[iA];
                Vector4 b = verticies[iB];
                Vector4 c = verticies[iC];
                Vector4 d = verticies[iD];
                mesh4D.AddTetrahedron(a, b, c, d);
                //mesh4D.AddTriangleShadow(a, b, c);
                //mesh4D.AddTriangleShadow(a, b, d);
                //mesh4D.AddTriangleShadow(a, c, d);
                //mesh4D.AddTriangleShadow(b, c, d);
            }
        }

        return new Mesh4DBuilder(mesh4D);
    }
}
