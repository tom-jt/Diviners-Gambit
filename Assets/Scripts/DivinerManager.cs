using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DivinerManager : NetworkBehaviour
{
    [Header("Assignments")]
    [SerializeField]
    private DrawCardManager drawScript;
    [SerializeField]
    private PlayerHandManager handScript;
    [SerializeField]
    private PlayerResourcesManager resourcesScript;

    private void OnDisable()
    {
        GameManager.OnTurnEnd -= OnDivinerPicked;
    }

    //diviners are gen -2
    //active abilities of diviners are gen -1
    //first round allow players to pick a diviner
    //then replace diviner cards with the active ability they chose
    public void InitialiseDivinerPicker()
    {
        drawScript.RpcGiveCardsByGeneration(-2, handScript.CreateNewHand(-1));
        handScript.RpcChangeHandIndex(-1);

        GameManager.OnTurnEnd += OnDivinerPicked;
    }

    private void OnDivinerPicked()
    {
        foreach (var player in GameManager.singleton.PlayerList)
        {
            if (player.myProfile.playedCard)
            {
                player.myProfile.chosenDivinerID = player.myProfile.playedCard.myStats.cardID;
            }
        }

        //delete diviner cards
        CardStats[] allCards = FindObjectsOfType<CardStats>();
        for (int index = 0; index < allCards.Length; index++)
            NetworkServer.Destroy(allCards[index].gameObject);

        resourcesScript.AfterDivinerResources();

        GameManager.OnTurnEnd -= OnDivinerPicked;
    }
}
