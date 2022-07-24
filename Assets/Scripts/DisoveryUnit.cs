using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisoveryUnit : MonoBehaviour
{
    [SerializeField] DiscoveryMeshCreator discovery;

    // Update is called once per frame
    void Update()
    {
        discovery.Discover(transform.position, 20);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, 20);
    }
}
