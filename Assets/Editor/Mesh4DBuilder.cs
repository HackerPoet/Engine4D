using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Mesh4DBuilder {
    public Mesh4D mesh4D;
    public Mesh4DBuilder(Mesh4D mesh) { mesh4D = mesh; }

    //Average the normals at every vertex
    public Mesh4DBuilder Smoothen(bool separateSubmeshes = false, float groundBlendHeight = -99999.0f) {
        if (separateSubmeshes) {
            for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
                List<int> indicies = mesh4D.vIndices[s];
                Smoothen(mesh4D, indicies[0], indicies[indicies.Count - 1], groundBlendHeight);
            }
        } else {
            Smoothen(mesh4D, 0, mesh4D.vArray.Count, groundBlendHeight);
        }
        return this;
    }
    public static void Smoothen(Mesh4D mesh4D, int vIndexStart, int vIndexEnd, float groundBlendHeight = -99999.0f) {
        //Add up all the normals at every vertex
        List<Mesh4D.Vertex4D> vArray = mesh4D.vArray;
        Debug.Assert(vArray.Count % 4 == 0);
        Dictionary<Vector4, Vector4> vMap = new();
        Dictionary<Tuple<Vector4, Vector4>, Vector4> cMap = new();
        for (int i = vIndexStart; i < vIndexEnd; i += 4) {
            Vector4 a = vArray[i].va;
            Vector4 b = vArray[i].vb;
            Vector4 c = vArray[i].vc;
            Vector4 d = vArray[i].vd;
            ProcessSmoothVertex(mesh4D, vMap, cMap, a, Mesh4D.UnpackNormal(vArray[i].normal.a), b, c, d);
            ProcessSmoothVertex(mesh4D, vMap, cMap, b, Mesh4D.UnpackNormal(vArray[i].normal.b), a, c, d);
            ProcessSmoothVertex(mesh4D, vMap, cMap, c, Mesh4D.UnpackNormal(vArray[i].normal.c), a, b, d);
            ProcessSmoothVertex(mesh4D, vMap, cMap, d, Mesh4D.UnpackNormal(vArray[i].normal.d), a, b, c);
        }

        //Apply the normals to the mesh
        for (int i = vIndexStart; i < vIndexEnd; ++i) {
            Mesh4D.Vertex4D v = vArray[i];
            v.normal = new Mesh4D.PackedNormal(Mesh4D.PackNormal(GetSmoothNormal(mesh4D, vMap, cMap, v.va, v.vb, v.vc, v.vd, groundBlendHeight)),
                                               Mesh4D.PackNormal(GetSmoothNormal(mesh4D, vMap, cMap, v.vb, v.va, v.vc, v.vd, groundBlendHeight)),
                                               Mesh4D.PackNormal(GetSmoothNormal(mesh4D, vMap, cMap, v.vc, v.va, v.vb, v.vd, groundBlendHeight)),
                                               Mesh4D.PackNormal(GetSmoothNormal(mesh4D, vMap, cMap, v.vd, v.va, v.vb, v.vc, groundBlendHeight)));
            vArray[i] = v;
        }
    }

    //Average the normals at every vertex
    public Mesh4DBuilder SmoothenSubmeshes(float groundBlendHeight = -99999.0f) {
        //Add up all the normals at every vertex
        List<Mesh4D.Vertex4D> vArray = mesh4D.vArray;
        Debug.Assert(vArray.Count % 4 == 0);
        Dictionary<Vector4, Vector4> vMap = new();
        Dictionary<Tuple<Vector4, Vector4>, Vector4> cMap = new();
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            vMap.Clear();
            cMap.Clear();
            List<int> indices = mesh4D.vIndices[s];
            Debug.Assert(indices.Count % 6 == 0);
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                Vector4 a = v.va;
                Vector4 b = v.vb;
                Vector4 c = v.vc;
                Vector4 d = v.vd;
                ProcessSmoothVertex(mesh4D, vMap, cMap, a, Mesh4D.UnpackNormal(v.normal.a), b, c, d);
                ProcessSmoothVertex(mesh4D, vMap, cMap, b, Mesh4D.UnpackNormal(v.normal.b), a, c, d);
                ProcessSmoothVertex(mesh4D, vMap, cMap, c, Mesh4D.UnpackNormal(v.normal.c), a, b, d);
                ProcessSmoothVertex(mesh4D, vMap, cMap, d, Mesh4D.UnpackNormal(v.normal.d), a, b, c);
            }

            //Apply the normals to the mesh
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                v.normal = new Mesh4D.PackedNormal(Mesh4D.PackNormal(GetSmoothNormal(mesh4D, vMap, cMap, v.va, v.vb, v.vc, v.vd, groundBlendHeight)),
                                                   Mesh4D.PackNormal(GetSmoothNormal(mesh4D, vMap, cMap, v.vb, v.va, v.vc, v.vd, groundBlendHeight)),
                                                   Mesh4D.PackNormal(GetSmoothNormal(mesh4D, vMap, cMap, v.vc, v.va, v.vb, v.vd, groundBlendHeight)),
                                                   Mesh4D.PackNormal(GetSmoothNormal(mesh4D, vMap, cMap, v.vd, v.va, v.vb, v.vc, groundBlendHeight)));
                vArray[indices[i]] = v;
                vArray[indices[i + 1]] = v;
                vArray[indices[i + 2]] = v;
                vArray[indices[i + 5]] = v;
            }
        }
        return this;
    }

    //Average the normals up to a certain radius around every vertex
    public Mesh4DBuilder AreaSmoothen(float radius) {
        //Create list of all normals at every vertex
        List<Mesh4D.Vertex4D> vArray = mesh4D.vArray;
        Debug.Assert(vArray.Count % 4 == 0);
        List<Tuple<Vector4, Vector4>> normalPairs = new();
        for (int i = 0; i < vArray.Count; i += 4) {
            Vector4 a = vArray[i].va;
            Vector4 b = vArray[i].vb;
            Vector4 c = vArray[i].vc;
            Vector4 d = vArray[i].vd;
            normalPairs.Add(new Tuple<Vector4, Vector4>(a, Mesh4D.UnpackNormal(vArray[i].normal.a)));
            normalPairs.Add(new Tuple<Vector4, Vector4>(b, Mesh4D.UnpackNormal(vArray[i].normal.b)));
            normalPairs.Add(new Tuple<Vector4, Vector4>(c, Mesh4D.UnpackNormal(vArray[i].normal.c)));
            normalPairs.Add(new Tuple<Vector4, Vector4>(d, Mesh4D.UnpackNormal(vArray[i].normal.d)));
        }

        //Apply the normals to the mesh
        for (int i = 0; i < vArray.Count; i += 4) {
            Mesh4D.Vertex4D v = vArray[i];
            v.normal = new Mesh4D.PackedNormal(Mesh4D.PackNormal(AreaNormal(normalPairs, v.va, radius)),
                                               Mesh4D.PackNormal(AreaNormal(normalPairs, v.vb, radius)),
                                               Mesh4D.PackNormal(AreaNormal(normalPairs, v.vc, radius)),
                                               Mesh4D.PackNormal(AreaNormal(normalPairs, v.vd, radius)));
            vArray[i] = v;
            vArray[i+1] = v;
            vArray[i+2] = v;
            vArray[i+3] = v;
        }
        return this;
    }

    //Apply pseudo-random noise to the mesh
    public Mesh4DBuilder Perturb(float scale) {
        return Perturb(scale, scale, scale, scale);
    }
    public Mesh4DBuilder Perturb(float scaleX, float scaleY, float scaleZ, float scaleW) {
        Vector4 scale = new Vector4(scaleX, scaleY, scaleZ, scaleW);
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> indices = mesh4D.vIndices[s];
            Debug.Assert(indices.Count % 6 == 0);
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                v.va += Vector4.Scale(PerturbHash(v.va), scale);
                v.vb += Vector4.Scale(PerturbHash(v.vb), scale);
                v.vc += Vector4.Scale(PerturbHash(v.vc), scale);
                v.vd += Vector4.Scale(PerturbHash(v.vd), scale);
                v.normal = Mesh4D.PackedNormal.Flat(v.va, v.vb, v.vc, v.vd);
                mesh4D.vArray[indices[i]] = v;
                mesh4D.vArray[indices[i + 1]] = v;
                mesh4D.vArray[indices[i + 2]] = v;
                mesh4D.vArray[indices[i + 5]] = v;
            }
            List<int> indices_s = mesh4D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.sArray[indices_s[i]];
                v.vertex += Vector4.Scale(PerturbHash(v.vertex), scale);
                mesh4D.sArray[indices_s[i]] = v;
            }
            List<int> indices_w = mesh4D.wIndices[s];
            Debug.Assert(indices_w.Count % 2 == 0);
            for (int i = 0; i < indices_w.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.wArray[indices_w[i]];
                v.vertex += Vector4.Scale(PerturbHash(v.vertex), scale);
                mesh4D.wArray[indices_w[i]] = v;
            }
        }
        return this;
    }

    //Apply wave noise to the mesh
    public Mesh4DBuilder Wave(Vector4 freq, Vector4 direction) {
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> indices = mesh4D.vIndices[s];
            Debug.Assert(indices.Count % 6 == 0);
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                v.va += PerturbWave(v.va, freq, direction);
                v.vb += PerturbWave(v.vb, freq, direction);
                v.vc += PerturbWave(v.vc, freq, direction);
                v.vd += PerturbWave(v.vd, freq, direction);
                v.normal = Mesh4D.PackedNormal.Flat(v.va, v.vb, v.vc, v.vd);
                mesh4D.vArray[indices[i]] = v;
                mesh4D.vArray[indices[i + 1]] = v;
                mesh4D.vArray[indices[i + 2]] = v;
                mesh4D.vArray[indices[i + 5]] = v;
            }
            List<int> indices_s = mesh4D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.sArray[indices_s[i]];
                v.vertex += PerturbWave(v.vertex, freq, direction);
                mesh4D.sArray[indices_s[i]] = v;
            }
            List<int> indices_w = mesh4D.wIndices[s];
            Debug.Assert(indices_w.Count % 2 == 0);
            for (int i = 0; i < indices_w.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.wArray[indices_w[i]];
                v.vertex += PerturbWave(v.vertex, freq, direction);
                mesh4D.wArray[indices_w[i]] = v;
            }
        }
        return this;
    }

    //Translate the mesh
    public Mesh4DBuilder Translate(float x, float y, float z, float w) {
        return Affine(Matrix4x4.identity, new Vector4(x, y, z, w));
    }
    //Rotate the mesh
    public Mesh4DBuilder Rotate(float angle, int axis1, int axis2) {
        return Affine(Transform4D.PlaneRotation(angle, axis1, axis2), Vector4.zero);
    }
    //Scale the mesh
    public Mesh4DBuilder Scale(float scaleX, float scaleY, float scaleZ, float scaleW) {
        return Affine(Transform4D.ScaleMatrix(new Vector4(scaleX, scaleY, scaleZ, scaleW)), Vector4.zero);
    }
    //Affine transform y = Ax + b
    public Mesh4DBuilder Affine(Matrix4x4 A, Vector4 b) {
        return Homographic(A, b, Vector4.zero, 1.0f);
    }
    //Homographic transform y = (Ax + b) / (cx + d)
    public Mesh4DBuilder Homographic(Matrix4x4 A, Vector4 b, Vector4 c, float d) {
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> indices = mesh4D.vIndices[s];
            Debug.Assert(indices.Count % 6 == 0);
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                v.va = (A * v.va + b) / (Vector4.Dot(v.va, c) + d);
                v.vb = (A * v.vb + b) / (Vector4.Dot(v.vb, c) + d);
                v.vc = (A * v.vc + b) / (Vector4.Dot(v.vc, c) + d);
                v.vd = (A * v.vd + b) / (Vector4.Dot(v.vd, c) + d);
                v.normal = Mesh4D.PackedNormal.Flat(v.va, v.vb, v.vc, v.vd);
                mesh4D.vArray[indices[i]] = v;
                mesh4D.vArray[indices[i + 1]] = v;
                mesh4D.vArray[indices[i + 2]] = v;
                mesh4D.vArray[indices[i + 5]] = v;
            }
            List<int> indices_s = mesh4D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.sArray[indices_s[i]];
                v.vertex = (A * v.vertex + b) / (Vector4.Dot(v.vertex, c) + d);
                mesh4D.sArray[indices_s[i]] = v;
            }
            List<int> indices_w = mesh4D.wIndices[s];
            Debug.Assert(indices_w.Count % 2 == 0);
            for (int i = 0; i < indices_w.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.wArray[indices_w[i]];
                v.vertex = (A * v.vertex + b) / (Vector4.Dot(v.vertex, c) + d);
                mesh4D.wArray[indices_w[i]] = v;
            }
        }
        return this;
    }

    //Twist the mesh
    public Mesh4DBuilder Twist(float angle, int axis1, int axis2, Vector4 pivot, int iterpAxis) {
        Matrix4x4 rotate = Transform4D.PlaneRotation(angle, axis1, axis2);
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> indices = mesh4D.vIndices[s];
            Debug.Assert(indices.Count % 6 == 0);
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                v.va = Transform4D.Slerp(Matrix4x4.identity, rotate, v.va[iterpAxis]) * (v.va - pivot) + pivot;
                v.vb = Transform4D.Slerp(Matrix4x4.identity, rotate, v.vb[iterpAxis]) * (v.vb - pivot) + pivot;
                v.vc = Transform4D.Slerp(Matrix4x4.identity, rotate, v.vc[iterpAxis]) * (v.vc - pivot) + pivot;
                v.vd = Transform4D.Slerp(Matrix4x4.identity, rotate, v.vd[iterpAxis]) * (v.vd - pivot) + pivot;
                v.normal = Mesh4D.PackedNormal.Flat(v.va, v.vb, v.vc, v.vd);
                mesh4D.vArray[indices[i]] = v;
                mesh4D.vArray[indices[i + 1]] = v;
                mesh4D.vArray[indices[i + 2]] = v;
                mesh4D.vArray[indices[i + 5]] = v;
            }
            List<int> indices_s = mesh4D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.sArray[indices_s[i]];
                v.vertex = Transform4D.Slerp(Matrix4x4.identity, rotate, v.vertex[iterpAxis]) * (v.vertex - pivot) + pivot;
                mesh4D.sArray[indices_s[i]] = v;
            }
            List<int> indices_w = mesh4D.wIndices[s];
            Debug.Assert(indices_w.Count % 2 == 0);
            for (int i = 0; i < indices_w.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.wArray[indices_w[i]];
                v.vertex = Transform4D.Slerp(Matrix4x4.identity, rotate, v.vertex[iterpAxis]) * (v.vertex - pivot) + pivot;
                mesh4D.wArray[indices_w[i]] = v;
            }
        }
        return this;
    }

    //Apply fluff the mesh
    public Mesh4DBuilder Fluff(float scale) {
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> indices = mesh4D.vIndices[s];
            Debug.Assert(indices.Count % 6 == 0);
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                Vector4 mean = (v.va + v.vb + v.vc + v.vd) / 4.0f;
                v.va = (v.va - mean) * scale + mean;
                v.vb = (v.vb - mean) * scale + mean;
                v.vc = (v.vc - mean) * scale + mean;
                v.vd = (v.vd - mean) * scale + mean;
                v.ao = Mesh4D.Twiddle(0x3065);
                mesh4D.vArray[indices[i]] = v;
                mesh4D.vArray[indices[i + 1]] = v;
                mesh4D.vArray[indices[i + 2]] = v;
                mesh4D.vArray[indices[i + 5]] = v;
            }
        }
        return this;
    }

    //Flip the normals of the mesh
    public Mesh4DBuilder FlipNormals(bool hasVertexAO = false) {
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> indices = mesh4D.vIndices[s];
            Debug.Assert(indices.Count % 6 == 0);
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                Vector4 temp = v.va; v.va = v.vb; v.vb = temp;
                v.normal = new Mesh4D.PackedNormal(Mesh4D.PackedNormal.Flip(v.normal.b),
                                                   Mesh4D.PackedNormal.Flip(v.normal.a),
                                                   Mesh4D.PackedNormal.Flip(v.normal.c),
                                                   Mesh4D.PackedNormal.Flip(v.normal.d));
                if (hasVertexAO) { v.ao = (v.ao & 0xFFFF0000) | ((v.ao & 0x00FF) << 8) | ((v.ao & 0xFF00) >> 8); }
                Debug.Assert(indices[i] == indices[i + 3]);
                Debug.Assert(indices[i + 2] == indices[i + 4]);
                mesh4D.vArray[indices[i]] = v;
                mesh4D.vArray[indices[i + 1]] = v;
                mesh4D.vArray[indices[i + 2]] = v;
                mesh4D.vArray[indices[i + 5]] = v;
            }
        }
        return this;
    }

    //Stretch and renormalize normal vectors
    public Mesh4DBuilder StretchNormals(Vector4 scale) {
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> indices = mesh4D.vIndices[s];
            Debug.Assert(indices.Count % 6 == 0);
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                v.normal.a = Mesh4D.PackNormal(Vector4.Scale(Mesh4D.UnpackNormal(v.normal.a), scale));
                v.normal.b = Mesh4D.PackNormal(Vector4.Scale(Mesh4D.UnpackNormal(v.normal.b), scale));
                v.normal.c = Mesh4D.PackNormal(Vector4.Scale(Mesh4D.UnpackNormal(v.normal.c), scale));
                v.normal.d = Mesh4D.PackNormal(Vector4.Scale(Mesh4D.UnpackNormal(v.normal.d), scale));
                mesh4D.vArray[indices[i]] = v;
                mesh4D.vArray[indices[i + 1]] = v;
                mesh4D.vArray[indices[i + 2]] = v;
                mesh4D.vArray[indices[i + 5]] = v;
            }
        }
        return this;
    }

    //Merge vertices that are too close together
    public Mesh4DBuilder MergeVerts(float minDist) {
        //Add up all the normals at every vertex
        float minDistSq = minDist * minDist;
        List<Mesh4D.Vertex4D> vArray = mesh4D.vArray;
        Debug.Assert(vArray.Count % 4 == 0);
        List<Vector4> uniqueVerts = new();
        for (int i = 0; i < vArray.Count; ++i) {
            Mesh4D.Vertex4D v = vArray[i];
            UpdateNearestVert(ref v.va, uniqueVerts, minDistSq);
            UpdateNearestVert(ref v.vb, uniqueVerts, minDistSq);
            UpdateNearestVert(ref v.vc, uniqueVerts, minDistSq);
            UpdateNearestVert(ref v.vd, uniqueVerts, minDistSq);
            vArray[i] = v;
        }
        return this;
    }

    public Mesh4DBuilder VertexAOAxis(int axis, float minVal = 0.0f, float maxVal = 1.0f) {
        //Get bounds of axes
        float minBound = float.MaxValue;
        float maxBound = float.MinValue;
        for (int i = 0; i < mesh4D.vArray.Count; i += 4) {
            Mesh4D.Vertex4D v = mesh4D.vArray[i];
            minBound = Mathf.Min(minBound, v.va[axis]);
            minBound = Mathf.Min(minBound, v.vb[axis]);
            minBound = Mathf.Min(minBound, v.vc[axis]);
            minBound = Mathf.Min(minBound, v.vd[axis]);
            maxBound = Mathf.Max(maxBound, v.va[axis]);
            maxBound = Mathf.Max(maxBound, v.vb[axis]);
            maxBound = Mathf.Max(maxBound, v.vc[axis]);
            maxBound = Mathf.Max(maxBound, v.vd[axis]);
        }

        //Apply the vertex AO
        for (int i = 0; i < mesh4D.vArray.Count; i += 4) {
            Mesh4D.Vertex4D v = mesh4D.vArray[i];
            uint ua = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.va[axis] - minBound) / (maxBound - minBound))) * 255.0f);
            uint ub = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.vb[axis] - minBound) / (maxBound - minBound))) * 255.0f);
            uint uc = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.vc[axis] - minBound) / (maxBound - minBound))) * 255.0f);
            uint ud = (uint)(Mathf.Clamp01(Mathf.Lerp(minVal, maxVal, (v.vd[axis] - minBound) / (maxBound - minBound))) * 255.0f);
            v.ao = ua | (ub << 8) | (uc << 16) | (ud << 24);
            mesh4D.vArray[i] = v;
            mesh4D.vArray[i + 1] = v;
            mesh4D.vArray[i + 2] = v;
            mesh4D.vArray[i + 3] = v;
        }
        return this;
    }

    public Mesh4DBuilder Spike(float distMul) {
        Mesh4D newMesh = new Mesh4D(mesh4D.vIndices.Length);
        List<Mesh4D.Vertex4D> verts = mesh4D.vArray;
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> vIndicies = mesh4D.vIndices[s];
            Debug.Assert(vIndicies.Count % 6 == 0);
            int endIx = vIndicies.Count;
            for (int i = 0; i < endIx; i += 6) {
                Mesh4D.Vertex4D tetra = verts[vIndicies[i]];
                Vector4 a = tetra.va;
                Vector4 b = tetra.vb;
                Vector4 c = tetra.vc;
                Vector4 d = tetra.vd;
                Vector4 e =  (a + b + c + d) * distMul / 4.0f;
                newMesh.AddTetrahedronNormal(e, a, b, c, e);
                newMesh.AddTetrahedronNormal(e, a, b, e, d);
                newMesh.AddTetrahedronNormal(e, a, e, c, d);
                newMesh.AddTetrahedronNormal(e, e, b, c, d);
                newMesh.AddTriangleShadow(a, b, e);
                newMesh.AddTriangleShadow(a, c, e);
                newMesh.AddTriangleShadow(a, d, e);
                newMesh.AddTriangleShadow(b, c, e);
                newMesh.AddTriangleShadow(b, d, e);
                newMesh.AddTriangleShadow(c, d, e);
            }
            newMesh.NextSubmesh();
        }
        mesh4D = newMesh;
        return this;
    }

    public Mesh4DBuilder GeoPoke(bool normalize = true, bool extraPoke = false) {
        Mesh4D newMesh = new Mesh4D(mesh4D.vIndices.Length);
        List<Mesh4D.Vertex4D> verts = mesh4D.vArray;
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> vIndicies = mesh4D.vIndices[s];
            Debug.Assert(vIndicies.Count % 6 == 0);
            int endIx = vIndicies.Count;
            for (int i = 0; i < endIx; i += 6) {
                Mesh4D.Vertex4D tetra = verts[vIndicies[i]];
                Vector4 a = tetra.va;
                Vector4 b = tetra.vb;
                Vector4 c = tetra.vc;
                Vector4 d = tetra.vd;
                Vector4 ab = (a + b) * 0.5f;
                Vector4 ac = (a + c) * 0.5f;
                Vector4 ad = (a + d) * 0.5f;
                Vector4 bc = (b + c) * 0.5f;
                Vector4 bd = (b + d) * 0.5f;
                Vector4 cd = (c + d) * 0.5f;
                Vector4 a_b = (a + a + b) / 3.0f;
                Vector4 a_c = (a + a + c) / 3.0f;
                Vector4 a_d = (a + a + d) / 3.0f;
                Vector4 b_a = (b + b + a) / 3.0f;
                Vector4 b_c = (b + b + c) / 3.0f;
                Vector4 b_d = (b + b + d) / 3.0f;
                Vector4 c_a = (c + c + a) / 3.0f;
                Vector4 c_b = (c + c + b) / 3.0f;
                Vector4 c_d = (c + c + d) / 3.0f;
                Vector4 d_a = (d + d + a) / 3.0f;
                Vector4 d_b = (d + d + b) / 3.0f;
                Vector4 d_c = (d + d + c) / 3.0f;
                Vector4 abc = (a + b + c) / 3.0f;
                Vector4 abd = (a + b + d) / 3.0f;
                Vector4 acd = (a + c + d) / 3.0f;
                Vector4 bcd = (b + c + d) / 3.0f;
                Vector4 n = a + b + c + d;
                if (normalize) {
                    a.Normalize();
                    b.Normalize();
                    c.Normalize();
                    d.Normalize();
                    ab.Normalize();
                    ac.Normalize();
                    ad.Normalize();
                    bc.Normalize();
                    bd.Normalize();
                    cd.Normalize();
                    a_b.Normalize();
                    a_c.Normalize();
                    a_d.Normalize();
                    b_a.Normalize();
                    b_c.Normalize();
                    b_d.Normalize();
                    c_a.Normalize();
                    c_b.Normalize();
                    c_d.Normalize();
                    d_a.Normalize();
                    d_b.Normalize();
                    d_c.Normalize();
                    abc.Normalize();
                    abd.Normalize();
                    acd.Normalize();
                    bcd.Normalize();
                }
                if (extraPoke) {
                    newMesh.AddTetrahedronNormal(n, a, a_b, a_c, a_d);
                    newMesh.AddTetrahedronNormal(n, b, b_a, b_c, b_d);
                    newMesh.AddTetrahedronNormal(n, c, c_a, c_b, c_d);
                    newMesh.AddTetrahedronNormal(n, d, d_a, d_b, d_c);

                    newMesh.AddTetrahedronNormal(n, abc, abd, a_b, b_a);
                    newMesh.AddTetrahedronNormal(n, abc, acd, a_c, c_a);
                    newMesh.AddTetrahedronNormal(n, abc, bcd, b_c, c_b);
                    newMesh.AddTetrahedronNormal(n, abd, acd, a_d, d_a);
                    newMesh.AddTetrahedronNormal(n, abd, bcd, b_d, d_b);
                    newMesh.AddTetrahedronNormal(n, acd, bcd, c_d, d_c);

                    newMesh.AddTetrahedronNormal(n, abc, abd, acd, bcd);

                    newMesh.AddTetrahedronNormal(n, abc, a_d, a_b, a_c);
                    newMesh.AddTetrahedronNormal(n, abc, a_d, a_c, acd);
                    newMesh.AddTetrahedronNormal(n, abc, a_d, acd, abd);
                    newMesh.AddTetrahedronNormal(n, abc, a_d, abd, a_b);

                    newMesh.AddTetrahedronNormal(n, abd, b_c, b_a, b_d);
                    newMesh.AddTetrahedronNormal(n, abd, b_c, b_d, bcd);
                    newMesh.AddTetrahedronNormal(n, abd, b_c, bcd, abc);
                    newMesh.AddTetrahedronNormal(n, abd, b_c, abc, b_a);

                    newMesh.AddTetrahedronNormal(n, acd, c_b, c_a, c_d);
                    newMesh.AddTetrahedronNormal(n, acd, c_b, c_d, bcd);
                    newMesh.AddTetrahedronNormal(n, acd, c_b, bcd, abc);
                    newMesh.AddTetrahedronNormal(n, acd, c_b, abc, c_a);

                    newMesh.AddTetrahedronNormal(n, bcd, d_a, d_b, d_c);
                    newMesh.AddTetrahedronNormal(n, bcd, d_a, d_c, acd);
                    newMesh.AddTetrahedronNormal(n, bcd, d_a, acd, abd);
                    newMesh.AddTetrahedronNormal(n, bcd, d_a, abd, d_b);
                } else {
                    newMesh.AddTetrahedronNormal(n, a, ab, ac, ad);
                    newMesh.AddTetrahedronNormal(n, b, ab, bc, bd);
                    newMesh.AddTetrahedronNormal(n, c, ac, bc, cd);
                    newMesh.AddTetrahedronNormal(n, d, ad, bd, cd);

                    newMesh.AddTetrahedronNormal(n, ab, cd, ac, ad);
                    newMesh.AddTetrahedronNormal(n, ab, cd, ac, bc);
                    newMesh.AddTetrahedronNormal(n, ab, cd, bd, ad);
                    newMesh.AddTetrahedronNormal(n, ab, cd, bd, bc);
                }
            }
            newMesh.NextSubmesh();
        }
        mesh4D = newMesh;
        return this;
    }

    public Mesh4DBuilder Spherize(float ratio, float aboveW=-99999.0f) {
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            List<int> vIndicies = mesh4D.vIndices[s];
            Debug.Assert(vIndicies.Count % 6 == 0);
            int endIx = vIndicies.Count;
            for (int i = 0; i < endIx; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[vIndicies[i]];
                if (v.va.w > aboveW) { v.va = v.va * (1.0f - ratio) + ratio * v.va.normalized; }
                if (v.vb.w > aboveW) { v.vb = v.vb * (1.0f - ratio) + ratio * v.vb.normalized; }
                if (v.vc.w > aboveW) { v.vc = v.vc * (1.0f - ratio) + ratio * v.vc.normalized; }
                if (v.vd.w > aboveW) { v.vd = v.vd * (1.0f - ratio) + ratio * v.vd.normalized; }
                v.normal = Mesh4D.PackedNormal.Flat(v.va, v.vb, v.vc, v.vd);
                mesh4D.vArray[vIndicies[i]] = v;
                mesh4D.vArray[vIndicies[i + 1]] = v;
                mesh4D.vArray[vIndicies[i + 2]] = v;
                mesh4D.vArray[vIndicies[i + 5]] = v;
            }
            List<int> indices_s = mesh4D.sIndices[s];
            Debug.Assert(indices_s.Count % 3 == 0);
            for (int i = 0; i < indices_s.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.sArray[indices_s[i]];
                if (v.vertex.w > aboveW) { v.vertex = v.vertex * (1.0f - ratio) + ratio * v.vertex.normalized; }
                mesh4D.sArray[indices_s[i]] = v;
            }
            List<int> indices_w = mesh4D.wIndices[s];
            Debug.Assert(indices_w.Count % 2 == 0);
            for (int i = 0; i < indices_w.Count; ++i) {
                Mesh4D.Shadow4D v = mesh4D.wArray[indices_w[i]];
                if (v.vertex.w > aboveW) { v.vertex = v.vertex * (1.0f - ratio) + ratio * v.vertex.normalized; }
                mesh4D.wArray[indices_w[i]] = v;
            }
        }
        return this;
    }

    public Mesh4DBuilder SplitAllTets() {
        Debug.Assert(mesh4D.vIndices.Length == 1);
        List<int> vIndicies = mesh4D.vIndices[0];
        int endIx = vIndicies.Count;
        Debug.Assert(endIx % 6 == 0);
        Mesh4D newMesh = new Mesh4D(endIx / 6);
        newMesh.vArray = mesh4D.vArray;
        for (int i = 0; i < endIx; i += 1) {
            newMesh.vIndices[i / 6].Add(vIndicies[i]);
        }
        mesh4D = newMesh;
        return this;
    }

    public Mesh4DBuilder ShadowsFromTets() {
        mesh4D.ClearShadows();
        for (mesh4D.curSubMesh = 0; mesh4D.curSubMesh < mesh4D.vIndices.Length; ++mesh4D.curSubMesh) {
            List<int> indices = mesh4D.vIndices[mesh4D.curSubMesh];
            Debug.Assert(indices.Count % 6 == 0);
            for (int i = 0; i < indices.Count; i += 6) {
                Mesh4D.Vertex4D v = mesh4D.vArray[indices[i]];
                mesh4D.AddTriangleShadow(v.va, v.vb, v.vc);
                mesh4D.AddTriangleShadow(v.va, v.vb, v.vd);
                mesh4D.AddTriangleShadow(v.va, v.vd, v.vc);
                mesh4D.AddTriangleShadow(v.vd, v.vb, v.vc);
            }
        }
        return this;
    }

    //Generate the mesh and shadow
    public Mesh4DBuilder Build(string name, bool shadowsOnly = false) {
        string path = "Assets/Meshes4D/" + name + ".mesh";
        if (!shadowsOnly) {
            mesh4D.GenerateMesh(GetMesh(path));
        }
        if (mesh4D.sArray.Count > 0) {
            mesh4D.GenerateShadowMesh(GetShadowMesh(path));
            mesh4D.GenerateWireMesh(GetWireMesh(path));
        }
        Debug.Log("Generated " + name + ".mesh");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return this;
    }

    //Just print some debugging statistics
    public Mesh4DBuilder Stats() {
        string str = "";
        for (int s = 0; s < mesh4D.vIndices.Length; ++s) {
            str += "Tetras[" + s + "]:  " + (mesh4D.vIndices[s].Count / 6) + "\n";
            str += "Shadows[" + s + "]: " + (mesh4D.sIndices[s].Count / 3) + "\n";
            str += "Wires[" + s + "]:   " + (mesh4D.wIndices[s].Count / 2) + "\n";
        }
        Debug.Log(str);
        return this;
    }

    private static void ProcessSmoothVertex(Mesh4D mesh4D, Dictionary<Vector4, Vector4> vMap, Dictionary<Tuple<Vector4, Vector4>, Vector4> cMap,
                                            Vector4 a, Vector4 n, Vector4 v1, Vector4 v2, Vector4 v3) {
        for (int i = 0; i < mesh4D.conePoints.Count; ++i) {
            if (a == mesh4D.conePoints[i]) {
                Tuple<Vector4, Vector4> pair1 = new Tuple<Vector4, Vector4>(a, v1);
                Tuple<Vector4, Vector4> pair2 = new Tuple<Vector4, Vector4>(a, v2);
                Tuple<Vector4, Vector4> pair3 = new Tuple<Vector4, Vector4>(a, v3);
                if (cMap.ContainsKey(pair1)) { cMap[pair1] += n; } else { cMap.Add(pair1, n); }
                if (cMap.ContainsKey(pair2)) { cMap[pair2] += n; } else { cMap.Add(pair2, n); }
                if (cMap.ContainsKey(pair3)) { cMap[pair3] += n; } else { cMap.Add(pair3, n); }
                return;
            }
        }
        if (vMap.ContainsKey(a)) {
            vMap[a] += n;
        } else {
            vMap.Add(a, n);
        }
    }

    private static Vector4 GetSmoothNormal(Mesh4D mesh4D, Dictionary<Vector4, Vector4> vMap, Dictionary<Tuple<Vector4, Vector4>, Vector4> cMap,
                                           Vector4 a, Vector4 v1, Vector4 v2, Vector4 v3, float groundBlendHeight) {
        if (a.y < groundBlendHeight) { return -(Vector4)Vector3.up; }
        for (int i = 0; i < mesh4D.conePoints.Count; ++i) {
            if (a == mesh4D.conePoints[i]) {
                Tuple<Vector4, Vector4> pair1 = new Tuple<Vector4, Vector4>(a, v1);
                Tuple<Vector4, Vector4> pair2 = new Tuple<Vector4, Vector4>(a, v2);
                Tuple<Vector4, Vector4> pair3 = new Tuple<Vector4, Vector4>(a, v3);
                return cMap[pair1] + cMap[pair2] + cMap[pair3];
            }
        }
        return vMap[a];
    }

    private static Vector4 AreaNormal(List<Tuple<Vector4, Vector4>> pairs, Vector4 v, float radius) {
        Vector4 sum = Vector4.zero;
        foreach (Tuple<Vector4, Vector4> pair in pairs) {
            if (Vector4.Distance(pair.Item1, v) < radius) {
                sum += pair.Item2;
            }
        }
        return sum.normalized;
    }

    private static Vector4 PerturbHash(Vector4 v) {
        uint x = (uint)Mathf.FloorToInt((((v.x * 137.9251f) % 1.0f + 1.0f) % 1.0f) * 65535.0f);
        uint y = (uint)Mathf.FloorToInt((((v.y * 721.1217f) % 1.0f + 1.0f) % 1.0f) * 65535.0f);
        uint z = (uint)Mathf.FloorToInt((((v.z * 345.6782f) % 1.0f + 1.0f) % 1.0f) * 65535.0f);
        uint w = (uint)Mathf.FloorToInt((((v.w * 156.2573f) % 1.0f + 1.0f) % 1.0f) * 65535.0f);
        PseudoRandom._seed = x ^ (y << 4) ^ (z << 8) ^ (w << 12);
        return PseudoRandom.Ball4D();
    }

    private static Vector4 PerturbWave(Vector4 v, Vector4 freq, Vector4 dir) {
        float sx = Mathf.Sin(v.x * freq.x);
        float sy = Mathf.Sin(v.y * freq.y);
        float sz = Mathf.Sin(v.z * freq.z);
        float sw = Mathf.Sin(v.w * freq.w);
        return dir * (sx + sy + sz + sw);
    }

    private static void UpdateNearestVert(ref Vector4 v, List<Vector4> uniqueVerts, float minDistSq) {
        for (int i = 0; i < uniqueVerts.Count; ++i) {
            if ((uniqueVerts[i] - v).sqrMagnitude <= minDistSq) {
                v = uniqueVerts[i];
                return;
            }
        }
        uniqueVerts.Add(v);
    }

    public static Mesh GetMesh(string path) {
        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existingMesh == null) {
            Mesh mesh = new Mesh();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        } else {
            return existingMesh;
        }
    }

    public static Mesh GetShadowMesh(string path) {
        return GetMesh(path.Replace(".mesh", "_s.mesh"));
    }
    public static Mesh GetWireMesh(string path) {
        return GetMesh(path.Replace(".mesh", "_w.mesh"));
    }
}
