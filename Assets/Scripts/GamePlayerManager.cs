using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class GamePlayerManager : NetworkBehaviour
{
    //general stuff
    private GameManager gameScript;
    public GameObject cardPrefab;
    public GameObject divinerCardPrefab;

    //player areas
    //VERY IMPORTANT NOTE: THESE VARIABELES ARE ONLY ASSIGNED IN THE CLIENT'S OWN PLAYERMANAGER
    //I.E. USE "NetworkClient.connection.identity.GetComponent<GamePlayerManager>().playerAreas[]"
    private readonly Dictionary<int, PlayerAreaManager> playerAreas = new Dictionary<int, PlayerAreaManager>();

    //profile containing id, name, and everything else
    public PlayerProfile myProfile;

    [Header("Audio")]
    [SerializeField]
    private AudioClip sfxDraw;
    [SerializeField]
    private AudioClip sfxFlip;
    [SerializeField]
    private AudioClip sfxDeath;
    [SerializeField]
    private AudioClip sfxBeginTarget;
    [SerializeField]
    private AudioClip sfxChosenTarget;
    [SerializeField]
    private AudioClip sfxPlace;


    public void HandlePresceneChange(string name, int icon, bool host)
    {
        myProfile.userName = name;
        myProfile.icon = icon;
        myProfile.isHost = host;
    }

    [TargetRpc]
    public void TargetInitialisePlayer(GamePlayerManager[] currentPlayers)
    {
        gameScript = FindObjectOfType<GameManager>();

        AssignPlayerAreas(currentPlayers);

        CmdInitialisationComplete();
    }

    [Command]
    private void CmdInitialisationComplete()
    {
        gameScript = FindObjectOfType<GameManager>();
        gameScript.initialisedCounter++;
    }

    private void AssignPlayerAreas(GamePlayerManager[] currentPlayers)
    {
        playerAreas.Clear();

        PlayerAreaManager[] tempPlayerAreas = FindObjectsOfType<PlayerAreaManager>();
        int playerIndex = myProfile.id;
        int keyID;

        for (int index = 0; index < currentPlayers.Length; index++)
        {
            if (tempPlayerAreas[index].isConnectedPlayer)
            {
                keyID = myProfile.id;
            }
            else
            {
                playerIndex = (playerIndex < currentPlayers.Length - 1) ? playerIndex + 1 : 0;
                keyID = playerIndex;
            }

            playerAreas.Add(keyID, tempPlayerAreas[index]);

            PlayerAreaManager area = playerAreas[keyID];
            GamePlayerManager player = currentPlayers[keyID];

            area.myPlayerID = keyID;
            area.UpdatePlayerName(player.myProfile.userName);
            area.UpdatePlayerIcon(player.myProfile.icon);
            area.ToggleDeath(false);
        }
    }

    //commands must begin with Cmd
    //client request procedure in server
    [Command]
    public void CmdDrawCard(int cardID, int amount, Transform hand)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject cardTemplate = CardDatabaseManager.CardDatabase[cardID].cardGeneration == -2 ? divinerCardPrefab : cardPrefab;
            GameObject instantiatedCard = Instantiate(cardTemplate, Vector2.zero, Quaternion.identity);
            NetworkServer.Spawn(instantiatedCard, connectionToClient);

            CardStats stats = instantiatedCard.GetComponent<CardStats>();
            DeepCopyCardFromDatabase(stats, cardID);
            stats.myStats.owner = this;

            //assign card visuals to owner
            TargetAssignCardStats(stats, cardID);

            //place cards for all players
            RpcDealtCard(instantiatedCard, hand);
        }
    }

    [TargetRpc]
    private void TargetAssignCardStats(CardStats stats, int newID)
    {
        //if host player, prevents overriding server values
        if (!isServer)
        {
            DeepCopyCardFromDatabase(stats, newID);
            stats.myStats.owner = this;
        }

        stats.UpdateCardText();
    }

    private void DeepCopyCardFromDatabase(CardStats stats, int newID)
    {
        stats.myStats = CardDatabaseManager.CardDatabase[newID].DeepCopy();
    }

    [ClientRpc]
    public void RpcUpdateManaText(int id, float newMana) 
    {
        string text;
        if (Mathf.Floor(newMana) != newMana)
        {
            text = newMana + "+";
        }
        else
        {
            text = newMana.ToString();
        }

        GamePlayerManager myPlayer = hasAuthority ? this : NetworkClient.connection.identity.GetComponent<GamePlayerManager>();
        myPlayer.playerAreas[id].UpdateManaText(text);  
    }
    
    [ClientRpc]
    public void RpcUpdateHealthText(int id, float newHealth)
    {
        string text;
        text = newHealth.ToString();

        GamePlayerManager myPlayer = hasAuthority ? this : NetworkClient.connection.identity.GetComponent<GamePlayerManager>();
        myPlayer.playerAreas[id].UpdateHealthText(text);
    }
    
    [ClientRpc]
    private void RpcDealtCard(GameObject card, Transform hand)
    {
        CardVisuals visuals = card.GetComponent<CardVisuals>();

        if (hasAuthority)
        {
            //flip card to show to player
            card.GetComponent<CardVisuals>().Flip();

            //move it to the player's hand
            visuals.ForefrontCard(false, hand);

            //show border if the card is playable
            CmdSingleCardPlayability(card);

            //sort hand
            SortPlayerHand(hand);

            //play audio
            FindObjectOfType<AudioManager>().PlayClipInstance(sfxDraw);
        }
    }

    [ClientRpc]
    public void RpcAllCardPlayability()
    {
        NetworkIdentity identity = NetworkClient.connection.identity;
        GamePlayerManager player = identity.GetComponent<GamePlayerManager>();
        player.CmdAllCardPlayability(player.GetPlayerCards());
    }

    [TargetRpc]
    public void TargetPlayerCardPlayability()
    {
        CmdAllCardPlayability(GetPlayerCards());
    }

    private CardStats[] GetPlayerCards() {
        List<CardStats> cards = new();
        cards.AddRange(FindObjectOfType<BaseHandRoot>().GetComponentsInChildren<CardStats>());
        cards.Add(FindObjectOfType<DragCardRoot>().GetComponentInChildren<CardStats>());
        return cards.ToArray();
    }

    [Command]
    private void CmdAllCardPlayability(CardStats[] stats)
    {
        for (int index = 0; index < stats.Length; index++)
        {
            if (stats[index]) 
            {
                SetCardPlayability(stats[index].gameObject);
            }
        }
    }

    [Command]
    private void CmdSingleCardPlayability(GameObject card)
    {
        SetCardPlayability(card);
    }

    [Server]
    private void SetCardPlayability(GameObject card)
    {
        CardStats stats = card.GetComponent<CardStats>();
        float cost = CalculateManaCost(stats.myStats);

        bool playable = false;
        //0 mana cards are always playable
        if (cost >= 0f)
        {
            if ((cost == 0f) || (stats.myStats.owner.myProfile.mana >= cost))
            {
                playable = true;
            }

            //update mana cost on server and client
            //if cost < 0, then card is unplayable and is not updated
            stats.myStats.manaCost = cost;
        }

        stats.GetComponent<CardVisuals>().ToggleDraggable(playable);
        stats.myStats.owner.TargetUpdateCardPlayability(stats, playable, cost);
    }

    [TargetRpc]
    private void TargetUpdateCardPlayability(CardStats stats, bool playable, float mana)
    {
        stats.GetComponent<CardVisuals>().ToggleDraggable(playable);

        if (mana >= 0f)
        {
            stats.myStats.manaCost = mana;
            stats.UpdateCardText();
        }
    }

    [Server]
    private float CalculateManaCost(CardInfo info) //returning negative value guarantees card cannot be played
    {
        float mana = CardDatabaseManager.CardDatabase[info.cardID].manaCost;

        mana += myProfile.manaCostOffset;

        if (myProfile.cardTypeManaOffset.TryGetValue(info.type, out float typeCost))
        {
            if (typeCost == 0) //0 offset means card type cannot be played
            {
                return -1f;
            }
            else
            {
                mana += typeCost;
            }
        }

        if (myProfile.indvCardManaOffset.TryGetValue(info.cardID, out float indvCost))
        {
            if (indvCost == 0) //0 offset means card of this id cannot be played
            {
                return -1f;
            }
            else
            {
                mana += indvCost;
            }
        }

        //you cannot have negative total mana (so that playing a card doesn't give you mana back)
        if (mana < 0f)
        {
            return 0f;
        }
        else
        {
            return mana;
        }
    }

    private void SortPlayerHand(Transform playerHand)
    {
        //sort by mana cost

        //bubble sort ascending
        bool didSwap = false;
        for (int loop = 0; loop < playerHand.childCount - 1; loop++)
        {
            for (int index = 1; index < playerHand.childCount - loop; index++)
            {
                if (GetCardInHand(playerHand, index).myStats.manaCost < GetCardInHand(playerHand, index - 1).myStats.manaCost)
                {
                    GetCardInHand(playerHand, index).transform.SetSiblingIndex(index - 1);

                    didSwap = true;
                }
            }

            if (!didSwap)
                break;
            else
                didSwap = false;
        }
    }

    private CardStats GetCardInHand(Transform playerHand, int index) => playerHand.GetChild(index).GetComponent<CardStats>();

    [Command]
    public void CmdTryPlayCard(GameObject card)
    {
        bool success = false;
        if ((myProfile.status == PlayerStatus.NotPlayed) && card.GetComponent<CardVisuals>().isDraggable && (myProfile.playedCard == null))
        {
            //assign card to server
            myProfile.playedCard = card.GetComponent<CardStats>();
            success = true;
        }

        RpcPlayedCard(myProfile.id, card, success);
    }

    [ClientRpc]
    private void RpcPlayedCard(int myID, GameObject card, bool success)
    {
        if (!success)
        {
            if (hasAuthority)
            {
                card.GetComponent<CardVisuals>().ForefrontCard(false);
            }

            return;
        }

        if (hasAuthority)
        {
            //card successfully played, placed in dropzone
            card.transform.SetParent(playerAreas[myID].dropZone, false);
            CheckIfTargets(card.GetComponent<CardStats>());
        }
        else
        {
            GamePlayerManager clientPlayer = NetworkClient.connection.identity.GetComponent<GamePlayerManager>();
            Transform enemyDropZone = clientPlayer.playerAreas[myID].dropZone;
            card.transform.SetParent(enemyDropZone, false);
        }

        card.GetComponent<CardVisuals>().ToggleDraggable(false);
        card.transform.localPosition = Vector2.zero;
        
        FindObjectOfType<AudioManager>().PlayClipInstance(sfxPlace);
    }

    private void CheckIfTargets(CardStats card)
    {
        if (card.myStats.doesTarget)
        {
            FindObjectOfType<AudioManager>().PlayClipInstance(sfxBeginTarget);
            CmdToggleOnTargets();
        }
        else
        {
            CmdPlayerFinishTurn();
        }
    }

    [Command]
    private void CmdToggleOnTargets()
    {
        List<int> targetIDs = new();
        for (int index = 0; index < GameManager.singleton.PlayerList.Count; index++)
        {
            PlayerProfile profile = GameManager.singleton.PlayerList[index].myProfile;

            if (profile.isTargetable)
            {
                targetIDs.Add(index);
            }
        }

        //if ALL players cannot be targeted, force card to target yourself
        if (targetIDs == null)
        {
            targetIDs.Add(myProfile.id);
        }

        TargetToggleHitboxes(targetIDs);
    }

    [TargetRpc]
    private void TargetToggleHitboxes(List<int> targetIDs) => ToggleHitboxes(targetIDs);

    [Client]
    public void ToggleHitboxes(List<int> targetIDs = null)
    {
        //not null = toggle hitboxes on
        if (targetIDs != null)
        {
            foreach (var player in targetIDs)
            {
                playerAreas[player].ToggleTargetHitbox(true);
            }
        } 
        else //null = toggle hitboxes off
        {
            for (int index = 0; index < playerAreas.Count; index++)
            {
                playerAreas[index].ToggleTargetHitbox(false);
            }
        }
    }

    [Command]
    public void CmdOnTargetPlayer(int targetID)
    {
        myProfile.playedCard.GetComponent<CardStats>().myStats.target = GameManager.singleton.PlayerList[targetID];

        TargetAfterPlayerSelected(myProfile.id, targetID);
    }

    [TargetRpc]
    private void TargetAfterPlayerSelected(int myID, int targetID)
    {
        FindObjectOfType<AudioManager>().PlayClipInstance(sfxChosenTarget);

        ToggleHitboxes();

        UISpawnArrow(myID, targetID);

        //player has fully completed their action
        CmdPlayerFinishTurn();
    }

    public void UISpawnArrow(int ownerID, int targetID)
    {
        gameScript.uiScript.SpawnArrow(playerAreas[ownerID].dropZone.position, playerAreas[targetID].iconRoot.position);
    }

    [Client]
    private void ResetPlayerDeaths()
    {
        for (int index = 0; index < playerAreas.Count; index++)
            playerAreas[index].ToggleDeath(false);
    } 

    [ClientRpc]
    public void RpcResetPlayerAreas()
    {
        NetworkIdentity identity = NetworkClient.connection.identity;
        GamePlayerManager player = identity.GetComponent<GamePlayerManager>();
        player.ToggleHitboxes();
        player.ResetPlayerDeaths();
    }

    [Command]
    private void CmdPlayerFinishTurn() => gameScript.PlayerFinishTurn(this, PlayerStatus.PlayedCard);

    [ClientRpc]
    public void RpcRevealEnemyCard(int ownerID, int cardID)
    {
        if (!hasAuthority)
        {
            GamePlayerManager clientPlayer = NetworkClient.connection.identity.GetComponent<GamePlayerManager>();
            CardStats enemyCard = clientPlayer.playerAreas[ownerID].dropZone.GetComponentInChildren<CardStats>();

            if (!isServer)
            {
                DeepCopyCardFromDatabase(enemyCard, cardID);
            }

            enemyCard.UpdateCardText();
            enemyCard.GetComponent<CardVisuals>().RevealEnemyCard();
        } else {
            FindObjectOfType<AudioManager>().PlayClipInstance(sfxFlip);
        }
    }

    [ClientRpc]
    public void RpcPlayerDeath(int playerID)
    {
        NetworkIdentity identity = NetworkClient.connection.identity;
        GamePlayerManager player = identity.GetComponent<GamePlayerManager>();

        //update player icon
        player.playerAreas[playerID].ToggleDeath(true);
        
        //death sound in addition to hurt sound
        FindObjectOfType<AudioManager>().PlayClipInstance(sfxDeath);
    }

    public void SetPlayerStatusToGrid(GameObject status, int ownerID)
    {
        playerAreas[ownerID].PlaceStatusInGrid(status);
    }

    [Command]
    public void CmdRequestReturnToLobby()
    {
        CustomNetworkManager networkScript = FindObjectOfType<CustomNetworkManager>();
        StartCoroutine(MenuTransitionManager.LoadingDelay(networkScript.SwitchScene, false));
        RpcToggleLoading(false);
    }

    [ClientRpc]
    public void RpcToggleLoading(bool exit)
    {
        StartCoroutine(MenuTransitionManager.LoadingDelay(null, exit));
    }

    [ClientRpc]
    public void RpcPlayAnimation(int id, string anim, bool onCard)
    {
        NetworkClient.connection.identity.GetComponent<GamePlayerManager>().playerAreas[id].PlayAnimation(anim, onCard);
    }
}
