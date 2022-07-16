using ConcaveHull;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        //upate clusters
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

        foreach (TreeCluster cluster in clustersInRadius)
        {
            RimPoints clusterPoints = cluster.Hull.FindRimPoints(transform.position.ToV2());
            if (clusterPoints != null)
            {
                rimPoints.Add(clusterPoints);
                obstructions.Add(new Obstruction(clusterPoints));
            }
        }

        List<RimPoints> openPoints = rimPoints.OrderBy(p => GetAngleFromTo(Vector2.zero, p.Smallest.OnPoint)).ToList();
        List<RimPoints> closePoints = rimPoints.OrderBy(p => GetAngleFromTo(Vector2.zero, p.Biggest.OnPoint)).ToList();

        //debug
        openAngles.Clear();
        closeAngles.Clear();

        foreach (RimPoints point in openPoints)
        {
            openAngles.Add(point.Smallest.OnAngle);
        }

        foreach (RimPoints point in closePoints)
        {
            closeAngles.Add(point.Biggest.OnAngle);
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

            float nextOpenAngle = nextToOpen == null ? float.MaxValue : nextToOpen.Smallest.OnAngle;
            float nextCloseAngle = nextToClose == null ? float.MaxValue : nextToClose.Biggest.OnAngle;

            bool nextIsOpen = (nextOpenAngle < nextCloseAngle) && nextToOpen != null;
            bool nextIsClose = (nextCloseAngle < nextOpenAngle) && nextToClose != null;

            if ((!nextIsOpen && !nextIsClose) || !nextIsClose && Mathf.Abs(Mathf.DeltaAngle(angle, nextOpenAngle)) > fillerSize)
            {
                angle += fillerSize;
                meshPoints.Add(CheckForPointAtAngle(angle));
            }
            else
            {

                if (nextIsOpen)
                {
                    if (!IsObstructed(obstructions, nextToOpen.Smallest))
                    {
                        meshPoints.Add(CheckForPointAtAngle((nextToOpen.Smallest.OnAngle) - 1f));
                        Debug.DrawLine(transform.position + meshPoints.Last(), transform.position, Color.cyan);

                        float distance = Mathf.Min(nextToOpen.Smallest.OnPoint.magnitude, innerRadius + outerRadius);
                        meshPoints.Add(Get3DPointFromLength(nextToOpen.Smallest.OnAngle, distance) - transform.position);
                    }

                    angle = nextToOpen.Smallest.OnAngle + 0.5f;
                    toClose.Add(nextToOpen);
                }
                else if (nextIsClose)
                {
                    if (!IsObstructed(obstructions, nextToClose.Biggest))
                    {
                        float distance = Mathf.Min(nextToClose.Biggest.OnPoint.magnitude, innerRadius + outerRadius);
                        meshPoints.Add(Get3DPointFromLength(nextToClose.Biggest.OnAngle, distance) - transform.position);
                        meshPoints.Add(CheckForPointAtAngle((nextToClose.Biggest.OnAngle) + 1f));
                        Debug.DrawLine(transform.position + meshPoints.Last(), transform.position, Color.blue);
                    }
                    angle = nextToClose.Biggest.OnAngle + 0.5f;
                    toClose.Remove(nextToClose);

                }

                toClose = toClose.OrderBy(c => c.Biggest.OnAngle).ToList();
            }
        }

        //create mesh
        CreatePositiveMesh(meshPoints.ToArray());
    }

    private bool IsObstructed(List<Obstruction> obstructions, RimPoint point)
    {
        float distance = point.OnPoint.magnitude;
        foreach (Obstruction obstruction in obstructions)
        {
            bool insideRange = Mathf.DeltaAngle(point.OnAngle, obstruction.Start) < 0f && Mathf.DeltaAngle(point.OnAngle, obstruction.End) > 0f;
            if (insideRange && distance > obstruction.Distance)
                return true;
        }

        return false;
    }

    private RimPoints GetNext(List<RimPoints> rimPoints, float angle, bool small)
    {
        if (rimPoints == null || rimPoints.Count == 0) return null;

        if (small)
        {
            foreach (RimPoints smallest in rimPoints.OrderBy(r => r.Smallest.OnAngle))
            {
                if (smallest.Smallest.OnAngle > angle)
                    return smallest;
            }
        }
        else
        {
            foreach (RimPoints biggest in rimPoints.OrderBy(r => r.Biggest.OnAngle))
            {
                if (biggest.Biggest.OnAngle > angle)
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

        return transform.position + new Vector3(x, 0, y).normalized * length + length * falloff * Vector3.down;
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
    public float Start, End;
    public float Distance;
    public RimPoints Points;

    public Obstruction(RimPoints clusterPoints)
    {
        Points = clusterPoints;
        Start = clusterPoints.Smallest.OnAngle;
        End = clusterPoints.Biggest.OnAngle;
        Distance = (clusterPoints.Smallest.OnPoint.magnitude + clusterPoints.Biggest.OnPoint.magnitude) / 2f;
    }
}
