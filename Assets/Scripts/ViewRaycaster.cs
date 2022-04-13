using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ViewRaycaster : MonoBehaviour
{
    [SerializeField] AnimationCurve distanceDebugCurve;
    [SerializeField] int checkAmount360 = 45;
    [SerializeField] int detailScanDepth = 8;
    [SerializeField] float minSignificantDistance = 5f, minSignificantHitDifference = 2f;
    [SerializeField] float viewFalloff = 0.2f;
    [SerializeField] int maxShadowDistance = 20;

    [SerializeField] int raycastCounterDebug = 0;

    Dictionary<float, AreaScanResult> scanData = new Dictionary<float, AreaScanResult>();

    private Dictionary<float, AreaScanResult> Do360Scan(int checks)
    {
        Dictionary<float, AreaScanResult> scan = new Dictionary<float, AreaScanResult>();
        for (int i = 0; i < checks; i++)
        {
            float angle = (i / (float)checks) * 360f;

            Vector3 hit = DoRaycast(transform.position, angle, maxShadowDistance, debug: true) - transform.position;

            AreaScanResult result = new AreaScanResult { Hit = hit, Distance = Mathf.Abs(hit.magnitude) };

            scan.Add(angle, result);
        }

        return scan;
    }
    private Dictionary<float, AreaScanResult> DoDetailScan(Dictionary<float, AreaScanResult> scanData, List<AreaScanDetail> extremes)
    {
        foreach (AreaScanDetail detail in extremes)
        {
            float angle = Mathf.LerpAngle(detail.StartAngle, detail.EndAngle, 0.5f);
            Vector3 hit = DoRaycast(transform.position, angle, maxShadowDistance, debug: true) - transform.position;
            if (!scanData.ContainsKey(angle))
                scanData.Add(angle, new AreaScanResult { Hit = hit, Distance = Mathf.Abs(hit.magnitude) });
        }

        return scanData;
    }

    // Update is called once per frame
    void Update()
    {
        raycastCounterDebug = 0;

        scanData.Clear();
        scanData = Do360Scan(checkAmount360);

        //List<AreaScanDetail> extremes = new List<AreaScanDetail>();
        //
        //for (int i = 0; i < detailScanDepth; i++)
        //{
        //    extremes = FetchExtremesByDistanceToOrigin(scanData);
        //    scanData = DoDetailScan(scanData, extremes);
        //}

        DrawDistanceCurve(scanData);
        CreatePositiveMesh((scanData.OrderBy(scan => scan.Key).ToArray()).Select(foo => foo.Value.Hit).ToArray());
    }


    private List<AreaScanDetail> FetchExtremesByDistanceToOrigin(Dictionary<float, AreaScanResult> scanData)
    {
        List<AreaScanDetail> results = new List<AreaScanDetail>();
        AreaScanInformation before = null;

        foreach (KeyValuePair<float, AreaScanResult> current in scanData.OrderBy(a => a.Key))
        {
            if (before != null)
            {
                bool bothAir = before.Value.Distance > 19 && current.Value.Distance > 19;
                bool sharesXOrY = (before.Value.Hit.x == current.Value.Hit.x || before.Value.Hit.y == current.Value.Hit.y);
                float distanceDifference = Mathf.Abs(before.Value.Distance - current.Value.Distance);
                float hitDifference = Vector2.Distance(before.Value.Hit, current.Value.Hit);

                if (!bothAir && !sharesXOrY && (distanceDifference > minSignificantDistance || hitDifference > minSignificantHitDifference))
                {
                    AreaScanDetail detail = new AreaScanDetail();
                    detail.StartAngle = before.Angle;
                    detail.EndAngle = current.Key;
                    results.Add(detail);
                }
            }

            before = new AreaScanInformation() { Angle = current.Key, Value = current.Value };
        }

        return results;
    }


    private void CreateNegativeMesh(Vector3[] vector2)
    {
        Mesh mesh;
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        mesh = GetComponent<MeshFilter>().mesh;

        for (int i = 0; i < vector2.Length; i++)
        {
            int index = i * 2;

            Vector3 fix = new Vector3(vector2[i].x, vector2[i].y, vector2[i].z);
            fix = fix.normalized * maxShadowDistance;
            newVertices.Add(new Vector3(fix.x, fix.y, fix.z));
            newVertices.Add(new Vector3(vector2[i].x, vector2[i].y, vector2[i].z));

            if (index > 0 && index < vector2.Length * 2)
            {
                newTriangles.Add(index);
                newTriangles.Add(index + 1);
                newTriangles.Add(index - 2);
                newTriangles.Add(index + 1);
                newTriangles.Add(index - 1);
                newTriangles.Add(index - 2);
            }

            if (i > 0)
            {
                Debug.DrawLine(transform.position + new Vector3(vector2[i - 1].x, 0, vector2[i - 1].y), transform.position + new Vector3(vector2[i].x, 0, vector2[i].y));
            }
        }

        newTriangles.Add(newVertices.Count - 2);
        newTriangles.Add(newVertices.Count - 1);
        newTriangles.Add(1);
        newTriangles.Add(newVertices.Count - 2);
        newTriangles.Add(1);
        newTriangles.Add(0);

        mesh.Clear();
        mesh.vertices = newVertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.Optimize();
    }

    private void CreatePositiveMesh(Vector3[] vector2)
    {
        Mesh mesh;
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        mesh = GetComponent<MeshFilter>().mesh;

        newVertices.Add(Vector3.zero);

        for (int i = 0; i < vector2.Length; i++)
        {
            int index = i;

            newVertices.Add(new Vector3(vector2[i].x, vector2[i].y, vector2[i].z));

            if (index > 1 && index < vector2.Length)
            {
                newTriangles.Add(0);
                newTriangles.Add(index - 1);
                newTriangles.Add(index);
            }

            if (i > 0)
            {
                Debug.DrawLine(transform.position + new Vector3(vector2[i - 1].x, 0, vector2[i - 1].y), transform.position + new Vector3(vector2[i].x, 0, vector2[i].y));
            }
        }

        newTriangles.Add(0);
        newTriangles.Add(vector2.Length - 1);
        newTriangles.Add(1);

        mesh.Clear();
        mesh.vertices = newVertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.Optimize();
    }

    Vector3 DoRaycast(Vector3 origin, float angle, float length, bool debug = false, int debugColorIndex = 0)
    {
        Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward + Vector3.down * viewFalloff;
        Debug.DrawRay(origin, dir * 1f);
        return DoRaycast(origin, dir, length, debug, debugColorIndex);
    }

    Vector3 DoRaycast(Vector3 origin, Vector3 dir, float length, bool debug = false, int debugColorIndex = 0)
    {
        raycastCounterDebug++;

        RaycastHit hit;

        if (Physics.Raycast(origin, dir, out hit, length, LayerMask.GetMask("Default")))
        {
            if (debug)
                Debug.DrawLine(origin, hit.point, (debugColorIndex == 0) ? Color.red : Color.green);

            return hit.point;
        }
        else
        {
            if (debug)
                Debug.DrawLine(origin, origin + (dir * length), (debugColorIndex == 0) ? Color.blue : Color.yellow);
        }

        return origin + (dir * length);
    }
    private void DrawDistanceCurve(Dictionary<float, AreaScanResult> scanData)
    {
        distanceDebugCurve = new AnimationCurve();
        foreach (KeyValuePair<float, AreaScanResult> scan in scanData)
        {
            distanceDebugCurve.AddKey(scan.Key, scan.Value.Distance);
        }
    }
}
public class AreaScanResult
{
    public Vector3 Hit;
    public float Distance;
}

public class AreaScanInformation
{
    public float Angle;
    public AreaScanResult Value;
}

public class AreaScanDetail
{
    public float StartAngle;
    public float EndAngle;
}
