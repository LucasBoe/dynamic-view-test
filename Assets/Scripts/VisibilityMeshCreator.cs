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
    [SerializeField] TreeClusterCreator treeClusters;

    [SerializeField, ReadOnly] List<TreeCluster> clustersInRadius;

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, innerRadius);

        clustersInRadius = treeClusters.GetClustersInsideRange(transform.position.ToV2(), innerRadius + outerRadius);
        foreach (TreeCluster cluster in clustersInRadius)
        {
            cluster.Hull.GizmoDrawBounds();
        }


        int raycastAmount = 32;
        int raycastSize = 10;

        List<Vector3> viewAreaPoints = new List<Vector3>();

        for (int i = 0; i < raycastAmount; i++)
        {
            float theta = i * 2 * Mathf.PI / raycastAmount;
            float x = Mathf.Sin(theta) * innerRadius;
            float y = Mathf.Cos(theta) * innerRadius;


            Vector3 p = transform.position + new Vector3(x, raycastSize, y);
            Vector3 p2;

            Ray ray = new Ray(p, Vector3.down);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, raycastSize, LayerMask.GetMask("Terrain")))
            {
                p = hit.point;
            }
            else
            {
                p = new Vector3(p.x, p.y - raycastSize, p.z);

                Vector2 startP = transform.position.ToV2();
                float r = outerRadius * innerRadius;
                Vector2 targetP = p.ToV2() + new Vector2(x, y).normalized * outerRadius * innerRadius;


                V2Line line = new V2Line(startP, targetP);

                Intersection intersection = GetClosestIntersectionPoint(line);

                if (intersection.DoesIntersect)
                    r = intersection.Distance;

                p = transform.position + new Vector3(x, 0, y).normalized * r + r * falloff * Vector3.down;
            }

            Gizmos.DrawLine(transform.position, p);
            Gizmos.DrawSphere(p, 0.15f);

            p -= transform.position;
            viewAreaPoints.Add(p);
        }

        CreatePositiveMesh(viewAreaPoints.ToArray());
    }

    private void CreatePositiveMesh(Vector3[] points)
    {
        Mesh mesh;
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        mesh = GetComponent<MeshFilter>().sharedMesh;

        newVertices.Add(Vector3.zero);

        for (int i = 0; i < points.Length; i++)
        {
            int index = i;

            newVertices.Add(new Vector3(points[i].x, points[i].y, points[i].z));

            if (index > 1 && index < points.Length)
            {
                newTriangles.Add(0);
                newTriangles.Add(index - 1);
                newTriangles.Add(index);
            }

            if (i > 0)
            {
                Debug.DrawLine(transform.position + new Vector3(points[i - 1].x, 0, points[i - 1].y), transform.position + new Vector3(points[i].x, 0, points[i].y));
            }
        }

        newTriangles.Add(0);
        newTriangles.Add(points.Length - 1);
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
