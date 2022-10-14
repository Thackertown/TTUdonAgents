
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VRCMirrorToggle : UdonSharpBehaviour
{
    [SerializeField] private GameObject mirrorToToggle;

    void Start()
    {
        
    }

    public override void Interact()
    {
        mirrorToToggle.SetActive(!mirrorToToggle.activeInHierarchy);

    }
}
