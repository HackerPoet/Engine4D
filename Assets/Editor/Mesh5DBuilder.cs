using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Mesh5DBuilder {
    public Mesh5D mesh5D;
    public Mesh5DBuilder(Mesh5D mesh) { mesh5D = mesh; }

    //Average the normals at every vertex
    public Mesh5DBuilder Smoothen() {
        Smoothen(mesh5D, 0, mesh5D.vArray.Count);
        return this;
    }
    public static void Smoothen(Mesh5D mesh5D, int vIndexStart, int vIndexEnd) {
        //Add up all the normals at every vertex
        List<Mesh5D.Vertex5D> vArray = mesh5D.vArray;
        Debug.Assert(vArray.Count % 5 == 0);
        Dictionary<Vector5, Vector5> vMap = new();
        Dictionary<Tuple<Vector5, Vector5>, Vector5> cMap = new();
        for (int i = vIndexStart; i < vIndexEnd; i += 5) {
            Vector5 a = vArray[i].va5;
            Vector5 b = vArray[i].vb5;
            Vector5 c = vArray[i].vc5;
            Vector5 d = vArray[i].vd5;
            Vector5 e = vArray[i].ve5;
            ProcessSmoothVertex(mesh5D, vMap, cMap, a, Mesh5D.UnpackNormal(vArray[i].normal.pa), b, c, d, e);
            ProcessSmoothVertex(mesh5D, vMap, cMap, b, Mesh5D.UnpackNormal(vArray[i].normal.pb), a, c, d, e);
            ProcessSmoothVertex(mesh5D, vMap, cMap, c, Mesh5D.UnpackNormal(vArray[i].normal.pc), a, b, d, e);
            ProcessSmoothVertex(mesh5D, vMap, cMap, d, Mesh5D.UnpackNormal(vArray[i].normal.pd), a, b, c, e);
            ProcessSmoothVertex(mesh5D, vMap, cMap, e, Mesh5D.UnpackNormal(vArray[i].normal.pe), a, b, c, d);
        }

        //Apply the normals to the mesh
        for (int i = vIndexStart; i < vIndexEnd; ++i) {
            Mesh5D.Vertex5D v = vArray[i];
            v.normal = new Mesh5D.PackedNormal(Mesh5D.PackNormal(GetSmoothNormal(mesh5D, vMap, cMap, v.va5, v.vb5, v.vc5, v.vd5, v.ve5)),
                                               Mesh5D.PackNormal(GetSmoothNormal(mesh5D, vMap, cMap, v.vb5, v.va5, v.vc5, v.vd5, v.ve5)),
                                               Mesh5D.PackNormal(GetSmoothNormal(mesh5D, vMap, cMap, v.vc5, v.va5, v.vb5, v.vd5, v.ve5)),
                                               Mesh5D.PackNormal(GetSmoothNormal(mesh5D, vMap, cMap, v.vd5, v.va5, v.vb5, v.vc5, v.ve5)),
                                               Mesh5D.PackNormal(GetSmoothNormal(mesh5D, vMap, cMap, v.ve5, v.va5, v.vb5, v.vc5, v.vd5)));
            vArray[i] = v;
        }
    }

    //Apply pseudo-random noise to the mesh
    public Mesh5DBuilder Perturb(float scale) {
        for (int s = 0; s < mesh5D.vIndices.Length; ++s) {
            List<int> indices = mesh5D.vIndices[s];
            Debug.Assert(indices.Count % 9 == 0);
            for (int i = 0; i < indices.Count; i += 9) {
                Mesh5D.Vertex5D v = mesh5D.vArray[indices[i]];
                v.va5 += PerturbHash(v.va5) * scale;
                v.vb5 += PerturbHash(v.vb5) * scale;
                v.vc5 += PerturbHash(v.vc5) * scale;
                v.vd5 += PerturbHash(v.vd5) * scale;
                v.ve5 += PerturbHash(v.ve5) * scale;
                v.normal = Mesh5D.PackedNormal.Flat(v.va5, v.vb5, v.vc5, v.vd5, v.ve5);
                mesh5D.vArray[indices[i]] = v;
                mesh5D.vArray[indices[i + 1]] = v;
                mesh5D.vArray[indices[i + 2]] = v;
                mesh5D.vArray[indices[i + 5]] = v;
                mesh5D.vArray[indices[i + 8]] = v;
            }
            List<int> indices_s = mesh5D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; i += 3) {
                Mesh5D.Shadow5D va = mesh5D.sArray[indices_s[i]];
                Mesh5D.Shadow5D vb = mesh5D.sArray[indices_s[i + 1]];
                Mesh5D.Shadow5D vc = mesh5D.sArray[indices_s[i + 2]];
                va.vertex5 += PerturbHash(va.vertex5) * scale;
                vb.vertex5 += PerturbHash(vb.vertex5) * scale;
                vc.vertex5 += PerturbHash(vc.vertex5) * scale;
                mesh5D.sArray[indices_s[i]] = va;
                mesh5D.sArray[indices_s[i + 1]] = vb;
                mesh5D.sArray[indices_s[i + 2]] = vc;
            }
        }
        return this;
    }

    //Apply wave noise to the mesh
    public Mesh5DBuilder Wave(Vector5 freq, Vector5 direction) {
        for (int s = 0; s < mesh5D.vIndices.Length; ++s) {
            List<int> indices = mesh5D.vIndices[s];
            Debug.Assert(indices.Count % 9 == 0);
            for (int i = 0; i < indices.Count; i += 9) {
                Mesh5D.Vertex5D v = mesh5D.vArray[indices[i]];
                v.va5 += PerturbWave(v.va5, freq, direction);
                v.vb5 += PerturbWave(v.vb5, freq, direction);
                v.vc5 += PerturbWave(v.vc5, freq, direction);
                v.vd5 += PerturbWave(v.vd5, freq, direction);
                v.ve5 += PerturbWave(v.ve5, freq, direction);
                v.normal = Mesh5D.PackedNormal.Flat(v.va5, v.vb5, v.vc5, v.vd5, v.ve5);
                mesh5D.vArray[indices[i]] = v;
                mesh5D.vArray[indices[i + 1]] = v;
                mesh5D.vArray[indices[i + 2]] = v;
                mesh5D.vArray[indices[i + 5]] = v;
                mesh5D.vArray[indices[i + 8]] = v;
            }
            List<int> indices_s = mesh5D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; i += 3) {
                Mesh5D.Shadow5D va = mesh5D.sArray[indices_s[i]];
                Mesh5D.Shadow5D vb = mesh5D.sArray[indices_s[i + 1]];
                Mesh5D.Shadow5D vc = mesh5D.sArray[indices_s[i + 2]];
                va.vertex5 += PerturbWave(va.vertex5, freq, direction);
                vb.vertex5 += PerturbWave(vb.vertex5, freq, direction);
                vc.vertex5 += PerturbWave(vc.vertex5, freq, direction);
                mesh5D.sArray[indices_s[i]] = va;
                mesh5D.sArray[indices_s[i + 1]] = vb;
                mesh5D.sArray[indices_s[i + 2]] = vc;
            }
        }
        return this;
    }

    //Apply fluff the mesh
    public Mesh5DBuilder Fluff(float scale) {
        for (int s = 0; s < mesh5D.vIndices.Length; ++s) {
            List<int> indices = mesh5D.vIndices[s];
            Debug.Assert(indices.Count % 9 == 0);
            for (int i = 0; i < indices.Count; i += 9) {
                Mesh5D.Vertex5D v = mesh5D.vArray[indices[i]];
                Vector5 mean = (v.va5 + v.vb5 + v.vc5 + v.vd5 + v.ve5) / 5.0f;
                v.va5 = (v.va5 - mean) * scale + mean;
                v.vb5 = (v.vb5 - mean) * scale + mean;
                v.vc5 = (v.vc5 - mean) * scale + mean;
                v.vd5 = (v.vd5 - mean) * scale + mean;
                v.ve5 = (v.ve5 - mean) * scale + mean;
                v.ao = Mesh5D.Twiddle(0x3065);
                mesh5D.vArray[indices[i]] = v;
                mesh5D.vArray[indices[i + 1]] = v;
                mesh5D.vArray[indices[i + 2]] = v;
                mesh5D.vArray[indices[i + 5]] = v;
                mesh5D.vArray[indices[i + 8]] = v;
            }
        }
        return this;
    }

    //Translate the mesh
    public Mesh5DBuilder Translate(Vector5 delta) {
        for (int s = 0; s < mesh5D.vIndices.Length; ++s) {
            List<int> indices = mesh5D.vIndices[s];
            Debug.Assert(indices.Count % 9 == 0);
            for (int i = 0; i < indices.Count; i += 9) {
                Mesh5D.Vertex5D v = mesh5D.vArray[indices[i]];
                v.va5 += delta;
                v.vb5 += delta;
                v.vc5 += delta;
                v.vd5 += delta;
                v.ve5 += delta;
                mesh5D.vArray[indices[i]] = v;
                mesh5D.vArray[indices[i + 1]] = v;
                mesh5D.vArray[indices[i + 2]] = v;
                mesh5D.vArray[indices[i + 5]] = v;
                mesh5D.vArray[indices[i + 8]] = v;
            }
            List<int> indices_s = mesh5D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; i += 3) {
                Mesh5D.Shadow5D va = mesh5D.sArray[indices_s[i]];
                Mesh5D.Shadow5D vb = mesh5D.sArray[indices_s[i + 1]];
                Mesh5D.Shadow5D vc = mesh5D.sArray[indices_s[i + 2]];
                va.vertex5 += delta;
                vb.vertex5 += delta;
                vc.vertex5 += delta;
                mesh5D.sArray[indices_s[i]] = va;
                mesh5D.sArray[indices_s[i + 1]] = vb;
                mesh5D.sArray[indices_s[i + 2]] = vc;
            }
        }
        return this;
    }

    //Apply wave noise to the mesh
    //NOTE: Flattens normals
    public Mesh5DBuilder Scale(float scale) {
        return Scale(Vector5.one * scale);
    }
    public Mesh5DBuilder Scale(Vector5 scale) {
        for (int s = 0; s < mesh5D.vIndices.Length; ++s) {
            List<int> indices = mesh5D.vIndices[s];
            Debug.Assert(indices.Count % 9 == 0);
            for (int i = 0; i < indices.Count; i += 9) {
                Mesh5D.Vertex5D v = mesh5D.vArray[indices[i]];
                v.va5 = Vector5.Scale(v.va5, scale);
                v.vb5 = Vector5.Scale(v.vb5, scale);
                v.vc5 = Vector5.Scale(v.vc5, scale);
                v.vd5 = Vector5.Scale(v.vd5, scale);
                v.ve5 = Vector5.Scale(v.ve5, scale);
                v.normal = Mesh5D.PackedNormal.Flat(v.va5, v.vb5, v.vc5, v.vd5, v.ve5);
                mesh5D.vArray[indices[i]] = v;
                mesh5D.vArray[indices[i + 1]] = v;
                mesh5D.vArray[indices[i + 2]] = v;
                mesh5D.vArray[indices[i + 5]] = v;
                mesh5D.vArray[indices[i + 8]] = v;
            }
            List<int> indices_s = mesh5D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; i += 3) {
                Mesh5D.Shadow5D va = mesh5D.sArray[indices_s[i]];
                Mesh5D.Shadow5D vb = mesh5D.sArray[indices_s[i + 1]];
                Mesh5D.Shadow5D vc = mesh5D.sArray[indices_s[i + 2]];
                va.vertex5 = Vector5.Scale(va.vertex5, scale);
                vb.vertex5 = Vector5.Scale(vb.vertex5, scale);
                vc.vertex5 = Vector5.Scale(vc.vertex5, scale);
                mesh5D.sArray[indices_s[i]] = va;
                mesh5D.sArray[indices_s[i + 1]] = vb;
                mesh5D.sArray[indices_s[i + 2]] = vc;
            }
            List<int> indices_w = mesh5D.wIndices[s];
            Debug.Assert(indices_w.Count % 2 == 0);
            for (int i = 0; i < indices_w.Count; i += 2) {
                Mesh5D.Shadow5D va = mesh5D.wArray[indices_w[i]];
                Mesh5D.Shadow5D vb = mesh5D.wArray[indices_w[i + 1]];
                va.vertex5 = Vector5.Scale(va.vertex5, scale);
                vb.vertex5 = Vector5.Scale(vb.vertex5, scale);
                mesh5D.wArray[indices_w[i]] = va;
                mesh5D.wArray[indices_w[i + 1]] = vb;
            }
        }
        return this;
    }

    //Rotate the mesh
    //NOTE: Flattens normals
    public Mesh5DBuilder Rotate(float angle, int axis1, int axis2) {
        Matrix5x5 rotate = Transform5D.PlaneRotation(angle, axis1, axis2);
        for (int s = 0; s < mesh5D.vIndices.Length; ++s) {
            List<int> indices = mesh5D.vIndices[s];
            Debug.Assert(indices.Count % 9 == 0);
            for (int i = 0; i < indices.Count; i += 9) {
                Mesh5D.Vertex5D v = mesh5D.vArray[indices[i]];
                v.va5 = rotate * v.va5;
                v.vb5 = rotate * v.vb5;
                v.vc5 = rotate * v.vc5;
                v.vd5 = rotate * v.vd5;
                v.ve5 = rotate * v.ve5;
                v.normal = Mesh5D.PackedNormal.Flat(v.va5, v.vb5, v.vc5, v.vd5, v.ve5);
                mesh5D.vArray[indices[i]] = v;
                mesh5D.vArray[indices[i + 1]] = v;
                mesh5D.vArray[indices[i + 2]] = v;
                mesh5D.vArray[indices[i + 5]] = v;
                mesh5D.vArray[indices[i + 8]] = v;
            }
            List<int> indices_s = mesh5D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; i += 3) {
                Mesh5D.Shadow5D va = mesh5D.sArray[indices_s[i]];
                Mesh5D.Shadow5D vb = mesh5D.sArray[indices_s[i + 1]];
                Mesh5D.Shadow5D vc = mesh5D.sArray[indices_s[i + 2]];
                va.vertex5 = rotate * va.vertex5;
                vb.vertex5 = rotate * vb.vertex5;
                vc.vertex5 = rotate * vc.vertex5;
                mesh5D.sArray[indices_s[i]] = va;
                mesh5D.sArray[indices_s[i + 1]] = vb;
                mesh5D.sArray[indices_s[i + 2]] = vc;
            }
            List<int> indices_w = mesh5D.wIndices[s];
            Debug.Assert(indices_w.Count % 2 == 0);
            for (int i = 0; i < indices_w.Count; i += 2) {
                Mesh5D.Shadow5D va = mesh5D.wArray[indices_w[i]];
                Mesh5D.Shadow5D vb = mesh5D.wArray[indices_w[i + 1]];
                va.vertex5 = rotate * va.vertex5;
                vb.vertex5 = rotate * vb.vertex5;
                mesh5D.wArray[indices_w[i]] = va;
                mesh5D.wArray[indices_w[i + 1]] = vb;
            }
        }
        return this;
    }

    //Flip the normals of the mesh
    public Mesh5DBuilder FlipNormals(bool hasVertexAO = false) {
        for (int s = 0; s < mesh5D.vIndices.Length; ++s) {
            List<int> indices = mesh5D.vIndices[s];
            Debug.Assert(indices.Count % 9 == 0);
            for (int i = 0; i < indices.Count; i += 9) {
                Mesh5D.Vertex5D v = mesh5D.vArray[indices[i]];
                Vector5 temp = v.va5; v.va5 = v.vb5; v.vb5 = temp;
                v.normal = new Mesh5D.PackedNormal(v.normal.pb.Flip(),
                                                   v.normal.pa.Flip(),
                                                   v.normal.pc.Flip(),
                                                   v.normal.pd.Flip(),
                                                   v.normal.pe.Flip());
                if (hasVertexAO) { v.ao = (v.ao & 0xFFFFF000) | ((v.ao & 0x03F) << 6) | ((v.ao & 0xFC0) >> 6); }
                Debug.Assert(indices[i] == indices[i + 3]);
                Debug.Assert(indices[i] == indices[i + 6]);
                Debug.Assert(indices[i + 2] == indices[i + 4]);
                Debug.Assert(indices[i + 5] == indices[i + 7]);
                mesh5D.vArray[indices[i]] = v;
                mesh5D.vArray[indices[i + 1]] = v;
                mesh5D.vArray[indices[i + 2]] = v;
                mesh5D.vArray[indices[i + 5]] = v;
                mesh5D.vArray[indices[i + 8]] = v;
            }
        }
        return this;
    }

    //Stretch and renormalize normal vectors
    public Mesh5DBuilder StretchNormals(Vector5 scale) {
        for (int s = 0; s < mesh5D.vIndices.Length; ++s) {
            List<int> indices = mesh5D.vIndices[s];
            Debug.Assert(indices.Count % 9 == 0);
            for (int i = 0; i < indices.Count; i += 9) {
                Mesh5D.Vertex5D v = mesh5D.vArray[indices[i]];
                v.normal = new Mesh5D.PackedNormal(Mesh5D.PackNormal(Vector5.Scale(Mesh5D.UnpackNormal(v.normal.pa), scale)),
                                                   Mesh5D.PackNormal(Vector5.Scale(Mesh5D.UnpackNormal(v.normal.pb), scale)),
                                                   Mesh5D.PackNormal(Vector5.Scale(Mesh5D.UnpackNormal(v.normal.pc), scale)),
                                                   Mesh5D.PackNormal(Vector5.Scale(Mesh5D.UnpackNormal(v.normal.pd), scale)),
                                                   Mesh5D.PackNormal(Vector5.Scale(Mesh5D.UnpackNormal(v.normal.pd), scale)));
                mesh5D.vArray[indices[i]] = v;
                mesh5D.vArray[indices[i + 1]] = v;
                mesh5D.vArray[indices[i + 2]] = v;
                mesh5D.vArray[indices[i + 5]] = v;
                mesh5D.vArray[indices[i + 8]] = v;
            }
        }
        return this;
    }

    //Merge vertices that are too close together
    public Mesh5DBuilder MergeVerts(float minDist) {
        //Add up all the normals at every vertex
        float minDistSq = minDist * minDist;
        List<Mesh5D.Vertex5D> vArray = mesh5D.vArray;
        Debug.Assert(vArray.Count % 5 == 0);
        List<Vector5> uniqueVerts = new();
        for (int i = 0; i < vArray.Count; ++i) {
            Mesh5D.Vertex5D v = vArray[i];
            v.va5 = UpdateNearestVert(v.va5, uniqueVerts, minDistSq);
            v.vb5 = UpdateNearestVert(v.vb5, uniqueVerts, minDistSq);
            v.vc5 = UpdateNearestVert(v.vc5, uniqueVerts, minDistSq);
            v.vd5 = UpdateNearestVert(v.vd5, uniqueVerts, minDistSq);
            v.ve5 = UpdateNearestVert(v.ve5, uniqueVerts, minDistSq);
            vArray[i] = v;
        }
        return this;
    }

    public Mesh5DBuilder VertexAOAxis(int axis, float minVal = 0.0f, float maxVal = 1.0f) {
        //Get bounds of axes
        float minBound = float.MaxValue;
        float maxBound = float.MinValue;
        for (int i = 0; i < mesh5D.vArray.Count; i += 5) {
            Mesh5D.Vertex5D v = mesh5D.vArray[i];
            minBound = Mathf.Min(minBound, v.va5[axis]);
            minBound = Mathf.Min(minBound, v.vb5[axis]);
            minBound = Mathf.Min(minBound, v.vc5[axis]);
            minBound = Mathf.Min(minBound, v.vd5[axis]);
            minBound = Mathf.Min(minBound, v.ve5[axis]);
            maxBound = Mathf.Max(maxBound, v.va5[axis]);
            maxBound = Mathf.Max(maxBound, v.vb5[axis]);
            maxBound = Mathf.Max(maxBound, v.vc5[axis]);
            maxBound = Mathf.Max(maxBound, v.vd5[axis]);
            maxBound = Mathf.Max(maxBound, v.ve5[axis]);
        }

        //Apply the vertex AO
        for (int i = 0; i < mesh5D.vArray.Count; i += 5) {
            Mesh5D.Vertex5D v = mesh5D.vArray[i];
            uint ua = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.va5[axis] - minBound) / (maxBound - minBound))) * 63.0f);
            uint ub = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.vb5[axis] - minBound) / (maxBound - minBound))) * 63.0f);
            uint uc = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.vc5[axis] - minBound) / (maxBound - minBound))) * 63.0f);
            uint ud = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.vd5[axis] - minBound) / (maxBound - minBound))) * 63.0f);
            uint ue = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.ve5[axis] - minBound) / (maxBound - minBound))) * 63.0f);
            v.ao = ua | (ub << 6) | (uc << 12) | (ud << 18) | (ue << 24);
            mesh5D.vArray[i] = v;
            mesh5D.vArray[i + 1] = v;
            mesh5D.vArray[i + 2] = v;
            mesh5D.vArray[i + 3] = v;
            mesh5D.vArray[i + 4] = v;
        }
        return this;
    }

    public Mesh5DBuilder VertexAORadial(float minVal = 0.0f, float maxVal = 1.0f) {
        //Get bounds of axes
        float minBound = float.MaxValue;
        float maxBound = float.MinValue;
        for (int i = 0; i < mesh5D.vArray.Count; i += 5) {
            Mesh5D.Vertex5D v = mesh5D.vArray[i];
            minBound = Mathf.Min(minBound, v.va5.magnitude);
            minBound = Mathf.Min(minBound, v.vb5.magnitude);
            minBound = Mathf.Min(minBound, v.vc5.magnitude);
            minBound = Mathf.Min(minBound, v.vd5.magnitude);
            minBound = Mathf.Min(minBound, v.ve5.magnitude);
            maxBound = Mathf.Max(maxBound, v.va5.magnitude);
            maxBound = Mathf.Max(maxBound, v.vb5.magnitude);
            maxBound = Mathf.Max(maxBound, v.vc5.magnitude);
            maxBound = Mathf.Max(maxBound, v.vd5.magnitude);
            maxBound = Mathf.Max(maxBound, v.ve5.magnitude);
        }

        //Apply the vertex AO
        for (int i = 0; i < mesh5D.vArray.Count; i += 5) {
            Mesh5D.Vertex5D v = mesh5D.vArray[i];
            uint ua = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.va5.magnitude - minBound) / (maxBound - minBound))) * 63.0f);
            uint ub = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.vb5.magnitude - minBound) / (maxBound - minBound))) * 63.0f);
            uint uc = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.vc5.magnitude - minBound) / (maxBound - minBound))) * 63.0f);
            uint ud = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.vd5.magnitude - minBound) / (maxBound - minBound))) * 63.0f);
            uint ue = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.ve5.magnitude - minBound) / (maxBound - minBound))) * 63.0f);
            v.ao = ua | (ub << 6) | (uc << 12) | (ud << 18) | (ue << 24);
            mesh5D.vArray[i] = v;
            mesh5D.vArray[i + 1] = v;
            mesh5D.vArray[i + 2] = v;
            mesh5D.vArray[i + 3] = v;
            mesh5D.vArray[i + 4] = v;
        }
        return this;
    }

    public Mesh5DBuilder Merge(Mesh5DBuilder other, bool newSubmeshs=true) {
        if (!newSubmeshs) { mesh5D.curSubMesh -= 1; }
        for (int s = 0; s < other.mesh5D.vIndices.Length; ++s) {
            List<int> vIndices = other.mesh5D.vIndices[s];
            List<int> sIndices = other.mesh5D.sIndices[s];
            List<int> wIndices = other.mesh5D.wIndices[s];
            mesh5D.AddRawIndices(vIndices.ToArray(), sIndices.ToArray(), wIndices.ToArray(), mesh5D.curSubMesh);
            mesh5D.AddRawVerts(other.mesh5D.vArray, other.mesh5D.sArray, other.mesh5D.wArray, Transform5D.identity);
            mesh5D.NextSubmesh();
        }
        return this;
    }

    //Generate the mesh and shadow
    public Mesh5DBuilder Build(string name) {
        string path = "Assets/Meshes5D/" + name + ".mesh";
        mesh5D.GenerateMesh(Mesh4DBuilder.GetMesh(path));
        if (mesh5D.sArray.Count > 0) {
            mesh5D.GenerateShadowMesh(Mesh4DBuilder.GetShadowMesh(path));
            mesh5D.GenerateWireMesh(Mesh4DBuilder.GetWireMesh(path));
        }
        Debug.Log("Generated " + name + ".mesh");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return this;
    }

    private static void ProcessSmoothVertex(Mesh5D mesh5D, Dictionary<Vector5, Vector5> vMap, Dictionary<Tuple<Vector5, Vector5>, Vector5> cMap,
                                     Vector5 a, Vector5 n, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4) {
        for (int i = 0; i < mesh5D.conePoints.Count; ++i) {
            if (a == mesh5D.conePoints[i]) {
                Tuple<Vector5, Vector5> pair1 = new Tuple<Vector5, Vector5>(a, v1);
                Tuple<Vector5, Vector5> pair2 = new Tuple<Vector5, Vector5>(a, v2);
                Tuple<Vector5, Vector5> pair3 = new Tuple<Vector5, Vector5>(a, v3);
                Tuple<Vector5, Vector5> pair4 = new Tuple<Vector5, Vector5>(a, v4);
                if (cMap.ContainsKey(pair1)) { cMap[pair1] += n; } else { cMap.Add(pair1, n); }
                if (cMap.ContainsKey(pair2)) { cMap[pair2] += n; } else { cMap.Add(pair2, n); }
                if (cMap.ContainsKey(pair3)) { cMap[pair3] += n; } else { cMap.Add(pair3, n); }
                if (cMap.ContainsKey(pair4)) { cMap[pair4] += n; } else { cMap.Add(pair4, n); }
                return;
            }
        }
        if (vMap.ContainsKey(a)) {
            vMap[a] += n;
        } else {
            vMap.Add(a, n);
        }
    }

    private static Vector5 GetSmoothNormal(Mesh5D mesh5D, Dictionary<Vector5, Vector5> vMap, Dictionary<Tuple<Vector5, Vector5>, Vector5> cMap,
                                    Vector5 a, Vector5 v1, Vector5 v2, Vector5 v3, Vector5 v4) {
        for (int i = 0; i < mesh5D.conePoints.Count; ++i) {
            if (a == mesh5D.conePoints[i]) {
                Tuple<Vector5, Vector5> pair1 = new Tuple<Vector5, Vector5>(a, v1);
                Tuple<Vector5, Vector5> pair2 = new Tuple<Vector5, Vector5>(a, v2);
                Tuple<Vector5, Vector5> pair3 = new Tuple<Vector5, Vector5>(a, v3);
                Tuple<Vector5, Vector5> pair4 = new Tuple<Vector5, Vector5>(a, v4);
                return cMap[pair1] + cMap[pair2] + cMap[pair3] + cMap[pair4];
            }
        }
        return vMap[a];
    }

    private static Vector5 PerturbHash(Vector5 a) {
        uint x = (uint)Mathf.FloorToInt((((a.x * 137.9251f) % 1.0f + 1.0f) % 1.0f) * 65535.0f);
        uint y = (uint)Mathf.FloorToInt((((a.y * 721.1217f) % 1.0f + 1.0f) % 1.0f) * 65535.0f);
        uint z = (uint)Mathf.FloorToInt((((a.z * 345.6782f) % 1.0f + 1.0f) % 1.0f) * 65535.0f);
        uint w = (uint)Mathf.FloorToInt((((a.w * 156.2573f) % 1.0f + 1.0f) % 1.0f) * 65535.0f);
        uint v = (uint)Mathf.FloorToInt((((a.v * 281.1094f) % 1.0f + 1.0f) % 1.0f) * 65535.0f);
        PseudoRandom._seed = x ^ (y << 3) ^ (z << 6) ^ (w << 9) ^ (v << 12);
        return PseudoRandom.Ball5D();
    }

    private static Vector5 PerturbWave(Vector5 v, Vector5 freq, Vector5 dir) {
        float sx = Mathf.Sin(v.x * freq.x);
        float sy = Mathf.Sin(v.y * freq.y);
        float sz = Mathf.Sin(v.z * freq.z);
        float sw = Mathf.Sin(v.w * freq.w);
        float sv = Mathf.Sin(v.v * freq.v);
        return dir * (sx + sy + sz + sw + sv);
    }

    private static Vector5 UpdateNearestVert(Vector5 v, List<Vector5> uniqueVerts, float minDistSq) {
        for (int i = 0; i < uniqueVerts.Count; ++i) {
            if ((uniqueVerts[i] - v).sqrMagnitude <= minDistSq) {
                return uniqueVerts[i];
            }
        }
        uniqueVerts.Add(v);
        return v;
    }
}
