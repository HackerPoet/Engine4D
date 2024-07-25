#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CompileTemplates : MonoBehaviour {
    [MenuItem("4D/Compile Templates")]
    public static void CompileTemplatesMenu() {
        CompileTemplate("BasicCamera");
        CompileTemplate("BoxCollider", "Scripts/Colliders");
        CompileTemplate("CameraControl");
        CompileTemplate("Collider", "Scripts/Colliders");
        CompileTemplate("ColliderGroup", "Scripts/Colliders");
        CompileTemplate("ConeCollider", "Scripts/Colliders");
        CompileTemplate("ConicFrustumCollider", "Scripts/Colliders");
        CompileTemplate("DiskCollider", "Scripts/Colliders");
        CompileTemplate("HolinderCollider", "Scripts/Colliders");
        CompileTemplate("MovingPlatform");
        CompileTemplate("Object");
        CompileTemplate("Occlusion");
        CompileTemplate("ParallelotopeCollider", "Scripts/Colliders");
        CompileTemplate("ParticleSystem");
        CompileTemplate("Physical");
        CompileTemplate("SphereCollider", "Scripts/Colliders");
        CompileTemplate("SpherinderCollider", "Scripts/Colliders");
        CompileTemplate("Transform");
    }

    public static void CompileTemplate(string name, string outputFolder = "Scripts") {
        //Read contents of file
        StreamReader reader = new StreamReader("Assets/Templates/" + name + "ND.cxx");
        string fileContents = reader.ReadToEnd();
        reader.Close();

        //Generate for dimensions 4 and 5
        CompileTemplate(name, fileContents, outputFolder, 4);
        CompileTemplate(name, fileContents, outputFolder, 5);
        AssetDatabase.Refresh();
    }

    private static void CompileTemplate(string name, string fileContents, string outputFolder, int D) {
        //Names for template replacement
        string outputPath = "Assets/" + outputFolder  + "/" + name + D + "D.cs";
        string transformName = name + D + "D";
        string subVectorName = "Vector" + (D - 1);
        string vectorName = "Vector" + D;
        string matrixName = "Matrix" + D + "x" + D;
        string qName = (D == 4 ? "Quaternion" : "Isocline");
        string lastComp = (D == 4 ? "w" : "v");
        string dimsName = D.ToString();
        string subDimsName = (D - 1).ToString();

        //Do the replacing
        fileContents = fileContents.Replace(name + "ND", transformName);
        fileContents = fileContents.Replace("SUBVECTOR", subVectorName);
        fileContents = fileContents.Replace("VECTOR", vectorName);
        fileContents = fileContents.Replace("MATRIX", matrixName);
        fileContents = fileContents.Replace("QTYPE", qName);
        fileContents = fileContents.Replace("LAST", lastComp);
        fileContents = fileContents.Replace("DIMS", dimsName);
        fileContents = fileContents.Replace("<D>", dimsName + "D");
        fileContents = fileContents.Replace("<D-1>", subDimsName + "D");

        StreamWriter writer = new StreamWriter(outputPath);
        writer.WriteLine("//#########[---------------------------]#########");
        writer.WriteLine("//#########[  GENERATED FROM TEMPLATE  ]#########");
        writer.WriteLine("//#########[---------------------------]#########");
        writer.Write(fileContents);
        writer.Close();
    }
}

#endif
