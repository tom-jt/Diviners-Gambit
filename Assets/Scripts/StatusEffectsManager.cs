using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum StatusType
{
    Misc,
    Passive,
    Buff,
    Debuff
}

public class StatusEffectPreset
{
    public int statusID;

    public string name;
    public string description;

    public StatusType type;

    public StatusEffectPreset DeepCopy() => new StatusEffectPreset(statusID, name, description, type);

    public StatusEffectPreset(int _id = -1, string _name = "", string _desc = "", StatusType _type = StatusType.Buff)
    {
        statusID = _id;
        name = _name;
        description = _desc;
        type = _type;
    }
}

public class StatusEffectsManager : NetworkBehaviour
{
    public static List<StatusEffectPreset> statusEffects;

    [SerializeField]
    private HealthManaManager hmScript;
    [SerializeField]
    private GameObject statusEffectPrefab;

    private void Start()
    {
        statusEffects = new List<StatusEffectPreset>
        {
            new StatusEffectPreset(
        0,
        "Energised",
        "Gain {A} mana at the end of every turn.",
        StatusType.Buff
        ),

            new StatusEffectPreset(
        1,
        "Crippled",
        "Lose {A} mana at the end of every turn.",
        StatusType.Debuff
        ),

            new StatusEffectPreset(
        2,
        "Withering",
        "Lose {A} health at the end of every turn.",
        StatusType.Debuff
        ),

            new StatusEffectPreset(
        3,
        "Regeneration",
        "Gain {A} health at the end of every turn.",
        StatusType.Buff
        ),

            new StatusEffectPreset(
        4,
        "Cannot Attack",
        "Unable to use attack cards.",
        StatusType.Debuff
        ),

            new StatusEffectPreset(
        5,
        "Cannot Defend",
        "Unable to use defense cards.",
        StatusType.Debuff
        ),

            new StatusEffectPreset(
        6,
        "Invincible",
        "You do not take damage.",
        StatusType.Buff
        ),

            new StatusEffectPreset(
        7,
        "Negated",
        "Your card effects do nothing.",
        StatusType.Misc
        ),

            new StatusEffectPreset(
        8,
        "Untargetable",
        "You cannot be targeted by card effects.",
        StatusType.Buff
        ),

            new StatusEffectPreset(
        9,
        "Cost Down",
        "Reduce the cost of {A} by {B} mana.",
        StatusType.Buff
        ),

            new StatusEffectPreset(
        10,
        "Type Cost Up",
        "Increase the cost of {A} cards by {B} mana.",
        StatusType.Debuff
        ),

            new StatusEffectPreset(
        11,
        "Spirit Bleed",
        "You must play Mana or lose 1 health.",
        StatusType.Debuff
        ),

            new StatusEffectPreset(
        12,
        "Under Attack",
        "Take {A} {B} damage at the end of the turn.",
        StatusType.Debuff
        ),

            new StatusEffectPreset(
        13,
        "Vulnerable",
        "Increase your damage taken by {A}.",
        StatusType.Debuff
        ),

            new StatusEffectPreset(
        14,
        "Warded",
        "Reduce your damage taken by {A}.",
        StatusType.Buff
        ),

            new StatusEffectPreset(
        15,
        "Shockwaves",
        "Your cards that affect another target player now also affect all other players (except you).",
        StatusType.Buff
        ),

            new StatusEffectPreset(
        16,
        "Clone",
        "You cards execute twice.",
        StatusType.Buff
        ),

            new StatusEffectPreset(
        17,
        "Enrage",
        "Increase your damage dealt by {A}.",
        StatusType.Buff
        ),

            new StatusEffectPreset(
        18,
        "Execute Effect",
        "Execute the effect of {A} on your current target at the end of the turn.",
        StatusType.Misc
        ),

            new StatusEffectPreset(
        19,
        "Aphrodite - Eternal Beauty",
        "Take 1 less damage for 3 turns after the fifth turn.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        20,
        "Loki - Backstab",
        "If your attack target used a utility card, they lose 1 additional health.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        21,
        "Ra - Solar Charge",
        "When you gain mana, gain 1 extra mana.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        22,
        "Sun Wukong - Cloud Hop",
        "When you take damage, you cannot be targeted by card effects for 2 turns.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        23,
        "Sun Wukong - Mischievous Clone",
        "When you take damage, your card effects execute twice for 2 turns.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        24,
        "Aphrodite",
        "+1 Health\nPassive - Take 1 less damage for 3 turns after the fifth turn.\nActive - All other players cannot attack for 2 turns.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        25,
        "Loki",
        "-1 Health / +1 Mana\nPassive - If your attack target used a utility card, they lose 1 additional health.\nActive - Deal 1 slash damage to a target player.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        26,
        "Baba Yaga",
        "Active (3 uses) - Give a debuff to a target player for 2 turns.\nActive - Block all damage and remove all active debuffs from yourself.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        27,
        "Ra",
        "Passive - When you gain mana, gain 1 extra mana.\nActive - Lose all your mana, deal 1 magic damage to a target player for each mana lost.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        28,
        "Sun Wukong",
        "+1 Health / -1 Mana\nPassive - When you take damage, you cannot be targeted by card effects for 2 turns.\nPassive - When you take damage, your card effects execute twice for 2 turns.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        29,
        "Bellona",
        "Active - Deal 1 slash damage to a target player. If successful, you gain 2 mana.\nActive - Deal 1 gun damage to a target player. If successful, you cannot be targeted by card effects for 1 turn.",
        StatusType.Passive
        ),
            
            new StatusEffectPreset(
        30,
        "Game Mode - Auto Aid",
        "When a player's health drops to 0 or below and they have 3 mana or more, they lose 3 mana to set their health back to 1.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        31,
        "Game Mode - Clone Zone",
        "All cards execute twice.",
        StatusType.Passive
        ),

            new StatusEffectPreset(
        32,
        "Game Mode - Deflation",
        "Players start with a lot of mana and cannot gain any more. If a player has less than 1 mana, they die.",
        StatusType.Passive
        )
        };
    }

    public bool SpawnStatusEffect(GamePlayerManager player, int id, int turnCount, bool skipFirstCount, float floatPar = -1f, int intPar = -1)
    {
        GameObject effect = Instantiate(statusEffectPrefab);
        NetworkServer.Spawn(effect);

        IndividualStatusEffect indvStatusScript = effect.GetComponent<IndividualStatusEffect>();
        indvStatusScript.myStatus = statusEffects[id].DeepCopy();

        indvStatusScript.InitialiseStatusEffect(this, hmScript, player, turnCount, skipFirstCount, floatPar, intPar);

        return true;
    }
}
