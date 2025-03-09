using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GenerationHandScript : NetworkBehaviour
{
    [SerializeField]
    private Animator anim;

    public void ToggleHand(bool showHand)
    {
        anim.SetTrigger(showHand ? "ShowHand" : "HideHand");
    }
}
