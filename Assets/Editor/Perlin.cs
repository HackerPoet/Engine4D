using UnityEngine;

public static class Perlin {
    public static float Noise(float x) {
        int X = Mathf.FloorToInt(x) & 0xff;
        x -= Mathf.Floor(x);
        float u = Fade(x);
        return Lerp(u, Grad(perm[X], x), Grad(perm[X+1], x-1)) * 2;
    }

    public static float Noise(float x, float y) {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        float u = Fade(x);
        float v = Fade(y);
        int A = (perm[X  ] + Y) & 0xff;
        int B = (perm[X+1] + Y) & 0xff;
        return Lerp(v, Lerp(u, Grad(perm[A  ], x, y  ), Grad(perm[B  ], x-1, y  )),
                       Lerp(u, Grad(perm[A+1], x, y-1), Grad(perm[B+1], x-1, y-1)));
    }

    public static float Noise(Vector2 coord) {
        return Noise(coord.x, coord.y);
    }

    public static float Noise(float x, float y, float z) {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        int Z = Mathf.FloorToInt(z) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        z -= Mathf.Floor(z);
        float u = Fade(x);
        float v = Fade(y);
        float w = Fade(z);
        int A  = (perm[X  ] + Y) & 0xff;
        int B  = (perm[X+1] + Y) & 0xff;
        int AA = (perm[A  ] + Z) & 0xff;
        int BA = (perm[B  ] + Z) & 0xff;
        int AB = (perm[A+1] + Z) & 0xff;
        int BB = (perm[B+1] + Z) & 0xff;
        return Lerp(w, Lerp(v, Lerp(u, Grad(perm[AA  ], x, y  , z  ), Grad(perm[BA  ], x-1, y  , z  )),
                               Lerp(u, Grad(perm[AB  ], x, y-1, z  ), Grad(perm[BB  ], x-1, y-1, z  ))),
                       Lerp(v, Lerp(u, Grad(perm[AA+1], x, y  , z-1), Grad(perm[BA+1], x-1, y  , z-1)),
                               Lerp(u, Grad(perm[AB+1], x, y-1, z-1), Grad(perm[BB+1], x-1, y-1, z-1))));
    }

    public static float Noise(Vector3 coord) {
        return Noise(coord.x, coord.y, coord.z);
    }

    public static float Noise(float x, float y, float z, float w) {
        int X = Mathf.FloorToInt(x) & 0xff;
        int Y = Mathf.FloorToInt(y) & 0xff;
        int Z = Mathf.FloorToInt(z) & 0xff;
        int W = Mathf.FloorToInt(w) & 0xff;
        x -= Mathf.Floor(x);
        y -= Mathf.Floor(y);
        z -= Mathf.Floor(z);
        w -= Mathf.Floor(w);
        float fx = Fade(x);
        float fy = Fade(y);
        float fz = Fade(z);
        float fw = Fade(w);
        int A = (perm[X] + Y) & 0xff;
        int B = (perm[X + 1] + Y) & 0xff;
        int AA = (perm[A] + Z) & 0xff;
        int BA = (perm[B] + Z) & 0xff;
        int AB = (perm[A + 1] + Z) & 0xff;
        int BB = (perm[B + 1] + Z) & 0xff;
        int AAA = (perm[AA] + W) & 0xff;
        int BAA = (perm[BA] + W) & 0xff;
        int ABA = (perm[AB] + W) & 0xff;
        int BBA = (perm[BB] + W) & 0xff;
        int AAB = (perm[AA + 1] + W) & 0xff;
        int BAB = (perm[BA + 1] + W) & 0xff;
        int ABB = (perm[AB + 1] + W) & 0xff;
        int BBB = (perm[BB + 1] + W) & 0xff;
        return Lerp(fw, Lerp(fz, Lerp(fy, Lerp(fx, Grad(perm[AAA  ], x, y  , z  , w  ), Grad(perm[BAA  ], x-1, y  , z  , w  )),
                                          Lerp(fx, Grad(perm[ABA  ], x, y-1, z  , w  ), Grad(perm[BBA  ], x-1, y-1, z  , w  ))),
                                 Lerp(fy, Lerp(fx, Grad(perm[AAB  ], x, y  , z-1, w  ), Grad(perm[BAB  ], x-1, y  , z-1, w  )),
                                          Lerp(fx, Grad(perm[ABB  ], x, y-1, z-1, w  ), Grad(perm[BBB  ], x-1, y-1, z-1, w  )))),
                        Lerp(fz, Lerp(fy, Lerp(fx, Grad(perm[AAA+1], x, y  , z  , w-1), Grad(perm[BAA+1], x-1, y  , z  , w-1)),
                                          Lerp(fx, Grad(perm[ABA+1], x, y-1, z  , w-1), Grad(perm[BBA+1], x-1, y-1, z  , w-1))),
                                 Lerp(fy, Lerp(fx, Grad(perm[AAB+1], x, y  , z-1, w-1), Grad(perm[BAB+1], x-1, y  , z-1, w-1)),
                                          Lerp(fx, Grad(perm[ABB+1], x, y-1, z-1, w-1), Grad(perm[BBB+1], x-1, y-1, z-1, w-1)))));
    }

