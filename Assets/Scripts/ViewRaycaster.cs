using System;
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

    private Dictionary<float, AreaScanInformation> DoHeightAngleScan(int checks)
    {
        Dictionary<float, AreaScanInformation> scan = new Dictionary<float, AreaScanInformation>();
        for (int i = 0; i < checks; i++)
        {
            float angleX = (i / (float)checks) * 360f;
            float angleUp = 0f;

            RaycastHitResult lastResult = new RaycastHitResult() { HasHit = true };

            while (angleUp < 45f)
            {
                angleUp += 4.5f;
                RaycastHitResult newResult = DoRaycast(transform.position, angleX, maxShadowDistance, upAngle: angleUp);

                if (!newResult.HasHit)
                    break;

                if (angleUp >= 45f)
                {
                    angleUp = 0f;
                    break;
                }
            }

            Vector3 local = lastResult.Point - transform.position;


            AreaScanInformation result = new AreaScanInformation() { Value = new AreaScanResult() { Hit = local, Distance = Mathf.Abs(local.magnitude) }, Angle = angleUp };

            scan.Add(angleX, result);
        }

        return scan;
    }

    private Dictionary<float, AreaScanResult> Do360Scan(int checks, Dictionary<float, AreaScanInformation> additionalHeigtSamples = null)
    {
        Dictionary<float, AreaScanResult> scan = new Dictionary<float, AreaScanResult>();

        for (int i = 0; i < checks; i++)
        {
            float angleX = (i / (float)checks) * 360f;
            float angleHeight = additionalHeigtSamples == null ? 0 : Sample(additionalHeigtSamples, angleX) * 0.75f;

            Vector3 global = DoRaycast(transform.position, angleX, angleHeight, maxShadowDistance);
            Vector3 local = global - transform.position;

            AreaScanResult result = new AreaScanResult { Hit = local, Distance = Mathf.Abs(local.magnitude) };

            scan.Add(angleX, result);
        }

        return scan;
    }

    private float Sample(Dictionary<float, AreaScanInformation> additionalHeigtSamples, float angleX)
    {
        float sampleAngleMaxDifference = 360 / additionalHeigtSamples.Count;

        float[] angles = additionalHeigtSamples.Keys.ToArray();

        for (int i = 0; i < additionalHeigtSamples.Count; i++)
        {
            bool last = i == additionalHeigtSamples.Count - 1;

            float previousX = angles[i];
            float nextX = angles[last ? 0 : i + 1];

            float prevDelta = Mathf.Abs(Mathf.DeltaAngle(angleX, previousX));
            float nextDelta = Mathf.Abs(Mathf.DeltaAngle(angleX, nextX));

            if (prevDelta < sampleAngleMaxDifference && nextDelta < sampleAngleMaxDifference)
            {
                float lerp = prevDelta / sampleAngleMaxDifference;

                float prevHeight = additionalHeigtSamples[previousX].Angle;
                float nextHeight = additionalHeigtSamples[nextX].Angle;
                float height = Mathf.LerpAngle(prevHeight, nextHeight, lerp);

                return height * 1f;
            }
            else if (prevDelta < 1f)
            {
                return additionalHeigtSamples[previousX].Angle;
            }
            else if (nextDelta < 1f)
            {
                return additionalHeigtSamples[nextX].Angle;
            }
        }


        Debug.Log($"angleX = { angleX }");

        return 0f;
    }
    public float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    // Update is called once per frame
    void Update()
    {
        raycastCounterDebug = 0;
        scanData.Clear();

        Dictionary<float, AreaScanInformation> additionalHeigtSamples = DoHeightAngleScan(36);
        scanData = Do360Scan(checkAmount360, additionalHeigtSamples);

        DrawDistanceCurve(scanData);
        CreatePositiveMesh((scanData.OrderBy(scan => scan.Key).ToArray()).Select(foo => foo.Value.Hit).ToArray());
    }

    private void CreatePositiveMesh(Vector3[] points)
    {
        Mesh mesh;
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        mesh = GetComponent<MeshFilter>().mesh;

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

    RaycastHitResult DoRaycast(Vector3 origin, float angle, float length, bool debug = false, int debugColorIndex = 0, float upAngle = 0f)
    {
        Vector3 dir = ((Quaternion.Euler(0, angle, 0) * Vector3.forward) + Mathf.Sin(upAngle / Mathf.Rad2Deg) * Vector3.up).normalized;
        Debug.DrawRay(origin, dir * 1f);
        return DoRaycast(origin, dir, length, debug, debugColorIndex);
    }
    Vector3 DoRaycast(Vector3 origin, float angle, float upAngle, float length)
    {
        Vector3 dir = ((Quaternion.Euler(0, angle, 0) * Vector3.forward) + Mathf.Sin(upAngle / Mathf.Rad2Deg) * Vector3.up).normalized;
        Debug.DrawRay(origin, dir * 1f);

        RaycastHitResult result = DoRaycast(origin, dir, length, false);

        if (result.HasHit)
            return result.Point;

        return origin + dir * length;
    }

    RaycastHitResult DoRaycast(Vector3 origin, Vector3 dir, float length, bool debug = false, int debugColorIndex = 0)
    {
        raycastCounterDebug++;

        RaycastHit hit;

        if (Physics.Raycast(origin, dir, out hit, length, LayerMask.GetMask("Default")))
        {
            if (debug)
                Debug.DrawLine(origin, hit.point, (debugColorIndex == 0) ? Color.red : Color.green);

            return new RaycastHitResult() { HasHit = true, Point = hit.point };
        }
        else
        {
            if (debug)
                Debug.DrawLine(origin, origin + (dir * length), (debugColorIndex == 0) ? Color.blue : Color.yellow);
        }

        return new RaycastHitResult() { HasHit = false, Point = hit.point };
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

public struct RaycastHitResult
{
    public bool HasHit;
    public Vector3 Point;
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
