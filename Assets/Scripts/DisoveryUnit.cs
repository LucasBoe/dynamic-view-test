using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisoveryUnit : MonoBehaviour
{
    [SerializeField] DiscoveryMeshCreator discovery;
    [SerializeField] VisibilityMeshCreator visibilityMeshCreator;

    // Update is called once per frame
    void Update()
    {
        discovery.Discover(transform.position, (int)visibilityMeshCreator.SmallestDistance + 5);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, visibilityMeshCreator.SmallestDistance + 5);
    }
}
