
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PatrolPoint : UdonSharpBehaviour
{
    public GameObject occupant;


#if !COMPILER_UDONSHARP && UNITY_EDITOR

    private void OnDrawGizmos()
    {
        /* Draw Sphere Above */
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up, 0.2f);


        /* Draw Quad Below */
        Gizmos.color = Color.white;
        Gizmos.DrawCube(transform.position + new Vector3(0, 0.01f, 0), new Vector3(1, 0.01f, 1));
    }

#endif

}
