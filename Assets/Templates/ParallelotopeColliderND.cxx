#define USE_<D>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallelotopeColliderND : Collider<D> {
    public VECTOR pos = VECTOR.zero;
    public VECTOR ax1 = (VECTOR)new Vector3(1, 0, 0);
    public VECTOR ax2 = (VECTOR)new Vector3(0, 1, 0);
    public VECTOR ax3 = (VECTOR)new Vector3(0, 0, 1);
#if USE_5D
    public VECTOR ax4 = (VECTOR)new Vector4(0, 0, 0, 1);
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

    public override VECTOR NP(VECTOR localPt) {
        //Shift coordinate system to the origin
        VECTOR p = localPt - pos;

        //Since this is flat right now, just project to the space
#if USE_5D
        VECTOR ax5 = Transform<D>.MakeNormal(ax1, ax2, ax3, ax4);
        p -= VECTOR.Project(p, ax5);
        VECTOR np = NP4(p, ax1, ax2, ax3, ax4);
#else
        VECTOR ax4 = Transform<D>.MakeNormal(ax1, ax2, ax3);
        p -= VECTOR.Project(p, ax4);
        VECTOR np = NP3(p, ax1, ax2, ax3);
#endif

        //Return the parallelepiped distance
        return pos + np;
    }

#if USE_5D
    //Distance to parallelotope
    //NOTE: p must already be on the space
    public static VECTOR NP4(VECTOR p, VECTOR ax1, VECTOR ax2, VECTOR ax3, VECTOR ax4) {
        //Get new cofactors
        VECTOR cf1 = ax1 - Transform<D>.Project(ax1, ax2, ax3, ax4);
        VECTOR cf2 = ax2 - Transform<D>.Project(ax2, ax3, ax4, ax1);
        VECTOR cf3 = ax3 - Transform<D>.Project(ax3, ax4, ax1, ax2);
        VECTOR cf4 = ax4 - Transform<D>.Project(ax4, ax3, ax1, ax2);

        //Calculate projection factors
        float dp1 = VECTOR.Dot(cf1, p);
        float dp2 = VECTOR.Dot(cf2, p);
        float dp3 = VECTOR.Dot(cf3, p);
        float dp4 = VECTOR.Dot(cf4, p);

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
        VECTOR p1 = p - ax1; p1 -= VECTOR.Project(p1, cf1);
        VECTOR p2 = p - ax2; p2 -= VECTOR.Project(p2, cf2);
        VECTOR p3 = p - ax3; p3 -= VECTOR.Project(p3, cf3);
        VECTOR p4 = p - ax4; p4 -= VECTOR.Project(p4, cf4);

        //Calculate nearest point on each parallelepiped
        VECTOR np1 = ax1 + NP3(p1, ax2, ax3, ax4);
        VECTOR np2 = ax2 + NP3(p2, ax3, ax4, ax1);
        VECTOR np3 = ax3 + NP3(p3, ax4, ax1, ax2);
        VECTOR np4 = ax4 + NP3(p4, ax1, ax2, ax3);

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
    public static VECTOR NP3(VECTOR p, VECTOR ax1, VECTOR ax2, VECTOR ax3) {
        //Get new cofactors
        VECTOR cf1 = ax1 - Transform<D>.Project(ax1, ax2, ax3);
        VECTOR cf2 = ax2 - Transform<D>.Project(ax2, ax3, ax1);
        VECTOR cf3 = ax3 - Transform<D>.Project(ax3, ax1, ax2);

        //Calculate projection factors
        float dp1 = VECTOR.Dot(cf1, p);
        float dp2 = VECTOR.Dot(cf2, p);
        float dp3 = VECTOR.Dot(cf3, p);

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
        VECTOR p1 = p - ax1; p1 -= VECTOR.Project(p1, cf1);
        VECTOR p2 = p - ax2; p2 -= VECTOR.Project(p2, cf2);
        VECTOR p3 = p - ax3; p3 -= VECTOR.Project(p3, cf3);

        //Calculate nearest point on each parallelogram
        VECTOR np1 = ax1 + NP2(p1, ax2, ax3);
        VECTOR np2 = ax2 + NP2(p2, ax3, ax1);
        VECTOR np3 = ax3 + NP2(p3, ax1, ax2);

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
    public static VECTOR NP2(VECTOR p, VECTOR ax1, VECTOR ax2) {
        //Get new cofactors
        VECTOR cf1 = ax1 - Transform<D>.Project(ax1, ax2);
        VECTOR cf2 = ax2 - Transform<D>.Project(ax2, ax1);

        //Calculate projection factors
        float dp1 = VECTOR.Dot(cf1, p);
        float dp2 = VECTOR.Dot(cf2, p);

        //If we're already in the parallelogram, then we're done
        if (Mathf.Abs(dp1) <= cf1.sqrMagnitude &&
            Mathf.Abs(dp2) <= cf2.sqrMagnitude) {
            return p;
        }

        //Flip sign of axes if they're in the wrong direction
        ax1 *= Mathf.Sign(dp1);
        ax2 *= Mathf.Sign(dp2);

        //Project onto each line
        VECTOR p1 = p - ax1; p1 -= VECTOR.Project(p1, cf1);
        VECTOR p2 = p - ax2; p2 -= VECTOR.Project(p2, cf2);

        //Calculate nearest point on each line
        VECTOR np1 = ax1 + NP1(p1, ax2);
        VECTOR np2 = ax2 + NP1(p2, ax1);

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
    public static VECTOR NP1(VECTOR p, VECTOR ax1) {
        return ax1 * Mathf.Clamp(VECTOR.Dot(p, ax1) / ax1.sqrMagnitude, -1.0f, 1.0f);
    }

    public static bool IsParallelotope(VECTOR a1, VECTOR a2, VECTOR A1, VECTOR A2, VECTOR b1, VECTOR b2, VECTOR B1, VECTOR B2) {
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

    public static bool IsParallelotope(VECTOR wa1, VECTOR wa2, VECTOR wA1, VECTOR wA2, VECTOR wb1, VECTOR wb2, VECTOR wB1, VECTOR wB2,
                                       VECTOR va1, VECTOR va2, VECTOR vA1, VECTOR vA2, VECTOR vb1, VECTOR vb2, VECTOR vB1, VECTOR vB2) {
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
