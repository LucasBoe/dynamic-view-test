using UnityEngine;

namespace ConcaveHull {
    public class Node {

        public int id;
        public Vector2 Pos;
        public double x => Pos.x;
        public double y => Pos.y;
        public double cos; // Used for middlepoint calculations
        public Node(Vector2 pos, int id) {
            this.Pos = pos;
            this.id = id;
        }
    }
}