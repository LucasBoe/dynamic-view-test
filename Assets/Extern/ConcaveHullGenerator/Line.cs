using System;
using UnityEngine;

namespace ConcaveHull
{
    public class Line
    {
        public Node[] nodes = new Node[2];
        public Vector2 SmoothLeft, SmoothRight;
        public Line(Node n1, Node n2)
        {
            nodes[0] = n1;
            nodes[1] = n2;

            Vector2 left = n1.Pos, right = n2.Pos;
            Vector2 perp = Vector2.Perpendicular((left - right).normalized);

            SmoothLeft = right + (left - right) * 0.9f + perp;
            SmoothRight = left + (right - left) * 0.9f + perp;
        }
        public static double getLength(Node node1, Node node2)
        {
            /* It actually calculates relative length */
            double length;
            length = Math.Pow(node1.y - node2.y, 2) + Math.Pow(node1.x - node2.x, 2);
            //length = Math.sqrt(Math.Pow(node1.y - node2.y, 2) + Math.Pow(node1.x - node2.x, 2));
            return length;
        }
    }
}