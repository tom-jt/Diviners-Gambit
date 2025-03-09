using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DrawCardManager : NetworkBehaviour
{
    [ClientRpc]
    public void RpcGiveCardsByGeneration(int generation, Transform hand)
    {
        Transform handRoot = FindObjectOfType<BaseHandRoot>().transform;
        if (hand.parent != handRoot)
        {
            hand.SetParent(handRoot, false);
        }

        NetworkIdentity identity = NetworkClient.connection.identity;
        GamePlayerManager playerScript = identity.GetComponent<GamePlayerManager>();

        for (int index = 0; index < CardDatabaseManager.CardDatabase.Count; index++)
        {
            //find every card that belongs to this generation and draw it
            if (CardDatabaseManager.CardDatabase[index].cardGeneration == generation)
            {
                playerScript.CmdDrawCard(index, 1, hand);
            }
        }
    }

    [TargetRpc]
    public void TargetObtainCard(NetworkConnection conn, int id, Transform hand)
    {
        NetworkIdentity identity = NetworkClient.connection.identity;
        GamePlayerManager playerScript = identity.GetComponent<GamePlayerManager>();

        playerScript.CmdDrawCard(id, 1, hand);
    }
}
