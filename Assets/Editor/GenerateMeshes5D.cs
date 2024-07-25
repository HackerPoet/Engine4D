using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public class GenerateMeshes5D {
    [MenuItem("4D/Generate 5D Meshes")]
    public static void Generate5DMeshesMenu() {
        //Generate 5D meshes
        GenerateFlatHypercube().Build("flat_hypercube");
        Generate5Cube().Build("5cube").VertexAOAxis(0, 1.0f, 1.0f).Build("5cube_noAO");
        Generate5DFlat("5cell").Build("flat_5cell");
        Generate5DFlat("600cell").Build("flat_600cell");
        Generate5DPyramid("600cell", 1.0f).Build("5cone");
        Generate5DExtrude("600cell", 1.0f, null, false, true).Build("5spherinder_halfcap");
        Generate5DExtrude("24cell", 1.0f, null, false, false).Build("5ico_cylinder_uncapped");
        GenerateRevolve("24cell", 16, new Vector4(0.0f, 0.0f, 0.0f, 1.5f)).VertexAORadial(0.0f, 1.0f).Smoothen().Build("5torus");
        Generate5DPyramid("24cell", 3.0f, null, false).Rotate(90.0f, 1, 4).Build("5cone_nocap");
        OFFParser.LoadOFF5D("Hexateron").Build("5simplex");
        OFFParser.LoadOFF5D("Triacontaditeron").Build("5orthoplex");
        OFFParser.LoadOFF5D("320Tera").Build("320tera");
        OFFParser.LoadOFF5D("3840Tera").Build("3840tera").Smoothen().Build("3840tera_smooth");
        Color3840Cell(OFFParser.LoadOFF5D("3840Tera")).Build("3840tera_colored");
        Debug.Log("Done!");
    }

    //#############################################################################
    //# Utility
    //#############################################################################
    private static Vector4 CenterOfMass(NativeArray<Mesh4D.Vertex4D> verticies, int[] indices) {
        Vector4 sum = Vector4.zero;
        float volume = 0.0f;
        for (int i = 0; i < indices.Length; i += 6) {
            Mesh4D.Vertex4D v = verticies[indices[i]];
            Vector4 a = v.va;
            Vector4 b = v.vb;
            Vector4 c = v.vc;
            Vector4 d = v.vd;
            Vector4 tetrahedronCenter = (a + b + c + d) / 4.0f;
            float tetrahedronVolume = Transform4D.MakeNormal(a - d, b - d, c - d).magnitude;
            sum += tetrahedronCenter * tetrahedronVolume;
            volume += tetrahedronVolume;
        }
        return sum / volume;
    }

    private static void ConvexCheck(Vector4 a, Vector4 b, Vector4 c, Vector4 d, Vector4 center) {
        float tsp = Vector4.Dot(Transform4D.MakeNormal(a - center, b - center, c - center), d - center);
        Debug.Assert(tsp > 0.0f, "Mesh is not convex enough! " + tsp);
    }

    //#############################################################################
    //# 5D Generation
    //#############################################################################
    private static Mesh5DBuilder GenerateFlatHypercube() {
        //Create a new mesh5D
        Debug.Log("Generating flat_hypercube...");
        Mesh5D mesh5D = new Mesh5D();

        //Add just a default flat cube
        AddFlatHypercube(mesh5D, Matrix5x5.identity, Vector5.zero);
        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder Generate5Cube() {
        //Create a new mesh5D
        Debug.Log("Generating 5cube...");
        Mesh5D mesh5D = new Mesh5D();

        //Assemble the 10 cell 'faces'
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(90.0f, 0, 4), new Vector5(1, 0, 0, 0, 0));
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(-90.0f, 0, 4), new Vector5(-1, 0, 0, 0, 0));
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(90.0f, 1, 4), new Vector5(0, 1, 0, 0, 0));
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(-90.0f, 1, 4), new Vector5(0, -1, 0, 0, 0));
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(90.0f, 2, 4), new Vector5(0, 0, 1, 0, 0));
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(-90.0f, 2, 4), new Vector5(0, 0, -1, 0, 0));
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(90.0f, 3, 4), new Vector5(0, 0, 0, 1, 0));
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(-90.0f, 3, 4), new Vector5(0, 0, 0, -1, 0));
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(0.0f, 0, 4), new Vector5(0, 0, 0, 0, 1), true);
        AddFlatHypercube(mesh5D, Transform5D.PlaneRotation(180.0f, 0, 4), new Vector5(0, 0, 0, 0, -1), true);
        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder GenerateTetrarect(float height) {
        //Create a new mesh4D
        Mesh5D mesh5D = new Mesh5D();

        //Add points
        Vector5 v0a = new Vector5(0, 0, 0, 0, 0);
        Vector5 v0b = new Vector5(0, 0, 0, 0, height);
        Vector5 v1a = new Vector5(1, 0, 0, 0, 0);
        Vector5 v1b = new Vector5(1, 0, 0, 0, height);
        Vector5 v2a = new Vector5(0, 1, 0, 0, 0);
        Vector5 v2b = new Vector5(0, 1, 0, 0, height);
        Vector5 v3a = new Vector5(0, 0, 1, 0, 0);
        Vector5 v3b = new Vector5(0, 0, 1, 0, height);
        Vector5 v4a = new Vector5(0, 0, 0, 1, 0);
        Vector5 v4b = new Vector5(0, 0, 0, 1, height);
        mesh5D.AddSimplex(v0b, v1b, v2b, v3b, v4b);
        mesh5D.AddTetraPrism(v0b, v1b, v2b, v3b, v0a, v1a, v2a, v3a);
        mesh5D.AddTetraPrism(v0b, v1b, v4b, v2b, v0a, v1a, v4a, v2a);
        mesh5D.AddTetraPrism(v0b, v1b, v3b, v4b, v0a, v1a, v3a, v4a);
        mesh5D.AddTetraPrism(v0b, v2b, v4b, v3b, v0a, v2a, v4a, v3a);
        mesh5D.AddTetraPrism(v1b, v2b, v3b, v4b, v1a, v2a, v3a, v4a);

        //Add shadows
        mesh5D.AddQuadShadow(v0b, v1b, v0a, v1a);
        mesh5D.AddQuadShadow(v0b, v2b, v0a, v2a);
        mesh5D.AddQuadShadow(v0b, v3b, v0a, v3a);
        mesh5D.AddQuadShadow(v0b, v4b, v0a, v4a);
        mesh5D.AddQuadShadow(v1b, v2b, v1a, v2a);
        mesh5D.AddQuadShadow(v1b, v3b, v1a, v3a);
        mesh5D.AddQuadShadow(v1b, v4b, v1a, v4a);
        mesh5D.AddQuadShadow(v2b, v3b, v2a, v3a);
        mesh5D.AddQuadShadow(v2b, v4b, v2a, v4a);
        mesh5D.AddQuadShadow(v3b, v4b, v3a, v4a);
        mesh5D.AddTriangleShadow(v0b, v1b, v2b);
        mesh5D.AddTriangleShadow(v0b, v1b, v3b);
        mesh5D.AddTriangleShadow(v0b, v1b, v4b);
        mesh5D.AddTriangleShadow(v0b, v2b, v3b);
        mesh5D.AddTriangleShadow(v0b, v2b, v4b);
        mesh5D.AddTriangleShadow(v0b, v3b, v4b);
        mesh5D.AddTriangleShadow(v1b, v2b, v3b);
        mesh5D.AddTriangleShadow(v1b, v2b, v4b);
        mesh5D.AddTriangleShadow(v1b, v3b, v4b);
        mesh5D.AddTriangleShadow(v2b, v3b, v4b);
        return new Mesh5DBuilder(mesh5D);
    }

    //Extrudes *FLAT* tetras (No w-depth) to *FLAT* cells (No v-depth)
    private static Mesh5DBuilder Generate5DExtrudeFlat(string flat4D, float length) {
        //Create a new mesh4D
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/" + flat4D + ".mesh");
        Mesh mesh4D_s = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/" + flat4D + "_s.mesh");

        //Create a new mesh5D
        Mesh5D mesh5D = new Mesh5D(mesh4D.subMeshCount);
        Vector5 extrude = new Vector5(0.0f, 0.0f, 0.0f, length, 0.0f);

        //Acquire mesh data
        Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D);
        Mesh.MeshDataArray meshData_s = Mesh.AcquireReadOnlyMeshData(mesh4D_s);
        Debug.Assert(mesh4D.subMeshCount == mesh4D_s.subMeshCount);

        //Scan through the 4D mesh
        NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
        NativeArray<Mesh4D.Shadow4D> verts_s = meshData_s[0].GetVertexData<Mesh4D.Shadow4D>(0);
        Debug.Assert(verts.Length % 4 == 0);
        Debug.Assert(verts_s.Length % 3 == 0);
        for (int s = 0; s < mesh4D.subMeshCount; ++s) {
            //Add the simplices
            int[] indices = mesh4D.GetIndices(s);
            Debug.Assert(indices.Length % 6 == 0);
            for (int i = 0; i < indices.Length; i += 6) {
                Mesh4D.Vertex4D v = verts[indices[i]];
                Vector5 a1 = (Vector5)v.va;
                Vector5 b1 = (Vector5)v.vb;
                Vector5 c1 = (Vector5)v.vc;
                Vector5 d1 = (Vector5)v.vd;
                Vector5 a2 = a1 + extrude;
                Vector5 b2 = b1 + extrude;
                Vector5 c2 = c1 + extrude;
                Vector5 d2 = d1 + extrude;
                a1 -= extrude;
                b1 -= extrude;
                c1 -= extrude;
                d1 -= extrude;

                //Walls of the prism
                mesh5D.AddTetraPrism(a1, b1, c1, d1, a2, b2, c2, d2);
            }

            //Copy the shadow from the full 4D object
            int[] indices_s = mesh4D_s.GetIndices(s);
            Debug.Assert(indices_s.Length % 3 == 0);
            for (int i = 0; i < indices_s.Length; i += 3) {
                Mesh4D.Shadow4D va = verts_s[indices_s[i]];
                Mesh4D.Shadow4D vb = verts_s[indices_s[i + 1]];
                Mesh4D.Shadow4D vc = verts_s[indices_s[i + 2]];
                mesh5D.AddTriangleShadow((Vector5)va.vertex, (Vector5)vb.vertex, (Vector5)vc.vertex);
            }

            mesh5D.NextSubmesh();
        }

        //Dispose the mesh data correctly
        meshData.Dispose();
        meshData_s.Dispose();

        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder Generate5DFlat(string fname4D, Vector4[] centerBias = null) {
        //Load the mesh4D
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/" + fname4D + ".mesh");

        //Create a new mesh5D
        Mesh5D mesh5D = new Mesh5D(mesh4D.subMeshCount);

        //Scan through the 4D mesh
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D)) {
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(verts.Length % 4 == 0);
            for (int s = 0; s < mesh4D.subMeshCount; ++s) {
                //Add the simplices
                int[] indices = mesh4D.GetIndices(s);
                Debug.Assert(indices.Length % 6 == 0);
                Vector4 center = CenterOfMass(verts, indices);
                if (centerBias != null) { center += centerBias[s]; }
                for (int i = 0; i < indices.Length; i += 6) {
                    Mesh4D.Vertex4D v = verts[indices[i]];
                    Vector5 a1 = (Vector5)v.va;
                    Vector5 b1 = (Vector5)v.vb;
                    Vector5 c1 = (Vector5)v.vc;
                    Vector5 d1 = (Vector5)v.vd;

                    //Turn every tetrahedron into a pentatope
                    ConvexCheck(v.va, v.vb, v.vc, v.vd, center);
                    mesh5D.AddSimplex(a1, b1, c1, d1, (Vector5)center);

                    //Shadow
                    mesh5D.AddTriangleShadow(a1, b1, c1);
                    mesh5D.AddTriangleShadow(b1, c1, d1);
                    mesh5D.AddTriangleShadow(c1, d1, a1);
                    mesh5D.AddTriangleShadow(d1, a1, b1);
                }
                mesh5D.NextSubmesh();
            }
        }
        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder Generate5DHoleFlat(string fname4D, float thickness, float height = 0.0f) {
        //Load the mesh4D
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/" + fname4D + ".mesh");

        //Create a new mesh5D
        Mesh5D mesh5D = new Mesh5D(mesh4D.subMeshCount);

        //Scan through the 4D mesh
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D)) {
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(verts.Length % 4 == 0);
            Vector5 bump = new Vector5(0.0f, 0.0f, 0.0f, 0.0f, height);
            for (int s = 0; s < mesh4D.subMeshCount; ++s) {
                //Add the simplices
                int[] indices = mesh4D.GetIndices(s);
                Debug.Assert(indices.Length % 6 == 0);
                for (int i = 0; i < indices.Length; i += 6) {
                    Mesh4D.Vertex4D v = verts[indices[i]];
                    Vector5 a1 = (Vector5)v.va;
                    Vector5 b1 = (Vector5)v.vb;
                    Vector5 c1 = (Vector5)v.vc;
                    Vector5 d1 = (Vector5)v.vd;
                    Vector5 a2 = a1 * (thickness + 1.0f) + bump;
                    Vector5 b2 = b1 * (thickness + 1.0f) + bump;
                    Vector5 c2 = c1 * (thickness + 1.0f) + bump;
                    Vector5 d2 = d1 * (thickness + 1.0f) + bump;

                    if (thickness > 0.0f) {
                        mesh5D.AddTetraPrism(a1, b1, c1, d1, a2, b2, c2, d2);
                    } else {
                        mesh5D.AddTetraPrism(a1, b1, d1, c1, a2, b2, d2, c2);
                    }

                    //Shadow
                    mesh5D.AddQuadShadow(a1, a2, b1, b2);
                    mesh5D.AddQuadShadow(a1, a2, c1, c2);
                    mesh5D.AddQuadShadow(a1, a2, d1, d2);
                    mesh5D.AddQuadShadow(b1, b2, c1, c2);
                    mesh5D.AddQuadShadow(b1, b2, d1, d2);
                    mesh5D.AddQuadShadow(c1, c2, d1, d2);
                }
                mesh5D.NextSubmesh();
            }
        }
        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder Generate5DExtrude(string filepath, float length, Vector4[] centerBias = null, bool capTop = true, bool capBottom = true, bool vertAO = true) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/" + filepath + ".mesh");

        //Create a new mesh5D
        Mesh5D mesh5D = new Mesh5D(mesh4D.subMeshCount);
        Vector5 extrude = new Vector5(0.0f, 0.0f, 0.0f, 0.0f, length);

        //Scan through the 4D mesh
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D)) {
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(verts.Length % 4 == 0);
            for (int s = 0; s < mesh4D.subMeshCount; ++s) {
                //Add the simplices
                int[] indices = mesh4D.GetIndices(s);
                Debug.Assert(indices.Length % 6 == 0);
                Vector4 center = CenterOfMass(verts, indices);
                if (centerBias != null) { center += centerBias[s]; }
                for (int i = 0; i < indices.Length; i += 6) {
                    Mesh4D.Vertex4D v = verts[indices[i]];
                    Vector5 a1 = (Vector5)v.va;
                    Vector5 b1 = (Vector5)v.vb;
                    Vector5 c1 = (Vector5)v.vc;
                    Vector5 d1 = (Vector5)v.vd;
                    Vector5 a2 = a1 + extrude;
                    Vector5 b2 = b1 + extrude;
                    Vector5 c2 = c1 + extrude;
                    Vector5 d2 = d1 + extrude;
                    a1 -= extrude;
                    b1 -= extrude;
                    c1 -= extrude;
                    d1 -= extrude;

                    //Caps for the prism
                    //TODO: Fix the convex check
                    //ConvexCheck(a1, b1, c1, d1, center);
                    if (capBottom) {
                        mesh5D.AddSimplex(a1, b1, c1, d1, (Vector5)center - extrude, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                        mesh5D.AddTriangleShadow(a1, b1, c1);
                        mesh5D.AddTriangleShadow(b1, c1, d1);
                        mesh5D.AddTriangleShadow(c1, d1, a1);
                        mesh5D.AddTriangleShadow(d1, a1, b1);
                    }
                    if (capTop) {
                        mesh5D.AddSimplex(a2, b2, c2, d2, (Vector5)center + extrude, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f);
                        mesh5D.AddTriangleShadow(a2, b2, c2);
                        mesh5D.AddTriangleShadow(b2, c2, d2);
                        mesh5D.AddTriangleShadow(c2, d2, a2);
                        mesh5D.AddTriangleShadow(d2, a2, b2);
                    }

                    //Walls of the prism
                    if (vertAO) {
                        mesh5D.AddTetraPrism(a2, b2, c2, d2, a1, b1, c1, d1, 1.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                    } else {
                        mesh5D.AddTetraPrism(a2, b2, c2, d2, a1, b1, c1, d1);
                    }

                    //Shadows
                    mesh5D.AddQuadShadow(a1, a2, b1, b2);
                    mesh5D.AddQuadShadow(a1, a2, c1, c2);
                    mesh5D.AddQuadShadow(a1, a2, d1, d2);
                    mesh5D.AddQuadShadow(b1, b2, c1, c2);
                    mesh5D.AddQuadShadow(b1, b2, d1, d2);
                    mesh5D.AddQuadShadow(c1, c2, d1, d2);
                }
                mesh5D.NextSubmesh();
            }
        }
        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder Generate5DPyramid(string filepath, float length, Vector4[] centerBias = null, bool capped = true) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/" + filepath + ".mesh");

        //Flatten the mesh
        //Create a new mesh5D
        Mesh5D mesh5D = new Mesh5D(mesh4D.subMeshCount);
        Vector5 extrude = new Vector5(0.0f, 0.0f, 0.0f, 0.0f, length);

        //Scan through the 4D mesh
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D)) {
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(verts.Length % 4 == 0);
            for (int s = 0; s < mesh4D.subMeshCount; ++s) {
                int[] indices = mesh4D.GetIndices(s);
                Debug.Assert(indices.Length % 6 == 0);
                Vector4 center = CenterOfMass(verts, indices);
                Vector5 centerExtrude = (Vector5)center + extrude;
                if (centerBias != null) { center += centerBias[s]; }
                for (int i = 0; i < indices.Length; i += 6) {
                    Mesh4D.Vertex4D v = verts[indices[i]];
                    Vector5 a1 = (Vector5)v.va;
                    Vector5 b1 = (Vector5)v.vb;
                    Vector5 c1 = (Vector5)v.vc;
                    Vector5 d1 = (Vector5)v.vd;

                    //Walls of the pyramid
                    ConvexCheck(v.va, v.vb, v.vc, v.vd, center);
                    mesh5D.AddSimplex(a1, b1, c1, d1, centerExtrude);
                    mesh5D.AddTriangleShadow(a1, b1, centerExtrude);
                    mesh5D.AddTriangleShadow(a1, c1, centerExtrude);
                    mesh5D.AddTriangleShadow(a1, d1, centerExtrude);
                    mesh5D.AddTriangleShadow(b1, c1, centerExtrude);
                    mesh5D.AddTriangleShadow(b1, d1, centerExtrude);
                    mesh5D.AddTriangleShadow(c1, d1, centerExtrude);

                    //Cap of the pyramid
                    if (capped) {
                        mesh5D.AddSimplex(a1, b1, c1, d1, (Vector5)center);
                        mesh5D.AddTriangleShadow(a1, b1, c1);
                        mesh5D.AddTriangleShadow(b1, c1, d1);
                        mesh5D.AddTriangleShadow(c1, d1, a1);
                        mesh5D.AddTriangleShadow(d1, a1, b1);
                    }
                }
                mesh5D.NextSubmesh();
            }
        }
        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder GenerateRevolve(string filepath, int segments, float revAngle = 360.0f) {
        return GenerateRevolve(filepath, segments, Vector4.zero, revAngle);
    }
    private static Mesh5DBuilder GenerateRevolve(string filepath, int segments, Vector4 offset, float revAngle = 360.0f) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/" + filepath + ".mesh");

        //Create a new mesh5D
        Mesh5D mesh5D = new Mesh5D(mesh4D.subMeshCount);

        //Scan through the 4D mesh
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D)) {
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(verts.Length % 4 == 0);
            for (int s = 0; s < mesh4D.subMeshCount; ++s) {
                int[] indices = mesh4D.GetIndices(s);
                Debug.Assert(indices.Length % 6 == 0);
                for (int r = 0; r < segments; ++r) {
                    //Determine the angle for this segment
                    float angle1 = r * revAngle / segments;
                    float angle2 = (r + 1) * revAngle / segments;
                    if (revAngle == 360.0f) {
                        angle2 = ((r + 1) % segments) * 360.0f / segments;
                    }

                    //Convert angle to matrix
                    Matrix5x5 m1 = Transform5D.PlaneRotation(angle1, 3, 4);
                    Matrix5x5 m2 = Transform5D.PlaneRotation(angle2, 3, 4);

                    for (int i = 0; i < indices.Length; i += 6) {
                        Mesh4D.Vertex4D vert = verts[indices[i]];
                        Vector4 a = vert.va + offset;
                        Vector4 b = vert.vb + offset;
                        Vector4 c = vert.vc + offset;
                        Vector4 d = vert.vd + offset;
                        Debug.Assert(a.w > -1e-6f && b.w > -1e-6f && c.w > -1e-6f && d.w > -1e-6f, "Negative w coordinates in revolution mesh: " + a.w + " " + b.w + " " + c.w + " " + d.w);
                        a.w = Mathf.Max(0.0f, a.w);
                        b.w = Mathf.Max(0.0f, b.w);
                        c.w = Mathf.Max(0.0f, c.w);
                        d.w = Mathf.Max(0.0f, d.w);
                        Vector5 a1 = m1 * (Vector5)a;
                        Vector5 b1 = m1 * (Vector5)b;
                        Vector5 c1 = m1 * (Vector5)c;
                        Vector5 d1 = m1 * (Vector5)d;
                        Vector5 a2 = m2 * (Vector5)a;
                        Vector5 b2 = m2 * (Vector5)b;
                        Vector5 c2 = m2 * (Vector5)c;
                        Vector5 d2 = m2 * (Vector5)d;

                        //Walls of the prism
                        mesh5D.AddTetraPrism(a1, b1, c1, d1, a2, b2, c2, d2,
                                             vert.aoA, vert.aoB, vert.aoC, vert.aoD,
                                             vert.aoA, vert.aoB, vert.aoC, vert.aoD);
                        mesh5D.AddTriangleShadow(a1, b1, c1);
                        mesh5D.AddTriangleShadow(b1, c1, d1);
                        mesh5D.AddTriangleShadow(c1, d1, a1);
                        mesh5D.AddTriangleShadow(d1, a1, b1);
                        mesh5D.AddQuadShadow(a1, a2, b1, b2);
                        mesh5D.AddQuadShadow(a1, a2, c1, c2);
                        mesh5D.AddQuadShadow(a1, a2, d1, d2);
                        mesh5D.AddQuadShadow(b1, b2, c1, c2);
                        mesh5D.AddQuadShadow(b1, b2, d1, d2);
                        mesh5D.AddQuadShadow(c1, c2, d1, d2);
                    }
                }
                mesh5D.NextSubmesh();
            }
        }
        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder GenerateDuoPrism(string flat4D, string mesh3D, string ringSurface, bool twoMaterials = false) {
        Mesh5D mesh5D = new Mesh5D(twoMaterials ? 2 : 1);

        GenerateDuoPrism31(mesh5D, ringSurface, flat4D);
        //GenerateDuoPrismShadow(mesh4D, surface1, surface2);
        if (!twoMaterials) { mesh5D.curSubMesh = 0; }
        //GenerateDuoPrismHalf(mesh5D, surface2, surface1, true, biSmooth);
        GenerateDuoPrism22(mesh5D, ringSurface, mesh3D);
        return new Mesh5DBuilder(mesh5D);
    }
    private static void GenerateDuoPrism31(Mesh5D mesh5D, string chainName, string flatVolumeName) {
        //Load the line chain and surface for duo prism
        List<OFFParser.Line2D> lines = OFFParser.LoadOBJ2D(chainName);
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/" + flatVolumeName + ".mesh");

        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D)) {
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(mesh4D.subMeshCount == 1 && verts.Length % 4 == 0);
            int[] surfaceIndices = mesh4D.GetIndices(0);
            Debug.Assert(surfaceIndices.Length % 6 == 0);

            for (int ix1 = 0; ix1 < lines.Count; ++ix1) {
                Vector2 pxy = lines[ix1].a;
                Vector2 nxy = lines[ix1].b;
                for (int ix2 = 0; ix2 < surfaceIndices.Length; ix2 += 6) {
                    Mesh4D.Vertex4D surfaceVert = verts[surfaceIndices[ix2]];
                    Vector4 a = surfaceVert.va;
                    Vector4 b = surfaceVert.vb;
                    Vector4 c = surfaceVert.vc;
                    Vector4 d = surfaceVert.vd;
                    Debug.Assert(Mathf.Abs(a.w) < 1e-4f, "Non-flat vertex: " + a.w);
                    Debug.Assert(Mathf.Abs(b.w) < 1e-4f, "Non-flat vertex: " + b.w);
                    Debug.Assert(Mathf.Abs(c.w) < 1e-4f, "Non-flat vertex: " + c.w);
                    Debug.Assert(d.magnitude < 1e-4f, "Non-flat vertex: " + d);

                    //Define vertices for first prism cell
                    Vector5 a1 = new Vector5(pxy.x, -pxy.y, a.x, a.y, a.z);
                    Vector5 a2 = new Vector5(nxy.x, -nxy.y, a.x, a.y, a.z);
                    Vector5 b1 = new Vector5(pxy.x, -pxy.y, b.x, b.y, b.z);
                    Vector5 b2 = new Vector5(nxy.x, -nxy.y, b.x, b.y, b.z);
                    Vector5 c1 = new Vector5(pxy.x, -pxy.y, c.x, c.y, c.z);
                    Vector5 c2 = new Vector5(nxy.x, -nxy.y, c.x, c.y, c.z);
                    Vector5 d1 = new Vector5(pxy.x, -pxy.y, d.x, d.y, d.z);
                    Vector5 d2 = new Vector5(nxy.x, -nxy.y, d.x, d.y, d.z);

                    mesh5D.AddTetraPrism(a1, b1, c1, d1, a2, b2, c2, d2);
                    mesh5D.AddTriangleShadow(a1, b1, c1);
                    mesh5D.AddTriangleShadow(a1, b1, d1);
                    mesh5D.AddTriangleShadow(a1, c1, d1);
                    mesh5D.AddTriangleShadow(b1, c1, d1);
                }
            }
            mesh5D.NextSubmesh();
        }
    }
    private static void GenerateDuoPrism22(Mesh5D mesh5D, string ringAreaName, string mesh3DName) {
        //Load the line chain and surface for duo prism
        Mesh meshRing = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Editor/Surface/" + ringAreaName + ".fbx");
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + mesh3DName + ".fbx");
        Debug.Assert(meshRing.subMeshCount == 1 && mesh3D.subMeshCount == 1);

        Vector3[] ringVerts = meshRing.vertices;
        int[] ringIndices = meshRing.GetIndices(0);
        Debug.Assert(meshRing.GetTopology(0) == MeshTopology.Triangles && ringIndices.Length % 3 == 0);

        Vector3[] verts = mesh3D.vertices;
        int[] indices = mesh3D.GetIndices(0);
        Debug.Assert(mesh3D.GetTopology(0) == MeshTopology.Triangles && indices.Length % 3 == 0);

        for (int ix1 = 0; ix1 < ringIndices.Length; ix1 += 3) {
            Vector3 ra = ringVerts[ringIndices[ix1]];
            Vector3 rb = ringVerts[ringIndices[ix1 + 1]];
            Vector3 rc = ringVerts[ringIndices[ix1 + 2]];
            Debug.Assert(Mathf.Abs(ra.z) < 1e-4f, "Non-flat vertex: " + ra.z);
            Debug.Assert(Mathf.Abs(rb.z) < 1e-4f, "Non-flat vertex: " + rb.z);
            Debug.Assert(Mathf.Abs(rc.z) < 1e-4f, "Non-flat vertex: " + rc.z);
            for (int ix2 = 0; ix2 < indices.Length; ix2 += 3) {
                Vector3 a = verts[indices[ix2]];
                Vector3 b = verts[indices[ix2 + 1]];
                Vector3 c = verts[indices[ix2 + 2]];

                //Define vertices for cell
                Vector5 aa = new Vector5(ra.x, -ra.y, a.x, a.y, a.z);
                Vector5 ab = new Vector5(ra.x, -ra.y, b.x, b.y, b.z);
                Vector5 ac = new Vector5(ra.x, -ra.y, c.x, c.y, c.z);
                Vector5 ba = new Vector5(rb.x, -rb.y, a.x, a.y, a.z);
                Vector5 bb = new Vector5(rb.x, -rb.y, b.x, b.y, b.z);
                Vector5 bc = new Vector5(rb.x, -rb.y, c.x, c.y, c.z);
                Vector5 ca = new Vector5(rc.x, -rc.y, a.x, a.y, a.z);
                Vector5 cb = new Vector5(rc.x, -rc.y, b.x, b.y, b.z);
                Vector5 cc = new Vector5(rc.x, -rc.y, c.x, c.y, c.z);

                mesh5D.AddDuoPrism(aa, ab, ac, ba, bb, bc, ca, cb, cc);
                //TODO: Figure out shadows
            }
        }
        mesh5D.NextSubmesh();
    }

    private static Mesh5DBuilder GeneratePathExtrude(string chainName, string volume4DName, bool pathSmooth=false, int numSubMeshes=1) {
        //Load the line chain and surface for duo prism
        Mesh5D mesh5D = new Mesh5D(numSubMeshes);
        List<OFFParser.Line2D> lines = OFFParser.LoadOBJ2D(chainName);
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/" + volume4DName + ".mesh");

        Vector5 extrudeDir = new Vector5(0, 0, 0, 0, 1);
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D)) {
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(mesh4D.subMeshCount == 1 && verts.Length % 4 == 0);
            int[] surfaceIndices = mesh4D.GetIndices(0);
            Debug.Assert(surfaceIndices.Length % 6 == 0);
            for (int ix1 = 0; ix1 < lines.Count; ++ix1) {
                Vector2 pxy = lines[ix1].a;
                Vector2 nxy = lines[ix1].b;
                float pAO = lines[ix1].aoA;
                float nAO = lines[ix1].aoB;
                int vStartIndex = mesh5D.vArray.Count;
                for (int ix2 = 0; ix2 < surfaceIndices.Length; ix2 += 6) {
                    Mesh4D.Vertex4D surfaceVert = verts[surfaceIndices[ix2]];
                    Vector5 a = (Vector5)surfaceVert.va;
                    Vector5 b = (Vector5)surfaceVert.vb;
                    Vector5 c = (Vector5)surfaceVert.vc;
                    Vector5 d = (Vector5)surfaceVert.vd;

                    //Define vertices for first prism cell
                    Vector5 a1 = a * pxy.x + extrudeDir * pxy.y;
                    Vector5 a2 = a * nxy.x + extrudeDir * nxy.y;
                    Vector5 b1 = b * pxy.x + extrudeDir * pxy.y;
                    Vector5 b2 = b * nxy.x + extrudeDir * nxy.y;
                    Vector5 c1 = c * pxy.x + extrudeDir * pxy.y;
                    Vector5 c2 = c * nxy.x + extrudeDir * nxy.y;
                    Vector5 d1 = d * pxy.x + extrudeDir * pxy.y;
                    Vector5 d2 = d * nxy.x + extrudeDir * nxy.y;

                    if (Mathf.Abs(pxy.x) < 1e-6f) {
                        mesh5D.AddSimplex(a2, b2, c2, d2, a1,
                                          nAO, nAO, nAO, nAO, pAO);
                        mesh5D.AddTriangleShadow(a2, b2, a1);
                        mesh5D.AddTriangleShadow(a2, c2, a1);
                        mesh5D.AddTriangleShadow(a2, d2, a1);
                        mesh5D.AddTriangleShadow(b2, c2, a1);
                        mesh5D.AddTriangleShadow(b2, d2, a1);
                        mesh5D.AddTriangleShadow(c2, d2, a1);
                    } else if (Mathf.Abs(nxy.x) < 1e-6f) {
                        mesh5D.AddSimplex(a2, b1, c1, d1, a1,
                                          nAO, pAO, pAO, pAO, pAO);
                        mesh5D.AddTriangleShadow(a1, b1, a2);
                        mesh5D.AddTriangleShadow(a1, c1, a2);
                        mesh5D.AddTriangleShadow(a1, d1, a2);
                        mesh5D.AddTriangleShadow(b1, c1, a2);
                        mesh5D.AddTriangleShadow(b1, d1, a2);
                        mesh5D.AddTriangleShadow(c1, d1, a2);
                    } else {
                        mesh5D.AddTetraPrism(a1, b1, c1, d1, a2, b2, c2, d2,
                                             pAO, pAO, pAO, pAO, nAO, nAO, nAO, nAO);
                        mesh5D.AddQuadShadow(a1, b1, a2, b2);
                        mesh5D.AddQuadShadow(a1, c1, a2, c2);
                        mesh5D.AddQuadShadow(a1, d1, a2, d2);
                        mesh5D.AddQuadShadow(b1, c1, b2, c2);
                        mesh5D.AddQuadShadow(b1, d1, b2, d2);
                        mesh5D.AddQuadShadow(c1, d1, c2, d2);
                    }
                }
                //Smooth only this layer if path smoothing enabled
                if (pathSmooth) {
                    Mesh5DBuilder.Smoothen(mesh5D, vStartIndex, mesh5D.vArray.Count);
                }
            }
            mesh5D.NextSubmesh();
        }
        return new Mesh5DBuilder(mesh5D);
    }

    public static Mesh5DBuilder MergeMeshes5D(string name) {
        //Get the primitive mesh from a GameObject.
        Debug.Log("Generating " + name + "...");
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modeling/" + name + ".prefab");
        Debug.Assert(model != null, "Could not find model '" + name + "' in Modeling folder.");
        MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
        Object5D[] objs5D = model.GetComponentsInChildren<Object5D>();

        //Awaken all Object5D components
        foreach (Object5D obj5D in objs5D) {
            obj5D.Awake();
            obj5D.transform.hasChanged = true;
        }

        //Get a material count for all the meshes in the object
        Dictionary<Material, int> matMap = new();
        foreach (MeshRenderer renderer in renderers) {
            if (!renderer.enabled) { continue; }
            Material[] sharedMaterials = renderer.sharedMaterials;
            for (int i = 0; i < sharedMaterials.Length; ++i) {
                Material sharedMaterial = sharedMaterials[i];
                if (!matMap.ContainsKey(sharedMaterial)) {
                    matMap[sharedMaterial] = matMap.Count;
                }
            }
        }

        //Create a new Mesh5D
        Mesh5D mesh5D = new Mesh5D(matMap.Count);

        //Add meshes for each materials
        foreach (MeshRenderer renderer in renderers) {
            //Get meshes
            if (!renderer.enabled) { continue; }
            Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            Mesh mesh_s = renderer.GetComponent<ShadowFilter>()?.shadowMesh;
            Mesh mesh_w = renderer.GetComponent<ShadowFilter>()?.wireMesh;
            Debug.Assert(mesh != null, "Mesh renderer '" + renderer.name + "' did not have a mesh");
            Object5D obj5D = renderer.GetComponent<Object5D>();
            Debug.Assert(obj5D != null, "Mesh renderer '" + renderer.name + "' did not have an Object5D");
            Material[] sharedMaterials = renderer.sharedMaterials;

            //Merge all indices
            for (int i = 0; i < sharedMaterials.Length; ++i) {
                Material sharedMaterial = sharedMaterials[i];
                int[] vIndices = mesh.GetIndices(i);
                int[] sIndices = (mesh_s ? mesh_s.GetIndices(i) : new int[0]);
                int[] wIndices = (mesh_w ? mesh_w.GetIndices(i) : new int[0]);
                int subMesh = matMap[sharedMaterial];
                mesh5D.AddRawIndices(vIndices, sIndices, wIndices, subMesh);
            }

            //Merge all vertices
            Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh);
            NativeArray<Mesh5D.Vertex5D> vVerts = meshData[0].GetVertexData<Mesh5D.Vertex5D>(0);
            Debug.Assert(vVerts.Length % 5 == 0, "Invalid number of vertices");
            if (mesh_s) {
                Mesh.MeshDataArray meshData_s = Mesh.AcquireReadOnlyMeshData(mesh_s);
                Mesh.MeshDataArray meshData_w = Mesh.AcquireReadOnlyMeshData(mesh_w);
                NativeArray<Mesh5D.Shadow5D> sVerts = meshData_s[0].GetVertexData<Mesh5D.Shadow5D>(0);
                NativeArray<Mesh5D.Shadow5D> wVerts = meshData_w[0].GetVertexData<Mesh5D.Shadow5D>(0);
                Debug.Assert(sVerts.Length % 3 == 0, "Invalid number of vertices");
                Debug.Assert(mesh.subMeshCount == mesh_s.subMeshCount);
                mesh5D.AddRawVerts(vVerts, sVerts, wVerts, obj5D.WorldTransform5D());
                meshData_s.Dispose();
            } else {
                mesh5D.AddRawVerts(vVerts, obj5D.WorldTransform5D());
            }

            //Dispose the mesh data correctly
            meshData.Dispose();
        }
        return new Mesh5DBuilder(mesh5D);
    }

    private static void AddFlatHypercube(Mesh5D mesh5D, Matrix5x5 rotate, Vector5 offset, bool parity=false) {
        //Add a unit cube with rotation and translation
        Vector5 v0 = offset + rotate * new Vector5(-1, -1, -1, -1, 0);
        Vector5 v1 = offset + rotate * new Vector5(1, -1, -1, -1, 0);
        Vector5 v2 = offset + rotate * new Vector5(-1, 1, -1, -1, 0);
        Vector5 v3 = offset + rotate * new Vector5(1, 1, -1, -1, 0);
        Vector5 v4 = offset + rotate * new Vector5(-1, -1, 1, -1, 0);
        Vector5 v5 = offset + rotate * new Vector5(1, -1, 1, -1, 0);
        Vector5 v6 = offset + rotate * new Vector5(-1, 1, 1, -1, 0);
        Vector5 v7 = offset + rotate * new Vector5(1, 1, 1, -1, 0);
        Vector5 v8 = offset + rotate * new Vector5(-1, -1, -1, 1, 0);
        Vector5 v9 = offset + rotate * new Vector5(1, -1, -1, 1, 0);
        Vector5 v10 = offset + rotate * new Vector5(-1, 1, -1, 1, 0);
        Vector5 v11 = offset + rotate * new Vector5(1, 1, -1, 1, 0);
        Vector5 v12 = offset + rotate * new Vector5(-1, -1, 1, 1, 0);
        Vector5 v13 = offset + rotate * new Vector5(1, -1, 1, 1, 0);
        Vector5 v14 = offset + rotate * new Vector5(-1, 1, 1, 1, 0);
        Vector5 v15 = offset + rotate * new Vector5(1, 1, 1, 1, 0);
        if (!parity) {
            mesh5D.AddCell(v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15);
            mesh5D.AddCellShadow(v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15);
        } else {
            mesh5D.AddCell(v1, v3, v0, v2, v5, v7, v4, v6, v9, v11, v8, v10, v13, v15, v12, v14);
            mesh5D.AddCellShadow(v1, v3, v0, v2, v5, v7, v4, v6, v9, v11, v8, v10, v13, v15, v12, v14);
        }
    }

    private static Mesh5DBuilder GenerateLandscape(float radius, float size, float minHeight, float maxHeight) {
        //Create a new mesh5D
        Debug.Log("Generating landscape...");
        Mesh5D mesh5D = new Mesh5D();

        //Load OFF file
        List<Vector4> verticies = new List<Vector4>();
        List<Vector4> faces = new List<Vector4>();
        List<Vector4> tetras = new List<Vector4>();
        OFFParser.ParseCenters4D("Hexacosichoron", verticies, faces, tetras);

        //Combine lists
        verticies.AddRange(faces);
        verticies.AddRange(tetras);

        //4-simplex vertices
        float root5 = 1.0f / Mathf.Sqrt(5);
        Vector4[] simplex = new Vector4[] {
            new Vector4(1, 1, 1, -root5),
            new Vector4(1, -1, -1, -root5),
            new Vector4(-1, 1, -1, -root5),
            new Vector4(-1, -1, 1, -root5),
            new Vector4(0, 0, 0, 4.0f * root5),
        };

        //Use vertices to construct simplices
        PseudoRandom._seed = 1337;
        Vector5 up = new Vector5(0.0f, 1.0f, 0.0f, 0.0f, 0.0f);
        for (int i = 0; i < verticies.Count; ++i) {
            Vector4 center = verticies[i] * radius;
            Matrix4x4 rot = PseudoRandom.Rotation4D().matrix;
            Vector5 a = Transform5D.InsertY(center + (rot * simplex[0]) * size, 0.0f);
            Vector5 b = Transform5D.InsertY(center + (rot * simplex[1]) * size, 0.0f);
            Vector5 c = Transform5D.InsertY(center + (rot * simplex[2]) * size, 0.0f);
            Vector5 d = Transform5D.InsertY(center + (rot * simplex[3]) * size, 0.0f);
            Vector5 e = Transform5D.InsertY(center + (rot * simplex[4]) * size, 0.0f);
            Vector5 tip = Transform5D.InsertY(center, PseudoRandom.Range(minHeight, maxHeight));
            uint ao = (uint)(Mathf.Clamp(PseudoRandom.Float() * 64.0f, 0.0f, 63.0f));
            uint aoAll = ao | (ao << 6) | (ao << 12) | (ao << 18) | (ao << 24);
            mesh5D.AddSimplexNormal(up, a, b, c, d, tip, aoAll);
            mesh5D.AddSimplexNormal(up, a, b, c, e, tip, aoAll);
            mesh5D.AddSimplexNormal(up, a, b, d, e, tip, aoAll);
            mesh5D.AddSimplexNormal(up, a, c, d, e, tip, aoAll);
            mesh5D.AddSimplexNormal(up, b, c, d, e, tip, aoAll);
        }

        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder GenerateLily(float[] radii, float[] height, float[] stretch) {
        //Create a new mesh5D
        Debug.Log("Generating Lily...");
        Mesh mesh4D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes4D/600cell.mesh");
        Mesh5D mesh5D = new Mesh5D();
        float BASE_MUL = 1.5f;
        float BASE_PUSH_SCALE = 0.7f; // 1.4f;
        float low = 0.4f;
        float high = 1.0f;
        PseudoRandom._seed = 12345;

        //Scan through the 4D mesh
        using (Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh4D)) {
            NativeArray<Mesh4D.Vertex4D> verts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(verts.Length % 4 == 0);
            Debug.Assert(mesh4D.subMeshCount == 1);
            int[] indices = mesh4D.GetIndices(0);
            Debug.Assert(indices.Length % 6 == 0);
            for (int s = 0; s < radii.Length; ++s) {
                Matrix4x4 randRot = PseudoRandom.RotationMatrix4D();
                Vector5 up = new Vector5(0.0f, height[s], 0.0f, 0.0f, 0.0f);
                for (int i = 0; i < indices.Length; i += 6) {
                    Mesh4D.Vertex4D v = verts[indices[i]];
                    Vector5 a = Transform5D.InsertY(randRot * v.va * radii[s], 0.0f);
                    Vector5 b = Transform5D.InsertY(randRot * v.vb * radii[s], 0.0f);
                    Vector5 c = Transform5D.InsertY(randRot * v.vc * radii[s], 0.0f);
                    Vector5 d = Transform5D.InsertY(randRot * v.vd * radii[s], 0.0f);

                    //Use vertices to construct simplices
                    Vector5 center = (a + b + c + d) * 0.25f;
                    a += BASE_MUL * (a - center);
                    b += BASE_MUL * (b - center);
                    c += BASE_MUL * (c - center);
                    d += BASE_MUL * (d - center);
                    Vector5 e = center * BASE_PUSH_SCALE;
                    Vector5 top = center * stretch[s] + up;

                    mesh5D.AddSimplexNormal(-center, a, b, c, d, top, low, low, low, low, high);
                    mesh5D.AddSimplexNormal(center, a, b, c, e, top, low, low, low, low, high);
                    mesh5D.AddSimplexNormal(center, a, b, d, e, top, low, low, low, low, high);
                    mesh5D.AddSimplexNormal(center, a, c, d, e, top, low, low, low, low, high);
                    mesh5D.AddSimplexNormal(center, b, c, d, e, top, low, low, low, low, high);
                    if (s == 0) {
                        mesh5D.AddTriangleShadow(top, a, b);
                        mesh5D.AddTriangleShadow(top, a, c);
                        mesh5D.AddTriangleShadow(top, a, d);
                        mesh5D.AddTriangleShadow(top, b, c);
                        mesh5D.AddTriangleShadow(top, b, d);
                        mesh5D.AddTriangleShadow(top, c, d);
                    }
                }
            }
        }

        return new Mesh5DBuilder(mesh5D);
    }

    private static Mesh5DBuilder Color3840Cell(Mesh5DBuilder mesh3840) {
        List<int> indices = mesh3840.mesh5D.vIndices[0];
        List<Mesh5D.Vertex5D> verts = mesh3840.mesh5D.vArray;
        List<Vector5> normals = new();
        float[] coords = new float[5] { 0.1995054f, 0.2264488f, 0.2686101f, 0.3500596f, 0.8451186f };
        List<int> group = new();
        for (int i = 0; i < indices.Count; i += 9) {
            Mesh5D.Vertex5D v = verts[indices[i]];
            Vector5 n = Transform5D.MakeNormal(v.va5 - v.ve5, v.vb5 - v.ve5, v.vc5 - v.ve5, v.vd5 - v.ve5);
            normals.Add(n / n.magnitude);
            group.Add(0);
        }
        Debug.Assert(normals.Count == 3840);
        for (int i = 0; i < normals.Count; ++i) {
            //float dp1 = Vector5.Dot(normals[0], normals[i]);
            Vector5 n = normals[i];
            float sign = Mathf.Sign(n.x) * Mathf.Sign(n.y) * Mathf.Sign(n.z) * Mathf.Sign(n.w) * Mathf.Sign(n.v);
            Vector5 permute = Vector5.zero;
            for (int j = 0; j < 5; ++j) {
                for (int k = 0; k < 5; ++k) {
                    if (Mathf.Abs(Mathf.Abs(n[j]) - coords[k]) < 1e-3) {
                        permute[j] = k;
                        break;
                    }
                }
            }
            Vector5 visited = Vector5.zero;
            int parity = 0;
            for (int start = 0; start < 5; start++) {
                if (visited[start] != 0.0f) continue;
                visited[start] = 1.0f;
                for (int j = (int)permute[start]; j != start; j = (int)permute[j]) {
                    parity += 1;
                    visited[j] = 1.0f;
                }
            }
            group[i] = (parity % 2) + (sign < 0.0f ? 0 : 2);
        }
        for (int i = 0; i < indices.Count; i += 9) {
            Mesh5D.Vertex5D v = verts[indices[i]];
            uint ao = (uint)group[i / 9] * 51;
            v.ao = ao | (ao << 6) | (ao << 12) | (ao << 18) | (ao << 24);
            verts[indices[i]] = v;
            verts[indices[i + 1]] = v;
            verts[indices[i + 2]] = v;
            verts[indices[i + 5]] = v;
            verts[indices[i + 8]] = v;
        }
        return mesh3840;
    }

    private static Mesh5DBuilder GenerateCompound(Mesh5DBuilder meshBuilder, string group, int numColors = -1, int maxTouching = -1) {
        List<Matrix5x5> rots = GenerateGroups.LoadGroup5D(group);
        Mesh5DBuilder compound = new Mesh5DBuilder(new Mesh5D());
        Debug.Assert(meshBuilder.mesh5D.vIndices.Length == 1);
        int[] vIndices = meshBuilder.mesh5D.vIndices[0].ToArray();
        int[] sIndices = meshBuilder.mesh5D.sIndices[0].ToArray();
        int[] wIndices = meshBuilder.mesh5D.wIndices[0].ToArray();
        List<Mesh5D.Vertex5D> vArray = meshBuilder.mesh5D.vArray;
        List<Mesh5D.Shadow5D> sArray = meshBuilder.mesh5D.sArray;
        List<Mesh5D.Shadow5D> wArray = meshBuilder.mesh5D.wArray;

        //Also make a flipped mesh
        Mesh5DBuilder flippedBuilder = new Mesh5DBuilder(new Mesh5D());
        flippedBuilder.mesh5D.AddRawIndices(vIndices, sIndices, wIndices, 0);
        flippedBuilder.mesh5D.AddRawVerts(vArray, sArray, wArray, Transform5D.identity);
        flippedBuilder.FlipNormals();
        int[] vIndicesFlip = flippedBuilder.mesh5D.vIndices[0].ToArray();
        List<Mesh5D.Vertex5D> vArrayFlip = flippedBuilder.mesh5D.vArray;
        Debug.Assert(vIndices.Length == vIndicesFlip.Length);

        //Create a list of unique vertices for the base
        HashSet<Vector5> vertSet = new();
        foreach (Mesh5D.Vertex5D v in vArray) {
            vertSet.Add(v.va5);
            vertSet.Add(v.vb5);
            vertSet.Add(v.vc5);
            vertSet.Add(v.vd5);
            vertSet.Add(v.ve5);
        }
        List<Vector5> vertList = new List<Vector5>(vertSet);
        List<List<Vector5>> existingPoints = new();
        List<Transform5D> finalTransforms = new();

        //Iterate through all symmetries of the group
        foreach (Matrix5x5 rot in rots) {
            //Check if the rotation is symmetric to an existing one
            Transform5D transform = new Transform5D(rot, Vector5.zero);
            List<Vector5> newVerts = new();
            foreach (Vector5 v in vertList) {
                newVerts.Add(transform * v);
            }
            bool duplicate = false;
            foreach (List<Vector5> points in existingPoints) {
                int numFailed = 0;
                foreach (Vector5 tv in newVerts) {
                    foreach (Vector5 ev in points) {
                        if ((tv - ev).sqrMagnitude < 1e-4) {
                            numFailed += 1;
                            break;
                        }
                    }
                }
                if (numFailed == vertList.Count) {
                    duplicate = true;
                    break;
                }
            }
            if (duplicate) {
                continue;
            }

            //Not a duplicate so add to compound
            existingPoints.Add(newVerts);
            finalTransforms.Add(transform);
        }
        if (maxTouching >= 0) {
            List<Transform5D> filteredTransforms = new();
            for (int i = 0; i < existingPoints.Count; ++i) {
                int numTouching = 0;
                for (int j = 0; j < existingPoints.Count; ++j) {
                    if (i == j) { continue; }
                    foreach (Vector5 vi in existingPoints[i]) {
                        foreach (Vector5 vj in existingPoints[j]) {
                            if ((vi - vj).sqrMagnitude < 1e-4) {
                                numTouching += 1;
                                break;
                            }
                        }
                    }
                    if (numTouching > maxTouching) {
                        break;
                    }
                }
                Debug.Log(numTouching);
                if (numTouching <= maxTouching) {
                    filteredTransforms.Add(finalTransforms[i]);
                }
            }
            finalTransforms = filteredTransforms;
        }
        List<Mesh5D.Vertex5D> compoundVArray = compound.mesh5D.vArray;
        numColors = (numColors > 0 ? numColors : finalTransforms.Count);
        for (int i = 0; i < finalTransforms.Count; ++i) {
            float color = (i % numColors) / (float)Mathf.Max(numColors - 1, 1);
            bool flip = (finalTransforms[i].matrix.determinant < 0.0f);
            compound.mesh5D.AddRawIndices(flip ? vIndicesFlip : vIndices, sIndices, wIndices, 0);
            compound.mesh5D.AddRawVerts(flip ? vArrayFlip : vArray, sArray, wArray, finalTransforms[i]);
            for (int j = compoundVArray.Count - vArray.Count; j < compoundVArray.Count; ++j) {
                Mesh5D.Vertex5D v = compoundVArray[j];
                uint ao = (uint)(color * 64.0f);
                v.ao = ao | (ao << 6) | (ao << 12) | (ao << 18) | (ao << 24);
                compoundVArray[j] = v;
            }
        }
        return compound;
    }
}
