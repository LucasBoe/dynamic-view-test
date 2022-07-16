using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector2Util
{
    public static Vector2 GetClosestPointOnLineSegment(this V2Line line, Vector2 point)
    {
        Vector2 lineStart = new Vector2(line.X1, line.Y1);
        Vector2 lineEnd = new Vector2(line.X2, line.Y2);

        Vector2 AP = point - lineStart;       //Vector from A to P   
        Vector2 AB = lineEnd - lineStart;       //Vector from A to B  

        float magnitudeAB = AB.sqrMagnitude;     //Magnitude of AB vector (it's length squared)     
        float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
        float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

        if (distance < 0)     //Check if point projection is over vectorAB     
        {
            return lineStart;

        }
        else if (distance > 1)
        {
            return lineEnd;
        }
        else
        {
            return lineStart + AB * distance;
        }
    }
    public static bool DoesIntersect(this V2Line line, V2Line otherLine)
    {
        var a1 = line.Y2 - line.Y1;
        var b1 = line.X1 - line.X2;
        var c1 = line.X2 * line.Y1 - line.X1 * line.Y2;

        /* Compute r3 and r4.
         */

        var r3 = a1 * otherLine.X1 + b1 * otherLine.Y1 + c1;
        var r4 = a1 * otherLine.X2 + b1 * otherLine.Y2 + c1;

        /* Check signs of r3 and r4.  If both point 3 and point 4 lie on
         * same side of line 1, the line segments do not intersect.
         */

        if (r3 != 0 && r4 != 0 && Mathf.Sign(r3) == Mathf.Sign(r4))
        {
            return false; // DONT_INTERSECT
        }

        /* Compute a2, b2, c2 */

        var a2 = otherLine.Y2 - otherLine.Y1;
        var b2 = otherLine.X1 - otherLine.X2;
        var c2 = otherLine.X2 * otherLine.Y1 - otherLine.X1 * otherLine.Y2;

        /* Compute r1 and r2 */

        var r1 = a2 * line.X1 + b2 * line.Y1 + c2;
        var r2 = a2 * line.X2 + b2 * line.Y2 + c2;

        /* Check signs of r1 and r2.  If both point 1 and point 2 lie
         * on same side of second line segment, the line segments do
         * not intersect.
         */
        if (r1 != 0 && r2 != 0 && Mathf.Sign(r1) == Mathf.Sign(r2))
        {
            return false; // DONT_INTERSECT
        }

        return true;
    }

    public static Intersection GetIntersection(this V2Line line, V2Line otherLine)
    {
        var a1 = line.Y2 - line.Y1;
        var b1 = line.X1 - line.X2;
        var c1 = line.X2 * line.Y1 - line.X1 * line.Y2;

        /* Compute r3 and r4.
         */

        var r3 = a1 * otherLine.X1 + b1 * otherLine.Y1 + c1;
        var r4 = a1 * otherLine.X2 + b1 * otherLine.Y2 + c1;

        /* Check signs of r3 and r4.  If both point 3 and point 4 lie on
         * same side of line 1, the line segments do not intersect.
         */

        if (r3 != 0 && r4 != 0 && Mathf.Sign(r3) == Mathf.Sign(r4))
        {
            return new Intersection(); // DONT_INTERSECT
        }

        /* Compute a2, b2, c2 */

        var a2 = otherLine.Y2 - otherLine.Y1;
        var b2 = otherLine.X1 - otherLine.X2;
        var c2 = otherLine.X2 * otherLine.Y1 - otherLine.X1 * otherLine.Y2;

        /* Compute r1 and r2 */

        var r1 = a2 * line.X1 + b2 * line.Y1 + c2;
        var r2 = a2 * line.X2 + b2 * line.Y2 + c2;

        /* Check signs of r1 and r2.  If both point 1 and point 2 lie
         * on same side of second line segment, the line segments do
         * not intersect.
         */
        if (r1 != 0 && r2 != 0 && Mathf.Sign(r1) == Mathf.Sign(r2))
        {
            return new Intersection(); // DONT_INTERSECT
        }

        var denom = a1 * b2 - a2 * b1;
        if (denom == 0)
        {
            return new Intersection(); //( COLLINEAR );
        }
        var offset = denom < 0 ? -denom / 2 : denom / 2;

        /* The denom/2 is to get rounding instead of truncating.  It
         * is added or subtracted to the numerator, depending upon the
         * sign of the numerator.
         */

        var num = b1 * c2 - b2 * c1;
        var x = (num < 0 ? num - offset : num + offset) / denom;

        num = a2 * c1 - a1 * c2;
        var y = (num < 0 ? num - offset : num + offset) / denom;
        return new Intersection()
        {
            DoesIntersect = true,
            Point = new Vector2(x, y),
            Distance = new Vector2(x - line.X1, y - line.Y1).magnitude
        };
    }

    public static int Modulo(this int a, int b)
    {
        while (a < 0) a += b;
        return a % b;
    }
    public static float GetAngleFromTo(Vector2 from, Vector2 to)
    {
        Vector2 dir = (to - from).normalized;
        return Mathf.Atan2(dir.x, dir.y);
    }

    public static Vector2 ToV2(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }

    public static Vector3 ToV3(this Vector2 vector2, float y = 10f)
    {
        return new Vector3(vector2.x, y, vector2.y);
    }

    public static Vector3 ToV3(float x, float y, float z = 10f)
    {
        return ToV3(new Vector2(x, y), z);
    }

    public class V2Line
    {
        public float X1, Y1, X2, Y2;

        public V2Line(Vector3 p1V3, Vector3 p2V3)
        {
            Vector2 p1V2 = p1V3.ToV2();
            Vector2 p2V2 = p2V3.ToV2();

            X1 = p1V2.x;
            Y1 = p1V2.y;
            X2 = p2V2.x;
            Y2 = p2V2.y;
        }

        public V2Line(Vector2 p1V2, Vector2 p2V2)
        {
            X1 = p1V2.x;
            Y1 = p1V2.y;
            X2 = p2V2.x;
            Y2 = p2V2.y;
        }

        public Vector3 GetV31(float y = 10f) => new Vector3(X1, y, Y1);
        public Vector3 GetV32(float y = 10f) => new Vector3(X2, y, Y2);
    }

    public class Intersection
    {
        public bool DoesIntersect = false;
        public Vector2 Point = Vector2.zero;
        public float Distance = -1f;
    }
}
