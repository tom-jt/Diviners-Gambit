using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum PlayerStatus
{
    NotPlayed,
    PlayedCard,
    Dead
}

public class PlayerProfile : NetworkBehaviour
{
    //server var
    [HideInInspector] public bool isHost = false;
    [HideInInspector] public PlayerStatus status = PlayerStatus.PlayedCard;
    [HideInInspector] public CardStats playedCard = null;
    [HideInInspector] public List<IndividualStatusEffect> statusEffects = new List<IndividualStatusEffect>();

    [HideInInspector] public bool isInvincible = false;
    [HideInInspector] public bool isTargetable = true;
    [HideInInspector] public bool canUseEffects = true;
    [HideInInspector] public float damageTakenOffset = 0f;
    [HideInInspector] public float damageDealtoffset = 0f;
    [HideInInspector] public float healthGainedOffset = 0f;
    [HideInInspector] public float manaGainedOffset = 0f;
    [HideInInspector] public float manaCostOffset = 0f;

    //modifies mana cost of differenet card types
    //unaffected types should not be in the dictionary
    //affected types with value of 0f means the type cannot be played at all
    [HideInInspector] public Dictionary<CardType, float> cardTypeManaOffset = new Dictionary<CardType, float>();
    //modifies mana cost of individual cards
    [HideInInspector] public Dictionary<int, float> indvCardManaOffset = new Dictionary<int, float>();

    //common var
    [SyncVar] [HideInInspector] public int id = -1;

    [SyncVar] [HideInInspector] public int icon = -1;
    [SyncVar] [HideInInspector] public string userName = string.Empty;

    [SyncVar] [HideInInspector] public float health = -1f;
    [SyncVar] [HideInInspector] public float mana = -1f;

    [SyncVar] [HideInInspector] public int chosenDivinerID = -1;
}
