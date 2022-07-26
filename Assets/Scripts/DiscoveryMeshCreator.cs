using NaughtyAttributes;
using System.Linq;
using UnityEngine;

public class DiscoveryMeshCreator : MonoBehaviour
{
    private Vector3[] vertices;
    public int xSize, zSize;
    public int resolution = 3;

    [SerializeField] Terrain terrain;
    [SerializeField] Texture2D texture;

    internal void Discover(Vector3 position, int radius)
    {
        int x = (int)position.x / resolution, y = (int)position.z / resolution;
        int rad = radius / resolution;
        float rSquared = rad * rad;

        Color color = Color.Lerp(Color.white, Color.gray, 1f);

        for (int u = x - rad; u < x + rad + 1; u++)
            for (int v = y - rad; v < y + rad + 1; v++)
                if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared)
                    texture.SetPixel(u, v, color);

        texture.Apply();

        GetComponent<MeshRenderer>().sharedMaterial.renderQueue = 4000;
        GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
    }

    [Button]
    private void Generate()
    {

        Mesh mesh = new Mesh();
        mesh.name = "Discovery";

        xSize = (int)(terrain.terrainData.size.x / resolution);
        zSize = (int)(terrain.terrainData.size.z / resolution);

        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++, i++)
            {
                Vector3 origin = terrain.transform.position;
                Vector3 xz = new Vector3(x * resolution, 0, z * resolution);
                vertices[i] = new Vector3(x * resolution, terrain.SampleHeight(origin + xz), z * resolution);
                uv[i] = new Vector2(x / (float)xSize, z / (float)zSize);
            }
        }
        mesh.vertices = vertices;

        int[] triangles = new int[xSize * zSize * 6];
        for (int ti = 0, vi = 0, y = 0; y < zSize; y++, vi++)
        {
            for (int x = 0; x < xSize; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
                triangles[ti + 5] = vi + xSize + 2;
            }
        }
        mesh.triangles = triangles;
        mesh.vertices = vertices;
        mesh.uv = uv;

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        texture = new Texture2D(xSize, zSize);
        Color[] pixels = Enumerable.Repeat(Color.black, xSize * zSize).ToArray();
        texture.SetPixels(pixels);
        texture.Apply();
    }
}
