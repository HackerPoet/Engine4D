using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Mesh5D {
    public List<Vertex5D> vArray = new List<Vertex5D>();
    public List<Shadow5D> sArray = new List<Shadow5D>();
    public List<Shadow5D> wArray = new List<Shadow5D>();
    public List<Vector5> conePoints = new();
    public List<int>[] vIndices = null;
    public List<int>[] sIndices = null;
    public List<int>[] wIndices = null;
    public int curSubMesh = 0;

    struct Vector5Triple {
        public Vector5Triple(Vector5 a, Vector5 b, Vector5 c) {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        public Vector5 a;
        public Vector5 b;
        public Vector5 c;
    }
    struct Vector5Double {
        public Vector5Double(Vector5 a, Vector5 b) {
            this.a = a;
            this.b = b;
        }
        public Vector5 a;
        public Vector5 b;
    }
    private HashSet<Vector5Triple> shadowHashset = new();
    private HashSet<Vector5Double> wireHashset = new();
    private bool InHashSet(Vector5 a, Vector5 b, Vector5 c) {
        return shadowHashset.Contains(new Vector5Triple(a, b, c)) |
               shadowHashset.Contains(new Vector5Triple(a, c, b)) |
               shadowHashset.Contains(new Vector5Triple(b, a, c)) |
               shadowHashset.Contains(new Vector5Triple(b, c, a)) |
               shadowHashset.Contains(new Vector5Triple(c, a, b)) |
               shadowHashset.Contains(new Vector5Triple(c, b, a));
    }
    private bool InHashSet(Vector5 a, Vector5 b) {
        return wireHashset.Contains(new Vector5Double(a, b)) ||
               wireHashset.Contains(new Vector5Double(b, a));
    }

    public struct SingleNormal {
        public SingleNormal(byte x, byte y, byte z, byte w, byte v) {
            Debug.Assert(x != 0 && y != 0 && z != 0 && w != 0 && v != 0);
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
            this.v = v;
        }
        public SingleNormal Flip() {
            return new SingleNormal((byte)(256 - x), (byte)(256 - y), (byte)(256 - z), (byte)(256 - w), (byte)(256 - v));
        }
        public byte x, y, z, w, v;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct PackedNormal {
        public PackedNormal(SingleNormal a, SingleNormal b, SingleNormal c, SingleNormal d, SingleNormal e) {
            ax = a.x; ay = a.y; az = a.z; aw = a.w; av = a.v;
            bx = b.x; by = b.y; bz = b.z; bw = b.w; bv = b.v;
            cx = c.x; cy = c.y; cz = c.z; cw = c.w; cv = c.v;
            dx = d.x; dy = d.y; dz = d.z; dw = d.w; dv = d.v;
            ex = e.x; ey = e.y; ez = e.z; ew = e.w; ev = e.v;
            //Also initialize unused memory to zero
            u1 = u2 = u3 = 0;
            vertAV = 0.0f;
        }
        public static PackedNormal Flat(Vector5 a, Vector5 b, Vector5 c, Vector5 d, Vector5 e) {
            Vector5 n = Transform5D.MakeNormal(a - e, b - e, c - e, d - e);
            SingleNormal p = PackNormal(-n);
            return new PackedNormal(p, p, p, p, p);
        }
        public SingleNormal pa {
            get { return new SingleNormal(ax, ay, az, aw, av); }
            set { ax = value.x; ay = value.y; az = value.z; aw = value.w; av = value.v; }
        }
        public SingleNormal pb {
            get { return new SingleNormal(bx, by, bz, bw, bv); }
            set { bx = value.x; by = value.y; bz = value.z; bw = value.w; bv = value.v; }
        }
        public SingleNormal pc {
            get { return new SingleNormal(cx, cy, cz, cw, cv); }
            set { cx = value.x; cy = value.y; cz = value.z; cw = value.w; cv = value.v; }
        }
        public SingleNormal pd {
            get { return new SingleNormal(dx, dy, dz, dw, dv); }
            set { dx = value.x; dy = value.y; dz = value.z; dw = value.w; dv = value.v; }
        }
        public SingleNormal pe {
            get { return new SingleNormal(ex, ey, ez, ew, ev); }
            set { ex = value.x; ey = value.y; ez = value.z; ew = value.w; ev = value.v; }
        }

        public byte ax,ay,bx,by;
        public byte cx,cy,dx,dy;
        public byte ex,ey,av,bv;
        public byte cv,dv,ev,u1;
        public byte az,aw,bz,bw;
        public byte cz,cw,dz,dw;
        public byte ez,ew,u2,u3;
        public float vertAV;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Vertex5D {
        public Vector4 va;
        private PackedNormal _normal;
        public Vector4 vb;
        public Vector4 vc;
        public Vector4 vd;
        public Vector4 ve;
        public Vector4 v_bcde;
        public uint ao;

        public static readonly VertexAttributeDescriptor[] layout = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.Normal,   VertexAttributeFormat.UInt32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.UInt32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1,VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord2,VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord3,VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord4,VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord5,VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord6,VertexAttributeFormat.UInt32, 1),
        };

        public Vertex5D(Vector5 va, Vector5 vb, Vector5 vc, Vector5 vd, Vector5 ve, PackedNormal normal, uint ao) {
            this.va = (Vector4)va;
            this._normal = normal;
            this._normal.vertAV = va.v;
            this.vb = (Vector4)vb;
            this.vc = (Vector4)vc;
            this.vd = (Vector4)vd;
            this.ve = (Vector4)ve;
            this.v_bcde = new Vector4(vb.v, vc.v, vd.v, ve.v);
            this.ao = ao;
        }

        public Vector5 va5 {
            get { return new Vector5(va.x, va.y, va.z, va.w, _normal.vertAV); }
            set { va.x = value.x; va.y = value.y; va.z = value.z; va.w = value.w; _normal.vertAV = value.v; }
        }
        public Vector5 vb5 {
            get { return new Vector5(vb.x, vb.y, vb.z, vb.w, v_bcde.x); }
            set { vb.x = value.x; vb.y = value.y; vb.z = value.z; vb.w = value.w; v_bcde.x = value.v; }
        }
        public Vector5 vc5 {
            get { return new Vector5(vc.x, vc.y, vc.z, vc.w, v_bcde.y); }
            set { vc.x = value.x; vc.y = value.y; vc.z = value.z; vc.w = value.w; v_bcde.y = value.v; }
        }
        public Vector5 vd5 {
            get { return new Vector5(vd.x, vd.y, vd.z, vd.w, v_bcde.z); }
            set { vd.x = value.x; vd.y = value.y; vd.z = value.z; vd.w = value.w; v_bcde.z = value.v; }
        }
        public Vector5 ve5 {
            get { return new Vector5(ve.x, ve.y, ve.z, ve.w, v_bcde.w); }
            set { ve.x = value.x; ve.y = value.y; ve.z = value.z; ve.w = value.w; v_bcde.w = value.v; }
        }
        public PackedNormal normal {
            get { return _normal; }
            set { value.vertAV = _normal.vertAV;
                  _normal = value;
            }
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Shadow5D {
        public Vector4 vertex;
        public float v_v;

        public static readonly VertexAttributeDescriptor[] layout = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32, 1),
        };

        public Shadow5D(Vector5 vertex) {
            this.vertex = (Vector4)vertex;
            this.v_v = vertex.v;
        }

        public Vector5 vertex5 {
            get { return new Vector5(vertex.x, vertex.y, vertex.z, vertex.w, v_v); }
            set { vertex.x = value.x; vertex.y = value.y; vertex.z = value.z; vertex.w = value.w; v_v = value.v; }
        }
    }

    public Mesh5D(int submeshCount = 1) {
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
        return ((ao & 0xF) << 16) | ((ao & 0xF0) << 8) | (ao & 0xF00) | ((ao & 0xF000) >> 8) | ((ao & 0xF0000) >> 16);
    }

    public void MarkConePoint(Vector5 pt) {
        conePoints.Add(pt);
    }

    public void AddSimplexNormal(Vector5 n, Vector5 a, Vector5 b, Vector5 c, Vector5 d, Vector5 e, uint aoAll=0) {
        float nsign = Vector5.Dot(n, Transform5D.MakeNormal(b - a, c - a, d - a, e - a));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f, "Simplex has no volume");
        if (nsign < 0) {
            AddSimplex(b, a, c, d, e, aoAll);
        } else {
            AddSimplex(a, b, c, d, e, aoAll);
        }
    }
    public void AddSimplexNormal(Vector5 n, Vector5 a, Vector5 b, Vector5 c, Vector5 d, Vector5 e,
                                            float a_c, float b_c, float c_c, float d_c, float e_c) {
        float nsign = Vector5.Dot(n, Transform5D.MakeNormal(b - a, c - a, d - a, e - a));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f, "Simplex has no volume");
        if (nsign < 0) {
            AddSimplex(b, a, c, d, e, b_c, a_c, c_c, d_c, e_c);
        } else {
            AddSimplex(a, b, c, d, e, a_c, b_c, c_c, d_c, e_c);
        }
    }
    public void AddSimplex(Vector5 a, Vector5 b, Vector5 c, Vector5 d, Vector5 e) {
        AddSimplex(a, b, c, d, e, 0);
    }
    public void AddSimplex(Vector5 a, Vector5 b, Vector5 c, Vector5 d, Vector5 e,
                           float a_c, float b_c, float c_c, float d_c, float e_c) {
        uint ua = (uint)(Mathf.Clamp(a_c * 64.0f, 0.0f, 63.0f));
        uint ub = (uint)(Mathf.Clamp(b_c * 64.0f, 0.0f, 63.0f));
        uint uc = (uint)(Mathf.Clamp(c_c * 64.0f, 0.0f, 63.0f));
        uint ud = (uint)(Mathf.Clamp(d_c * 64.0f, 0.0f, 63.0f));
        uint ue = (uint)(Mathf.Clamp(e_c * 64.0f, 0.0f, 63.0f));
        AddSimplex(a, b, c, d, e, ua | (ub << 6) | (uc << 12) | (ud << 18) | (ue << 24));
    }
    public void AddSimplex(Vector5 a, Vector5 b, Vector5 c, Vector5 d, Vector5 e, uint ao) {
        PackedNormal pn = PackedNormal.Flat(a, b, c, d, e);
        if ((pn.pa.x | pn.pa.y | pn.pa.z | pn.pa.w | pn.pa.v) == 0) { return; }
        AddSimplex(a, b, c, d, e, pn, ao);
    }
    public void AddSimplex(Vector5 a, Vector5 b, Vector5 c, Vector5 d, Vector5 e, PackedNormal p, uint ao) {
        vArray.Add(new Vertex5D(a, b, c, d, e, p, ao));
        vArray.Add(new Vertex5D(a, b, c, d, e, p, ao));
        vArray.Add(new Vertex5D(a, b, c, d, e, p, ao));
        vArray.Add(new Vertex5D(a, b, c, d, e, p, ao));
        vArray.Add(new Vertex5D(a, b, c, d, e, p, ao));
        vIndices[curSubMesh].Add(vArray.Count - 5);
        vIndices[curSubMesh].Add(vArray.Count - 4);
        vIndices[curSubMesh].Add(vArray.Count - 3);
        vIndices[curSubMesh].Add(vArray.Count - 5);
        vIndices[curSubMesh].Add(vArray.Count - 3);
        vIndices[curSubMesh].Add(vArray.Count - 2);
        vIndices[curSubMesh].Add(vArray.Count - 5);
        vIndices[curSubMesh].Add(vArray.Count - 2);
        vIndices[curSubMesh].Add(vArray.Count - 1);
    }

    public void AddCell(Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                        Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11, Vector5 v12, Vector5 v13, Vector5 v14, Vector5 v15) {
        AddSimplex(v0, v1, v2, v4, v8,    Twiddle(0x01248));
        AddSimplex(v8, v4, v12, v13, v14, Twiddle(0x84CDE));
        AddSimplex(v2, v8, v10, v11, v14, Twiddle(0x28ABE));
        AddSimplex(v4, v2, v6, v7, v14,   Twiddle(0x4267E));
        AddSimplex(v8, v1, v9, v11, v13,  Twiddle(0x819BD));
        AddSimplex(v1, v4, v5, v7, v13,   Twiddle(0x1457D));
        AddSimplex(v2, v1, v3, v7, v11,   Twiddle(0x2137B));
        AddSimplex(v7, v11, v13, v14, v15,Twiddle(0x7BDEF));
        AddSimplex(v2, v1, v4, v8, v14,   Twiddle(0x2148E));
        AddSimplex(v2, v1, v8, v11, v14,  Twiddle(0x218BE));
        AddSimplex(v4, v1, v7, v13, v14,  Twiddle(0x417DE));
        AddSimplex(v7, v1, v11, v13, v14, Twiddle(0x71BDE));
        AddSimplex(v1, v4, v8, v13, v14,  Twiddle(0x148DE));
        AddSimplex(v1, v2, v4, v7, v14,   Twiddle(0x1247E));
        AddSimplex(v1, v8, v11, v13, v14, Twiddle(0x18BDE));
        AddSimplex(v1, v2, v7, v11, v14,  Twiddle(0x127BE));
    }
    public void AddCell(Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                        Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11, Vector5 v12, Vector5 v13, Vector5 v14, Vector5 v15, uint aoAll) {
        AddSimplex(v0, v1, v2, v4, v8, aoAll);
        AddSimplex(v8, v4, v12, v13, v14, aoAll);
        AddSimplex(v2, v8, v10, v11, v14, aoAll);
        AddSimplex(v4, v2, v6, v7, v14, aoAll);
        AddSimplex(v8, v1, v9, v11, v13, aoAll);
        AddSimplex(v1, v4, v5, v7, v13, aoAll);
        AddSimplex(v2, v1, v3, v7, v11, aoAll);
        AddSimplex(v7, v11, v13, v14, v15, aoAll);
        AddSimplex(v2, v1, v4, v8, v14, aoAll);
        AddSimplex(v2, v1, v8, v11, v14, aoAll);
        AddSimplex(v4, v1, v7, v13, v14, aoAll);
        AddSimplex(v7, v1, v11, v13, v14, aoAll);
        AddSimplex(v1, v4, v8, v13, v14, aoAll);
        AddSimplex(v1, v2, v4, v7, v14, aoAll);
        AddSimplex(v1, v8, v11, v13, v14, aoAll);
        AddSimplex(v1, v2, v7, v11, v14, aoAll);
    }

    public void AddHalfCell(Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                            Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11) {
        AddSimplex(v1, v8, v4, v0, v10, Twiddle(0x0951B));
        AddSimplex(v1, v4, v0, v10, v3, Twiddle(0x051B2));
        AddSimplex(v0, v4, v10, v3, v6, Twiddle(0x15B27));
        AddSimplex(v0, v10, v3, v6, v2, Twiddle(0x1B273));
        AddSimplex(v1, v9, v5, v8, v11, Twiddle(0x0849A));
        AddSimplex(v1, v5, v8, v11, v3, Twiddle(0x049A2));
        AddSimplex(v8, v5, v11, v3, v7, Twiddle(0x94A26));
        AddSimplex(v8, v11, v3, v7, v10,Twiddle(0x9A26B));
        AddSimplex(v5, v4, v1, v8, v6,  Twiddle(0x45097));
        AddSimplex(v5, v1, v8, v6, v7,  Twiddle(0x40976));
        AddSimplex(v8, v1, v6, v7, v3,  Twiddle(0x90762));
        AddSimplex(v8, v6, v7, v3, v10, Twiddle(0x9762B));
    }
    public void AddHalfCell(Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                            Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11, uint aoAll) {
        AddSimplex(v1, v8, v4, v0, v10, aoAll);
        AddSimplex(v1, v4, v0, v10, v3, aoAll);
        AddSimplex(v0, v4, v10, v3, v6, aoAll);
        AddSimplex(v0, v10, v3, v6, v2, aoAll);
        AddSimplex(v1, v9, v5, v8, v11, aoAll);
        AddSimplex(v1, v5, v8, v11, v3, aoAll);
        AddSimplex(v8, v5, v11, v3, v7, aoAll);
        AddSimplex(v8, v11, v3, v7, v10, aoAll);
        AddSimplex(v5, v4, v1, v8, v6, aoAll);
        AddSimplex(v5, v1, v8, v6, v7, aoAll);
        AddSimplex(v8, v1, v6, v7, v3, aoAll);
        AddSimplex(v8, v6, v7, v3, v10, aoAll);
    }
    public void AddTetraPrism(Vector5 a1, Vector5 b1, Vector5 c1, Vector5 d1, Vector5 a2, Vector5 b2, Vector5 c2, Vector5 d2, uint aoAll = 0) {
        //TODO: Made default tetra-prism have Cell AO.
        AddSimplex(b1, a1, c1, d1, a2, aoAll);
        AddSimplex(b1, c1, d1, a2, b2, aoAll);
        AddSimplex(d1, c1, a2, b2, c2, aoAll);
        AddSimplex(d1, a2, b2, c2, d2, aoAll);
    }
    public void AddTetraPrism(Vector5 a1, Vector5 b1, Vector5 c1, Vector5 d1, Vector5 a2, Vector5 b2, Vector5 c2, Vector5 d2,
                              float a1_c, float b1_c, float c1_c, float d1_c, float a2_c, float b2_c, float c2_c, float d2_c) {
        AddSimplex(b1, a1, c1, d1, a2, b1_c, a1_c, c1_c, d1_c, a2_c);
        AddSimplex(b1, c1, d1, a2, b2, b1_c, c1_c, d1_c, a2_c, b2_c);
        AddSimplex(d1, c1, a2, b2, c2, d1_c, c1_c, a2_c, b2_c, c2_c);
        AddSimplex(d1, a2, b2, c2, d2, d1_c, a2_c, b2_c, c2_c, d2_c);
    }
    public void AddDuoPrism(Vector5 a1, Vector5 b1, Vector5 c1, Vector5 a2, Vector5 b2, Vector5 c2, Vector5 a3, Vector5 b3, Vector5 c3, uint aoAll = 0) {
        //TODO: Find more efficient way to use less simplices without poking
        Vector5 poke = (a1 + a2 + a3 + b1 + b2 + b3 + c1 + c2 + c3) / 9.0f;
        Debug.Log("Poke:" + poke);
        AddFlatHalfCell(a1, a2, b1, b2, c1, c2, poke, aoAll);
        AddFlatHalfCell(a2, a3, b2, b3, c2, c3, poke, aoAll);
        AddFlatHalfCell(a3, a1, b3, b1, c3, c1, poke, aoAll);
        AddFlatHalfCell(a1, b1, a2, b2, a3, b3, poke, aoAll);
        AddFlatHalfCell(b1, c1, b2, c2, b3, c3, poke, aoAll);
        AddFlatHalfCell(c1, a1, c2, a2, c3, a3, poke, aoAll);
    }
    private void AddFlatHalfCell(Vector5 a1, Vector5 a2, Vector5 A1, Vector5 A2, Vector5 b1, Vector5 b2, Vector5 poke, uint aoAll = 0) {
        AddSimplex(A2, a1, b2, a2, poke, aoAll);
        AddSimplex(A1, a1, b1, A2, poke, aoAll);
        AddSimplex(b2, b1, a1, A2, poke, aoAll);
    }

    public void AddCellNormal(Vector5 n, Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                              Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11, Vector5 v12, Vector5 v13, Vector5 v14, Vector5 v15) {
        float nsign = -Vector5.Dot(n, Transform5D.MakeNormal(v1 - v0, v2 - v0, v4 - v0, v8 - v0));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f, "Cell has no volume");
        if (nsign < 0) {
            AddCell(v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15);
        } else {
            AddCell(v1, v0, v3, v2, v5, v4, v7, v6, v9, v8, v11, v10, v13, v12, v15, v14);
        }
    }
    public void AddCellNormal(Vector5 n, Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                              Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11, Vector5 v12, Vector5 v13, Vector5 v14, Vector5 v15, uint aoAll) {
        float nsign = -Vector5.Dot(n, Transform5D.MakeNormal(v1 - v0, v2 - v0, v4 - v0, v8 - v0));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f, "Cell has no volume");
        if (nsign < 0) {
            AddCell(v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, aoAll);
        } else {
            AddCell(v1, v0, v3, v2, v5, v4, v7, v6, v9, v8, v11, v10, v13, v12, v15, v14, aoAll);
        }
    }

    public void AddHalfCellNormal(Vector5 n, Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                                             Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11) {
        float nsign = -Vector5.Dot(n, Transform5D.MakeNormal(v1 - v0, v2 - v0, v4 - v0, v8 - v0));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f, "HalfCell has no volume");
        if (nsign < 0) {
            AddHalfCell(v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11);
        } else {
            AddHalfCell(v1, v0, v3, v2, v5, v4, v7, v6, v9, v8, v11, v10);
        }
    }
    public void AddHalfCellNormal(Vector5 n, Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                                             Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11, uint aoAll) {
        float nsign = -Vector5.Dot(n, Transform5D.MakeNormal(v1 - v0, v2 - v0, v4 - v0, v8 - v0));
        Debug.Assert(Mathf.Abs(nsign) > 1e-12f, "HalfCell has no volume");
        if (nsign < 0) {
            AddHalfCell(v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, aoAll);
        } else {
            AddHalfCell(v1, v0, v3, v2, v5, v4, v7, v6, v9, v8, v11, v10, aoAll);
        }
    }

    public void AddTriangleShadow(Vector5 a, Vector5 b, Vector5 c) {
        //Update wire mesh
        AddWire(a, b);
        AddWire(b, c);
        AddWire(c, a);

        //Check for degenerate triangles
        if (a == b || b == c || c == a) { return; }

        //Update hash-set
        if (InHashSet(a, b, c)) { return; }
        shadowHashset.Add(new Vector5Triple(a, b, c));

        //Update shadow mesh
        sArray.Add(new Shadow5D(a));
        sArray.Add(new Shadow5D(b));
        sArray.Add(new Shadow5D(c));
        sIndices[curSubMesh].Add(sArray.Count - 3);
        sIndices[curSubMesh].Add(sArray.Count - 2);
        sIndices[curSubMesh].Add(sArray.Count - 1);
    }

    public void AddQuadShadow(Vector5 a1, Vector5 a2, Vector5 b1, Vector5 b2) {
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
        sArray.Add(new Shadow5D(a1));
        sArray.Add(new Shadow5D(a2));
        sArray.Add(new Shadow5D(b1));
        sIndices[curSubMesh].Add(sArray.Count - 3);
        sIndices[curSubMesh].Add(sArray.Count - 2);
        sIndices[curSubMesh].Add(sArray.Count - 1);
        Debug.Assert(!InHashSet(b2, a2, b1), "No other quad components should be in the set");
        sArray.Add(new Shadow5D(b2));
        sArray.Add(new Shadow5D(a2));
        sArray.Add(new Shadow5D(b1));
        sIndices[curSubMesh].Add(sArray.Count - 3);
        sIndices[curSubMesh].Add(sArray.Count - 2);
        sIndices[curSubMesh].Add(sArray.Count - 1);
        Debug.Assert(!InHashSet(a2, a1, b2), "No other quad components should be in the set");
        Debug.Assert(!InHashSet(b1, a1, b2), "No other quad components should be in the set");

        //Update hash-set
        shadowHashset.Add(new Vector5Triple(a1, a2, b1));
        shadowHashset.Add(new Vector5Triple(b2, a2, b1));
        shadowHashset.Add(new Vector5Triple(a2, a1, b2));
        shadowHashset.Add(new Vector5Triple(b1, a1, b2));

        //Update wire mesh
        AddWire(a1, a2);
        AddWire(a2, b2);
        AddWire(b2, b1);
        AddWire(b1, a1);
    }

    public void AddCubicShadow(Vector5 a1, Vector5 a2, Vector5 A1, Vector5 A2, Vector5 b1, Vector5 b2, Vector5 B1, Vector5 B2) {
        AddQuadShadow(a1, b1, A1, B1);
        AddQuadShadow(a2, b2, A2, B2);
        AddQuadShadow(a1, a2, b1, b2);
        AddQuadShadow(B1, B2, A1, A2);
        AddQuadShadow(a1, a2, A1, A2);
        AddQuadShadow(B1, B2, b1, b2);
    }

    public void AddCellShadow(Vector5 v0, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4, Vector5 v5, Vector5 v6, Vector5 v7,
                              Vector5 v8, Vector5 v9, Vector5 v10, Vector5 v11, Vector5 v12, Vector5 v13, Vector5 v14, Vector5 v15) {
        AddQuadShadow(v0, v1, v2, v3);
        AddQuadShadow(v0, v1, v4, v5);
        AddQuadShadow(v0, v2, v4, v6);
        AddQuadShadow(v1, v3, v5, v7);
        AddQuadShadow(v2, v3, v6, v7);
        AddQuadShadow(v4, v5, v6, v7);
        AddQuadShadow(v8, v9, v10, v11);
        AddQuadShadow(v8, v9, v12, v13);
        AddQuadShadow(v8, v10, v12, v14);
        AddQuadShadow(v9, v11, v13, v15);
        AddQuadShadow(v10, v11, v14, v15);
        AddQuadShadow(v12, v13, v14, v15);
        AddQuadShadow(v0, v1, v8, v9);
        AddQuadShadow(v1, v3, v9, v11);
        AddQuadShadow(v2, v3, v10, v11);
        AddQuadShadow(v0, v2, v8, v10);
        AddQuadShadow(v0, v4, v8, v12);
        AddQuadShadow(v1, v5, v9, v13);
        AddQuadShadow(v2, v6, v10, v14);
        AddQuadShadow(v3, v7, v11, v15);
        AddQuadShadow(v4, v5, v12, v13);
        AddQuadShadow(v4, v6, v12, v14);
        AddQuadShadow(v5, v7, v13, v15);
        AddQuadShadow(v6, v7, v14, v15);
    }

    public void AddWire(Vector5 a, Vector5 b) {
        if (!InHashSet(a, b) && a != b) {
            wireHashset.Add(new Vector5Double(a, b));
            wArray.Add(new Shadow5D(a));
            wArray.Add(new Shadow5D(b));
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
        mesh.SetVertexBufferParams(vArray.Count, Vertex5D.layout);
        mesh.SetVertexBufferData(vArray, 0, 0, vArray.Count);
        for (int i = 0; i < vIndices.Length; ++i) {
            mesh.SetIndices(vIndices[i], MeshTopology.Triangles, i);
        }
    }

    public void GenerateShadowMesh(Mesh mesh) {
        mesh.Clear();
        mesh.indexFormat = (GetMaxIndex() >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16);
        mesh.subMeshCount = sIndices.Length;
        mesh.SetVertexBufferParams(sArray.Count, Shadow5D.layout);
        mesh.SetVertexBufferData(sArray, 0, 0, sArray.Count);
        for (int i = 0; i < sIndices.Length; ++i) {
            mesh.SetIndices(sIndices[i], MeshTopology.Triangles, i);
        }
    }

    public void GenerateWireMesh(Mesh mesh) {
        mesh.Clear();
        mesh.indexFormat = (GetMaxIndex() >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16);
        mesh.subMeshCount = wIndices.Length;
        mesh.SetVertexBufferParams(wArray.Count, Shadow5D.layout);
        mesh.SetVertexBufferData(wArray, 0, 0, wArray.Count);
        for (int s = 0; s < wIndices.Length; ++s) {
            mesh.SetIndices(wIndices[s], MeshTopology.Lines, s);
        }
    }

    public void AddRawIndices(int[] indices, int[] indices_s, int[] indices_w, int subMesh) {
        Debug.Assert(indices.Length % 9 == 0);
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
    public void AddRawVerts(IEnumerable<Vertex5D> verts, Transform5D tansform5D) {
        Matrix5x5 normalMatrix = tansform5D.matrix.inverse.transpose;
        foreach (Vertex5D vOrig in verts) {
            Vertex5D v = vOrig;
            v.va5 = tansform5D * v.va5;
            v.vb5 = tansform5D * v.vb5;
            v.vc5 = tansform5D * v.vc5;
            v.vd5 = tansform5D * v.vd5;
            v.ve5 = tansform5D * v.ve5;
            v.normal = new PackedNormal(PackNormal(normalMatrix * UnpackNormal(v.normal.pa)),
                                        PackNormal(normalMatrix * UnpackNormal(v.normal.pb)),
                                        PackNormal(normalMatrix * UnpackNormal(v.normal.pc)),
                                        PackNormal(normalMatrix * UnpackNormal(v.normal.pd)),
                                        PackNormal(normalMatrix * UnpackNormal(v.normal.pe)));
            vArray.Add(v);
        }
    }
    public void AddRawVerts(IEnumerable<Vertex5D> verts, IEnumerable<Shadow5D> verts_s, IEnumerable<Shadow5D> verts_w, Transform5D tansform5D) {
        AddRawVerts(verts, tansform5D);
        foreach (Shadow5D vOrig in verts_s) {
            Shadow5D v = vOrig;
            v.vertex5 = tansform5D * v.vertex5;
            sArray.Add(v);
        }
        foreach (Shadow5D wOrig in verts_w) {
            Shadow5D w = wOrig;
            w.vertex5 = tansform5D * w.vertex5;
            wArray.Add(w);
        }
    }

    public static SingleNormal PackNormal(Vector5 n) {
        n /= n.magnitude; //NOTE: the built-in Normalize doesn't work because Unity clips it... insane!
        return new SingleNormal((byte)Mathf.FloorToInt(n.x * 127f + 128.0f),
                                (byte)Mathf.FloorToInt(n.y * 127f + 128.0f),
                                (byte)Mathf.FloorToInt(n.z * 127f + 128.0f),
                                (byte)Mathf.FloorToInt(n.w * 127f + 128.0f),
                                (byte)Mathf.FloorToInt(n.v * 127f + 128.0f));
    }
    public static Vector5 UnpackNormal(SingleNormal p) {
        float x = (float)(p.x) - 128.0f;
        float y = (float)(p.y) - 128.0f;
        float z = (float)(p.z) - 128.0f;
        float w = (float)(p.w) - 128.0f;
        float v = (float)(p.v) - 128.0f;
        return new Vector5(x, y, z, w, v).normalized;
    }
}
