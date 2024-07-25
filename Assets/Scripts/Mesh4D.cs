using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Mesh4D {
    public List<Vertex4D> vArray = new();
    public List<Shadow4D> sArray = new();
    public List<Shadow4D> wArray = new();
    public List<Vector4> conePoints = new();
    public List<int>[] vIndices = null;
    public List<int>[] sIndices = null;
    public List<int>[] wIndices = null;
    public int curSubMesh = 0;

    struct Vector4Triple {
        public Vector4Triple(Vector4 a, Vector4 b, Vector4 c) {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        public Vector4 a;
        public Vector4 b;
        public Vector4 c;
    }
    struct Vector4Double {
        public Vector4Double(Vector4 a, Vector4 b) {
            this.a = a;
            this.b = b;
        }
        public Vector4 a;
        public Vector4 b;
    }
    private HashSet<Vector4Triple> shadowHashset = new();
    private HashSet<Vector4Double> wireHashset = new();
    private bool InHashSet(Vector4 a, Vector4 b, Vector4 c) {
        return shadowHashset.Contains(new Vector4Triple(a, b, c)) ||
               shadowHashset.Contains(new Vector4Triple(a, c, b)) ||
               shadowHashset.Contains(new Vector4Triple(b, a, c)) ||
               shadowHashset.Contains(new Vector4Triple(b, c, a)) ||
               shadowHashset.Contains(new Vector4Triple(c, a, b)) ||
               shadowHashset.Contains(new Vector4Triple(c, b, a));
    }
    private bool InHashSet(Vector4 a, Vector4 b) {
        return wireHashset.Contains(new Vector4Double(a, b)) ||
               wireHashset.Contains(new Vector4Double(b, a));
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct PackedNormal {
        public PackedNormal(uint a, uint b, uint c, uint d) {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
        public static PackedNormal Flat(Vector4 a, Vector4 b, Vector4 c, Vector4 d) {
            Vector4 n = Transform4D.MakeNormal(a - d, b - d, c - d);
            uint p = (n.magnitude >= 1e-12f ? PackNormal(-n) : 0);
            return new PackedNormal(p, p, p, p);
        }
        public static uint Flip(uint p) {
            uint x = 256 - ((p) & 0xFF);
            uint y = 256 - ((p >> 8) & 0xFF);
            uint z = 256 - ((p >> 16) & 0xFF);
            uint w = 256 - ((p >> 24) & 0xFF);
            return (x) | (y << 8) | (z << 16) | (w << 24);
        }
        public uint a, b, c, d;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Vertex4D {
        public Vector4 va;
        public PackedNormal normal;
        public Vector4 vb;
        public Vector4 vc;
        public Vector4 vd;
        public uint ao;

        public static readonly VertexAttributeDescriptor[] layout = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.Normal,   VertexAttributeFormat.UInt32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1,VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord2,VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord3,VertexAttributeFormat.UInt32, 1),
        };

        public Vertex4D(Vector4 va, Vector4 vb, Vector4 vc, Vector4 vd, PackedNormal normal, uint ao) {
            this.va = va;
            this.normal = normal;
            this.vb = vb;
            this.vc = vc;
            this.vd = vd;
            this.ao = ao;
        }

        public float aoA {
            get { return (float)(ao & 0xFF) / 255.0f; }
        }
        public float aoB {
            get { return (float)((ao >> 8) & 0xFF) / 255.0f; }
        }
        public float aoC {
            get { return (float)((ao >> 16) & 0xFF) / 255.0f; }
        }
        public float aoD {
            get { return (float)((ao >> 24) & 0xFF) / 255.0f; }
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Shadow4D {
        public Vector4 vertex;

        public static readonly VertexAttributeDescriptor[] layout = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4),
        };

        public Shadow4D(Vector4 vertex) {
            this.vertex = vertex;
        }
    }

    public Mesh4D(int submeshCount = 1) {
        vIndices = new List<int>[submeshCount];
        sIndices = new List<int>[submeshCount];
        wIndices = new List<int>[submeshCount];
        for (int i = 0; i < submeshCount; ++i) {
            vIndices[i] = new List<int>();
            sIndices[i] = new List<int>();
            wIndices[i] = new List<int>();
        }
    }

    public static uint Twiddle(uint ao) {
        return ((ao & 0xF) << 12) | ((ao & 0xF0) << 4) | ((ao & 0xF00) >> 4) | ((ao & 0xF000) >> 12);
    }

    public void ClearShadows() {
        sArray.Clear();
        wArray.Clear();
        shadowHashset.Clear();
        wireHashset.Clear();
        for (int i = 0; i < sIndices.Length; ++i) { sIndices[i].Clear(); }
        for (int i = 0; i < wIndices.Length; ++i) { wIndices[i].Clear(); }
    }

    public void MarkConePoint(Vector4 pt) {
        conePoints.Add(pt);
    }

    public void AddTetrahedron(Vector4 a, Vector4 b, Vector4 c, Vector4 d) {
        AddTetrahedron(a, b, c, d, 0);
    }
    public void AddTetrahedron(Vector4 a, Vector4 b, Vector4 c, Vector4 d, float a_c, float b_c, float c_c, float d_c) {
        uint ua = (uint)(Mathf.Clamp(a_c * 256.0f, 0.0f, 255.0f));
        uint ub = (uint)(Mathf.Clamp(b_c * 256.0f, 0.0f, 255.0f));
        uint uc = (uint)(Mathf.Clamp(c_c * 256.0f, 0.0f, 255.0f));
        uint ud = (uint)(Mathf.Clamp(d_c * 256.0f, 0.0f, 255.0f));
        AddTetrahedron(a, b, c, d, ua | (ub << 8) | (uc << 16) | (ud << 24));
    }
    public void AddTetrahedronNormal(Vector4 n, Vector4 a, Vector4 b, Vector4 c, Vector4 d) {
        float nsign = Vector4.Dot(n, Transform4D.MakeNormal(a - d, b - d, c - d));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f);
        if (nsign > 0) {
            AddTetrahedron(a, b, c, d, Twiddle(0x3065));
        } else {
            AddTetrahedron(b, a, c, d, Twiddle(0x3065));
        }
    }
    public void AddTetrahedron(Vector4 a, Vector4 b, Vector4 c, Vector4 d, uint ao) {
        PackedNormal pn = PackedNormal.Flat(a, b, c, d);
        if (pn.a == 0) { return; }
        AddTetrahedron(a, b, c, d, pn, ao);
    }
    public void AddTetrahedron(Vector4 a, Vector4 b, Vector4 c, Vector4 d, PackedNormal p, uint ao) {
        vArray.Add(new Vertex4D(a, b, c, d, p, ao));
        vArray.Add(new Vertex4D(a, b, c, d, p, ao));
        vArray.Add(new Vertex4D(a, b, c, d, p, ao));
        vArray.Add(new Vertex4D(a, b, c, d, p, ao));
        vIndices[curSubMesh].Add(vArray.Count - 4);
        vIndices[curSubMesh].Add(vArray.Count - 3);
        vIndices[curSubMesh].Add(vArray.Count - 2);
        vIndices[curSubMesh].Add(vArray.Count - 4);
        vIndices[curSubMesh].Add(vArray.Count - 2);
        vIndices[curSubMesh].Add(vArray.Count - 1);
    }

    public void AddCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2, bool parity=false) {
        if (parity) {
            AddTetrahedron(A1, b1, B2, a2, Twiddle(0x2471));
            AddTetrahedron(B2, b1, A1, B1, Twiddle(0x7426));
            AddTetrahedron(A1, b1, a2, a1, Twiddle(0x2410));
            AddTetrahedron(A1, a2, B2, A2, Twiddle(0x2173));
            AddTetrahedron(a2, b1, B2, b2, Twiddle(0x1475));
        } else {
            AddTetrahedron(A2, a1, B1, b2, Twiddle(0x3065));
            AddTetrahedron(B1, a1, A2, A1, Twiddle(0x6032));
            AddTetrahedron(A2, a1, b2, a2, Twiddle(0x3051));
            AddTetrahedron(A2, b2, B1, B2, Twiddle(0x3567));
            AddTetrahedron(b2, a1, B1, b1, Twiddle(0x5064));
        }
    }
    public void AddCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2, uint aoAll, bool parity = false) {
        if (parity) {
            AddTetrahedron(A1, b1, B2, a2, Twiddle(aoAll));
            AddTetrahedron(B2, b1, A1, B1, Twiddle(aoAll));
            AddTetrahedron(A1, b1, a2, a1, Twiddle(aoAll));
            AddTetrahedron(A1, a2, B2, A2, Twiddle(aoAll));
            AddTetrahedron(a2, b1, B2, b2, Twiddle(aoAll));
        } else {
            AddTetrahedron(A2, a1, B1, b2, Twiddle(aoAll));
            AddTetrahedron(B1, a1, A2, A1, Twiddle(aoAll));
            AddTetrahedron(A2, a1, b2, a2, Twiddle(aoAll));
            AddTetrahedron(A2, b2, B1, B2, Twiddle(aoAll));
            AddTetrahedron(b2, a1, B1, b1, Twiddle(aoAll));
        }
    }
    public void AddCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2,
                        float a1_c, float a2_c, float A1_c, float A2_c, float b1_c, float b2_c, float B1_c, float B2_c, bool parity = false) {
        if (parity) {
            AddTetrahedron(A1, b1, B2, a2, A1_c, b1_c, B2_c, a2_c);
            AddTetrahedron(B2, b1, A1, B1, B2_c, b1_c, A1_c, B1_c);
            AddTetrahedron(A1, b1, a2, a1, A1_c, b1_c, a2_c, a1_c);
            AddTetrahedron(A1, a2, B2, A2, A1_c, a2_c, B2_c, A2_c);
            AddTetrahedron(a2, b1, B2, b2, a2_c, b1_c, B2_c, b2_c);
        } else {
            AddTetrahedron(A2, a1, B1, b2, A2_c, a1_c, B1_c, b2_c);
            AddTetrahedron(B1, a1, A2, A1, B1_c, a1_c, A2_c, A1_c);
            AddTetrahedron(A2, a1, b2, a2, A2_c, a1_c, b2_c, a2_c);
            AddTetrahedron(A2, b2, B1, B2, A2_c, b2_c, B1_c, B2_c);
            AddTetrahedron(b2, a1, B1, b1, b2_c, a1_c, B1_c, b1_c);
        }
    }
    public void AddCellBiSmooth(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2, Vector4 n, Vector4 N, bool parity=false) {
        Vector4 faceNormal = Transform4D.MakeNormal(a2 - a1, A1 - a1, b1 - a1);
        Debug.Assert(Vector4.Dot(faceNormal, n) > 0.0f);
        Debug.Assert(Vector4.Dot(faceNormal, N) > 0.0f);
        uint p = PackNormal(-n);
        uint P = PackNormal(-N);
        if (parity) {
            AddTetrahedron(A1, b1, B2, a2, new PackedNormal(P, p, P, p), Twiddle(0x2471));
            AddTetrahedron(B2, b1, A1, B1, new PackedNormal(P, p, P, P), Twiddle(0x7426));
            AddTetrahedron(A1, b1, a2, a1, new PackedNormal(P, p, p, p), Twiddle(0x2410));
            AddTetrahedron(A1, a2, B2, A2, new PackedNormal(P, p, P, P), Twiddle(0x2173));
            AddTetrahedron(a2, b1, B2, b2, new PackedNormal(p, p, P, p), Twiddle(0x1475));
        } else {
            AddTetrahedron(A2, a1, B1, b2, new PackedNormal(P, p, P, p), Twiddle(0x3065));
            AddTetrahedron(B1, a1, A2, A1, new PackedNormal(P, p, P, P), Twiddle(0x6032));
            AddTetrahedron(A2, a1, b2, a2, new PackedNormal(P, p, p, p), Twiddle(0x3051));
            AddTetrahedron(A2, b2, B1, B2, new PackedNormal(P, p, P, P), Twiddle(0x3567));
            AddTetrahedron(b2, a1, B1, b1, new PackedNormal(p, p, P, p), Twiddle(0x5064));
        }
    }
    public void AddCellBiSmooth(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2, Vector4 n, Vector4 N, uint aoAll, bool parity=false) {
        Vector4 faceNormal = Transform4D.MakeNormal(a2 - a1, A1 - a1, b1 - a1);
        Debug.Assert(Vector4.Dot(faceNormal, n) > 0.0f);
        Debug.Assert(Vector4.Dot(faceNormal, N) > 0.0f);
        uint p = PackNormal(-n);
        uint P = PackNormal(-N);
        if (parity) {
            AddTetrahedron(A1, b1, B2, a2, new PackedNormal(P, p, P, p), Twiddle(aoAll));
            AddTetrahedron(B2, b1, A1, B1, new PackedNormal(P, p, P, P), Twiddle(aoAll));
            AddTetrahedron(A1, b1, a2, a1, new PackedNormal(P, p, p, p), Twiddle(aoAll));
            AddTetrahedron(A1, a2, B2, A2, new PackedNormal(P, p, P, P), Twiddle(aoAll));
            AddTetrahedron(a2, b1, B2, b2, new PackedNormal(p, p, P, p), Twiddle(aoAll));
        } else {
            AddTetrahedron(A2, a1, B1, b2, new PackedNormal(P, p, P, p), Twiddle(aoAll));
            AddTetrahedron(B1, a1, A2, A1, new PackedNormal(P, p, P, P), Twiddle(aoAll));
            AddTetrahedron(A2, a1, b2, a2, new PackedNormal(P, p, p, p), Twiddle(aoAll));
            AddTetrahedron(A2, b2, B1, B2, new PackedNormal(P, p, P, P), Twiddle(aoAll));
            AddTetrahedron(b2, a1, B1, b1, new PackedNormal(p, p, P, p), Twiddle(aoAll));
        }
    }

    public void AddHalfCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, bool parity = false) {
        if (parity) {
            AddTetrahedron(a1, A2, b2, a2, Twiddle(0x0751));
            AddTetrahedron(a1, A1, b1, A2, Twiddle(0x0647));
            AddTetrahedron(b1, b2, a1, A2, Twiddle(0x4507));
        } else {
            AddTetrahedron(A2, a1, b2, a2, Twiddle(0x7051));
            AddTetrahedron(A1, a1, b1, A2, Twiddle(0x6047));
            AddTetrahedron(b2, b1, a1, A2, Twiddle(0x5407));
        }
    }
    public void AddHalfCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, uint aoAll, bool parity = false) {
        if (parity) {
            AddTetrahedron(a1, A2, b2, a2, Twiddle(aoAll));
            AddTetrahedron(a1, A1, b1, A2, Twiddle(aoAll));
            AddTetrahedron(b1, b2, a1, A2, Twiddle(aoAll));
        } else {
            AddTetrahedron(A2, a1, b2, a2, Twiddle(aoAll));
            AddTetrahedron(A1, a1, b1, A2, Twiddle(aoAll));
            AddTetrahedron(b2, b1, a1, A2, Twiddle(aoAll));
        }
    }
    public void AddHalfCell(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2,
                            float a1_c, float a2_c, float A1_c, float A2_c, float b1_c, float b2_c, bool parity = false) {
        if (parity) {
            AddTetrahedron(a1, A2, b2, a2, a1_c, A2_c, b2_c, a2_c);
            AddTetrahedron(a1, A1, b1, A2, a1_c, A1_c, b1_c, A2_c);
            AddTetrahedron(b1, b2, a1, A2, b1_c, b2_c, a1_c, A2_c);
        } else {
            AddTetrahedron(A2, a1, b2, a2, A2_c, a1_c, b2_c, a2_c);
            AddTetrahedron(A1, a1, b1, A2, A1_c, a1_c, b1_c, A2_c);
            AddTetrahedron(b2, b1, a1, A2, b2_c, b1_c, a1_c, A2_c);
        }
    }
    public void AddHalfCellBiSmooth(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 n1, Vector4 n2) {
        Vector4 faceNormal = Transform4D.MakeNormal(a2 - a1, A1 - a1, b1 - a1);
        Debug.Assert(Vector4.Dot(faceNormal, n1) > 0.0f);
        Debug.Assert(Vector4.Dot(faceNormal, n2) > 0.0f);
        uint p1 = PackNormal(-n1);
        uint p2 = PackNormal(-n2);
        AddTetrahedron(A2, a1, b2, a2, new PackedNormal(p2, p1, p2, p2), Twiddle(0x3051));
        AddTetrahedron(A1, a1, b1, A2, new PackedNormal(p1, p1, p1, p2), Twiddle(0x2043));
        AddTetrahedron(b2, b1, a1, A2, new PackedNormal(p2, p1, p1, p2), Twiddle(0x5403));
    }

    public void AddPyramid(Vector4 tip, Vector4 a1, Vector4 a2, Vector4 b1, Vector4 b2) {
        AddTetrahedron(a1, a2, b1, tip);
        AddTetrahedron(b1, a2, b2, tip);
    }
    public void AddPyramid(Vector4 tip, Vector4 a1, Vector4 a2, Vector4 b1, Vector4 b2,
                           float tip_c, float a1_c, float a2_c, float b1_c, float b2_c) {
        AddTetrahedron(a1, a2, b1, tip, a1_c, a2_c, b1_c, tip_c);
        AddTetrahedron(b1, a2, b2, tip, b1_c, a2_c, b2_c, tip_c);
    }

    public void AddCellNormal(Vector4 n, Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2, bool parity=false) {
        float nsign = -Vector4.Dot(n, Transform4D.MakeNormal(a2 - a1, A1 - a1, b1 - a1));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f);
        if (nsign < 0) {
            AddCell(a1, a2, A1, A2, b1, b2, B1, B2, parity);
        } else {
            AddCell(a2, a1, A2, A1, b2, b1, B2, B1, parity);
        }
    }
    public void AddCellNormal(Vector4 n, Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2, uint aoAll, bool parity=false) {
        float nsign = -Vector4.Dot(n, Transform4D.MakeNormal(a2 - a1, A1 - a1, b1 - a1));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f);
        if (nsign < 0) {
            AddCell(a1, a2, A1, A2, b1, b2, B1, B2, aoAll, parity);
        } else {
            AddCell(a2, a1, A2, A1, b2, b1, B2, B1, aoAll, parity);
        }
    }

    public void AddHalfCellNormal(Vector4 n, Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2) {
        float nsign = -Vector4.Dot(n, Transform4D.MakeNormal(a2 - a1, A1 - a1, b1 - a1));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f);
        if (nsign < 0) {
            AddHalfCell(a1, a2, A1, A2, b1, b2);
        } else {
            AddHalfCell(a2, a1, A2, A1, b2, b1);
        }
    }
    public void AddHalfCellNormal(Vector4 n, Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, uint aoAll) {
        float nsign = -Vector4.Dot(n, Transform4D.MakeNormal(a2 - a1, A1 - a1, b1 - a1));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f);
        if (nsign < 0) {
            AddHalfCell(a1, a2, A1, A2, b1, b2, aoAll);
        } else {
            AddHalfCell(a2, a1, A2, A1, b2, b1, aoAll);
        }
    }

    public void AddTriangleShadow(Vector4 a, Vector4 b, Vector4 c) {
        //Update wire mesh
        AddWire(a, b);
        AddWire(b, c);
        AddWire(c, a);

        //Check for degenerate triangles
        if (a == b || b == c || c == a) { return; }

        //Update hash-set
        if (InHashSet(a, b, c)) { return; }
        shadowHashset.Add(new Vector4Triple(a, b, c));

        //Update shadow mesh
        sArray.Add(new Shadow4D(a));
        sArray.Add(new Shadow4D(b));
        sArray.Add(new Shadow4D(c));
        sIndices[curSubMesh].Add(sArray.Count - 3);
        sIndices[curSubMesh].Add(sArray.Count - 2);
        sIndices[curSubMesh].Add(sArray.Count - 1);
    }

    public void AddQuadShadow(Vector4 a1, Vector4 a2, Vector4 b1, Vector4 b2) {
        //Check for degenerate quads
        if (a1 == a2 || b2 == a2) {
            AddTriangleShadow(a1, b1, b2);
            return;
        } else if (a1 == b1 || b2 == b1) {
            AddTriangleShadow(a1, a2, b2);
            return;
        }

        //Check hash set
        if (InHashSet(a1, a2, b1)) { return; }

        //Update shadow mesh
        sArray.Add(new Shadow4D(a1));
        sArray.Add(new Shadow4D(a2));
        sArray.Add(new Shadow4D(b1));
        sIndices[curSubMesh].Add(sArray.Count - 3);
        sIndices[curSubMesh].Add(sArray.Count - 2);
        sIndices[curSubMesh].Add(sArray.Count - 1);
        Debug.Assert(!InHashSet(b2, a2, b1), "No other quad components should be in the set");
        sArray.Add(new Shadow4D(b2));
        sArray.Add(new Shadow4D(a2));
        sArray.Add(new Shadow4D(b1));
        sIndices[curSubMesh].Add(sArray.Count - 3);
        sIndices[curSubMesh].Add(sArray.Count - 2);
        sIndices[curSubMesh].Add(sArray.Count - 1);
        Debug.Assert(!InHashSet(a2, a1, b2), "No other quad components should be in the set");
        Debug.Assert(!InHashSet(b1, a1, b2), "No other quad components should be in the set");

        //Update hash-set
        shadowHashset.Add(new Vector4Triple(a1, a2, b1));
        shadowHashset.Add(new Vector4Triple(b2, a2, b1));
        shadowHashset.Add(new Vector4Triple(a2, a1, b2));
        shadowHashset.Add(new Vector4Triple(b1, a1, b2));

        //Update wire mesh
        AddWire(a1, a2);
        AddWire(a2, b2);
        AddWire(b2, b1);
        AddWire(b1, a1);
    }

    public void AddCellShadow(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2) {
        AddQuadShadow(a2, b2, a1, b1);
        AddQuadShadow(A1, B1, A2, B2);
        AddQuadShadow(A1, a1, B1, b1);
        AddQuadShadow(a2, A2, b2, B2);
        AddQuadShadow(a2, a1, A2, A1);
        AddQuadShadow(b1, b2, B1, B2);
    }

    public void AddWire(Vector4 a, Vector4 b) {
        if (!InHashSet(a, b) && a != b) {
            wireHashset.Add(new Vector4Double(a, b));
            wArray.Add(new Shadow4D(a));
            wArray.Add(new Shadow4D(b));
            wIndices[curSubMesh].Add(wArray.Count - 2);
            wIndices[curSubMesh].Add(wArray.Count - 1);
        }
    }

    public void NextSubmesh() {
        curSubMesh += 1;
    }

    public int GetMaxIndex() {
        int maxIndex = 0;
        for (int i = 0; i < vIndices.Length; ++i) {
            maxIndex = Mathf.Max(maxIndex, vIndices[i].Count);
        }
        return maxIndex;
    }

    public void GenerateMesh(Mesh mesh) {
        mesh.Clear();
        mesh.indexFormat = (GetMaxIndex() >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16);
        mesh.subMeshCount = vIndices.Length;
        mesh.SetVertexBufferParams(vArray.Count, Vertex4D.layout);
        mesh.SetVertexBufferData(vArray, 0, 0, vArray.Count);
        for (int i = 0; i < vIndices.Length; ++i) {
            mesh.SetIndices(vIndices[i], MeshTopology.Triangles, i);
        }
    }

    public void GenerateShadowMesh(Mesh mesh) {
        mesh.Clear();
        mesh.indexFormat = (GetMaxIndex() >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16);
        mesh.subMeshCount = sIndices.Length;
        mesh.SetVertexBufferParams(sArray.Count, Shadow4D.layout);
        mesh.SetVertexBufferData(sArray, 0, 0, sArray.Count);
        for (int i = 0; i < sIndices.Length; ++i) {
            mesh.SetIndices(sIndices[i], MeshTopology.Triangles, i);
        }
    }

    public void GenerateWireMesh(Mesh mesh) {
        mesh.Clear();
        mesh.indexFormat = (GetMaxIndex() >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16);
        mesh.subMeshCount = wIndices.Length;
        mesh.SetVertexBufferParams(wArray.Count, Shadow4D.layout);
        mesh.SetVertexBufferData(wArray, 0, 0, wArray.Count);
        for (int s = 0; s < wIndices.Length; ++s) {
            mesh.SetIndices(wIndices[s], MeshTopology.Lines, s);
        }
    }

    public static uint PackNormal(Vector4 n) {
        n /= n.magnitude; //NOTE: the built-in Normalize doesn't work because Unity clips it... insane!
        uint x = (uint)Mathf.FloorToInt(n.x * 127f + 128.0f);
        uint y = (uint)Mathf.FloorToInt(n.y * 127f + 128.0f);
        uint z = (uint)Mathf.FloorToInt(n.z * 127f + 128.0f);
        uint w = (uint)Mathf.FloorToInt(n.w * 127f + 128.0f);
        Debug.Assert(x != 0 && y != 0 && z != 0 && w != 0);
        return (x & 0xFF) | ((y & 0xFF) << 8) | ((z & 0xFF) << 16) | ((w & 0xFF) << 24);
    }
    public static Vector4 UnpackNormal(uint p) {
        float x = (float)(p & 0xFF) - 128.0f;
        float y = (float)((p >> 8) & 0xFF) - 128.0f;
        float z = (float)((p >> 16) & 0xFF) - 128.0f;
        float w = (float)((p >> 24) & 0xFF) - 128.0f;
        return new Vector4(x, y, z, w).normalized;
    }

    public void AddRawIndices(int[] indices, int[] indices_s, int[] indices_w, int subMesh) {
        Debug.Assert(indices.Length % 6 == 0);
        Debug.Assert(subMesh < vIndices.Length);
        int addIx = vArray.Count;
        for (int i = 0; i < indices.Length; ++i) {
            vIndices[subMesh].Add(indices[i] + addIx);
        }
        if (indices_s != null) {
            addIx = sArray.Count;
            Debug.Assert(indices_s.Length % 3 == 0);
            for (int i = 0; i < indices_s.Length; ++i) {
                sIndices[subMesh].Add(indices_s[i] + addIx);
            }
        }
        if (indices_w != null) {
            addIx = wArray.Count;
            Debug.Assert(indices_w.Length % 2 == 0);
            for (int i = 0; i < indices_w.Length; ++i) {
                wIndices[subMesh].Add(indices_w[i] + addIx);
            }
        }
    }
    public void AddRawVerts(IEnumerable<Vertex4D> verts, Transform4D transform4D) {
        Matrix4x4 normalMatrix = transform4D.matrix.inverse.transpose;
        foreach (Vertex4D vOrig in verts) {
            Vertex4D v = vOrig;
            v.va = transform4D * v.va;
            v.vb = transform4D * v.vb;
            v.vc = transform4D * v.vc;
            v.vd = transform4D * v.vd;
            v.normal.a = PackNormal(normalMatrix * UnpackNormal(v.normal.a));
            v.normal.b = PackNormal(normalMatrix * UnpackNormal(v.normal.b));
            v.normal.c = PackNormal(normalMatrix * UnpackNormal(v.normal.c));
            v.normal.d = PackNormal(normalMatrix * UnpackNormal(v.normal.d));
            vArray.Add(v);
        }
    }
    public void AddRawVerts(IEnumerable<Vertex4D> verts, IEnumerable<Shadow4D> verts_s, IEnumerable<Shadow4D> verts_w, Transform4D tansform4D) {
        AddRawVerts(verts, tansform4D);
        foreach (Shadow4D vOrig in verts_s) {
            Shadow4D v = vOrig;
            v.vertex = tansform4D * v.vertex;
            sArray.Add(v);
        }
        foreach (Shadow4D wOrig in verts_w) {
            Shadow4D w = wOrig;
            w.vertex = tansform4D * w.vertex;
            wArray.Add(w);
        }
    }
}
