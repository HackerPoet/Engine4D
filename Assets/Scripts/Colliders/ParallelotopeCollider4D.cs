//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
#define USE_4D
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallelotopeCollider4D : Collider4D {
    public Vector4 pos = Vector4.zero;
    public Vector4 ax1 = (Vector4)new Vector3(1, 0, 0);
    public Vector4 ax2 = (Vector4)new Vector3(0, 1, 0);
    public Vector4 ax3 = (Vector4)new Vector3(0, 0, 1);
#if USE_5D
    public Vector4 ax4 = (Vector4)new Vector4(0, 0, 0, 1);
#endif

    protected override void Awake() {
        base.Awake();
        boundsCheck = true;

        //Add bounding points
        for (int i1 = -1; i1 <= 1; i1 += 2) {
            for (int i2 = -1; i2 <= 1; i2 += 2) {
                for (int i3 = -1; i3 <= 1; i3 += 2) {
#if USE_5D
                    for (int i4 = -1; i4 <= 1; i4 += 2) {
                        AddBoundingPoint(pos + ax1 * i1 + ax2 * i2 + ax3 * i3 + ax4 * i4);
                    }
#else
                    AddBoundingPoint(pos + ax1 * i1 + ax2 * i2 + ax3 * i3);
#endif
                }
            }
        }
    }

    public override Vector4 NP(Vector4 localPt) {
        //Shift coordinate system to the origin
        Vector4 p = localPt - pos;

        //Since this is flat right now, just project to the space
#if USE_5D
        Vector4 ax5 = Transform4D.MakeNormal(ax1, ax2, ax3, ax4);
        p -= Vector4.Project(p, ax5);
        Vector4 np = NP4(p, ax1, ax2, ax3, ax4);
#else
        Vector4 ax4 = Transform4D.MakeNormal(ax1, ax2, ax3);
        p -= Vector4.Project(p, ax4);
        Vector4 np = NP3(p, ax1, ax2, ax3);
#endif

        //Return the parallelepiped distance
        return pos + np;
    }

#if USE_5D
    //Distance to parallelotope
    //NOTE: p must already be on the space
    public static Vector4 NP4(Vector4 p, Vector4 ax1, Vector4 ax2, Vector4 ax3, Vector4 ax4) {
        //Get new cofactors
        Vector4 cf1 = ax1 - Transform4D.Project(ax1, ax2, ax3, ax4);
        Vector4 cf2 = ax2 - Transform4D.Project(ax2, ax3, ax4, ax1);
        Vector4 cf3 = ax3 - Transform4D.Project(ax3, ax4, ax1, ax2);
        Vector4 cf4 = ax4 - Transform4D.Project(ax4, ax3, ax1, ax2);

        //Calculate projection factors
        float dp1 = Vector4.Dot(cf1, p);
        float dp2 = Vector4.Dot(cf2, p);
        float dp3 = Vector4.Dot(cf3, p);
        float dp4 = Vector4.Dot(cf4, p);

        //If we're already in the parallelotope, then we're done
        if (Mathf.Abs(dp1) <= cf1.sqrMagnitude &&
            Mathf.Abs(dp2) <= cf2.sqrMagnitude &&
            Mathf.Abs(dp3) <= cf3.sqrMagnitude &&
            Mathf.Abs(dp4) <= cf4.sqrMagnitude) {
            return p;
        }

        //Flip sign of axes if they're in the wrong direction
        ax1 *= Mathf.Sign(dp1);
        ax2 *= Mathf.Sign(dp2);
        ax3 *= Mathf.Sign(dp3);
        ax4 *= Mathf.Sign(dp4);

        //Project onto each parallelepiped
        Vector4 p1 = p - ax1; p1 -= Vector4.Project(p1, cf1);
        Vector4 p2 = p - ax2; p2 -= Vector4.Project(p2, cf2);
        Vector4 p3 = p - ax3; p3 -= Vector4.Project(p3, cf3);
        Vector4 p4 = p - ax4; p4 -= Vector4.Project(p4, cf4);

        //Calculate nearest point on each parallelepiped
        Vector4 np1 = ax1 + NP3(p1, ax2, ax3, ax4);
        Vector4 np2 = ax2 + NP3(p2, ax3, ax4, ax1);
        Vector4 np3 = ax3 + NP3(p3, ax4, ax1, ax2);
        Vector4 np4 = ax4 + NP3(p4, ax1, ax2, ax3);

        //Calculate the distance to each nearest point
        float distSq1 = (np1 - p).sqrMagnitude;
        float distSq2 = (np2 - p).sqrMagnitude;
        float distSq3 = (np3 - p).sqrMagnitude;
        float distSq4 = (np4 - p).sqrMagnitude;

        //Determine the point with the smallest distance and return it
        float minDist = Mathf.Min(Mathf.Min(distSq1, distSq2), Mathf.Min(distSq3, distSq4));
        if (distSq1 == minDist) {
            return np1;
        } else if (distSq2 == minDist) {
            return np2;
        } else if (distSq3 == minDist) {
            return np3;
        } else {
            return np4;
        }
    }
#endif

    //Distance to parallelepiped
    //NOTE: p must already be on the space
    public static Vector4 NP3(Vector4 p, Vector4 ax1, Vector4 ax2, Vector4 ax3) {
        //Get new cofactors
        Vector4 cf1 = ax1 - Transform4D.Project(ax1, ax2, ax3);
        Vector4 cf2 = ax2 - Transform4D.Project(ax2, ax3, ax1);
        Vector4 cf3 = ax3 - Transform4D.Project(ax3, ax1, ax2);

        //Calculate projection factors
        float dp1 = Vector4.Dot(cf1, p);
        float dp2 = Vector4.Dot(cf2, p);
        float dp3 = Vector4.Dot(cf3, p);

        //If we're already in the parallelepiped, then we're done
        if (Mathf.Abs(dp1) <= cf1.sqrMagnitude &&
            Mathf.Abs(dp2) <= cf2.sqrMagnitude &&
            Mathf.Abs(dp3) <= cf3.sqrMagnitude) {
            return p;
        }

        //Flip sign of axes if they're in the wrong direction
        ax1 *= Mathf.Sign(dp1);
        ax2 *= Mathf.Sign(dp2);
        ax3 *= Mathf.Sign(dp3);

        //Project onto each parallelogram
        Vector4 p1 = p - ax1; p1 -= Vector4.Project(p1, cf1);
        Vector4 p2 = p - ax2; p2 -= Vector4.Project(p2, cf2);
        Vector4 p3 = p - ax3; p3 -= Vector4.Project(p3, cf3);

        //Calculate nearest point on each parallelogram
        Vector4 np1 = ax1 + NP2(p1, ax2, ax3);
        Vector4 np2 = ax2 + NP2(p2, ax3, ax1);
        Vector4 np3 = ax3 + NP2(p3, ax1, ax2);

        //Calculate the distance to each nearest point
        float distSq1 = (np1 - p).sqrMagnitude;
        float distSq2 = (np2 - p).sqrMagnitude;
        float distSq3 = (np3 - p).sqrMagnitude;

        //Determine the point with the smallest distance and return it
        float minDist = Mathf.Min(Mathf.Min(distSq1, distSq2), distSq3);
        if (distSq1 == minDist) {
            return np1;
        } else if (distSq2 == minDist) {
            return np2;
        } else {
            return np3;
        }
    }

    //Distance to parallelogram
    //NOTE: p must already be on the plane
    public static Vector4 NP2(Vector4 p, Vector4 ax1, Vector4 ax2) {
        //Get new cofactors
        Vector4 cf1 = ax1 - Transform4D.Project(ax1, ax2);
        Vector4 cf2 = ax2 - Transform4D.Project(ax2, ax1);

        //Calculate projection factors
        float dp1 = Vector4.Dot(cf1, p);
        float dp2 = Vector4.Dot(cf2, p);

        //If we're already in the parallelogram, then we're done
        if (Mathf.Abs(dp1) <= cf1.sqrMagnitude &&
            Mathf.Abs(dp2) <= cf2.sqrMagnitude) {
            return p;
        }

        //Flip sign of axes if they're in the wrong direction
        ax1 *= Mathf.Sign(dp1);
        ax2 *= Mathf.Sign(dp2);

        //Project onto each line
        Vector4 p1 = p - ax1; p1 -= Vector4.Project(p1, cf1);
        Vector4 p2 = p - ax2; p2 -= Vector4.Project(p2, cf2);

        //Calculate nearest point on each line
        Vector4 np1 = ax1 + NP1(p1, ax2);
        Vector4 np2 = ax2 + NP1(p2, ax1);

        //Calculate the distance to each nearest point
        float distSq1 = (np1 - p).sqrMagnitude;
        float distSq2 = (np2 - p).sqrMagnitude;

        //Determine the point with the smallest distance and return it
        if (distSq1 < distSq2) {
            return np1;
        } else {
            return np2;
        }
    }

    //Distance to line segment
    public static Vector4 NP1(Vector4 p, Vector4 ax1) {
        return ax1 * Mathf.Clamp(Vector4.Dot(p, ax1) / ax1.sqrMagnitude, -1.0f, 1.0f);
    }

    public static bool IsParallelotope(Vector4 a1, Vector4 a2, Vector4 A1, Vector4 A2, Vector4 b1, Vector4 b2, Vector4 B1, Vector4 B2) {
        return ((a1 - a2) - (A1 - A2)).sqrMagnitude < 1e-9f &&
               ((a1 - a2) - (b1 - b2)).sqrMagnitude < 1e-9f &&
               ((a1 - a2) - (B1 - B2)).sqrMagnitude < 1e-9f &&
               ((a1 - A1) - (a2 - A2)).sqrMagnitude < 1e-9f &&
               ((a1 - A1) - (b1 - B1)).sqrMagnitude < 1e-9f &&
               ((a1 - A1) - (b2 - B2)).sqrMagnitude < 1e-9f &&
               ((a1 - b1) - (a2 - b2)).sqrMagnitude < 1e-9f &&
               ((a1 - b1) - (A1 - B1)).sqrMagnitude < 1e-9f &&
               ((a1 - b1) - (A2 - B2)).sqrMagnitude < 1e-9f;
    }

    public static bool IsParallelotope(Vector4 wa1, Vector4 wa2, Vector4 wA1, Vector4 wA2, Vector4 wb1, Vector4 wb2, Vector4 wB1, Vector4 wB2,
                                       Vector4 va1, Vector4 va2, Vector4 vA1, Vector4 vA2, Vector4 vb1, Vector4 vb2, Vector4 vB1, Vector4 vB2) {
        return ((wa1 - wa2) - (wA1 - wA2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wa2) - (wb1 - wb2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wa2) - (wB1 - wB2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wa2) - (va1 - va2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wa2) - (vA1 - vA2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wa2) - (vb1 - vb2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wa2) - (vB1 - vB2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wA1) - (wa2 - wA2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wA1) - (wb1 - wB1)).sqrMagnitude < 1e-9f &&
               ((wa1 - wA1) - (wb2 - wB2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wA1) - (va1 - vA1)).sqrMagnitude < 1e-9f &&
               ((wa1 - wA1) - (va2 - vA2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wA1) - (vb1 - vB1)).sqrMagnitude < 1e-9f &&
               ((wa1 - wA1) - (vb2 - vB2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wb1) - (wa2 - wb2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wb1) - (wA1 - wB1)).sqrMagnitude < 1e-9f &&
               ((wa1 - wb1) - (wA2 - wB2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wb1) - (va1 - vb1)).sqrMagnitude < 1e-9f &&
               ((wa1 - wb1) - (va2 - vb2)).sqrMagnitude < 1e-9f &&
               ((wa1 - wb1) - (vA1 - vB1)).sqrMagnitude < 1e-9f &&
               ((wa1 - wb1) - (vA2 - vB2)).sqrMagnitude < 1e-9f &&
               ((wa1 - va1) - (wa2 - va2)).sqrMagnitude < 1e-9f &&
               ((wa1 - va1) - (wA1 - vA1)).sqrMagnitude < 1e-9f &&
               ((wa1 - va1) - (wA2 - vA2)).sqrMagnitude < 1e-9f &&
               ((wa1 - va1) - (wb1 - vb1)).sqrMagnitude < 1e-9f &&
               ((wa1 - va1) - (wb2 - vb2)).sqrMagnitude < 1e-9f &&
               ((wa1 - va1) - (wB1 - vB1)).sqrMagnitude < 1e-9f &&
               ((wa1 - va1) - (wB2 - vB2)).sqrMagnitude < 1e-9f;
    }
}
