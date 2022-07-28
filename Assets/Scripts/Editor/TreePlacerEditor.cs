using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System;

public class TreePlacerEditor : EditorWindow
{
    Texture2D treeTex;
    Terrain terrain;
    GameObject treeParent;
    GameObject treePrefab;

    EditorCoroutine placeTreeCoroutine = null;

    int treeDistance = 3;
    bool treeBrushOn = false;

    float treeBrushSize = 10f;

    // Add menu item named "My Window" to the Window menu
    [MenuItem("Tools/Tree Placer")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(TreePlacerEditor));
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        treeTex = EditorGUILayout.ObjectField(treeTex, typeof(Texture2D), true) as Texture2D;
        terrain = EditorGUILayout.ObjectField(terrain, typeof(Terrain), true) as Terrain;
        treeParent = EditorGUILayout.ObjectField("parent", treeParent, typeof(GameObject), true) as GameObject;
        treePrefab = EditorGUILayout.ObjectField("prefab", treePrefab, typeof(GameObject), true) as GameObject;

        treeDistance = (int)EditorGUILayout.Slider("Tree Distance", treeDistance, 1, 10);

        if (GUILayout.Button("Place Trees"))
        {
            if (placeTreeCoroutine != null)
                EditorCoroutineUtility.StopCoroutine(placeTreeCoroutine);
            placeTreeCoroutine = EditorCoroutineUtility.StartCoroutine(PlaceTreesbasedonMapRoutine(), this);
        }

        if (GUILayout.Button("Stop!"))
        {
            EditorCoroutineUtility.StopCoroutine(placeTreeCoroutine);
        }

        Texture2D tex = EditorGUIUtility.FindTexture("tree_icon");

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.background = treeBrushOn ? MakeTex(10, 10, Color.red) : null;
        buttonStyle.normal.textColor = Color.red;
        if (GUILayout.Button(tex, buttonStyle)) treeBrushOn = !treeBrushOn;
        treeBrushSize = EditorGUILayout.Slider("Brush Size", treeBrushSize, 0, 50);




        if (GUILayout.Button(treeBrushOn.ToString()))
        {
            //
        }
    }
    void OnFocus()
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

                if ((e.type== EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag) && e.button == 0)
                    GUIUtility.hotControl = controlId;
            }
            SceneView.RepaintAll();
        }
        Handles.BeginGUI();
        Handles.EndGUI();
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

    private IEnumerator PlaceTreesbasedonMapRoutine()
    {
        for (int i = treeParent.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(treeParent.transform.GetChild(i).gameObject);
            if ((i % 100) == 1)
                yield return null;
        }

        Debug.Log("Old Trees Deleted");

        Vector2 textureSize = Vector2.one / treeTex.texelSize;

        for (int x = 0; x < textureSize.x; x += treeDistance)
        {
            for (int z = 0; z < textureSize.y; z += treeDistance)
            {
                bool shouldPlaceTree = CheckTreeAt(x, z);
                Vector3 terrainPos = GetTerrainPos(textureSize, x, z);

                if (shouldPlaceTree)
                {
                    Vector3 treePos = GetRandomTreePosFrom(terrainPos);
                    Instantiate(treePrefab, treePos, Quaternion.identity, treeParent.transform);
                    //Debug.DrawLine(treePos, treePos + Vector3.up, Color.green, 1f);
                }
            }
            yield return null;
        }

    }

    private Vector3 GetRandomTreePosFrom(Vector3 terrainPos)
    {
        float x = terrainPos.x + UnityEngine.Random.Range(0, treeDistance / 2f);
        float z = terrainPos.z + UnityEngine.Random.Range(0, treeDistance / 2f);
        float y = terrain.SampleHeight(new Vector3(x, 0, z));
        return new Vector3(x, y, z);
    }

    private Vector3 GetTerrainPos(Vector2 textureSize, float x, float y)
    {
        return new Vector3(terrain.terrainData.size.x * (x / textureSize.x), 0, terrain.terrainData.size.z * (y / textureSize.y));
    }

    private bool CheckTreeAt(int x, int y)
    {
        Color c = treeTex.GetPixel(x, y);
        float rgb = (c.r + c.g + c.b) / 3f;
        return (rgb > 0.5f);
    }
}
