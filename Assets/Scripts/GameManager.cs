using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mirror;
using TMPro;

public delegate void CardAction(CardInfo info);

public class GameManager : NetworkBehaviour
{
    public static GameManager singleton;
    public readonly List<GamePlayerManager> PlayerList = new List<GamePlayerManager>();

    public static Action OnTurnEnd;
    public static Action OnNewGameStart;
    public static Action OnFinishExecuteCardEffect;

    public static CardAction OnCardPlayed;

    private int turnCounter;
    public int TurnCounter 
    { 
        get => turnCounter; 
        private set 
        { 
            turnCounter = value;

            RpcUpdateTurnText(value == 0 ? "Picking Diviners" : "Turn " + value.ToString()); 
        } 
    }

    //clients var
    private int gamesWon = 0;
    private int gamesLost = 0;
    private int gamesTied = 0;

    [HideInInspector]
    public int initialisedCounter = 0;

    [Header("General Settings")]
    public float betweenRoundDelay = 1f;
    public float betweenGameDelay = 1.5f;
    public float betweenCardAnimationDelay = 1f;

    [Header("Assignments")]
    public VfxUIManager uiScript;
    [SerializeField]
    private PlayerResourcesManager resourcesScript;
    [SerializeField]
    private Transform enemyGridRoot;
    [SerializeField]
    private GameObject enemyAreaPrefab;
    [SerializeField]
    private Animator gameMsgAnimator;
    [SerializeField]
    private TextMeshProUGUI turnCounterText;
    [SerializeField]
    private TextMeshProUGUI gameCounterText;
    [SerializeField]
    private AudioClip sfxNewTurn;

    private void Start()
    {
        if (!singleton)
            singleton = this;
    }

    [Server]
    public void OnGameStart(GamePlayerManager[] currentPlayers)
    {
        ResetEvents();

        SpawnEnemyAreas(currentPlayers.Length);

        PlayerList.Clear();
        PlayerList.AddRange(currentPlayers);

        for (int index = 0; index < PlayerList.Count; index++)
        {
            GamePlayerManager player = PlayerList[index];

            player.myProfile.id = index;
            player.myProfile.status = PlayerStatus.NotPlayed;
            player.myProfile.canUseEffects = true;
            player.myProfile.isTargetable = true;

            StartCoroutine(WaitForSyncVar(index, currentPlayers));
        }

        initialisedCounter = 0;
        StartCoroutine(WaitForPlayerInitialise());
    }

    private void ResetEvents()
    {
        OnTurnEnd = null;
        OnFinishExecuteCardEffect = null;
    }

    private void SpawnEnemyAreas(int totalPlayers)
    {
        PlayerAreaManager[] enemyAreas = enemyGridRoot.GetComponentsInChildren<PlayerAreaManager>();
        if (enemyAreas.Length >= 1)
            return;

        for (int index = 0; index < totalPlayers - 1; index++)
        {
            GameObject instantiatedArea = Instantiate(enemyAreaPrefab, Vector2.zero, Quaternion.identity);
            NetworkServer.Spawn(instantiatedArea);
            RpcEnemyAreaInGrid(instantiatedArea);
        }
    }

    [ClientRpc]
    private void RpcEnemyAreaInGrid(GameObject enemyArea)
    {
        enemyArea.transform.SetParent(enemyGridRoot, false);
    }

    private IEnumerator WaitForSyncVar(int index, GamePlayerManager[] currentPlayers)
    {
        yield return null;

        PlayerList[index].TargetInitialisePlayer(currentPlayers);
    }

    private IEnumerator WaitForPlayerInitialise()
    {
        yield return new WaitUntil(delegate { return initialisedCounter >= PlayerList.Count; });

        bool divinerPicker = resourcesScript.InitialisePlayerResources();
        TurnCounter = divinerPicker ? 0 : 1;

        //does not matter which player manager calls it, just chose the first player on the list
        PlayerList[0].RpcResetPlayerAreas();
        RpcUpdateGameText();
    }

    public void PlayerFinishTurn(GamePlayerManager player, PlayerStatus value)
    {
        player.myProfile.status = value;

        for (int status = 0; status < PlayerList.Count; status++)
        {
            if (PlayerList[status].myProfile.status == PlayerStatus.NotPlayed)
                return;
        }

        RoundEnd();
    }

    private void RoundEnd()
    {
        //remove target arrows already on screen
        RpcDestroyArrows();

        //loop thorugh each card and execute their effects on server
        StartCoroutine(ExecuteCardEffects());
    }

    [ClientRpc]
    private void RpcDestroyArrows() => uiScript.DestroyArrows();

