using ConcaveHull;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Vector2Util;

[ExecuteInEditMode]
public class VisibilityMeshCreator : MonoBehaviour
{
    [SerializeField] float innerRadius;
    [SerializeField] float outerRadius;
    [SerializeField] float falloff;
    [SerializeField, Range(4, 512)] int raycastAmount = 12;
    [SerializeField, Range(0.1f, 90f)] float fillerSize = 22.5f;
    [SerializeField] TreeClusterCreator treeClusters;

    [SerializeField, ReadOnly] List<TreeCluster> clustersInRadius;
    [SerializeField, ReadOnly] List<float> openAngles;
    [SerializeField, ReadOnly] List<float> closeAngles;

    private void OnDrawGizmos()
    {
        foreach (TreeCluster cluster in clustersInRadius)
        {
            cluster.Hull.GizmoDrawBounds();
        }
    }

    // Update is called once per frame
    void Update()
    {
        clustersInRadius = treeClusters.GetClustersInsideRange(transform.position.ToV2(), innerRadius + outerRadius);

        //rim points
        List<RimPoints> rimPoints = new List<RimPoints>();
        List<Obstruction> obstructions = new List<Obstruction>();

        int counter = 0;

        foreach (TreeCluster cluster in clustersInRadius)
        {
            RimPoints clusterPoints = cluster.Hull.FindRimPoints(transform.position.ToV2());
            if (clusterPoints != null)
            {
                clusterPoints.ClusterIndex = counter;
                rimPoints.Add(clusterPoints);
                obstructions.Add(new Obstruction(clusterPoints, counter));
            }

            counter++;
        }

        List<RimPoints> openPoints = rimPoints.OrderBy(p => GetAngleFromTo(Vector2.zero, p.Smallest.Point)).ToList();
        List<RimPoints> closePoints = rimPoints.OrderBy(p => GetAngleFromTo(Vector2.zero, p.Biggest.Point)).ToList();

        //debug
        openAngles.Clear();
        closeAngles.Clear();

        foreach (RimPoints point in openPoints)
        {
            openAngles.Add(point.Smallest.Angle);
        }

        foreach (RimPoints point in closePoints)
        {
            closeAngles.Add(point.Biggest.Angle);
        }

        List<Vector3> meshPoints = new List<Vector3>();

        float angle = -180f;
        List<RimPoints> toClose = new List<RimPoints>();

        int loopCount = 0;

        while (loopCount < 100 && angle < 180f)
        {
            loopCount++;

            RimPoints nextToOpen = GetNext(openPoints, angle, small: true);
            RimPoints nextToClose = GetNext(closePoints, angle, small: false);


            float nextOpenAngle = nextToOpen == null ? float.MaxValue : nextToOpen.Smallest.Angle;
            float nextCloseAngle = nextToClose == null ? float.MaxValue : nextToClose.Biggest.Angle;

            const float angleOffset = 1f;

            bool nextIsOpen = (nextOpenAngle < nextCloseAngle) && nextToOpen != null;
            bool nextIsClose = (nextCloseAngle < nextOpenAngle) && nextToClose != null;

            //Debug.Log("current: " + angle + " next: " + (nextIsOpen ? nextOpenAngle : nextCloseAngle));

            if ((!nextIsOpen && !nextIsClose) || !nextIsClose && Mathf.Abs(Mathf.DeltaAngle(angle, nextOpenAngle)) > fillerSize)
            {
                angle += fillerSize;
                meshPoints.Add(CheckForPointAtAngle(angle));
            }
            else
            {

                GUIStyle styleB = new GUIStyle();
                styleB.normal.textColor = Color.blue;

                GUIStyle styleR = new GUIStyle();
                styleR.normal.textColor = Color.red;

                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color[] { Color.magenta, Color.blue, Color.green, Color.red, Color.yellow }[(nextIsOpen ? nextToOpen.ClusterIndex : nextToClose.ClusterIndex) % 5];

                if (nextIsOpen)
                {
                    RimPoint next = nextToOpen.Smallest;

                    if (!IsObstructed(obstructions, next))
                    {
                        float distance = Mathf.Min(next.Point.magnitude, innerRadius + outerRadius);

                        meshPoints.Add(CheckForPointAtAngle((next.Angle) - angleOffset));
                        //Handles.Label(transform.position + meshPoints.Last(), (next.Angle - angleOffset).ToString(), style);
                        meshPoints.Add(Get3DPointFromLength((next.Angle), distance) - transform.position);
                        //Handles.Label(transform.position + meshPoints.Last(), next.Angle.ToString(), style);

                    }
                    //else
                    //{
                    //    Vector3 p = Get3DPointFromLength(next.Angle, innerRadius + outerRadius) - transform.position;
                    //    Debug.DrawLine(transform.position + p, transform.position, Color.red);
                    //    Handles.Label(transform.position + p, "\n" + next.Angle.ToString(), styleR);
                    //}

                    angle = next.Angle;
                    toClose.Add(nextToOpen);
                }
                else if (nextIsClose)
                {
                    RimPoint next = nextToClose.Biggest;

                    if (!IsObstructed(obstructions, next))
                    {

                        float distance = Mathf.Min(next.Point.magnitude, innerRadius + outerRadius);

                        meshPoints.Add(Get3DPointFromLength(next.Angle, distance) - transform.position);
                        //Handles.Label(transform.position + meshPoints.Last(), (next.Angle).ToString(), style);
                        meshPoints.Add(CheckForPointAtAngle((next.Angle) + angleOffset));
                        //Handles.Label(transform.position + meshPoints.Last(), (next.Angle + angleOffset).ToString(), style);

                    }
                    //else
                    //{
                    //    Vector3 p = Get3DPointFromLength(next.Angle, innerRadius + outerRadius) - transform.position;
                    //    Debug.DrawLine(transform.position + p, transform.position, Color.red);
                    //    Handles.Label(transform.position + p, "\n" + next.Angle.ToString(), styleR);
                    //}
                    angle = next.Angle;
                    toClose.Remove(nextToClose);

                }

                toClose = toClose.OrderBy(c => c.Biggest.Angle).ToList();
            }
        }

        meshPoints = meshPoints.OrderBy(p => Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg).ToList();

        string str = "";

        foreach (Vector3 meshPoint in meshPoints)
        {
            Vector3 p = (meshPoint).normalized;
            float a = Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;
            float d = meshPoint.magnitude;
            str += a + " - " + d + "\n";
        }

        //Debug.Log(str);

        //create mesh
        CreatePositiveMesh(meshPoints.ToArray());
    }

