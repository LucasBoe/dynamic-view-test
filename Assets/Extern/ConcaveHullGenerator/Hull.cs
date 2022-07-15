using Assets.src;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConcaveHull
{
    [System.Serializable]
    public class Hull
    {
        public List<Node> UnusedNodes = new List<Node>();
        public List<Line> HullEdges = new List<Line>();
        public List<Line> HullConcaveEdges = new List<Line>();

        public List<Vector2> HullPoints = new List<Vector2>();

        public Bounds2D Bounds = new Bounds2D();

        public List<Line> GetHull(List<Node> nodes)
        {
            List<Node> convexH = new List<Node>();
            List<Line> exitLines = new List<Line>();

            convexH.AddRange(GrahamScan.CreateConvexHull(nodes));
            for (int i = 0; i < convexH.Count - 1; i++)
            {
                exitLines.Add(new Line(convexH[i], convexH[i + 1]));
            }
            exitLines.Add(new Line(convexH[convexH.Count - 1], convexH[0]));
            return exitLines;
        }

        public void SetConvexHull(List<Node> nodes)
        {
            UnusedNodes.AddRange(nodes);
            HullEdges.AddRange(GetHull(nodes));
            foreach (Line line in HullEdges)
            {
                foreach (Node node in line.nodes)
                {
                    UnusedNodes.RemoveAll(a => a.id == node.id);
                }
            }
        }

        public List<Line> SetConcaveHull(List<Node> nodes, double concavity, int scaleFactor)
        {
            Debug.Log($"nodes.Count = { nodes.Count }");

            UnusedNodes.AddRange(nodes);
            HullEdges.AddRange(GetHull(nodes));
            foreach (Line line in HullEdges)
            {
                foreach (Node node in line.nodes)
                {
                    UnusedNodes.RemoveAll(a => a.id == node.id);
                }
            }
            /* Concavity is a value used to restrict the concave angles 
             * It can go from -1 (no concavity) to 1 (extreme concavity) 
             * Avoid concavity == 1 if you don't want 0º angles
             * */
            bool aLineWasDividedInTheIteration;
            HullConcaveEdges.AddRange(HullEdges);
            do
            {
                aLineWasDividedInTheIteration = false;
                for (int linePositionInHull = 0; linePositionInHull < HullConcaveEdges.Count && !aLineWasDividedInTheIteration; linePositionInHull++)
                {
                    Line line = HullConcaveEdges[linePositionInHull];
                    List<Node> nearbyPoints = HullFunctions.getNearbyPoints(line, UnusedNodes, scaleFactor);
                    List<Line> dividedLine = HullFunctions.getDividedLine(line, nearbyPoints, HullConcaveEdges, concavity);
                    if (dividedLine.Count > 0)
                    { // Line divided!
                        aLineWasDividedInTheIteration = true;
                        UnusedNodes.Remove(UnusedNodes.Where(n => n.id == dividedLine[0].nodes[1].id).FirstOrDefault()); // Middlepoint no longer free
                        HullConcaveEdges.AddRange(dividedLine);
                        HullConcaveEdges.RemoveAt(linePositionInHull); // Divided line no longer exists
                    }
                }

                HullConcaveEdges = HullConcaveEdges.OrderByDescending(a => Line.getLength(a.nodes[0], a.nodes[1])).ToList();
            } while (aLineWasDividedInTheIteration);

            Bounds.Min = new Vector2(float.MaxValue, float.MaxValue);
            Bounds.Max = new Vector2(float.MinValue, float.MinValue);

            foreach (Line line in HullConcaveEdges)
            {
                HullPoints.Add(line.SmoothRight);
                HullPoints.Add(line.SmoothLeft);

                Bounds.Max.x = Mathf.Max(Bounds.Max.x, line.SmoothRight.x, line.SmoothLeft.x);
                Bounds.Max.y = Mathf.Max(Bounds.Max.y, line.SmoothRight.y, line.SmoothLeft.y);
                Bounds.Min.x = Mathf.Min(Bounds.Min.x, line.SmoothRight.x, line.SmoothLeft.x);
                Bounds.Min.y = Mathf.Min(Bounds.Min.y, line.SmoothRight.y, line.SmoothLeft.y);
            }

            HullPoints = sortVerticies(HullPoints);

            return HullConcaveEdges;
        }

        public void GizmoDrawHull()
        {
            Gizmos.color = Color.red;

            for (int i = 0; i < HullPoints.Count; i++)
            {
                int ii = (i == 0 ? HullPoints.Count : i) - 1;
                Gizmos.DrawLine(Vector2Util.ToV3(HullPoints[ii]), Vector2Util.ToV3(HullPoints[i]));
            }

        }

        public void GizmoDrawBounds()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(Vector2Util.ToV3(Bounds.Min.x, Bounds.Min.y), Vector2Util.ToV3(Bounds.Min.x, Bounds.Max.y));
            Gizmos.DrawLine(Vector2Util.ToV3(Bounds.Min.x, Bounds.Max.y), Vector2Util.ToV3(Bounds.Max.x, Bounds.Max.y));
            Gizmos.DrawLine(Vector2Util.ToV3(Bounds.Max.x, Bounds.Max.y), Vector2Util.ToV3(Bounds.Max.x, Bounds.Min.y));
            Gizmos.DrawLine(Vector2Util.ToV3(Bounds.Max.x, Bounds.Min.y), Vector2Util.ToV3(Bounds.Min.x, Bounds.Min.y));
        }

        public List<Vector2> sortVerticies(List<Vector2> points)
        {
            // get centroid
            Vector2 center = findCentroid(points);
            return points.OrderBy(x => Mathf.Atan2(x.x - center.x, x.y - center.y)).ToList();
        }

        public Vector2 findCentroid(List<Vector2> points)
        {
            float x = 0;
            float y = 0;
            foreach (Vector2 item in points)
            {
                x += item.x;
                y += item.y;
            }
            Vector2 center = new Vector2(0, 0);
            center.x = x / points.Count;
            center.y = y / points.Count;
            return center;
        }
    }

    [System.Serializable]
    public class Bounds2D
    {
        public Vector2 Min, Max;
        public Vector2 MinMax => new Vector2(Min.x, Max.y);
        public Vector2 MaxMin => new Vector2(Max.x, Min.y);
    }
}