#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public class GenerateMeshes4D : MonoBehaviour {
    [MenuItem("4D/Generate 4D Meshes")]
    public static void Generate4DMeshesMenu() {
        //Generate 4D meshes
        GenerateFlatCube().Build("flat_cube");
        GenerateFlatTetrahedron().Build("flat_tetrahedron");
        GenerateHyperCube().Build("hypercube").FlipNormals().Build("hypercube_inside");
        GenerateRampPrism().Build("ramp_prism");
        GenerateFlatHalfCube().Build("flat_halfcube");
        GenerateDuoPrism("triangle", "triangle").Build("duoprism");
        GenerateDuoPrism("disk24", "disk24", true, true).Build("duocylinder");
        GenerateDuoPrism("disk24", "disk24", true, true, true).Build("duocylinder_2mat");
        GenerateDuoPrism("disk24", "disk24_half", true, true).Build("duocylinder_half");
        GeneratePathExtrude("disk24_offset", "icosphere.fbx").MergeVerts(0.001f).Smoothen().Build("torisphere");
        GenerateRevolve("icosphere.fbx", 24, new Vector3(0, 0, 2.5f)).MergeVerts(0.001f).Smoothen().Build("spheritorus");
        GenerateRevolve("icosphere.fbx", 12, new Vector3(0, 0, 4), 180.0f).Smoothen().Build("spheritorus_half");
        GenerateRevolve("torus_lowpoly.fbx", 16, new Vector3(0, 0, 1), 360.0f).MergeVerts(0.001f).Smoothen().Build("tiger");
        GenerateRevolve("torus_lowpoly.fbx", 8, new Vector3(0, 0, 1), 180.0f).MergeVerts(0.001f).Smoothen().Build("tiger_half");
        GenerateRevolve("torus2_lowpoly.fbx", 16, new Vector3(0, 0, 2), 360.0f).MergeVerts(0.001f).Smoothen().Build("ditorus");
        GenerateRevolve("torus2_lowpoly.fbx", 8, new Vector3(0, 0, 2), 180.0f).MergeVerts(0.001f).Smoothen().Build("ditorus_half");
        Generate4DFlat("icosphere.fbx").Build("flat_sphere");
        Generate4DHoleFlat("icosphere_hipoly.fbx", 0.5f).Build("flat_sphere_hole");
        Generate4DHoleFlat("icosphere.fbx", 0.5f).Build("flat_sphere_hole_low");
        Generate4DExtrude("icosphere.fbx", 1.0f).Build("spherinder");
        Generate4DExtrude("icosphere.fbx", 1.0f, null, false, false).Smoothen().Build("spherinder_nocaps");
        Generate4DExtrude("icosphere_hipoly.fbx", 1.0f, null, false, false).Smoothen().Build("spherinder_hipoly_nocaps").FlipNormals().Build("spherinder_hipoly_inv");
        GenerateDuoPrism("disk24", "square", true).Build("cubinder");
        GenerateDuoPrism("disk24", "square_open", true).Build("cubinder_halfcap");
        Generate4DExtrude("trirect.fbx", 1.0f, null).Build("trirect_prism_allcaps");
        Generate4DExtrude("wedge.fbx", 1.0f).Scale(1.0f, 0.4f, 1.0f, 1.0f).Build("wedge_prism");
        Generate4DExtrude("tetrahedron.fbx", 0.2f, null, false).Build("tetra_prism");
        Generate4DPyramid("icosphere.fbx", 1.0f, null, false).Smoothen().Build("hyper_cone");
        Generate4DPyramid("icosphere.fbx", 0.6f, null, false).Smoothen().Build("spike");
        Generate4DTruncatedPyramid("cube_quads.fbx", 0.2f, 0.5f, null, false).Build("truncated_pyramid");
        Generate4DExtrude("icosahedron.fbx", 1.0f).Build("icosa_cylinder");
        Generate4DExtrude("icosahedron.fbx", 1.0f, null, false, false).Build("icosa_cylinder_uncapped");
        Generate4DPyramid("icosahedron.fbx", 1.0f, null, false).Build("icosa_pyramid_uncapped");
        OFFParser.LoadOFF4D("Pentachoron").Build("5cell");
        OFFParser.LoadOFF4D("Hexadecachoron").Build("16cell").GeoPoke().Build("128cell");
        OFFParser.LoadOFF4D("Icositetrachoron").Build("24cell");
        OFFParser.LoadOFF4D("TetrakisHexadecachoron", false).Build("64cell");
        OFFParser.LoadOFF4D("NotchedEnneacontahexachoron", false).Build("N96cell");
        OFFParser.LoadOFF4D("DisphenoidalEnneacontahexachoron", false).Build("D96cell");
        OFFParser.LoadOFF4D("Hexacosichoron").Build("600cell").GeoPoke().Build("glome");
        OFFParser.LoadOFF4D("Hexacosichoron").GeoPoke(true, true).Smoothen().Build("glome_hires");
        OFFParser.LoadOFF4D("Hexacosichoron").Smoothen().Build("600cell_smooth");
        OFFParser.LoadOFF4D("Hexacosichoron").Perturb(0.2f).Smoothen().Build("asteroid");
        Color600Cell(OFFParser.LoadOFF4D("Hexacosichoron")).Build("600cell_colored");
        MergeMeshes4D("Table").Build("table");
        Debug.Log("Done!");
    }

    //#############################################################################
    //# Utility
    //#############################################################################
    private static Vector3 CenterOfMass(Mesh mesh3D, int submesh) {
        MeshTopology topology = mesh3D.GetTopology(submesh);
        Vector3[] verticies = mesh3D.vertices;
        int[] indices = mesh3D.GetIndices(submesh);
        Vector3 sum = Vector3.zero;
        float area = 0.0f;
        if (topology == MeshTopology.Quads) {
            for (int i = 0; i < indices.Length; i += 4) {
                Vector3 a = verticies[indices[i]];
                Vector3 b = verticies[indices[i + 1]];
                Vector3 c = verticies[indices[i + 2]];
                Vector3 d = verticies[indices[i + 3]];
                Vector3 triangleCenter = (a + b + c + d) / 4.0f;
                float triangleArea = Vector3.Cross(a - c, b - c).magnitude;
                sum += triangleCenter * triangleArea;
                area += triangleArea;
            }
        } else {
            for (int i = 0; i < indices.Length; i += 3) {
                Vector3 a = verticies[indices[i]];
                Vector3 b = verticies[indices[i + 1]];
                Vector3 c = verticies[indices[i + 2]];
                Vector3 triangleCenter = (a + b + c) / 3.0f;
                float triangleArea = Vector3.Cross(a - c, b - c).magnitude;
                sum += triangleCenter * triangleArea;
                area += triangleArea;
            }
        }
        return sum / area;
    }

    private static void ConvexCheck(Vector3 a, Vector3 b, Vector3 c, Vector3 center) {
        float tsp = Vector3.Dot(Vector3.Cross(a - center, b - center), c - center);
        Debug.Assert(tsp > 0.0f, "Mesh is not convex enough! " + tsp);
    }

    private static void AddFlatCube(Mesh4D mesh4D, Matrix4x4 rotate, Vector4 offset, bool parity = false) {
        //Add a unit cube with rotation and translation
        Vector4 v1 = offset + rotate * new Vector4(-1, -1, -1, 0);
        Vector4 v2 = offset + rotate * new Vector4(1, -1, -1, 0);
        Vector4 v3 = offset + rotate * new Vector4(-1, 1, -1, 0);
        Vector4 v4 = offset + rotate * new Vector4(1, 1, -1, 0);
        Vector4 v5 = offset + rotate * new Vector4(-1, -1, 1, 0);
        Vector4 v6 = offset + rotate * new Vector4(1, -1, 1, 0);
        Vector4 v7 = offset + rotate * new Vector4(-1, 1, 1, 0);
        Vector4 v8 = offset + rotate * new Vector4(1, 1, 1, 0);
        mesh4D.AddCell(v1, v2, v3, v4, v5, v6, v7, v8, parity);
        mesh4D.AddCellShadow(v1, v2, v3, v4, v5, v6, v7, v8);
    }

    //#############################################################################
    //# 4D Generation
    //#############################################################################
    private static Mesh4DBuilder GenerateFlatCube() {
        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D();
        AddFlatCube(mesh4D, Matrix4x4.identity, Vector4.zero);
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder GenerateFlatTetrahedron() {
        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D();

        //Add just a default tetrahedron
        Vector4 a = new Vector4(-1, -1, -1, 0);
        Vector4 b = new Vector4(-1, 1, 1, 0);
        Vector4 c = new Vector4(1, -1, 1, 0);
        Vector4 d = new Vector4(1, 1, -1, 0);
        mesh4D.AddTetrahedron(a, b, c, d, Mesh4D.Twiddle(0x3065));
        mesh4D.AddTriangleShadow(a, b, c);
        mesh4D.AddTriangleShadow(a, b, d);
        mesh4D.AddTriangleShadow(a, c, d);
        mesh4D.AddTriangleShadow(b, c, d);
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder GenerateHyperCube(bool top = true, bool bottom = true) {
        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D();

        //Assemble the 8 cubic 'faces'
        AddFlatCube(mesh4D, Transform4D.PlaneRotation(90.0f, 0, 3), new Vector4(1, 0, 0, 0));
        AddFlatCube(mesh4D, Transform4D.PlaneRotation(-90.0f, 0, 3), new Vector4(-1, 0, 0, 0));
        AddFlatCube(mesh4D, Transform4D.PlaneRotation(90.0f, 1, 3), new Vector4(0, 1, 0, 0));
        AddFlatCube(mesh4D, Transform4D.PlaneRotation(-90.0f, 1, 3), new Vector4(0, -1, 0, 0));
        AddFlatCube(mesh4D, Transform4D.PlaneRotation(90.0f, 2, 3), new Vector4(0, 0, 1, 0));
        AddFlatCube(mesh4D, Transform4D.PlaneRotation(-90.0f, 2, 3), new Vector4(0, 0, -1, 0));
        if (top) { AddFlatCube(mesh4D, Transform4D.PlaneRotation(0.0f, 0, 3), new Vector4(0, 0, 0, 1), true); }
        if (bottom) { AddFlatCube(mesh4D, Transform4D.PlaneRotation(180.0f, 0, 3), new Vector4(0, 0, 0, -1), true); }
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder GenerateHyperCubeSD(int n, bool bottomFace = true) {
        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D();
        float offset = (n - 1.0f) * 0.5f;

        //Assemble the 8 cubic 'faces'
        Matrix4x4 scale = Transform4D.ScaleMatrix(Vector4.one / n);
        for (int x = 0; x < n; ++x) {
            float xx = (x - offset) * 2.0f / n;
            for (int y = 0; y < n; ++y) {
                float yy = (y - offset) * 2.0f / n;
                for (int z = 0; z < n; ++z) {
                    float zz = (z - offset) * 2.0f / n;
                    bool parity = (x + y + z) % 2 == 0;
                    bool parity2 = (n % 2 == 0 ? !parity : parity);
                    AddFlatCube(mesh4D, scale * Transform4D.PlaneRotation(90.0f, 0, 3), new Vector4(1, xx, yy, zz), parity);
                    AddFlatCube(mesh4D, scale * Transform4D.PlaneRotation(-90.0f, 0, 3), new Vector4(-1, xx, yy, zz), parity2);
                    AddFlatCube(mesh4D, scale * Transform4D.PlaneRotation(90.0f, 1, 3), new Vector4(xx, 1, yy, zz), parity);
                    AddFlatCube(mesh4D, scale * Transform4D.PlaneRotation(-90.0f, 1, 3), new Vector4(xx, -1, yy, zz), parity2);
                    AddFlatCube(mesh4D, scale * Transform4D.PlaneRotation(90.0f, 2, 3), new Vector4(xx, yy, 1, zz), parity);
                    AddFlatCube(mesh4D, scale * Transform4D.PlaneRotation(-90.0f, 2, 3), new Vector4(xx, yy, -1, zz), parity2);
                    AddFlatCube(mesh4D, scale * Transform4D.PlaneRotation(0.0f, 0, 3), new Vector4(xx, yy, zz, 1), !parity);
                    if (bottomFace) {
                        AddFlatCube(mesh4D, scale * Transform4D.PlaneRotation(180.0f, 0, 3), new Vector4(xx, yy, zz, -1), !parity2);
                    }
                }
            }
        }

        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder GenerateRampPrism() {
        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D();

        Vector4 a1 = new Vector4(-1, 1, 1, -1);
        Vector4 a2 = new Vector4(1, 1, 1, -1);
        Vector4 b1 = new Vector4(-1, 0, 1, -1);
        Vector4 b2 = new Vector4(1, 0, 1, -1);
        Vector4 c1 = new Vector4(-1, 0, -1, -1);
        Vector4 c2 = new Vector4(1, 0, -1, -1);
        Vector4 B1 = new Vector4(-1, 0, 1, 1);
        Vector4 B2 = new Vector4(1, 0, 1, 1);
        Vector4 C1 = new Vector4(-1, 0, -1, 1);
        Vector4 C2 = new Vector4(1, 0, -1, 1);

        //mesh4D.AddCell(b1, b2, B1, B2, c1, c2, C1, C2);
        mesh4D.AddHalfCell(a1, a2, b1, b2, B1, B2);
        mesh4D.AddHalfCell(a2, a1, c2, c1, C2, C1);
        mesh4D.AddHalfCell(b1, b2, a1, a2, c1, c2);
        mesh4D.AddHalfCell(B2, B1, a2, a1, C2, C1);
        mesh4D.AddPyramid(a1, b1, c1, B1, C1);
        mesh4D.AddPyramid(a2, b2, B2, c2, C2);

        mesh4D.AddCellShadow(b1, b2, B1, B2, c1, c2, C1, C2);
        mesh4D.AddQuadShadow(a1, a2, b1, b2);
        mesh4D.AddQuadShadow(a1, a2, B1, B2);
        mesh4D.AddQuadShadow(a1, a2, c1, c2);
        mesh4D.AddQuadShadow(a1, a2, C1, C2);
        mesh4D.AddTriangleShadow(a1, c1, C1);
        mesh4D.AddTriangleShadow(a1, C1, B1);
        mesh4D.AddTriangleShadow(a1, B1, b1);
        mesh4D.AddTriangleShadow(a1, b1, c1);
        mesh4D.AddTriangleShadow(a2, c2, C2);
        mesh4D.AddTriangleShadow(a2, C2, B2);
        mesh4D.AddTriangleShadow(a2, B2, b2);
        mesh4D.AddTriangleShadow(a2, b2, c2);

        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder GenerateFlatHalfCube() {
        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D();

        //Add a unit cube with rotation and translation
        Vector4 v0 = new Vector3(-1, -1, -1);
        Vector4 v1 = new Vector3(1, -1, -1);
        Vector4 v2 = new Vector3(-1, 1, -1);
        Vector4 v3 = new Vector3(1, 1, -1);
        Vector4 v4 = new Vector3(-1, -1, 1);
        Vector4 v5 = new Vector3(1, -1, 1);
        mesh4D.AddHalfCell(v0, v1, v2, v3, v4, v5);
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder GenerateDuoPrism(string surface1, string surface2, bool biSmooth1 = false, bool biSmooth2 = false, bool twoMaterials = false, bool halfOnly = false) {
        Mesh4D mesh4D = new Mesh4D(twoMaterials ? 2 : 1);

        GenerateDuoPrismHalf(mesh4D, surface1, surface2, false, biSmooth1);
        GenerateDuoPrismShadow(mesh4D, surface1, surface2);
        if (!halfOnly) {
            if (twoMaterials) { mesh4D.NextSubmesh(); }
            GenerateDuoPrismHalf(mesh4D, surface2, surface1, true, biSmooth2);
        }
        return new Mesh4DBuilder(mesh4D);
    }
    private static void GenerateDuoPrismHalf(Mesh4D mesh4D, string chainName, string surfaceName, bool reverse, bool biSmooth) {
        //Load the line chain and surface for duo prism
        List<OFFParser.Line2D> lines = OFFParser.LoadOBJ2D(chainName);
        Mesh surface = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Editor/Surface/" + surfaceName + ".fbx");
        Vector3[] surfaceVerts = surface.vertices;
        int[] surfaceIndices = surface.GetIndices(0);
        Debug.Assert(surface.GetTopology(0) == MeshTopology.Triangles && surfaceIndices.Length % 3 == 0);

        for (int ix1 = 0; ix1 < lines.Count; ++ix1) {
            Vector2 pxy = lines[ix1].a;
            Vector2 nxy = lines[ix1].b;
            int prevIx1 = (ix1 + lines.Count - 1) % lines.Count;
            int nextIx1 = (ix1 + 1) % lines.Count;
            for (int ix2 = 0; ix2 < surfaceIndices.Length; ix2 += 3) {
                Vector3 a = surfaceVerts[surfaceIndices[ix2]];
                Vector3 b = surfaceVerts[surfaceIndices[ix2 + 1]];
                Vector3 c = surfaceVerts[surfaceIndices[ix2 + 2]];
                Debug.Assert(Mathf.Abs(a.z) < 1e-4f, "Non-planar vertex: " + a.z);
                Debug.Assert(Mathf.Abs(b.z) < 1e-4f, "Non-planar vertex: " + b.z);
                Debug.Assert(Mathf.Abs(c.z) < 1e-4f, "Non-planar vertex: " + c.z);

                //Define vertices for first prism cell
                Vector4 a1, a2, b1, b2, c1, c2;
                if (reverse) {
                    a1 = new Vector4(a.x, a.y, nxy.x, nxy.y);
                    a2 = new Vector4(a.x, a.y, pxy.x, pxy.y);
                    b1 = new Vector4(b.x, b.y, nxy.x, nxy.y);
                    b2 = new Vector4(b.x, b.y, pxy.x, pxy.y);
                    c1 = new Vector4(c.x, c.y, nxy.x, nxy.y);
                    c2 = new Vector4(c.x, c.y, pxy.x, pxy.y);
                } else {
                    a1 = new Vector4(nxy.x, nxy.y, a.x, a.y);
                    a2 = new Vector4(pxy.x, pxy.y, a.x, a.y);
                    b1 = new Vector4(nxy.x, nxy.y, b.x, b.y);
                    b2 = new Vector4(pxy.x, pxy.y, b.x, b.y);
                    c1 = new Vector4(nxy.x, nxy.y, c.x, c.y);
                    c2 = new Vector4(pxy.x, pxy.y, c.x, c.y);
                }

                if (biSmooth) {
                    Vector4 n1, n2, nmid;
                    Vector2 dprev = lines[prevIx1].b - lines[prevIx1].a;
                    Vector2 dnext = lines[nextIx1].b - lines[nextIx1].a;
                    Vector2 dcur = nxy - pxy;
                    if (reverse) {
                        nmid = new Vector4(0.0f, 0.0f, dcur.y, -dcur.x).normalized;
                        n1 = (new Vector4(0.0f, 0.0f, dprev.y, -dprev.x).normalized + nmid).normalized;
                        n2 = (new Vector4(0.0f, 0.0f, dnext.y, -dnext.x).normalized + nmid).normalized;
                    } else {
                        nmid = new Vector4(dcur.y, -dcur.x, 0.0f, 0.0f).normalized;
                        n1 = (new Vector4(dprev.y, -dprev.x, 0.0f, 0.0f).normalized + nmid).normalized;
                        n2 = (new Vector4(dnext.y, -dnext.x, 0.0f, 0.0f).normalized + nmid).normalized;
                    }
                    mesh4D.AddHalfCellBiSmooth(a1, a2, b1, b2, c1, c2, n1, n2);
                } else {
                    mesh4D.AddHalfCell(a1, a2, b1, b2, c1, c2);
                }
                mesh4D.AddTriangleShadow(a1, b1, c1);
            }
        }
    }
    private static void GenerateDuoPrismShadow(Mesh4D mesh4D, string chain1Name, string chain2Name) {
        //Load the line chain and surface for duo prism
        List<OFFParser.Line2D> lines1 = OFFParser.LoadOBJ2D(chain1Name);
        List<OFFParser.Line2D> lines2 = OFFParser.LoadOBJ2D(chain2Name);

        for (int ix1 = 0; ix1 < lines1.Count; ++ix1) {
            Vector2 pxy = lines1[ix1].a;
            Vector2 nxy = lines1[ix1].b;
            for (int ix2 = 0; ix2 < lines2.Count; ++ix2) {
                Vector2 pwz = lines2[ix2].a;
                Vector2 nwz = lines2[ix2].b;

                Vector4 a1 = new Vector4(pxy.x, pxy.y, pwz.x, pwz.y);
                Vector4 a2 = new Vector4(nxy.x, nxy.y, pwz.x, pwz.y);
                Vector4 b1 = new Vector4(pxy.x, pxy.y, nwz.x, nwz.y);
                Vector4 b2 = new Vector4(nxy.x, nxy.y, nwz.x, nwz.y);
                mesh4D.AddQuadShadow(a1, a2, b1, b2);
            }
        }
    }

    public static Mesh4DBuilder GenerateRevolve(string filepath, int segments, float revAngle = 360.0f) {
        return GenerateRevolve(filepath, segments, Vector3.zero, revAngle);
    }
    public static Mesh4DBuilder GenerateRevolve(string filepath, int segments, Vector3 offset, float revAngle = 360.0f) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + filepath);

        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D(mesh3D.subMeshCount);

        //Scan through the 3D mesh
        Vector3[] verticies = mesh3D.vertices;
        Vector2[] uvs = mesh3D.uv;
        bool useUVs = (uvs != null && uvs.Length > 0);
        for (int s = 0; s < mesh3D.subMeshCount; ++s) {
            int[] indices = mesh3D.GetIndices(s);
            Debug.Assert(indices.Length % 3 == 0);
            for (int r = 0; r < segments; ++r) {
                //Determine the angle for this segment
                float angle1 = r * revAngle / segments;
                float angle2 = (r + 1) * revAngle / segments;
                if (revAngle == 360.0f) {
                    angle2 = ((r + 1) % segments) * 360.0f / segments;
                }

                //Convert angle to matrix
                Matrix4x4 m1 = Transform4D.PlaneRotation(angle1, 2, 3);
                Matrix4x4 m2 = Transform4D.PlaneRotation(angle2, 2, 3);

                for (int i = 0; i < indices.Length; i += 3) {
                    Vector4 a = verticies[indices[i]] + offset;
                    Vector4 b = verticies[indices[i + 1]] + offset;
                    Vector4 c = verticies[indices[i + 2]] + offset;
                    float auv = (useUVs ? uvs[indices[i]].y : 0.0f);
                    float buv = (useUVs ? uvs[indices[i + 1]].y : 0.0f);
                    float cuv = (useUVs ? uvs[indices[i + 2]].y : 0.0f);
                    Debug.Assert(a.z > -1e-6f && b.z > -1e-6f && c.z > -1e-6f, "Negative z coordinates in revolution mesh: " + a.z + " " + b.z + " " + c.z);
                    a.z = Mathf.Max(0.0f, a.z);
                    b.z = Mathf.Max(0.0f, b.z);
                    c.z = Mathf.Max(0.0f, c.z);
                    Vector4 a1 = m1 * a;
                    Vector4 b1 = m1 * b;
                    Vector4 c1 = m1 * c;
                    Vector4 a2 = m2 * a;
                    Vector4 b2 = m2 * b;
                    Vector4 c2 = m2 * c;

                    //Walls of the prism
                    mesh4D.AddHalfCell(a1, a2, b1, b2, c1, c2, auv, auv, buv, buv, cuv, cuv);
                    mesh4D.AddTriangleShadow(a1, b1, c1);
                    mesh4D.AddQuadShadow(a1, a2, b1, b2);
                    mesh4D.AddQuadShadow(a1, a2, c1, c2);
                    mesh4D.AddQuadShadow(b1, b2, c1, c2);
                }
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder Generate4DHoleExtrude(string filepath, float thickness, float length, bool capTop = true, bool capBottom = true, bool innerWalls = true) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + filepath);

        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D(mesh3D.subMeshCount);
        Vector4 extrude = new Vector4(0.0f, 0.0f, 0.0f, length);

        //Scan through each 3D mesh
        Vector3[] verticies = mesh3D.vertices;
        for (int s = 0; s < mesh3D.subMeshCount; ++s) {
            int[] indices = mesh3D.GetIndices(s);
            Debug.Assert(indices.Length % 3 == 0);
            for (int i = 0; i < indices.Length; i += 3) {
                Vector4 a1 = verticies[indices[i]];
                Vector4 b1 = verticies[indices[i + 1]];
                Vector4 c1 = verticies[indices[i + 2]];
                Vector4 A1 = a1 * (thickness + 1.0f);
                Vector4 B1 = b1 * (thickness + 1.0f);
                Vector4 C1 = c1 * (thickness + 1.0f);
                Vector4 a2 = a1 + extrude;
                Vector4 b2 = b1 + extrude;
                Vector4 c2 = c1 + extrude;
                Vector4 A2 = A1 + extrude;
                Vector4 B2 = B1 + extrude;
                Vector4 C2 = C1 + extrude;
                a1 -= extrude;
                b1 -= extrude;
                c1 -= extrude;
                A1 -= extrude;
                B1 -= extrude;
                C1 -= extrude;

                //Caps for the prism
                if (capBottom) {
                    mesh4D.AddHalfCell(a1, A1, c1, C1, b1, B1);
                    mesh4D.AddTriangleShadow(a1, b1, c1);
                    mesh4D.AddTriangleShadow(A1, B1, C1);
                }
                if (capTop) {
                    mesh4D.AddHalfCell(a2, A2, b2, B2, c2, C2);
                    mesh4D.AddTriangleShadow(a2, b2, c2);
                    mesh4D.AddTriangleShadow(A2, B2, C2);
                }

                //Inside walls of the prism
                if (innerWalls) {
                    mesh4D.AddHalfCell(c1, c2, a1, a2, b1, b2);
                    mesh4D.AddQuadShadow(a1, a2, b1, b2);
                    mesh4D.AddQuadShadow(a1, a2, c1, c2);
                    mesh4D.AddQuadShadow(b1, b2, c1, c2);
                }

                //Outside walls of the prism
                mesh4D.AddHalfCell(C1, C2, B1, B2, A1, A2);
                mesh4D.AddQuadShadow(A1, A2, B1, B2);
                mesh4D.AddQuadShadow(A1, A2, C1, C2);
                mesh4D.AddQuadShadow(B1, B2, C1, C2);
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder Generate4DFlat(string filepath, Vector3[] centerBias = null, bool centerAbsolute = false) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + filepath);

        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D(mesh3D.subMeshCount);

        //Scan through each 3D mesh
        Vector3[] verticies = mesh3D.vertices;
        for (int s = 0; s < mesh3D.subMeshCount; ++s) {
            int[] indices = mesh3D.GetIndices(s);
            Vector3 center = CenterOfMass(mesh3D, s);
            if (centerBias != null) {
                center = (centerAbsolute ? centerBias[s] : center + centerBias[s]);
            }
            Debug.Assert(indices.Length % 3 == 0);
            for (int i = 0; i < indices.Length; i += 3) {
                Vector4 a = verticies[indices[i]];
                Vector4 b = verticies[indices[i + 1]];
                Vector4 c = verticies[indices[i + 2]];
                ConvexCheck(a, b, c, center);
                mesh4D.AddTetrahedron(a, b, c, center);
                mesh4D.AddTriangleShadow(a, b, c);
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    public static Mesh4DBuilder Generate4DHoleFlat(string filepath, float thickness, float height=0.0f) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + filepath);

        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D(mesh3D.subMeshCount);

        //Scan through each 3D mesh
        Vector3[] verticies = mesh3D.vertices;
        Vector4 bump = new Vector4(0.0f, 0.0f, 0.0f, height);
        for (int s = 0; s < mesh3D.subMeshCount; ++s) {
            int[] indices = mesh3D.GetIndices(s);
            Debug.Assert(indices.Length % 3 == 0);
            for (int i = 0; i < indices.Length; i += 3) {
                Vector4 a1 = verticies[indices[i]];
                Vector4 b1 = verticies[indices[i + 1]];
                Vector4 c1 = verticies[indices[i + 2]];
                Vector4 a2 = a1 * (thickness + 1.0f) + bump;
                Vector4 b2 = b1 * (thickness + 1.0f) + bump;
                Vector4 c2 = c1 * (thickness + 1.0f) + bump;

                if (thickness > 0.0f) {
                    mesh4D.AddHalfCell(a1, a2, b1, b2, c1, c2);
                } else {
                    mesh4D.AddHalfCell(a1, a2, c1, c2, b1, b2);
                }
                mesh4D.AddQuadShadow(a1, a2, b1, b2);
                mesh4D.AddQuadShadow(a1, a2, c1, c2);
                mesh4D.AddQuadShadow(b1, b2, c1, c2);
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    //Extrudes *FLAT* triangles (No z-depth) to *FLAT* tetras (No w-depth)
    public static Mesh4DBuilder Generate4DExtrudeFlat(string flat3D, float length) {
        //Create a new mesh4D
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + flat3D);

        //Create a new mesh5D
        Mesh4D mesh4D = new Mesh4D(mesh3D.subMeshCount);
        Vector4 extrude = new Vector4(0.0f, 0.0f, length, 0.0f);

        //Acquire mesh data
        Vector3[] verts = mesh3D.vertices;

        //Scan through the 3D mesh
        for (int s = 0; s < mesh3D.subMeshCount; ++s) {
            //Add the simplices
            int[] indices = mesh3D.GetIndices(s);
            Debug.Assert(indices.Length % 3 == 0);
            for (int i = 0; i < indices.Length; i += 3) {
                Vector4 a1 = (Vector4)verts[indices[i]];
                Vector4 b1 = (Vector4)verts[indices[i + 1]];
                Vector4 c1 = (Vector4)verts[indices[i + 2]];
                Vector4 a2 = a1 + extrude;
                Vector4 b2 = b1 + extrude;
                Vector4 c2 = c1 + extrude;
                a1 -= extrude;
                b1 -= extrude;
                c1 -= extrude;

                //Walls of the prism
                mesh4D.AddHalfCell(a1, a2, b1, b2, c1, c2);
                mesh4D.AddTriangleShadow(a1, b1, c1);
                mesh4D.AddTriangleShadow(a2, b2, c2);
                mesh4D.AddQuadShadow(a1, a2, b1, b2);
                mesh4D.AddQuadShadow(a1, a2, c1, c2);
                mesh4D.AddQuadShadow(b1, b2, c1, c2);
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    public static Mesh4DBuilder Generate4DExtrude(string filepath, float length, Vector3[] centerBias = null, bool capTop = true, bool capBottom = true, bool vertAO = true, float pushUV = 0.0f, bool shadowTop = false, bool shadowBottom = false) {
        return Generate4DTruncatedPyramid(filepath, length, 1.0f, centerBias, capBottom, capTop, vertAO, pushUV, true, shadowBottom, shadowTop);
    }

    private static Mesh4DBuilder GeneratePathExtrude(string chainName, string volume3DName, bool pathSmooth = false, int numSubMeshes = 1) {
        //Load the line chain and surface for duo prism
        Mesh4D mesh4D = new Mesh4D(numSubMeshes);
        List<OFFParser.Line2D> lines = OFFParser.LoadOBJ2D(chainName);
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + volume3DName);

        Vector4 extrudeDir = new Vector4(0, 0, 0, 1);
        Vector3[] verts = mesh3D.vertices;
        Debug.Assert(mesh3D.subMeshCount == 1);
        int[] surfaceIndices = mesh3D.GetIndices(0);
        MeshTopology topology = mesh3D.GetTopology(0);
        if (topology == MeshTopology.Triangles) {
            Debug.Assert(surfaceIndices.Length % 3 == 0);
            //Needed to correctly match parity in tetrahedron boundaries
            Dictionary<Tuple<Vector3, Vector3>, int> parityCheck = new();
            for (int ix1 = 0; ix1 < lines.Count; ++ix1) {
                Vector2 pxy = lines[ix1].a;
                Vector2 nxy = lines[ix1].b;
                float pAO = lines[ix1].aoA;
                float nAO = lines[ix1].aoB;
                int vStartIndex = mesh4D.vArray.Count;
                parityCheck.Clear();
                for (int ix2 = 0; ix2 < surfaceIndices.Length; ix2 += 3) {
                    Vector4 a = verts[surfaceIndices[ix2]];
                    Vector4 b = verts[surfaceIndices[ix2 + 1]];
                    Vector4 c = verts[surfaceIndices[ix2 + 2]];
                    int parity = RunParityCheck(parityCheck, a, b, c);

                    //Define vertices for first prism cell
                    Vector4 a1 = a * pxy.x + extrudeDir * pxy.y;
                    Vector4 a2 = a * nxy.x + extrudeDir * nxy.y;
                    Vector4 b1 = b * pxy.x + extrudeDir * pxy.y;
                    Vector4 b2 = b * nxy.x + extrudeDir * nxy.y;
                    Vector4 c1 = c * pxy.x + extrudeDir * pxy.y;
                    Vector4 c2 = c * nxy.x + extrudeDir * nxy.y;

                    if (Mathf.Abs(pxy.x) < 1e-6f) {
                        mesh4D.AddTetrahedron(b2, a2, c2, a1,
                                              nAO, nAO, nAO, pAO);
                        mesh4D.AddTriangleShadow(a2, b2, a1);
                        mesh4D.AddTriangleShadow(a2, c2, a1);
                        mesh4D.AddTriangleShadow(b2, c2, a1);
                    } else if (Mathf.Abs(nxy.x) < 1e-6f) {
                        mesh4D.AddTetrahedron(a1, b1, c1, a2,
                                              pAO, pAO, pAO, nAO);
                        mesh4D.AddTriangleShadow(a1, b1, a2);
                        mesh4D.AddTriangleShadow(a1, c1, a2);
                        mesh4D.AddTriangleShadow(b1, c1, a2);
                    } else {
                        AddHalfCellParity(mesh4D, a1, a2, b1, b2, c1, c2,
                                          pAO, nAO, pAO, nAO, pAO, nAO, parity);
                        mesh4D.AddQuadShadow(a1, b1, a2, b2);
                        mesh4D.AddQuadShadow(a1, c1, a2, c2);
                        mesh4D.AddQuadShadow(b1, c1, b2, c2);
                    }
                }
                //Smooth only this layer if path smoothing enabled
                if (pathSmooth) {
                    Mesh4DBuilder.Smoothen(mesh4D, vStartIndex, mesh4D.vArray.Count);
                }
            }
        } else if (topology == MeshTopology.Quads) {
            Debug.Assert(surfaceIndices.Length % 4 == 0);
            for (int ix1 = 0; ix1 < lines.Count; ++ix1) {
                Vector2 pxy = lines[ix1].a;
                Vector2 nxy = lines[ix1].b;
                float pAO = lines[ix1].aoA;
                float nAO = lines[ix1].aoB;
                int vStartIndex = mesh4D.vArray.Count;
                for (int ix2 = 0; ix2 < surfaceIndices.Length; ix2 += 4) {
                    Vector4 a = verts[surfaceIndices[ix2]];
                    Vector4 b = verts[surfaceIndices[ix2 + 3]];
                    Vector4 c = verts[surfaceIndices[ix2 + 1]];
                    Vector4 d = verts[surfaceIndices[ix2 + 2]];

                    //Define vertices for first prism cell
                    Vector4 a1 = a * pxy.x + extrudeDir * pxy.y;
                    Vector4 a2 = a * nxy.x + extrudeDir * nxy.y;
                    Vector4 b1 = b * pxy.x + extrudeDir * pxy.y;
                    Vector4 b2 = b * nxy.x + extrudeDir * nxy.y;
                    Vector4 c1 = c * pxy.x + extrudeDir * pxy.y;
                    Vector4 c2 = c * nxy.x + extrudeDir * nxy.y;
                    Vector4 d1 = d * pxy.x + extrudeDir * pxy.y;
                    Vector4 d2 = d * nxy.x + extrudeDir * nxy.y;

                    if (Mathf.Abs(pxy.x) < 1e-6f) {
                        mesh4D.AddPyramid(a1, a2, b2, c2, d2, pAO, nAO, nAO, nAO, nAO);
                        mesh4D.AddTriangleShadow(a2, b2, a1);
                        mesh4D.AddTriangleShadow(b2, d2, a1);
                        mesh4D.AddTriangleShadow(d2, c2, a1);
                        mesh4D.AddTriangleShadow(c2, a2, a1);
                    } else if (Mathf.Abs(nxy.x) < 1e-6f) {
                        mesh4D.AddPyramid(a2, a1, c1, b1, d1, nAO, pAO, pAO, pAO, pAO);
                        mesh4D.AddTriangleShadow(a1, b1, a2);
                        mesh4D.AddTriangleShadow(b1, d1, a2);
                        mesh4D.AddTriangleShadow(d1, c1, a2);
                        mesh4D.AddTriangleShadow(c1, a1, a2);
                    } else {
                        mesh4D.AddCell(a1, a2, b1, b2, c1, c2, d1, d2,
                                       pAO, nAO, pAO, nAO, pAO, nAO, pAO, nAO);
                        mesh4D.AddQuadShadow(a1, a2, b1, b2);
                        mesh4D.AddQuadShadow(b1, b2, d1, d2);
                        mesh4D.AddQuadShadow(d1, d2, c1, c2);
                        mesh4D.AddQuadShadow(c1, c2, a1, a2);
                    }
                }
                //Smooth only this layer if path smoothing enabled
                if (pathSmooth) {
                    Mesh4DBuilder.Smoothen(mesh4D, vStartIndex, mesh4D.vArray.Count);
                }
            }
        }
        mesh4D.NextSubmesh();
        return new Mesh4DBuilder(mesh4D);
    }

    public static Mesh4DBuilder Generate4DPyramid(string filepath, float length, Vector3[] centerBias = null, bool capped = true) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + filepath);

        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D(mesh3D.subMeshCount);
        Vector4 extrude = new Vector4(0.0f, 0.0f, 0.0f, length);

        //Scan through the 3D mesh
        Vector3[] verticies = mesh3D.vertices;
        for (int s = 0; s < mesh3D.subMeshCount; ++s) {
            int[] indices = mesh3D.GetIndices(s);
            Debug.Assert(indices.Length % 3 == 0);
            Vector3 center = CenterOfMass(mesh3D, s);
            if (centerBias != null) { center += centerBias[s]; }
            Vector4 centerExtrude = (Vector4)center + extrude;
            mesh4D.MarkConePoint(centerExtrude);
            for (int i = 0; i < indices.Length; i += 3) {
                Vector4 a1 = verticies[indices[i]];
                Vector4 b1 = verticies[indices[i + 1]];
                Vector4 c1 = verticies[indices[i + 2]];

                //Caps for the prism
                ConvexCheck(a1, b1, c1, center);
                if (capped) {
                    mesh4D.AddTetrahedron(a1, b1, c1, center);
                    mesh4D.AddTriangleShadow(a1, b1, c1);
                }
                mesh4D.AddTetrahedron(a1, b1, c1, centerExtrude, Mesh4D.Twiddle(0x3065));
                mesh4D.AddTriangleShadow(a1, b1, centerExtrude);
                mesh4D.AddTriangleShadow(a1, centerExtrude, c1);
                mesh4D.AddTriangleShadow(centerExtrude, b1, c1);
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder Generate4DTruncatedPyramid(string filepath, float length, float truncateRatio, Vector3[] centerBias = null, bool capBottom = true, bool capTop = true, bool vertAO = true, float pushUV = 0.0f, bool extrudeCentered = false, bool shadowBottom = false, bool shadowTop = false) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + filepath);
        Vector3[] verticies = mesh3D.vertices;
        Vector2[] uvs = mesh3D.uv;

        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D(mesh3D.subMeshCount);
        Vector4 extrude = new Vector4(0.0f, 0.0f, 0.0f, length);

        //Scan through the 3D mesh
        for (int s = 0; s < mesh3D.subMeshCount; ++s) {
            MeshTopology topology = mesh3D.GetTopology(s);
            int[] indices = mesh3D.GetIndices(s);
            Vector4 center = CenterOfMass(mesh3D, s);
            if (centerBias != null) { center += (Vector4)centerBias[s]; }
            if (topology == MeshTopology.Triangles) {
                Debug.Assert(indices.Length % 3 == 0);
                //Needed to correctly match parity in tetrahedron boundaries
                Dictionary<Tuple<Vector3, Vector3>, int> parityCheck = new();
                for (int i = 0; i < indices.Length; i += 3) {
                    //Run the parity check
                    Vector4 a1 = verticies[indices[i]];
                    Vector4 b1 = verticies[indices[i + 1]];
                    Vector4 c1 = verticies[indices[i + 2]];
                    int parity = RunParityCheck(parityCheck, a1, b1, c1);
                    if (pushUV > 0.0f && uvs.Length > 0) {
                        a1.w += uvs[indices[i]].y * pushUV;
                        b1.w += uvs[indices[i + 1]].y * pushUV;
                        c1.w += uvs[indices[i + 2]].y * pushUV;
                    }
                    Vector4 a2 = a1 * truncateRatio + extrude;
                    Vector4 b2 = b1 * truncateRatio + extrude;
                    Vector4 c2 = c1 * truncateRatio + extrude;
                    if (extrudeCentered) {
                        a1 -= extrude;
                        b1 -= extrude;
                        c1 -= extrude;
                    }

                    //Caps for the prism
                    if (capBottom) {
                        ConvexCheck(a1, b1, c1, center);
                        mesh4D.AddTetrahedron(a1, c1, b1, (extrudeCentered ? center - extrude : center));
                    }
                    if (capBottom || shadowBottom) {
                        mesh4D.AddTriangleShadow(a1, b1, c1);
                    }
                    if (capTop) {
                        ConvexCheck(a1, b1, c1, center);
                        mesh4D.AddTetrahedron(a2, b2, c2, center + extrude);
                    }
                    if (capTop || shadowTop) {
                        mesh4D.AddTriangleShadow(a2, b2, c2);
                    }

                    //Walls of the prism
                    if (vertAO) {
                        AddHalfCellParity(mesh4D, a1, a2, b1, b2, c1, c2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, parity);
                    } else {
                        AddHalfCellParity(mesh4D, a1, a2, b1, b2, c1, c2, parity);
                    }
                    mesh4D.AddQuadShadow(a1, a2, b1, b2);
                    mesh4D.AddQuadShadow(a1, a2, c1, c2);
                    mesh4D.AddQuadShadow(b1, b2, c1, c2);
                }
            } else if (topology == MeshTopology.Quads) {
                Debug.Assert(indices.Length % 4 == 0);
                for (int i = 0; i < indices.Length; i += 4) {
                    Vector4 a1 = verticies[indices[i]];
                    Vector4 b1 = verticies[indices[i + 3]];
                    Vector4 c1 = verticies[indices[i + 1]];
                    Vector4 d1 = verticies[indices[i + 2]];
                    if (pushUV > 0.0f && uvs.Length > 0) {
                        a1.w += uvs[indices[i]].y * pushUV;
                        b1.w += uvs[indices[i + 3]].y * pushUV;
                        c1.w += uvs[indices[i + 1]].y * pushUV;
                        d1.w += uvs[indices[i + 2]].y * pushUV;
                    }
                    Vector4 a2 = a1 * truncateRatio + extrude;
                    Vector4 b2 = b1 * truncateRatio + extrude;
                    Vector4 c2 = c1 * truncateRatio + extrude;
                    Vector4 d2 = d1 * truncateRatio + extrude;
                    if (extrudeCentered) {
                        a1 -= extrude;
                        b1 -= extrude;
                        c1 -= extrude;
                        d1 -= extrude;
                    }

                    //Caps for the prism
                    if (capBottom) {
                        ConvexCheck(a1, c1, b1, center);
                        mesh4D.AddPyramid((extrudeCentered ? center - extrude : center), a1, b1, c1, d1);
                    }
                    if (capBottom || shadowBottom) {
                        mesh4D.AddQuadShadow(a1, b1, c1, d1);
                    }
                    if (capTop) {
                        ConvexCheck(a1, c1, b1, center);
                        mesh4D.AddPyramid(center + extrude, a2, c2, b2, d2);
                    }
                    if (capTop || shadowTop) {
                        mesh4D.AddQuadShadow(a2, c2, b2, d2);
                    }

                    //Walls of the prism
                    if (vertAO) {
                        mesh4D.AddCell(a1, a2, b1, b2, c1, c2, d1, d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f);
                    } else {
                        mesh4D.AddCell(a1, a2, b1, b2, c1, c2, d1, d2);
                    }
                    mesh4D.AddQuadShadow(a1, a2, b1, b2);
                    mesh4D.AddQuadShadow(b1, b2, d1, d2);
                    mesh4D.AddQuadShadow(d1, d2, c1, c2);
                    mesh4D.AddQuadShadow(c1, c2, a1, a2);
                }
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder Generate4DBumperExtrude(string filepath, float length, float truncateRatio) {
        //Get the primitive mesh from a GameObject.
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + filepath);
        Vector3[] verticies = mesh3D.vertices;

        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D(mesh3D.subMeshCount);
        Vector4 extrude = new Vector4(0.0f, 0.0f, 0.0f, length);

        //Scan through the 3D mesh
        for (int s = 0; s < mesh3D.subMeshCount; ++s) {
            int[] indices = mesh3D.GetIndices(s);
            Debug.Assert(indices.Length % 3 == 0);
            for (int i = 0; i < indices.Length; i += 3) {
                //Run the parity check
                Vector4 a1 = verticies[indices[i]];
                Vector4 b1 = verticies[indices[i + 1]];
                Vector4 c1 = verticies[indices[i + 2]];
                Vector4 center1 = (a1 + b1 + c1) / 3.0f;
                Vector4 a2 = a1 * truncateRatio + extrude;
                Vector4 b2 = b1 * truncateRatio + extrude;
                Vector4 c2 = c1 * truncateRatio + extrude;

                //Walls of the prism
                mesh4D.AddTetrahedron(center1, a2, b2, c2, 1.0f, 1.0f, 1.0f, 1.0f);
                mesh4D.AddPyramid(center1, a1, a2, b1, b2, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                mesh4D.AddPyramid(center1, b1, b2, c1, c2, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                mesh4D.AddPyramid(center1, c1, c2, a1, a2, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);

                //Shadow
                mesh4D.AddQuadShadow(a1, a2, b1, b2);
                mesh4D.AddQuadShadow(a1, a2, c1, c2);
                mesh4D.AddQuadShadow(b1, b2, c1, c2);
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    public static Mesh4DBuilder MergeMeshes4D(string name) {
        //Get the primitive mesh from a GameObject.
        Debug.Log("Generating " + name + "...");
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modeling/" + name + ".prefab");
        Debug.Assert(model != null, "Could not find model '" + name + "' in Modeling folder.");
        MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
        Object4D[] objs4D = model.GetComponentsInChildren<Object4D>();

        //Awaken all Object4D components
        foreach (Object4D obj4D in objs4D) {
            obj4D.Awake();
            obj4D.transform.hasChanged = true;
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

        //Create a new Mesh4D
        Mesh4D mesh4D = new Mesh4D(matMap.Count);

        //Add meshes for each materials
        foreach (MeshRenderer renderer in renderers) {
            //Get meshes
            if (!renderer.enabled) { continue; }
            Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            Mesh mesh_s = renderer.GetComponent<ShadowFilter>()?.shadowMesh;
            Mesh mesh_w = renderer.GetComponent<ShadowFilter>()?.wireMesh;
            Debug.Assert(mesh != null, "Mesh renderer '" + renderer.name + "' did not have a mesh");
            Object4D obj4D = renderer.GetComponent<Object4D>();
            Debug.Assert(obj4D != null, "Mesh renderer '" + renderer.name + "' did not have an Object4D");
            Material[] sharedMaterials = renderer.sharedMaterials;

            //Merge all indices
            for (int i = 0; i < sharedMaterials.Length; ++i) {
                Material sharedMaterial = sharedMaterials[i];
                int[] vIndices = mesh.GetIndices(i);
                int[] sIndices = (mesh_s ? mesh_s.GetIndices(i) : new int[0]);
                int[] wIndices = (mesh_w ? mesh_w.GetIndices(i) : new int[0]);
                int subMesh = matMap[sharedMaterial];
                mesh4D.AddRawIndices(vIndices, sIndices, wIndices, subMesh);
            }

            //Merge all vertices
            Mesh.MeshDataArray meshData = Mesh.AcquireReadOnlyMeshData(mesh);
            NativeArray<Mesh4D.Vertex4D> vVerts = meshData[0].GetVertexData<Mesh4D.Vertex4D>(0);
            Debug.Assert(vVerts.Length % 4 == 0, "Invalid number of vertices");
            if (mesh_s) {
                Mesh.MeshDataArray meshData_s = Mesh.AcquireReadOnlyMeshData(mesh_s);
                Mesh.MeshDataArray meshData_w = Mesh.AcquireReadOnlyMeshData(mesh_w);
                NativeArray<Mesh4D.Shadow4D> sVerts = meshData_s[0].GetVertexData<Mesh4D.Shadow4D>(0);
                NativeArray<Mesh4D.Shadow4D> wVerts = meshData_w[0].GetVertexData<Mesh4D.Shadow4D>(0);
                Debug.Assert(sVerts.Length % 3 == 0, "Invalid number of vertices");
                Debug.Assert(mesh.subMeshCount == mesh_s.subMeshCount);
                mesh4D.AddRawVerts(vVerts, sVerts, wVerts, obj4D.WorldTransform4D());
                meshData_s.Dispose();
            } else {
                mesh4D.AddRawVerts(vVerts, obj4D.WorldTransform4D());
            }

            //Dispose the mesh data correctly
            meshData.Dispose();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder GenerateFernLeaf(int segments, float length, float scaleRatio, float angle) {
        //Create a new mesh4D
        Mesh4D mesh4D = new Mesh4D();
        PseudoRandom._seed = 0;

        //Create starting tetra vertices
        Vector4 pa = new Vector4(0, Mathf.Sqrt(3.0f)/2.0f, 0, 0);
        Vector4 pb = new Vector4(-1.0f, -Mathf.Sqrt(3.0f)/2.0f, 0, 0);
        Vector4 pc = new Vector4(1.0f, -Mathf.Sqrt(3.0f)/2.0f, 0, 0);

        //Create rotation matrix
        Matrix4x4 bend = Transform4D.PlaneRotation(angle, 2, 3);

        //Grow each segment
        Vector4 offset = new Vector4(0, 0, 0, length);
        for (int i = 0; i < segments; ++i) {
            //Generate a random translation for the next segment
            //Vector4 v = (Vector4)(PseudoRandom.Normal3D() * stdDev);
            offset = bend * (offset * scaleRatio); //Vector4.Scale(offset, new Vector4(0.5f, 0.5f, 0.5f, scaleRatio));
            float pAO = Mathf.Pow(i / (float)segments, 0.4f) * 0.8f + 0.2f;
            float nAO = Mathf.Pow((i + 1) / (float)segments, 0.4f) * 0.8f + 0.2f;

            //Find new vertices
            Vector4 na = pa + offset;
            Vector4 nb = pb + offset;
            Vector4 nc = pc + offset;
            Vector4 ncenter = (na + nb + nc) / 3.0f;

            if (i == segments - 1) {
                //Add cap for last segment
                mesh4D.AddTetrahedron(pa, pb, pc, ncenter, pAO, pAO, pAO, nAO);
            } else {
                //Rotate the new points
                na = scaleRatio * (na - ncenter) + ncenter;
                nb = scaleRatio * (nb - ncenter) + ncenter;
                nc = scaleRatio * (nc - ncenter) + ncenter;

                //Connect to old ones
                mesh4D.AddHalfCell(pb, nb, pa, na, pc, nc, pAO, nAO, pAO, nAO, pAO, nAO);

                //Update offset and vertices
                pa = na;
                pb = nb;
                pc = nc;
            }
        }
        return new Mesh4DBuilder(mesh4D);
    }

    private static Mesh4DBuilder GenerateTetUVHeight(string filepath, float vScale) {
        //Create a new mesh4D
        Mesh mesh3D = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + filepath);
        Debug.Assert(mesh3D != null);
        Mesh4D mesh4D = new Mesh4D(mesh3D.subMeshCount);

        //Scan through the 3D mesh
        Vector3[] verticies = mesh3D.vertices;
        Vector2[] uvs = mesh3D.uv;
        Debug.Assert(verticies.Length == uvs.Length);
        for (int s = 0; s < mesh3D.subMeshCount; ++s) {
            int[] indices = mesh3D.GetIndices(s);
            Debug.Assert(indices.Length % 4 == 0);
            for (int i = 0; i < indices.Length; i += 4) {
                int aIx = indices[i];
                int bIx = indices[i + 1];
                int cIx = indices[i + 2];
                int dIx = indices[i + 3];
                Vector4 a = verticies[aIx]; a.w = vScale * (uvs[aIx].y - 0.5f);
                Vector4 b = verticies[bIx]; b.w = vScale * (uvs[bIx].y - 0.5f);
                Vector4 c = verticies[cIx]; c.w = vScale * (uvs[cIx].y - 0.5f);
                Vector4 d = verticies[dIx]; d.w = vScale * (uvs[dIx].y - 0.5f);

                mesh4D.AddTetrahedron(a, b, c, d);
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }

    private static int RunParityCheck(Dictionary<Tuple<Vector3, Vector3>, int> plist, Vector3 a, Vector3 b, Vector3 c) {
        Tuple<Vector3, Vector3> ab = new(a, b);
        Tuple<Vector3, Vector3> bc = new(b, c);
        Tuple<Vector3, Vector3> ca = new(c, a);
        int abSign = plist.ContainsKey(ab) ? plist[ab] : 0;
        int bcSign = plist.ContainsKey(bc) ? plist[bc] : 0;
        int caSign = plist.ContainsKey(ca) ? plist[ca] : 0;
        ParityAddPair(plist, ab, ref abSign, bcSign, caSign);
        ParityAddPair(plist, bc, ref bcSign, caSign, abSign);
        ParityAddPair(plist, ca, ref caSign, abSign, bcSign);
        int parity = (abSign == 1 ? 1 : 0) | (bcSign == 1 ? 2 : 0) | (caSign == 1 ? 4 : 0);
        Debug.Assert(parity != 0 && parity != 7, abSign + " " + bcSign + " " + caSign);
        return parity;
    }

    private static void ParityAddPair(Dictionary<Tuple<Vector3, Vector3>, int> plist, Tuple<Vector3, Vector3> v, ref int signA, int signB, int signC) {
        //This heuristic approach happens to work, but more robust method may be needed in the future...
        if (signA == 0) {
            if (signB == 0 || signC == 0 || signB != signC) {
                signA = LexographicOrder(v.Item1, v.Item2) ? 1 : -1;
            } else {
                signA = -signB;
            }
            Tuple<Vector3, Vector3> u = new(v.Item2, v.Item1);
            Debug.Assert(!plist.ContainsKey(u));
            plist[v] = signA;
            plist[u] = -signA;
        }
    }

    private static bool LexographicOrder(Vector3 a, Vector3 b) {
        if (a.x != b.x) return a.x > b.x;
        if (a.y != b.y) return a.y > b.y;
        return a.z > b.z;
    }

    private static void AddHalfCellParity(Mesh4D mesh4D, Vector4 a1, Vector4 a2, Vector4 b1, Vector4 b2, Vector4 c1, Vector4 c2, int parity) {
        //Walls of the prism
        switch (parity) {
            default:
            case 1: mesh4D.AddHalfCell(a2, a1, b2, b1, c2, c1, false); break;
            case 2: mesh4D.AddHalfCell(b2, b1, c2, c1, a2, a1, false); break;
            case 3: mesh4D.AddHalfCell(c1, c2, a1, a2, b1, b2, true); break;
            case 4: mesh4D.AddHalfCell(c2, c1, a2, a1, b2, b1, false); break;
            case 5: mesh4D.AddHalfCell(b1, b2, c1, c2, a1, a2, true); break;
            case 6: mesh4D.AddHalfCell(a1, a2, b1, b2, c1, c2, true); break;
        }
    }
    private static void AddHalfCellParity(Mesh4D mesh4D, Vector4 a1, Vector4 a2, Vector4 b1, Vector4 b2, Vector4 c1, Vector4 c2,
                                          float a1_c, float a2_c, float b1_c, float b2_c, float c1_c, float c2_c, int parity) {
        //Walls of the prism
        switch (parity) {
            default:
            case 1: mesh4D.AddHalfCell(a2, a1, b2, b1, c2, c1, a2_c, a1_c, b2_c, b1_c, c2_c, c1_c, false); break;
            case 2: mesh4D.AddHalfCell(b2, b1, c2, c1, a2, a1, b2_c, b1_c, c2_c, c1_c, a2_c, a1_c, false); break;
            case 3: mesh4D.AddHalfCell(c1, c2, a1, a2, b1, b2, c1_c, c2_c, a1_c, a2_c, b1_c, b2_c, true); break;
            case 4: mesh4D.AddHalfCell(c2, c1, a2, a1, b2, b1, c2_c, c1_c, a2_c, a1_c, b2_c, b1_c, false); break;
            case 5: mesh4D.AddHalfCell(b1, b2, c1, c2, a1, a2, b1_c, b2_c, c1_c, c2_c, a1_c, a2_c, true); break;
            case 6: mesh4D.AddHalfCell(a1, a2, b1, b2, c1, c2, a1_c, a2_c, b1_c, b2_c, c1_c, c2_c, true); break;
        }
    }

    private static Mesh4DBuilder Color600Cell(Mesh4DBuilder mesh600) {
        List<int> indices = mesh600.mesh4D.vIndices[0];
        List<Mesh4D.Vertex4D> verts = mesh600.mesh4D.vArray;
        List<Vector4> normals = new();
        HashSet<Vector4> dict120 = new();
        List<int> group = new();
        for (int i = 0; i < indices.Count; i += 6) {
            Mesh4D.Vertex4D v = verts[indices[i]];
            Vector4 n = Transform4D.MakeNormal(v.va - v.vd, v.vb - v.vd, v.vc - v.vd);
            normals.Add(n / n.magnitude);
            group.Add(0);
            dict120.Add(v.va);
            dict120.Add(v.vb);
            dict120.Add(v.vc);
            dict120.Add(v.vd);
        }
        List<Vector4> verts120 = new(dict120);
        Debug.Assert(normals.Count == 600);
        Debug.Assert(verts120.Count == 120);
        Matrix4x4 srcMatrix1 = new Matrix4x4(normals[0], normals[8], normals[12], normals[46]);
        Matrix4x4 srcMatrix2 = new Matrix4x4(normals[1], normals[11], normals[14], normals[48]);
        Matrix4x4 srcMatrix3 = new Matrix4x4(normals[2], normals[13], normals[18], normals[43]);
        Matrix4x4 srcMatrix4 = new Matrix4x4(normals[3], normals[7], normals[34], normals[97]);
        Matrix4x4 dstMatrix = new Matrix4x4(verts120[0], verts120[1], verts120[2], verts120[3]);
        Matrix4x4 rot1 = dstMatrix * srcMatrix1.inverse;
        Matrix4x4 rot2 = dstMatrix * srcMatrix2.inverse;
        Matrix4x4 rot3 = dstMatrix * srcMatrix3.inverse;
        Matrix4x4 rot4 = dstMatrix * srcMatrix4.inverse;
        Matrix4x4 rotTest = rot4.transpose * rot4;
        Debug.Assert(Vector4.Distance(rotTest.GetColumn(0), new Vector4(1, 0, 0, 0)) < 1e-3f, rotTest.GetColumn(0));
        Debug.Assert(Vector4.Distance(rotTest.GetColumn(1), new Vector4(0, 1, 0, 0)) < 1e-3f, rotTest.GetColumn(1));
        Debug.Assert(Vector4.Distance(rotTest.GetColumn(2), new Vector4(0, 0, 1, 0)) < 1e-3f, rotTest.GetColumn(2));
        Debug.Assert(Vector4.Distance(rotTest.GetColumn(3), new Vector4(0, 0, 0, 1)) < 1e-3f, rotTest.GetColumn(3));
        for (int i = 0; i < normals.Count; ++i) {
            Vector4 v1 = rot1 * normals[i];
            Vector4 v2 = rot2 * normals[i];
            Vector4 v3 = rot3 * normals[i];
            Vector4 v4 = rot4 * normals[i];
            for (int j = 0; j < verts120.Count; ++j) {
                if (Vector4.Dot(verts120[j], v1) > 0.99f) {
                    group[i] = 1; break;
                } else if (Vector4.Dot(verts120[j], v2) > 0.99f) {
                    group[i] = 2; break;
                } else if (Vector4.Dot(verts120[j], v3) > 0.99f) {
                    group[i] = 3; break;
                } else if (Vector4.Dot(verts120[j], v4) > 0.99f) {
                    group[i] = 4; break;
                }
            }
        }
        for (int i = 0; i < indices.Count; i += 6) {
            Mesh4D.Vertex4D v = verts[indices[i]];
            uint ao = (uint)group[i / 6] * 51;
            v.ao = ao | (ao << 8) | (ao << 16) | (ao << 24);
            verts[indices[i]] = v;
            verts[indices[i + 1]] = v;
            verts[indices[i + 2]] = v;
            verts[indices[i + 5]] = v;
        }
        return mesh600;
    }

    private static Mesh4DBuilder GenerateCompound(Mesh4DBuilder meshBuilder, string group, int numColors = -1, int maxTouching = -1) {
        List<Matrix4x4> rots = GenerateGroups.LoadGroup(group);
        Mesh4DBuilder compound = new Mesh4DBuilder(new Mesh4D());
        Debug.Assert(meshBuilder.mesh4D.vIndices.Length == 1);
        int[] vIndices = meshBuilder.mesh4D.vIndices[0].ToArray();
        int[] sIndices = meshBuilder.mesh4D.sIndices[0].ToArray();
        int[] wIndices = meshBuilder.mesh4D.wIndices[0].ToArray();
        List<Mesh4D.Vertex4D> vArray = meshBuilder.mesh4D.vArray;
        List<Mesh4D.Shadow4D> sArray = meshBuilder.mesh4D.sArray;
        List<Mesh4D.Shadow4D> wArray = meshBuilder.mesh4D.wArray;

        //Also make a flipped mesh
        Mesh4DBuilder flippedBuilder = new Mesh4DBuilder(new Mesh4D());
        flippedBuilder.mesh4D.AddRawIndices(vIndices, sIndices, wIndices, 0);
        flippedBuilder.mesh4D.AddRawVerts(vArray, sArray, wArray, Transform4D.identity);
        flippedBuilder.FlipNormals();
        int[] vIndicesFlip = flippedBuilder.mesh4D.vIndices[0].ToArray();
        List<Mesh4D.Vertex4D> vArrayFlip = flippedBuilder.mesh4D.vArray;
        Debug.Assert(vIndices.Length == vIndicesFlip.Length);

        //Create a list of unique vertices for the base
        HashSet<Vector4> vertSet = new();
        foreach (Mesh4D.Vertex4D v in vArray) {
            vertSet.Add(v.va);
            vertSet.Add(v.vb);
            vertSet.Add(v.vc);
            vertSet.Add(v.vd);
        }
        List<Vector4> vertList = new List<Vector4>(vertSet);
        List<List<Vector4>> existingPoints = new();
        List<Transform4D> finalTransforms = new();

        //Iterate through all symmetries of the group
        foreach (Matrix4x4 rot in rots) {
            //Check if the rotation is symmetric to an existing one
            Transform4D transform = new Transform4D(rot, Vector4.zero);
            List<Vector4> newVerts = new();
            foreach (Vector4 v in vertList) {
                newVerts.Add(transform * v);
            }
            bool duplicate = false;
            foreach (List<Vector4> points in existingPoints) {
                int numFailed = 0;
                foreach (Vector4 tv in newVerts) {
                    foreach (Vector4 ev in points) {
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
            List<Transform4D> filteredTransforms = new();
            for (int i = 0; i < existingPoints.Count; ++i) {
                int numTouching = 0;
                for (int j = 0; j < existingPoints.Count; ++j) {
                    if (i == j) { continue; }
                    foreach (Vector4 vi in existingPoints[i]) {
                        foreach (Vector4 vj in existingPoints[j]) {
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
        List<Mesh4D.Vertex4D> compoundVArray = compound.mesh4D.vArray;
        numColors = (numColors > 0 ? numColors : finalTransforms.Count);
        for (int i = 0; i < finalTransforms.Count; ++i) {
            float color = (i % numColors) / (float)Mathf.Max(numColors - 1, 1);
            bool flip = (finalTransforms[i].matrix.determinant < 0.0f);
            compound.mesh4D.AddRawIndices(flip ? vIndicesFlip : vIndices, sIndices, wIndices, 0);
            compound.mesh4D.AddRawVerts(flip ? vArrayFlip : vArray, sArray, wArray, finalTransforms[i]);
            for (int j = compoundVArray.Count - vArray.Count; j < compoundVArray.Count; ++j) {
                Mesh4D.Vertex4D v = compoundVArray[j];
                uint ao = (uint)(color * 255.0f);
                v.ao = ao | (ao << 8) | (ao << 16) | (ao << 24);
                compoundVArray[j] = v;
            }
        }
        return compound;
    }

    private static Mesh4DBuilder GenerateWireframe(Mesh4DBuilder shadow, float thickness) {
        //Load the shadow mesh
        List<int>[] shadowIndices = shadow.mesh4D.sIndices;
        List<Mesh4D.Shadow4D> verts = shadow.mesh4D.sArray;
        Mesh4D mesh4D = new Mesh4D(shadowIndices.Length);

        //Scan through the shadow mesh
        Debug.Assert(verts.Count % 3 == 0);
        for (int s = 0; s < shadowIndices.Length; ++s) {
            List<int> indices = shadowIndices[s];
            Debug.Assert(indices.Count % 3 == 0);
            for (int i = 0; i < indices.Count; i += 3) {
                //Original vertices of the face
                Vector4 va1 = verts[indices[i]].vertex;
                Vector4 vb1 = verts[indices[i + 1]].vertex;
                Vector4 vc1 = verts[indices[i + 2]].vertex;
                Vector4 p = Transform4D.MakeNormal(va1, vb1, vc1).normalized;
                Vector4 center = (va1 + vb1 + vc1) / 3.0f;

                //Extruded verts
                Vector4 va2 = va1 - (va1.normalized * thickness) + p * (thickness * 0.5f);
                Vector4 vb2 = vb1 - (vb1.normalized * thickness) + p * (thickness * 0.5f);
                Vector4 vc2 = vc1 - (vc1.normalized * thickness) + p * (thickness * 0.5f);
                Vector4 va3 = va1 - (va1.normalized * thickness) - p * (thickness * 0.5f);
                Vector4 vb3 = vb1 - (vb1.normalized * thickness) - p * (thickness * 0.5f);
                Vector4 vc3 = vc1 - (vc1.normalized * thickness) - p * (thickness * 0.5f);

                //Add cells
                mesh4D.AddHalfCellNormal(center, va1, va2, vb1, vb2, vc1, vc2);
                mesh4D.AddHalfCellNormal(center, va3, va1, vb3, vb1, vc3, vc1);
                mesh4D.AddHalfCellNormal(-center, va2, va3, vb2, vb3, vc2, vc3);

                //Add shadows
                mesh4D.AddQuadShadow(va2, vb2, va3, vb3);
                mesh4D.AddQuadShadow(va2, vc2, va3, vc3);
                mesh4D.AddQuadShadow(vc2, vb2, vc3, vb3);
            }
            mesh4D.NextSubmesh();
        }
        return new Mesh4DBuilder(mesh4D);
    }
}
#endif