    private bool IsObstructed(List<Obstruction> obstructions, RimPoint point)
    {
        float distance = point.Point.magnitude;
        foreach (Obstruction obstruction in obstructions)
        {
            bool insideRange = Mathf.DeltaAngle(point.Angle, obstruction.Start) < 0f && Mathf.DeltaAngle(point.Angle, obstruction.End) > 0f;
            bool notSelf = obstruction.Points.Smallest != point && obstruction.Points.Biggest != point;
            if (insideRange && distance > obstruction.Distance && notSelf)
            {
                //Debug.Log(obstruction.Index + " is obstructing " + point.Angle + " with min: " + obstruction.Start + " and max: " + obstruction.End);
                return true;
            }
        }

        return false;
    }

    private RimPoints GetNext(List<RimPoints> rimPoints, float angle, bool small)
    {
        if (rimPoints == null || rimPoints.Count == 0) return null;

        if (small)
        {
            foreach (RimPoints smallest in rimPoints.OrderBy(r => r.Smallest.Angle))
            {
                if (smallest.Smallest.Angle > angle)
                    return smallest;
            }
        }
        else
        {
            foreach (RimPoints biggest in rimPoints.OrderBy(r => r.Biggest.Angle))
            {
                if (biggest.Biggest.Angle > angle)
                    return biggest;
            }
        }

        return null;
    }
    private Vector3 CheckForPointAtAngle(float angleToCheck)
    {
        const int raycastSize = 10;

        float x = Mathf.Sin(angleToCheck * Mathf.Deg2Rad) * innerRadius;
        float y = Mathf.Cos(angleToCheck * Mathf.Deg2Rad) * innerRadius;


        Vector3 p = transform.position + new Vector3(x, raycastSize, y);

        Ray ray = new Ray(p, Vector3.down);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, raycastSize, LayerMask.GetMask("Terrain")))
        {
            p = hit.point;
        }
        else
        {
            float length = outerRadius + innerRadius;
            Vector2 startP = transform.position.ToV2();
            Vector2 targetP = transform.position.ToV2() + new Vector2(x, y).normalized * length;

            V2Line line = new V2Line(startP, targetP);

            Intersection intersection = GetClosestIntersectionPoint(line);
            if (intersection.DoesIntersect) length = intersection.Distance;

            p = Get3DPointFromLength(angleToCheck, length);
        }

        Debug.DrawLine(transform.position, p, Color.Lerp(Color.black, Color.gray, (angleToCheck + 180f) / 360f));

        p -= transform.position;
        return p;
    }

    private Vector3 Get3DPointFromLength(float angleToCheck, float length)
    {
        float x = Mathf.Sin(angleToCheck * Mathf.Deg2Rad) * innerRadius;
        float y = Mathf.Cos(angleToCheck * Mathf.Deg2Rad) * innerRadius;

        int height = 100;

        Vector3 p = transform.position + new Vector3(x, 0, y).normalized * length;

        Ray ray = new Ray(p + Vector3.up * height / 2f, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, height, LayerMask.GetMask("Terrain")))
        {
            return hit.point;
        }

        //return Vector3.zero;
        return p + length * falloff * Vector3.down;
    }

    private void CreatePositiveMesh(Vector3[] points)
    {
        Mesh mesh;
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        mesh = GetComponent<MeshFilter>().sharedMesh;

        newVertices.Add(Vector3.zero);

        for (int i = 0; i <= points.Length; i++)
        {
            int index = i;

            if (index < points.Length)
                newVertices.Add(new Vector3(points[i].x, points[i].y, points[i].z));

            if (index > 1)
            {
                newTriangles.Add(0);
                newTriangles.Add(index - 1);
                newTriangles.Add(index);
            }
        }

        newTriangles.Add(0);
        newTriangles.Add(points.Length);
        newTriangles.Add(1);

        mesh.Clear();
        mesh.vertices = newVertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.Optimize();
    }

    private Intersection GetClosestIntersectionPoint(V2Line line)
    {
        List<Intersection> intersections = new List<Intersection>();

        foreach (TreeCluster cluster in clustersInRadius)
        {
            List<Vector2> hull = cluster.Hull.HullPoints;
            for (int i = 0; i < hull.Count; i++)
            {
                int ii = (i == 0 ? hull.Count : i) - 1;

                Intersection intersection = line.GetIntersection(new V2Line(hull[i], hull[ii]));

                if (intersection.DoesIntersect)
                    intersections.Add(intersection);
            }
        }

        if (intersections.Count == 0) return new Intersection();

        return intersections.OrderBy(i => Vector2.Distance(i.Point, new Vector2(line.X1, line.Y1))).FirstOrDefault();
    }
}

public class Obstruction
{
    public int Index;
    public float Start, End;
    public float Distance;
    public RimPoints Points;
    const float ANGLE_ADDITION = 0.0f;

    public Obstruction(RimPoints clusterPoints, int index)
    {
        Index = index;
        Points = clusterPoints;
        Start = clusterPoints.Smallest.Angle - ANGLE_ADDITION;
        End = clusterPoints.Biggest.Angle + ANGLE_ADDITION;
        Distance = (clusterPoints.Smallest.Point.magnitude + clusterPoints.Biggest.Point.magnitude) / 2f;
    }
}
