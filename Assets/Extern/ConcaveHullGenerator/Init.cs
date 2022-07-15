using UnityEngine;using System.Collections.Generic;namespace ConcaveHull
{    public class Init : MonoBehaviour
    {        List<Node> dot_list = new List<Node>(); //Used only for the demo
        Hull hull;        public string seed;        public int scaleFactor;        public int number_of_dots;        public double concavity;        void Start()
        {            dot_list = GetRandomDots(number_of_dots); //Used only for the demo
            GenerateHull();        }        public void GenerateHull()
        {            hull = new Hull();            hull.SetConcaveHull(dot_list, concavity, scaleFactor);        }        public List<Node> GetRandomDots(int number_of_dots)
        {
            List<Node> dots = new List<Node>();
            // This method is only used for the demo!
            System.Random pseudorandom = new System.Random(seed.GetHashCode());            for (int x = 0; x < number_of_dots; x++)
            {                dots.Add(new Node(new Vector2(pseudorandom.Next(0, 100), pseudorandom.Next(0, 100)), x));            }
            //Delete nodes that share same position
            for (int pivot_position = 0; pivot_position < dots.Count; pivot_position++)
            {                for (int position = 0; position < dots.Count; position++)
                {
                    if (dots[pivot_position].x == dots[position].x && dots[pivot_position].y == dots[position].y                        && pivot_position != position)
                    {                        dots.RemoveAt(position);                        position--;                    }
                }
            }            return dots;        }

        // Unity demo visualization
        void OnDrawGizmos()
        {            if (hull == null) return;

            // Convex hull
            Gizmos.color = Color.yellow;            for (int i = 0; i < hull.HullEdges.Count; i++)
            {                Vector2 left = new Vector2((float)hull.HullEdges[i].nodes[0].x, (float)hull.HullEdges[i].nodes[0].y);                Vector2 right = new Vector2((float)hull.HullEdges[i].nodes[1].x, (float)hull.HullEdges[i].nodes[1].y);                Gizmos.DrawLine(left, right);            }

            // Concave hull
            Gizmos.color = Color.blue;            for (int i = 0; i < hull.HullConcaveEdges.Count; i++)
            {                Vector2 left = new Vector2((float)hull.HullConcaveEdges[i].nodes[0].x, (float)hull.HullConcaveEdges[i].nodes[0].y);                Vector2 right = new Vector2((float)hull.HullConcaveEdges[i].nodes[1].x, (float)hull.HullConcaveEdges[i].nodes[1].y);                Gizmos.DrawLine(left, right);            }

            // Dots
            Gizmos.color = Color.red;            for (int i = 0; i < dot_list.Count; i++)
            {                Gizmos.DrawSphere(new Vector3((float)dot_list[i].x, (float)dot_list[i].y, 0), 0.5f);            }
        }    }}