    public static float Noise(Vector4 coord) {
        return Noise(coord.x, coord.y, coord.z, coord.w);
    }

    public static float Fbm(float x, int octave) {
        float f = 0.0f;
        float w = 0.5f;
        for (int i = 0; i < octave; i++) {
            f += w * Noise(x);
            x *= 2.0f;
            w *= 0.5f;
        }
        return f;
    }

    public static float Fbm(Vector2 coord, int octave) {
        float f = 0.0f;
        float w = 0.5f;
        for (int i = 0; i < octave; i++) {
            f += w * Noise(coord);
            coord *= 2.0f;
            w *= 0.5f;
        }
        return f;
    }

    public static float Fbm(float x, float y, int octave) {
        return Fbm(new Vector2(x, y), octave);
    }

    public static float Fbm(Vector3 coord, int octave) {
        float f = 0.0f;
        float w = 0.5f;
        for (int i = 0; i < octave; i++) {
            f += w * Noise(coord);
            coord *= 2.0f;
            w *= 0.5f;
        }
        return f;
    }

    public static float Fbm(float x, float y, float z, int octave) {
        return Fbm(new Vector3(x, y, z), octave);
    }

    public static float Fbm(Vector4 coord, int octave) {
        float f = 0.0f;
        float w = 0.5f;
        for (int i = 0; i < octave; i++) {
            f += w * Noise(coord);
            coord *= 2.0f;
            w *= 0.5f;
        }
        return f;
    }

    public static float Fbm(float x, float y, float z, float w, int octave) {
        return Fbm(new Vector4(x, y, z, w), octave);
    }

    static float Fade(float t) {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    static float Lerp(float t, float a, float b) {
        return a + t * (b - a);
    }

    static float Grad(int hash, float x) {
        return (hash & 1) == 0 ? x : -x;
    }

    static float Grad(int hash, float x, float y) {
        return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }

    static float Grad(int hash, float x, float y, float z) {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    static float Grad(int hash, float x, float y, float z, float w) {
        int s1 = (hash >> 2) & 0x03;
        int s2 = (hash >> 4) & 0x03;
        s2 = (s1 + (s2 == 0 ? 1 : s2)) % 4;
        float u = ((s1 & 1) == 0 ? ((s1 & 2) == 0 ? x : y) : ((s1 & 2) == 0 ? z : w));
        float v = ((s2 & 1) == 0 ? ((s2 & 2) == 0 ? x : y) : ((s2 & 2) == 0 ? z : w));
        return ((hash & 1) == 0 ? u : -u) + ((hash & 2) == 0 ? v : -v);
    }

    static readonly int[] perm = {
        151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
        151
    };
}
