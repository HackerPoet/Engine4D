#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class GenerateTextures : MonoBehaviour {
    public enum Voronoi {
        EDGE, CELL, DIST
    }

    [MenuItem("4D/Generate Textures")]
    public static void GenerateTexturesMenu() {
        Generate4DNoiseBox("Galaxy", 64, 2.0f, 1.0f, 0.0f, true);
        Generate3DNoise("Sand", 64, 20.0f, 1.0f, 0.0f, false);
        GenerateFractalNoise("Clouds", 128, 8, 2.0f, 1.0f, 1.5f, true);
        GenerateVoronoi("Voronoi", 128, 160, 1.0f, 0.0f, Voronoi.DIST);
        GenerateVoronoi("Flagstone", 128, 20, 10.0f, 0.25f, Voronoi.EDGE);
        GenerateVoronoi("Ice", 128, 25, 1000.0f, 1.0f, Voronoi.CELL);
        GenerateBlob("Star", 64, 0.15f, 30.0f, 1.5f);
    }

    private static string MakeTexturePath(string name, bool resource) {
        if (resource) {
            return "Assets/Resources/" + name + ".asset";
        } else {
            return "Assets/Textures3D/" + name + ".asset";
        }
    }

    private static Texture3D LoadOrCreateTexture(string path, int size, bool color = false, int mipMapCount = 1) {
        Texture3D texture = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if (texture == null || texture.width != size || mipMapCount != texture.mipmapCount) {
            Debug.Log("Making new texture for " + path);
            texture = new Texture3D(size, size, size, color ? TextureFormat.RGB24 : TextureFormat.R8, mipMapCount);
            AssetDatabase.CreateAsset(texture, path);
        }
        return texture;
    }

    private static void SaveTexture(string path, Texture3D texture) {
        bool updateMipmaps = (texture.mipmapCount > 1);
        texture.Apply(updateMipmaps);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static void Dilate(string origName, string newName) {
        string origPath = MakeTexturePath(origName, false);
        string newPath = MakeTexturePath(newName, false);
        Texture3D origTex = AssetDatabase.LoadAssetAtPath<Texture3D>(origPath);
        Texture3D texture = LoadOrCreateTexture(newPath, origTex.width, false);
        for (int i = 0; i < origTex.width; ++i) {
            for (int j = 0; j < origTex.height; ++j) {
                for (int k = 0; k < origTex.depth; ++k) {
                    float c = origTex.GetPixel(i, j, k).r;
                    c = Mathf.Max(c, origTex.GetPixel(Mathf.Max(i - 1, 0), j, k).r);
                    c = Mathf.Max(c, origTex.GetPixel(Mathf.Min(i + 1, origTex.width - 1), j, k).r);
                    c = Mathf.Max(c, origTex.GetPixel(i, Mathf.Max(j - 1, 0), k).r);
                    c = Mathf.Max(c, origTex.GetPixel(i, Mathf.Min(j + 1, origTex.height - 1), k).r);
                    c = Mathf.Max(c, origTex.GetPixel(i, j, Mathf.Max(k - 1, 0)).r);
                    c = Mathf.Max(c, origTex.GetPixel(i, j, Mathf.Min(k + 1, origTex.depth - 1)).r);
                    texture.SetPixel(i, j, k, new Color(c, c, c, 1.0f));
                }
            }
        }
        SaveTexture(newPath, texture);
    }

    private static void GenerateFractalNoise(string newName, int res, int maxIters, float alpha, float startScale, float scaleFactor, bool color) {
        string newPath = MakeTexturePath(newName, false);
        Texture3D texture = LoadOrCreateTexture(newPath, res, color);
        for (int i = 0; i < res; ++i) {
            float fx = i / (float)res;
            for (int j = 0; j < res; ++j) {
                float fy = j / (float)res;
                for (int k = 0; k < res; ++k) {
                    float fz = k / (float)res;
                    Color col = Color.clear;
                    float scale = startScale;
                    for (int iter = 0; iter < maxIters; ++iter) {
                        float nx = (fx + iter * 0.11f) % 1.0f;
                        float ny = (fy + iter * 0.17f) % 1.0f;
                        float nz = (fz + iter * 0.31f) % 1.0f;
                        Color c = Noise(nx, ny, nz, scale, 1.0f, 0.1f, color);
                        col += alpha * new Color(Mathf.Abs(c.r), Mathf.Abs(c.g), Mathf.Abs(c.b), 1.0f);
                        scale *= scaleFactor;
                    }
                    col /= maxIters;
                    texture.SetPixel(i, j, k, col);
                }
            }
        }
        SaveTexture(newPath, texture);
    }

    private static void GenerateBlob(string name, int res, float radius, float falloff, float power = 2.0f) {
        string path = MakeTexturePath(name, false);
        Texture3D texture = LoadOrCreateTexture(path, res, false);
        float radiusSq = Mathf.Pow(radius, power);
        for (int x = 0; x < texture.width; ++x) {
            float fx = x / (float)texture.width;
            float dx = Mathf.Pow(Mathf.Abs(2.0f * fx - 1.0f), power);
            for (int y = 0; y < texture.height; ++y) {
                float fy = y / (float)texture.height;
                float dy = Mathf.Pow(Mathf.Abs(2.0f * fy - 1.0f), power);
                for (int z = 0; z < texture.depth; ++z) {
                    float fz = z / (float)texture.depth;
                    float dz = Mathf.Pow(Mathf.Abs(2.0f * fz - 1.0f), power);
                    float d = dx + dy + dz - radiusSq;
                    float c = Mathf.Exp(-falloff * Mathf.Max(d, 0.0f));
                    texture.SetPixel(x, y, z, new Color(c, c, c, c));
                }
            }
        }
        SaveTexture(path, texture);
    }

    private static void Generate3DNoise(string name, int res, float scale, float alpha, float beta, bool color, bool resource = false) {
        string path = MakeTexturePath(name, resource);
        Texture3D texture = LoadOrCreateTexture(path, res, color);
        for (int x = 0; x < texture.width; ++x) {
            float fx = x / (float)texture.width;
            for (int y = 0; y < texture.height; ++y) {
                float fy = y / (float)texture.height;
                for (int z = 0; z < texture.depth; ++z) {
                    float fz = z / (float)texture.depth;
                    texture.SetPixel(x, y, z, Noise(fx, fy, fz, scale, alpha, beta, color));
                }
            }
        }
        SaveTexture(path, texture);
    }

    public static Color Noise(float fx, float fy, float fz, float scale, float alpha, float beta, bool color) {
        float dx = Mathf.Abs(2.0f * fx - 1.0f);
        float gx = (fx > 0.5f ? 1.0f - fx : fx);
        float dy = Mathf.Abs(2.0f * fy - 1.0f);
        float gy = (fy > 0.5f ? 1.0f - fy : fy);
        float dz = Mathf.Abs(2.0f * fz - 1.0f);
        float gz = (fz > 0.5f ? 1.0f - fz : fz);

        float d = Mathf.Max(dx, Mathf.Max(dy, dz));
        d *= d;
        d *= d;

        float r1 = Perlin.Noise(fx * scale, fy * scale, fz * scale);
        float r2 = Perlin.Noise(gx * scale, gy * scale, gz * scale);
        float r = r1 * (1.0f - d) + r2 * d;

        if (color) {
            float g1 = Perlin.Noise(fx * scale + 10.0f, fy * scale, fz * scale);
            float g2 = Perlin.Noise(gx * scale + 10.0f, gy * scale, gz * scale);
            float g = g1 * (1.0f - d) + g2 * d;

            float b1 = Perlin.Noise(fx * scale + 20.0f, fy * scale, fz * scale);
            float b2 = Perlin.Noise(gx * scale + 20.0f, gy * scale, gz * scale);
            float b = b1 * (1.0f - d) + b2 * d;

            return new Color(r * alpha + beta, g * alpha + beta, b * alpha + beta, 0.0f);
        } else {
            return new Color(r * alpha + beta, r * alpha + beta, r * alpha + beta, 0.0f);
        }
    }

    public static void GenerateVoronoi(string name, int res, int numPts, float sharpness, float minBrightness, Voronoi mode, bool rgbColor = false) {
        //Generate the random points in a unit cube
        Random.InitState(1337);
        Vector3[] pts = new Vector3[numPts];
        Vector3[] cellColor = new Vector3[numPts];
        for (int i = 0; i < numPts; ++i) {
            pts[i] = new Vector3(Random.value, Random.value, Random.value);
            if (rgbColor) {
                Quaternion q = Quaternion.AngleAxis(Random.value * 360.0f, Vector3.one);
                Vector3 v = Vector3.one * 0.5f + q * new Vector3(2.0f, -2.0f, 0.0f);
                v.x = Mathf.Clamp01(v.x);
                v.y = Mathf.Clamp01(v.y);
                v.z = Mathf.Clamp01(v.z);
                cellColor[i] = v;
            } else {
                cellColor[i] = Random.value * Vector3.one;
            }
        }

        //Make voronoi
        string path = MakeTexturePath(name, false);
        Texture3D texture = LoadOrCreateTexture(path, res, rgbColor);
        Vector3 curPt = Vector3.zero;
        for (int x = 0; x < texture.width; ++x) {
            curPt.x = x / (float)texture.width;
            for (int y = 0; y < texture.height; ++y) {
                curPt.y = y / (float)texture.height;
                for (int z = 0; z < texture.depth; ++z) {
                    curPt.z = z / (float)texture.depth;

                    //Get the two nearest points
                    float nearestDistSq1 = 999.0f;
                    float nearestDistSq2 = 999.0f;
                    int nearestIx1 = 0;
                    int nearestIx2 = 0;
                    for (int i = 0; i < numPts; ++i) {
                        Vector3 diff = curPt - pts[i];
                        if (diff.x < -0.5f) { diff.x += 1.0f; } else if (diff.x > 0.5f) { diff.x -= 1.0f; }
                        if (diff.y < -0.5f) { diff.y += 1.0f; } else if (diff.y > 0.5f) { diff.y -= 1.0f; }
                        if (diff.z < -0.5f) { diff.z += 1.0f; } else if (diff.z > 0.5f) { diff.z -= 1.0f; }
                        float distSq = diff.sqrMagnitude;
                        if (distSq < nearestDistSq1) {
                            nearestDistSq2 = nearestDistSq1;
                            nearestIx2 = nearestIx1;
                            nearestDistSq1 = distSq;
                            nearestIx1 = i;
                        } else if (distSq < nearestDistSq2) {
                            nearestDistSq2 = distSq;
                            nearestIx2 = i;
                        }
                    }

                    float minDist = Mathf.Sqrt(nearestDistSq1);
                    float d = Mathf.Sqrt(nearestDistSq2) - minDist;
                    if (mode == Voronoi.CELL) {
                        d = 1.0f - Mathf.Clamp(d * sharpness, 0.0f, 1.0f);
                        Vector3 c = minBrightness * Vector3.Lerp(cellColor[nearestIx1], cellColor[nearestIx2], 0.5f * d);
                        texture.SetPixel(x, y, z, new Color(c.x, c.y, c.z, 1.0f));
                    } else if (mode == Voronoi.EDGE) {
                        d = 1.0f - minBrightness * Mathf.Clamp(d * sharpness, 0.0f, 1.0f);
                        texture.SetPixel(x, y, z, new Color(d, d, d, 1.0f));
                    } else if (mode == Voronoi.DIST) {
                        d = Mathf.Clamp(minDist * sharpness, 0.0f, 1.0f);
                        texture.SetPixel(x, y, z, new Color(d, d, d, 1.0f));
                    }
                }
            }
        }

        //Save the result
        SaveTexture(path, texture);
    }

    private static void MeshTo3DTexture(string name, string meshName, int size, float scale, int mipmapCount=1) {
        //Get the texture to write to
        string path = MakeTexturePath(name, false);
        Texture3D texture = LoadOrCreateTexture(path, size, true, mipmapCount);

        //Get the 3D mesh
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes3D/" + meshName + ".fbx");
        Vector3[] verts = mesh.vertices;
        int[] indices = mesh.GetIndices(0);

        //Sample the grid
        for (int x = 0; x < texture.width; ++x) {
            float mx = 2.0f * x / (float)texture.width - 1.0f;
            for (int y = 0; y < texture.height; ++y) {
                float my = 2.0f * y / (float)texture.height - 1.0f;
                for (int z = 0; z < texture.depth; ++z) {
                    float mz = 2.0f * z / (float)texture.depth - 1.0f;

                    Vector3 p = new Vector3(mx, my, mz) * scale;
                    float winding_number = 0.0f;
                    for (int i = 0; i < indices.Length; i += 3) {
                        Vector3 a = verts[indices[i]] - p;
                        Vector3 b = verts[indices[i + 1]] - p;
                        Vector3 c = verts[indices[i + 2]] - p;
                        float am = a.magnitude;
                        float bm = b.magnitude;
                        float cm = c.magnitude;
                        float wy = Vector3.Dot(Vector3.Cross(a, b), c);
                        float wx = (am * bm * cm) + cm * Vector3.Dot(a, b) + am * Vector3.Dot(b, c) + bm * Vector3.Dot(c, a);
                        winding_number += Mathf.Atan2(wy, wx);
                    }
                    bool isInside = (winding_number >= Mathf.PI);

                    Color color = (isInside ? Color.white : Color.clear);
                    texture.SetPixel(x, y, z, color);
                }
            }
        }

        //Free the temporary object and save texture
        SaveTexture(path, texture);
    }

    private static void Generate4DNoiseBox(string name, int res, float scale, float alpha, float beta, bool color) {
        //Create the folder if one doesn't exist
        string folderPath = "Assets/Textures3D";
        if (!AssetDatabase.IsValidFolder(folderPath + "/" + name)) {
            AssetDatabase.CreateFolder(folderPath, name);
        }

        //Create the 8 matrix transforms needed
        Matrix4x4[] m = new Matrix4x4[8] {
            Transform4D.PlaneRotation(90.0f, 0, 3),
            Transform4D.PlaneRotation(-90.0f, 0, 3),
            Transform4D.PlaneRotation(90.0f, 1, 3),
            Transform4D.PlaneRotation(-90.0f, 1, 3),
            Transform4D.PlaneRotation(90.0f, 2, 3),
            Transform4D.PlaneRotation(-90.0f, 2, 3),
            Transform4D.PlaneRotation(0.0f, 0, 3),
            Transform4D.PlaneRotation(180.0f, 0, 3),
        };

        //Create the 8 textures
        Texture3D[] textures = new Texture3D[m.Length];
        for (int i = 0; i < m.Length; ++i) {
            textures[i] = LoadOrCreateTexture("Assets/Textures3D/" + name + "/" + name + i + ".asset", res, color);
        }

        //Populate hypercube map
        for (int i = 0; i < m.Length; ++i) {
            Vector4 gOffset = new Vector4(10.0f, 7.0f, 3.0f, 2.0f);
            Vector4 bOffset = new Vector4(20.0f, 11.0f, 5.0f, 1.0f);
            Vector4 v = new Vector4(0.0f, 0.0f, 0.0f, -1.0f);
            for (int x = 0; x < res; ++x) {
                v.x = 1.0f - 2.0f * x / (float)(res - 1);
                for (int y = 0; y < res; ++y) {
                    v.y = 1.0f - 2.0f * y / (float)(res - 1);
                    for (int z = 0; z < res; ++z) {
                        v.z = 1.0f - 2.0f * z / (float)(res - 1);
                        Vector4 f = (m[i] * v).normalized * scale;
                        float r = Perlin.Noise(f);
                        if (color) {
                            float g = Perlin.Noise(f + gOffset);
                            float b = Perlin.Noise(f + bOffset);
                            textures[i].SetPixel(x, y, z, new Color(r * alpha + beta, g * alpha + beta, b * alpha + beta, 0.0f));
                        } else {
                            textures[i].SetPixel(x, y, z, new Color(r * alpha + beta, r * alpha + beta, r * alpha + beta, 0.0f));
                        }
                    }
                }
            }
        }

        //Save the 8 textures
        for (int i = 0; i < m.Length; ++i) {
            textures[i].wrapMode = TextureWrapMode.Clamp;
            SaveTexture("Assets/Textures3D/" + name + "/" + name + i + ".asset", textures[i]);
        }
    }
}
#endif