    private IEnumerator ExecuteCardEffects()
    {
        //refill and reveal played cards
        HandleAllPlayedCards();

        foreach (var info in GetAllCardsWithPrio(CardPriority.DontExecute))
        {
            yield return new WaitForSeconds(betweenCardAnimationDelay);
            ExecuteEffect(info);
        }

        foreach (var info in GetUniquePrioCards(CardPriority.EffectNegate))
        {
            yield return new WaitForSeconds(betweenCardAnimationDelay);
            ExecuteEffect(info);
        }

        foreach (var info in GetUniquePrioCards(CardPriority.ManaLost))
        {
            yield return new WaitForSeconds(betweenCardAnimationDelay);
            ExecuteEffect(info);
        }

        foreach (var info in GetAllCardsWithPrio(CardPriority.Early))
        {
            yield return new WaitForSeconds(betweenCardAnimationDelay);
            ExecuteEffect(info);
        }

        foreach (var info in GetAllCardsWithPrio(CardPriority.Normal))
        {
            yield return new WaitForSeconds(betweenCardAnimationDelay);
            ExecuteEffect(info);
        }

        foreach (var info in GetAllCardsWithPrio(CardPriority.Late))
        {
            yield return new WaitForSeconds(betweenCardAnimationDelay);
            ExecuteEffect(info);
        }

        yield return new WaitForSeconds(betweenCardAnimationDelay);

        OnFinishExecuteCardEffect?.Invoke();

        yield return null;

        FinishExecuteCardEffects();
    }

    private List<CardInfo> GetAllCardsWithPrio(CardPriority prio)
    {
        List<CardInfo> infos = new List<CardInfo>();

        for (int index = 0; index < PlayerList.Count; index++)
        {
            if (PlayerList[index].myProfile.playedCard)
            {
                CardInfo info = PlayerList[index].myProfile.playedCard.myStats;

                if (info.priority == prio)
                {
                    infos.Add(info);
                }
            }
        }

        return infos;
    }

    private List<CardInfo> GetUniquePrioCards(CardPriority prio)
    {
        List<CardInfo> prioInfos = GetAllCardsWithPrio(prio);
        List<CardInfo> uniqueInfos = new List<CardInfo>();
        List<CardInfo> forbiddenInfos = new List<CardInfo>();

        if (prioInfos.Count == 1)
        {
            return prioInfos;
        }

        //loops through all cards with this priority
        foreach (var info in prioInfos)
        {
            if (info.doesTarget)
            {
                //if card targets,
                //check if the target's mana cost is higher or lower
                //the card with higher mana cost gets added to the unique list, and the lower one gets removed (if added previously)
                CardInfo targetInfo = info.target.myProfile.playedCard.myStats;

                if (targetInfo.priority == prio)
                {
                    if (info.manaCost != targetInfo.manaCost)
                    {
                        CardInfo higherInfo = info.manaCost > targetInfo.manaCost ? info : targetInfo;
                        CardInfo lowerInfo = info.manaCost > targetInfo.manaCost ? targetInfo : info;

                        //list must not already have this, and also must not already been checked and deemed to be targeted by a higher mana cost card
                        if (!uniqueInfos.Contains(higherInfo) && !forbiddenInfos.Contains(higherInfo))
                        {
                            uniqueInfos.Add(higherInfo);
                        }

                        if (uniqueInfos.Contains(lowerInfo))
                        {
                            uniqueInfos.Remove(lowerInfo);
                        }

                        forbiddenInfos.Add(lowerInfo);
                    }
                }
                else
                {
                    if (!uniqueInfos.Contains(info))
                    {
                        uniqueInfos.Add(info);
                    }
                }
            }
            else
            {
                //if the card doesnt target, (implies it affects all/most players)
                //loop through all other prio cards
                //if this card is the highest mana card (or tied highest), it will be the only card to execute (thus clearing the list and breaking)
                bool isHighestMana = true;
                for (int index = 0; index < uniqueInfos.Count; index++)
                {
                    if (uniqueInfos[index].manaCost > info.manaCost)
                    {
                        isHighestMana = false;
                        break;
                    }
                }

                if (isHighestMana)
                {
                    uniqueInfos.Clear();
                    uniqueInfos.Add(info);
                    break;
                }
            }
        }

        //returns a list of cards that should be executed
        return uniqueInfos;
    }

    private void ExecuteEffect(CardInfo info)
    {
        //loads each cards and execute its effects
        //deducts mana first
        //if mana cannot be deducted, then the card will not execute
        if (!resourcesScript.PayCostForCard(info.owner.myProfile.id, -info.manaCost))
        {
            return;
        }

        if ((info.priority != CardPriority.DontExecute) && info.owner.myProfile.canUseEffects)
        {
            info.effect?.Invoke(info);
            OnCardPlayed?.Invoke(info);
        }

        if (info.doesTarget && info.target != null) //extra safety net that the target exists
        {
            RpcSpawnArrows(info.owner.myProfile.id, info.target.myProfile.id);
        }
    }

    private void FinishExecuteCardEffects()
    {
        //update playable cards in hand after mana is used
        PlayerList[0].RpcAllCardPlayability();

        //check if a player is dead
        DidPlayerDie();

        //wait before resetting round
        StartCoroutine(RoundEndWait());
    }

    private void HandleAllPlayedCards()
    {
        for (int index = 0; index < PlayerList.Count; index++)
        {
            if (PlayerList[index].myProfile.playedCard)
            {
                CardInfo info = PlayerList[index].myProfile.playedCard.myStats;

                //reveal cards to all players
                info.owner.RpcRevealEnemyCard(info.owner.myProfile.id, info.cardID);

                //give the card back to the player's hand
                resourcesScript.RefillCard(info);
            }
        }
    }

