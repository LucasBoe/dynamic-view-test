using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConcaveHull;

[ExecuteInEditMode]
public class TreeClusterCreator : MonoBehaviour
{
    [SerializeField] float treshhold;
    [SerializeField] int scaleFactor;
    [SerializeField] double concavity;
    [SerializeField, ReadOnly] List<TreeCluster> clusters = new List<TreeCluster>();

    [Button]
    public void CreateClusters()
    {
        StartCoroutine(ClusterRoutine());
    }

    IEnumerator ClusterRoutine()
    {
        List<Transform> trees = new List<Transform>();

        foreach (Transform child in transform)
        {
            trees.Add(child);
        }

        List<Transform> unsortedTrees = new List<Transform>(trees);
        clusters = new List<TreeCluster>();

        while (unsortedTrees.Count > 0)
        {
            Transform startTree = unsortedTrees[0];
            unsortedTrees.RemoveAt(0);

            bool partOfOtherCluster = false;

            foreach (TreeCluster cluster in clusters)
            {
                if (!partOfOtherCluster)
                {
                    foreach (Transform tree in cluster.Trees)
                    {
                        if (DistanceCheck(startTree, tree))
                        {
                            partOfOtherCluster = true;
                            cluster.Trees.Add(startTree);
                            break;
                        }
                    }
                }
            }

            if (!partOfOtherCluster)
            {
                TreeCluster newCluster = new TreeCluster();
                newCluster.Trees.Add(startTree);
                clusters.Add(newCluster);
            }
        }
        yield return null;

        foreach (TreeCluster cluster in clusters)
        {
            cluster.CalculatedCenterPoint();
            cluster.Hull = new Hull();
            cluster.Hull.SetConcaveHull(cluster.GetTreeNodes(), concavity, scaleFactor);
        }
    }

    public List<TreeCluster> GetClustersInsideRange(Vector2 pos, float maxDistance)
    {
        List<TreeCluster> clusters = new List<TreeCluster>();

        foreach (TreeCluster cluster in this.clusters)
        {
            float distance = Mathf.Min(
                Vector2.Distance(pos, Vector2Util.GetClosestPointOnLineSegment(new Vector2Util.V2Line(cluster.Hull.Bounds.Min, cluster.Hull.Bounds.MinMax), pos)),
                Vector2.Distance(pos, Vector2Util.GetClosestPointOnLineSegment(new Vector2Util.V2Line(cluster.Hull.Bounds.MinMax, cluster.Hull.Bounds.Max), pos)),
                Vector2.Distance(pos, Vector2Util.GetClosestPointOnLineSegment(new Vector2Util.V2Line(cluster.Hull.Bounds.Max, cluster.Hull.Bounds.MaxMin), pos)),
                Vector2.Distance(pos, Vector2Util.GetClosestPointOnLineSegment(new Vector2Util.V2Line(cluster.Hull.Bounds.MaxMin, cluster.Hull.Bounds.Min), pos)));

            if (distance < maxDistance)
                clusters.Add(cluster);
        }

        return clusters;
    }

    private bool DistanceCheck(Transform startTree, Transform tree)
    {
        Vector2 p1 = new Vector2(tree.position.x, tree.position.z);
        Vector2 p2 = new Vector2(startTree.position.x, startTree.position.z);

        float distance = Vector2.Distance(p1, p2);

        bool insideRange = distance < treshhold;
        Debug.DrawLine(tree.position, startTree.position, insideRange ? Color.green : Color.red, 1f);
        return insideRange;
    }

    private void OnDrawGizmosSelected()
    {
        for (int j = 0; j < clusters.Count; j++)
        {
            TreeCluster cluster = clusters[j];
            NewRandomGizmoColorFromIndex(j);

            for (int i = 0; i < cluster.Trees.Count; i++)
            {
                int ii = (i == 0 ? cluster.Trees.Count : i) - 1;
                Gizmos.DrawLine(cluster.Trees[i].position, cluster.Trees[ii].position);
            }

            Hull hull = cluster.Hull;
            if (hull != null && hull.HullPoints != null)
            {
                hull.GizmoDrawHull();
                hull.GizmoDrawBounds();
            }
        }
    }

    private static void NewRandomGizmoColorFromIndex(int index)
    {
        UnityEngine.Random.InitState(index);
        Gizmos.color = new Color[] { Color.green, Color.blue, Color.cyan, Color.magenta }[UnityEngine.Random.Range(0, 4)];
    }
}

[System.Serializable]
public class TreeCluster
{
    public List<Transform> Trees = new List<Transform>();
    public Hull Hull;
    public Vector2 Center;

    internal void CalculatedCenterPoint()
    {
        for (int i = 0; i < Trees.Count; i++)
        {
            Center += Trees[i].position.ToV2();
        }

        Center /= Trees.Count;
    }

    internal List<Node> GetTreeNodes()
    {
        List<Node> nodes = new List<Node>();

        for (int i = 0; i < Trees.Count; i++)
        {
            Transform transform = Trees[i];
            nodes.Add(new Node(new Vector2(transform.position.x, transform.position.z), i));
        }

        return nodes;
    }
}
