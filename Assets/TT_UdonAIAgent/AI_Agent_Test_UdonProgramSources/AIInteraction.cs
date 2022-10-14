
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AIInteraction : UdonSharpBehaviour
{
    public TTAIAgent aiController;

    public override void Interact()
    {
        aiController.currentState = (aiController.currentState == 0) ? 1 : 0;
    }
}
