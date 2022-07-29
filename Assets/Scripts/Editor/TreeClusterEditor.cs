using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

[CustomEditor(typeof(TreeClusterCreator))]
public class TreeClusterEditor : Editor
{
    bool treeBrushOn = false;
    float treeBrushSize = 10f;
    float treeDistance = 3f;

    private void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (treeBrushOn)
        {
            Event e = Event.current;
            //Ray ray = sceneView.camera.ScreenPointToRay(Event.current.mousePosition);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, float.MaxValue, LayerMask.GetMask("Terrain")))
            {
                Handles.color = new Color(1, 1, 1, 0.2f);
                Handles.DrawSolidDisc(hit.point, Vector3.up, treeBrushSize);
                Handles.color = Color.white;
                Handles.DrawWireDisc(hit.point, Vector3.up, treeBrushSize);

                TreeClusterCreator clusterHolder = target as TreeClusterCreator;

                List<TreeClusterData> clusters = clusterHolder.GetClustersInsideRange(hit.point.ToV2(), treeBrushSize);

                TreeClusterData closest = TreeClusterCreator.GetClosestClusterToPoint(clusters, hit.point, treeBrushSize);

                foreach (TreeClusterData c in clusters)
                {
                    Handles.color = Color.gray;
                    ConcaveHull.Bounds2D bounds = c.Hull.Bounds;
                    Handles.DrawLine(Vector2Util.ToV3(bounds.Min.x, bounds.Min.y), Vector2Util.ToV3(bounds.Min.x, bounds.Max.y));
                    Handles.DrawLine(Vector2Util.ToV3(bounds.Min.x, bounds.Max.y), Vector2Util.ToV3(bounds.Max.x, bounds.Max.y));
                    Handles.DrawLine(Vector2Util.ToV3(bounds.Max.x, bounds.Max.y), Vector2Util.ToV3(bounds.Max.x, bounds.Min.y));
                    Handles.DrawLine(Vector2Util.ToV3(bounds.Max.x, bounds.Min.y), Vector2Util.ToV3(bounds.Min.x, bounds.Min.y));

                    Handles.color = c == closest ? Color.yellow : Color.white;
                    Vector3[] points = c.Hull.HullPoints.Select(h => h.ToV3()).ToArray();
                    Handles.DrawAAPolyLine(points);
                }

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag) && e.button == 0)
                    GUIUtility.hotControl = controlId;

                Handles.color = Color.white;

                int steps = Mathf.RoundToInt(treeBrushSize * 2 / treeDistance);

                float size = treeBrushSize;

                for (int x = 0; x < steps; x++)
                {
                    for (int y = 0; y < steps; y++)
                    {
                        int xx = Mathf.RoundToInt((hit.point.x - size + x * treeDistance) / treeDistance) * (int)treeDistance;
                        int yy = Mathf.RoundToInt((hit.point.z - size + y * treeDistance) / treeDistance) * (int)treeDistance;
                        UnityEngine.Random.InitState(xx * yy);
                        float randX = UnityEngine.Random.Range(-treeDistance / 3f, treeDistance / 3f);
                        UnityEngine.Random.InitState(xx * yy + 1);
                        float randY = UnityEngine.Random.Range(-treeDistance / 3f, treeDistance / 3f);
                        Vector2 pos = new Vector2(treeDistance / 2f + xx + randX, treeDistance / 2f + yy + randY);

                        if (Vector2.Distance(hit.point.ToV2(), pos) < treeBrushSize)
                            Handles.DrawSolidDisc(pos.ToV3(hit.point.y), Vector3.up, 0.25f);
                    }
                }
            }

            SceneView.RepaintAll();
        }
        Handles.BeginGUI();
        Handles.EndGUI();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Texture2D tex = EditorGUIUtility.FindTexture("tree_icon");

        GUIStyle ToggleButtonStyleNormal = "ObjectField";

        GUIStyle ToggleButtonStyleToggled = "Button";

        ToggleButtonStyleToggled.alignment = TextAnchor.MiddleCenter;


        if (GUILayout.Button(tex, treeBrushOn ? ToggleButtonStyleNormal : ToggleButtonStyleToggled)) treeBrushOn = !treeBrushOn;
        treeBrushSize = EditorGUILayout.Slider("Brush Size", treeBrushSize, 0, 50);
        treeDistance = EditorGUILayout.Slider("Brush Size", treeDistance, 1, 10);
    }
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }
}
