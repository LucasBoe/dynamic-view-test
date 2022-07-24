using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    Vector2 target;
    [SerializeField] private float walkSpeed = 3f;

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Input.GetMouseButtonDown(0) && Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
            target = hit.point.ToV2();

        Vector2 newPos = Vector2.MoveTowards(transform.position.ToV2(), target, Time.deltaTime * walkSpeed);

        ray = new Ray(newPos.ToV3(100), Vector3.down);

        if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Terrain")))
        {
            transform.position = hit.point;
        }
    }
}
