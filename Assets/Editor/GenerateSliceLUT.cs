#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class GenerateSliceLUT : MonoBehaviour {
    [MenuItem("4D/Generate LUTs")]
    public static void GenerateLUTsMenu() {
        GenerateLUT4D();
        GenerateLUT5D();
        AssetDatabase.Refresh();
        Debug.Log("Done Generating LUTs!");
    }

    private static void GenerateLUT4D() {
        const int TEX_WIDTH = 8;
        const int TEX_HEIGHT = 8;

        Texture2D texture = new Texture2D(TEX_WIDTH, TEX_HEIGHT, TextureFormat.RGBA32, false);

        int[] vix = new int[2];
        for (int x = 0; x < TEX_WIDTH; ++x) {
            int t = (x & 0x03);
            bool a = (x & 0x04) != 0;
            for (int y = 0; y < TEX_HEIGHT; ++y) {
                bool b = (y & 0x01) != 0;
                bool c = (y & 0x02) != 0;
                bool d = (y & 0x04) != 0;
                if (LUT4D(a, b, c, d, t, vix, out bool slice)) {
                    int flipT = (t > 0 ? 4 - t : 0);
                    LUT4D(a, b, c, d, flipT, vix, out slice);
                }
                Color outColor = new Color((0.5f + vix[0]) / 4.0f, (0.5f + vix[1]) / 4.0f, 0.0f, 1.0f);
                texture.SetPixel(x, y, outColor);
            }
        }

        SaveTexture(texture, "Assets/Resources/LUT4D.png");
    }

    private static void GenerateLUT5D() {
        const int TEX_WIDTH = 1 << 7;
        const int TEX_HEIGHT = 1 << 6;

        Texture2D texture = new Texture2D(TEX_WIDTH, TEX_HEIGHT, TextureFormat.RGBA32, false);

        int[] vix = new int[3];
        for (int x = 0; x < TEX_WIDTH; ++x) {
            int   t = (x & 0x07);
            bool ab = (x & 0x08) != 0;
            bool ac = (x & 0x10) != 0;
            bool ad = (x & 0x20) != 0;
            bool ae = (x & 0x40) != 0;
            for (int y = 0; y < TEX_HEIGHT; ++y) {
                bool bc = (y & 0x01) != 0;
                bool bd = (y & 0x02) != 0;
                bool be = (y & 0x04) != 0;
                bool cd = (y & 0x08) != 0;
                bool ce = (y & 0x10) != 0;
                bool de = (y & 0x20) != 0;
                if (LUT5D(ab, ac, ad, ae, bc, bd, be, cd, ce, de, t, vix, out bool slice)) {
                    int flipT = (t > 0 ? 5 - t : 0);
                    LUT5D(ab, ac, ad, ae, bc, bd, be, cd, ce, de, flipT, vix, out slice);
                }
                Color outColor = new Color((0.5f + vix[0]) / 5.0f, (0.5f + vix[1]) / 5.0f, (0.5f + vix[2]) / 5.0f, 1.0f);
                texture.SetPixel(x, y, outColor);
            }
        }

        SaveTexture(texture, "Assets/Resources/LUT5D.png");
    }

    private static void MaskToVIX(int mask, int[] vix) {
        Debug.Assert(mask != 0);
        int i = 0;
        for (int maskIx = 0; mask != 0; ++maskIx) {
            if ((mask & 0x01) != 0) {
                vix[i] = maskIx;
                i += 1;
            }
            mask >>= 1;
        }
        Debug.Assert(i == vix.Length);
    }

    private static int BitCount(int mask) {
        int count = 0;
        while (mask != 0) {
            if ((mask & 0x01) != 0) {
                count += 1;
            }
            mask >>= 1;
        }
        return count;
    }

    private static int BinaryLog(int n) {
        int b = 0;
        while (n != 0) {
            n >>= 1;
            b++;
        }
        return b;
    }

    private static bool AllOrNone(bool a, bool b, bool c) {
        return (a && b && c) || (!a && !b && !c);
    }

    private static bool LUT4D(bool a, bool b, bool c, bool d, int ix, int[] vix, out bool slice) {
        //Bit-masks
        const int A = 0x01;
        const int B = 0x02;
        const int C = 0x04;
        const int D = 0x08;

        //Default initialize to sliced (not rendered)
        vix[0] = 0;
        vix[1] = 0;
        slice = true;

        //Automatically reject if index is invalid
        if (ix >= 4) { return false; }

        //Create line and mask tables
        bool[] verts = new bool[] {
            a, b, c, d
        };
        bool[] lines = new bool[] {
            a != b, //0
            a != c, //1
            a != d, //2
            b != c, //3
            b != d, //4
            c != d, //5
        };
        int[] mask = new int[] {
            A | B, //0
            A | C, //1
            A | D, //2
            B | C, //3
            B | D, //4
            C | D, //5
        };

        Vector3[] v = new Vector3[] {
            new Vector3(-1, -1, -1),
            new Vector3(-1, 1, 1),
            new Vector3(1, -1, 1),
            new Vector3(1, 1, -1),
        };

        //Get vertex pair
        for (int i = 0; i < lines.Length; ++i) {
            if (!lines[i]) { continue; }
            if (ix == 0) { MaskToVIX(mask[i], vix); }
            for (int j = i + 1; j < lines.Length; ++j) {
                if (!lines[j]) { continue; }
                if (BitCount(mask[i] & mask[j]) != 1) { continue; }
                if (ix == 1) { MaskToVIX(mask[j], vix); }
                for (int k = i + 1; k < lines.Length; ++k) {
                    if (!lines[k] || k == j) { continue; }
                    if (BitCount(mask[j] & mask[k]) != 1) { continue; }
                    if (ix >= 2) { MaskToVIX(mask[k], vix); }
                    int[] ia = new int[2]; MaskToVIX(mask[i], ia);
                    int[] ib = new int[2]; MaskToVIX(mask[j], ib);
                    int[] ic = new int[2]; MaskToVIX(mask[k], ic);
                    Vector3 va = (v[ia[0]] - v[ia[1]]) * (verts[ia[0]] ? 1.0f : -1.0f);
                    Vector3 vb = (v[ib[0]] - v[ib[1]]) * (verts[ib[0]] ? 1.0f : -1.0f);
                    Vector3 vc = (v[ic[0]] - v[ic[1]]) * (verts[ic[0]] ? 1.0f : -1.0f);
                    bool needsFlip = Vector3.Dot(va, Vector3.Cross(vb, vc)) < 0.0f;
                    if (BitCount(mask[i] & mask[k]) == 1) {
                        slice = (ix >= 3);
                        return needsFlip;
                    }
                    for (int p = i + 1; p < lines.Length; ++p) {
                        if (!lines[p] || p == j || p == k) { continue; }
                        if (BitCount(mask[k] & mask[p]) != 1) { continue; }
                        if (ix >= 3) { MaskToVIX(mask[p], vix); }
                        if (BitCount(mask[i] & mask[p]) == 1) {
                            Debug.Assert(BitCount(mask[j] & mask[p]) != 1);
                            slice = (ix >= 4);
                            return !needsFlip;
                        }
                        return false;
                    }
                }
            }
        }
        return false;
    }

    private static bool LUT5D(bool ab, bool ac, bool ad, bool ae, bool bc, bool bd, bool be, bool cd, bool ce, bool de, int ix, int[] vix, out bool slice) {
        //Bit-masks
        const int A = 0x01;
        const int B = 0x02;
        const int C = 0x04;
        const int D = 0x08;
        const int E = 0x10;

        //Default initialize to sliced (not rendered)
        vix[0] = 0;
        vix[1] = 0;
        vix[2] = 0;
        slice = true;

        //Automatically reject if index is invalid
        if (ix >= 5) { return false; }

        //Create triangle and mask tables
        bool[] tris = new bool[] {
            AllOrNone(ab, bc, !ac), //0
            AllOrNone(ab, bd, !ad), //1
            AllOrNone(ab, be, !ae), //2
            AllOrNone(ac, cd, !ad), //3
            AllOrNone(ac, ce, !ae), //4
            AllOrNone(ad, de, !ae), //5
            AllOrNone(bc, cd, !bd), //6
            AllOrNone(bc, ce, !be), //7
            AllOrNone(bd, de, !be), //8
            AllOrNone(cd, de, !ce), //9
        };
        int[] mask = new int[] {
            A | B | C, //0
            A | B | D, //1
            A | B | E, //2
            A | C | D, //3
            A | C | E, //4
            A | D | E, //5
            B | C | D, //6
            B | C | E, //7
            B | D | E, //8
            C | D | E, //9
        };
        Dictionary<int, bool> lines = new() {
            { A | B, ab },
            { A | C, ac },
            { A | D, ad },
            { A | E, ae },
            { B | C, bc },
            { B | D, bd },
            { B | E, be },
            { C | D, cd },
            { C | E, ce },
            { D | E, de },
        };

        float root5 = 1.0f / Mathf.Sqrt(5);
        Vector4[] v = new Vector4[] {
            new Vector4(1, 1, 1, -root5),
            new Vector4(1, -1, -1, -root5),
            new Vector4(-1, 1, -1, -root5),
            new Vector4(-1, -1, 1, -root5),
            new Vector4(0, 0, 0, 4.0f * root5),
        };

        for (int i = 0; i < tris.Length; ++i) {
            if (!tris[i]) { continue; }
            if (ix == 0) { MaskToVIX(mask[i], vix); }
            for (int j = i + 1; j < tris.Length; ++j) {
                if (!tris[j]) { continue; }
                if (BitCount(mask[i] & mask[j]) != 2) { continue; }
                if (ix == 1) { MaskToVIX(mask[j], vix); }
                for (int k = i + 1; k < tris.Length; ++k) {
                    if (!tris[k] || k == j) { continue; }
                    if (BitCount(mask[j] & mask[k]) != 2) { continue; }
                    if (ix >= 2) { MaskToVIX(mask[k], vix); }
                    int commonMask = mask[i] & mask[j];
                    int[] common = new int[2]; MaskToVIX(commonMask, common);
                    int[] ia = new int[1]; MaskToVIX(mask[i] & ~commonMask, ia);
                    int[] ib = new int[1]; MaskToVIX(mask[j] & ~commonMask, ib);
                    int[] ic = new int[1]; MaskToVIX(mask[k] & ~mask[j], ic);
                    Vector4 va = (v[common[1]] - v[common[0]]);
                    Vector4 vb = (v[ia[0]] - v[common[0]]);
                    Vector4 vc = (v[ib[0]] - v[common[0]]);
                    Vector4 vd = (v[ic[0]] - v[common[0]]);
                    float sign = Vector4.Dot(Transform4D.MakeNormal(va, vb, vc), vd);
                    float sa = lines[(1 << common[1]) | (1 << common[0])] ? -1.0f : 1.0f;
                    bool needsFlip = sign * sa < 0.0f;
                    if (BitCount(mask[i] & mask[k]) == 2) {
                        slice = (ix >= 3);
                        return needsFlip;
                    }
                    for (int p = i + 1; p < tris.Length; ++p) {
                        if (!tris[p] || p == j || p == k) { continue; }
                        if (BitCount(mask[k] & mask[p]) != 2) { continue; }
                        if (ix >= 3) { MaskToVIX(mask[p], vix); }
                        if (BitCount(mask[i] & mask[p]) == 2) {
                            Debug.Assert(BitCount(mask[j] & mask[p]) != 2);
                            slice = (ix >= 4);
                            return needsFlip;
                        }
                        for (int q = i + 1; q < tris.Length; ++q) {
                            if (!tris[q] || q == j || q == k || q == p) { continue; }
                            if (BitCount(mask[i] & mask[q]) != 2) { continue; }
                            if (ix >= 4) { MaskToVIX(mask[q], vix); }
                            Debug.Assert(BitCount(mask[j] & mask[q]) != 2);
                            Debug.Assert(BitCount(mask[k] & mask[q]) != 2);
                            slice = (ix >= 5);
                            return needsFlip;
                        }
                        return false;
                    }
                }
            }
        }
        return false;
    }

    private static void SaveTexture(Texture2D texture, string path) {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }
}
#endif