    [ClientRpc]
    private void RpcSpawnArrows(int ownerID, int targetID)
    {
        NetworkIdentity identity = NetworkClient.connection.identity;
        GamePlayerManager player = identity.GetComponent<GamePlayerManager>();
        player.UISpawnArrow(ownerID, targetID);
    }

    private void DidPlayerDie()
    {
        for (int id = 0; id < PlayerList.Count; id++)
        {
            if (PlayerList[id].myProfile.status != PlayerStatus.Dead)
            {
                if (PlayerList[id].myProfile.health <= 0)
                {
                    //that player is dead
                    PlayerList[id].myProfile.status = PlayerStatus.Dead;
                    PlayerList[id].myProfile.isTargetable = false;
                    PlayerList[0].RpcPlayerDeath(id);
                }
            }
        }
    }

    private bool CheckGameEnd()
    {
        int winningPlayerID = -1;
        int playersAlive = PlayerList.Count;
        for (int id = 0; id < PlayerList.Count; id++)
        {
            if (PlayerList[id].myProfile.status == PlayerStatus.Dead)
                playersAlive--;
            else
                winningPlayerID = id;
        }

        if (playersAlive <= 1)
        {
            string playerName = string.Empty;
            if (winningPlayerID >= 0)
            {
                playerName = PlayerList[winningPlayerID].myProfile.userName;
            }

            RpcGameEndMsg(winningPlayerID, playerName);
            return true;
        }
        else
        {
            return false;
        }
    }

    [ClientRpc]
    private void RpcGameEndMsg(int winningID, string winnerName)
    {
        TextMeshProUGUI textBox = gameMsgAnimator.GetComponentInChildren<TextMeshProUGUI>(true);
        if (winningID == -1)
        {
            textBox.text = "Tied!";

            gamesTied++;
        }
        else
        {
            textBox.text = string.IsNullOrEmpty(winnerName) ? $"Player {winningID} Wins!" : $"{winnerName} Wins!";

            int id = NetworkClient.connection.identity.GetComponent<GamePlayerManager>().myProfile.id;
            if (winningID == id)
            {
                gamesWon++;
            }
            else
            {
                gamesLost++;
            }
        }

        UpdateGameText();

        gameMsgAnimator.Play("RevealMessage");
    }

    private IEnumerator RoundEndWait()
    {
        yield return new WaitForSeconds(betweenRoundDelay);

        RpcDestroyArrows();

        OnTurnEnd?.Invoke();

        yield return null;

        //check if a player is dead AGAIN after round end invoke
        DidPlayerDie();

        if (CheckGameEnd())
        {
            yield return new WaitForSeconds(betweenGameDelay);

            //delete cards
            CardStats[] allCards = FindObjectsOfType<CardStats>();
            for (int index = 0; index < allCards.Length; index++)
                NetworkServer.Destroy(allCards[index].gameObject);

            //start game
            OnGameRestart();

            OnNewGameStart?.Invoke();
        }
        else
        {
            //destroy played cards and reset status
            for (int index = 0; index < PlayerList.Count; index++)
            {
                if (PlayerList[index].myProfile.playedCard)
                    NetworkServer.Destroy(PlayerList[index].myProfile.playedCard.gameObject);

                if (PlayerList[index].myProfile.status == PlayerStatus.PlayedCard)
                    PlayerList[index].myProfile.status = PlayerStatus.NotPlayed;
            }

            TurnCounter++;
        }

        FindObjectOfType<AudioManager>().PlayClipInstance(sfxNewTurn);
    }

    [Server]
    public void OnGameRestart()
    {
        ResetEvents();

        for (int index = 0; index < PlayerList.Count; index++)
        {
            PlayerList[index].myProfile.status = PlayerStatus.NotPlayed;
            PlayerList[index].myProfile.isTargetable = true;

            for (int eff = PlayerList[index].myProfile.statusEffects.Count - 1; eff >= 0; eff--)
            {
                IndividualStatusEffect effect = PlayerList[index].myProfile.statusEffects[eff];
                if (effect.myStatus.type == StatusType.Passive)
                {
                    
                    effect.InvokeStatusEffect();
                }
                else
                {
                    effect.RemoveStatusEffect();
                }
            }
        }

        resourcesScript.ResetPlayerResources();

        //does not matter which player manager calls it, just chose the first player on the list
        PlayerList[0].RpcResetPlayerAreas();

        TurnCounter = 1;
    }

    [ClientRpc]
    private void RpcUpdateTurnText(string str) => turnCounterText.text = str;

    [ClientRpc]
    private void RpcUpdateGameText() => UpdateGameText();

    [Client]
    private void UpdateGameText() => gameCounterText.text = $"{gamesWon} : {gamesLost} : {gamesTied}";

    public void OnEndGameButton()
    {
        NetworkIdentity identity = NetworkClient.connection.identity;
        GamePlayerManager player = identity.GetComponent<GamePlayerManager>();
        player.CmdRequestReturnToLobby();
    }
}
