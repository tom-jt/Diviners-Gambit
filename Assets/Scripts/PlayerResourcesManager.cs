using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerResourcesManager : NetworkBehaviour
{
    [Header("Assignments")]
    [SerializeField]
    private HealthManaManager hmScript;
    [SerializeField]
    private DrawCardManager drawScript;
    [SerializeField]
    private PlayerHandManager handScript;
    [SerializeField]
    private DivinerManager divScript;
    [SerializeField]
    private StatusEffectsManager statusScript;

    //server only
    public float startingHealth = 3f;
    public float startingMana = 2f;
    public int gameMode = 1;
    public bool diviners = false;
    public string cardPool = CardDatabaseManager.EmptyCardPool;

    public void ResourcesUpdateGameSettings(int _health, int _mana, int _gameMode, bool _diviners, string _cardPool)
    {
        startingHealth = _health;
        startingMana = _mana;
        gameMode = _gameMode;
        diviners = _diviners;

        if ((_cardPool.Length != CardDatabaseManager.EmptyCardPool.Length) || (_cardPool == CardDatabaseManager.EmptyCardPool))
        {
            _cardPool = CardDatabaseManager.DefaultCardPool;
            Debug.LogWarning("Card pool is invalid, resetting to default pool..");
        }
        cardPool = _cardPool;
    }

    [Server]
    public bool InitialisePlayerResources()
    {
        hmScript.InitialiseManaHealth(startingHealth, startingMana);

        if (diviners)
        {
            divScript.InitialiseDivinerPicker();
        }
        else
        {
            AfterDivinerResources();
        }

        return diviners;
    }

    public void AfterDivinerResources()
    {
        InitialiseGameMode();
        InitialiseHand();
    }

    public void ResetPlayerResources()
    {
        hmScript.InitialiseManaHealth(startingHealth, startingMana);
        InitialiseHand();
    }

    public void InitialiseHand()
    {
        bool firstGen = true;
        if (diviners)
        {
            foreach (var player in GameManager.singleton.PlayerList)
            {
                if (player.myProfile.chosenDivinerID != -1)
                {
                    //get all the active abilities of a diviner
                    //which should have IDs immediately following the ID of the diviner
                    int activeAbilityID = player.myProfile.chosenDivinerID + 1;
                    while ((activeAbilityID < CardDatabaseManager.CardDatabase.Count) && (CardDatabaseManager.CardDatabase[activeAbilityID].cardGeneration == -1))
                    {
                        drawScript.TargetObtainCard(player.connectionToClient, activeAbilityID, handScript.GetHand(-1));
                        activeAbilityID++;
                    }
                }
            }

            //set default hand
            handScript.RpcChangeHandIndex(-1);
        }

        //card pool
        //draw the generations listed 1 in card pool string
        for (int gen = 0; gen < cardPool.Length; gen++)
        {
            if (cardPool[gen] == '1')
            {
                drawScript.RpcGiveCardsByGeneration(gen, handScript.CreateNewHand(gen));

                //set default hand
                if (firstGen)
                    handScript.RpcChangeHandIndex(gen);

                firstGen = false;
            }
        }

        handScript.RpcHandAnimations();
    }

    private void InitialiseGameMode()
    {
        int statusIndex = -1;
        switch (gameMode)
        {
            case 2:
                statusIndex = 30;
                break;
            case 3:
                statusIndex = 31;
                break;
            case 4:
                statusIndex = 32;
                break;
            default:
                break;
        }

        if (statusIndex != -1)
        {
            for (int player = 0; player < GameManager.singleton.PlayerList.Count; player++)
            {
                statusScript.SpawnStatusEffect(GameManager.singleton.PlayerList[player], statusIndex, -1, false);
            }
        }
    }

    public bool PayCostForCard(int playerID, float changeAmount)
    {
        //check to prevent players being able to play cards they cannot afford
        //this usually does not happen unless mana is changed during the turn by other card effects
        PlayerProfile playerProfile = GameManager.singleton.PlayerList[playerID].myProfile;
        if ((changeAmount < 0) && (playerProfile.mana < Mathf.Abs(changeAmount)))
            return false;

        return hmScript.ChangeMana(playerID, changeAmount);
    }

    public void RefillCard(CardInfo cardInfo)
    {
        if (cardInfo.cardGeneration <= -1)
            return;

        drawScript.TargetObtainCard(cardInfo.owner.connectionToClient, cardInfo.cardID, handScript.GetHand(cardInfo.cardGeneration));
    }
}
