using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public enum CardType
{
    Attack,
    Defense,
    Utility,
}

//priority:
//DontExecute = does not execute, e.g. defense cards

//EffectNegate, ManaSteal
//Early = status effects that would affect the current turn

//next frame

//Normal = standard speed, e.g. most cards

//next frame

//Late = status effects that would not affect the current turn

public enum CardPriority
{
    DontExecute,

    Early,
    Normal,
    Late,

    //special priorities
    EffectNegate,
    ManaLost,

}

public enum DamageType
{
    Slash,
    Gun,
    Magic,
    Misc
}

public class CardInfo
{
    public int cardID;

    public string cardName;
    public string description;
    public int cardGeneration; 
    //generation -2 are diviner cards, generation -1 are diviner abilities
    //active abilities of diviners should have IDs immediately following the ID of the diviner

    public CardPriority priority;

    //card types:
    //attack cards with higher mana nullifies lower mana attacks if they target eachother (hereafter mana conflict check)
    //defense cards have their effects automatically invoked when they are targeted for an effect
    //utility cards have nothing special and is the default
    public CardType type;
    public float manaCost;
    public bool doesTarget;

    public DamageType damageType; //ONLY ASSIGN THIS ONE FOR ATTACK CARDS, ONLY USED TO GET THE APPROPRIATE ANIMATION

    //these MUST be manually assigned after deep copy from database
    [HideInInspector] public GamePlayerManager owner = null;
    [HideInInspector] public GamePlayerManager target = null;
    [HideInInspector] public CardEffect effect;

    public CardInfo DeepCopy() => new CardInfo(cardID, cardName, cardGeneration, priority, type, manaCost, description, doesTarget, damageType, effect, owner, target);

    public CardInfo(int id = -1, string _cardName = "Missing", int _generation = -1, CardPriority _priority = CardPriority.DontExecute, CardType _type = CardType.Utility, float _manaCost = -1f,
        string _description = "Missing Desc", bool _doesTarget = false, DamageType _dmgType = DamageType.Misc, CardEffect _effect = null,
        GamePlayerManager _owner = null, GamePlayerManager _target = null)
    {
        cardID = id;
        cardGeneration = _generation;
        priority = _priority;
        type = _type;

        cardName = _cardName;
        description = _description;
        doesTarget = _doesTarget;
        manaCost = _manaCost;

        effect = _effect;
        owner = _owner;
        target = _target;

        damageType = _dmgType;
    }
}

public class CardDatabaseManager : NetworkBehaviour
{
    public static List<CardInfo> CardDatabase = new List<CardInfo>();

    public const string DefaultCardPool = "100000000";
    public const string EmptyCardPool = "000000000";
    //coloring rules:
    //use red, blue, #00B500 (slightly darker green), purple, and bold
    //red - attack cards, all damage types
    //blue - mana
    //#0B500 - defense cards, health
    //purple - status effects (e.g. Withering(1)), status effect verbs (e.g. 'do not take damage')
    //bold - affected players (e.g. 'target player', 'all other players')
    //database
    private void Start()
    {
        //comment card id
        CardDatabase = GetCards();

        CardEffectsManager.InitialiseEffects(GetComponent<HealthManaManager>(), GetComponent<StatusEffectsManager>());
    }

    public static List<CardInfo> GetCards() {
        return new List<CardInfo> {
            new CardInfo(
        0,
        "One Shot",
        -3,
        CardPriority.Normal,
        CardType.Attack,
        0,
        "Insta-kill a <b>target player</b>.",
        true
        ),

            new CardInfo(
        1,
        "Mana",
        0,
        CardPriority.Normal,
        CardType.Utility,
        0,
        "Gain <color=blue>1 mana</color>.",
        false
        ),

            new CardInfo(
        2,
        "Slash",
        0,
        CardPriority.Normal,
        CardType.Attack,
        1,
        "Deal <color=red>1 slash damage</color> to a <b>target player</b>.",
        true,
        DamageType.Slash
        ),

            new CardInfo(
        3,
        "Deflect",
        0,
        CardPriority.DontExecute,
        CardType.Defense,
        0,
        "Block <color=red>slash damage</color>.",
        false
        ),

            new CardInfo(
        4,
        "Gun",
        0,
        CardPriority.Normal,
        CardType.Attack,
        2,
        "Deal <color=red>1 gun damage</color> to a <b>target player</b>.",
        true,
        DamageType.Gun
        ),

            new CardInfo(
        5,
        "Bullet Repel",
        0,
        CardPriority.DontExecute,
        CardType.Defense,
        0,
        "Block <color=red>gun damage</color>.",
        false
        ),

            new CardInfo(
        6,
        "Fireball",
        0,
        CardPriority.Normal,
        CardType.Attack,
        4,
        "Deal <color=red>2 magic damage</color> to a <b>target player</b>.",
        true,
        DamageType.Magic
        ),

            new CardInfo(
        7,
        "Sweep",
        0,
        CardPriority.Normal,
        CardType.Attack,
        2,
        "Deal <color=red>1 slash damage</color> to <b>all other players</b>.",
        false,
        DamageType.Slash
        ),

            new CardInfo(
        8,
        "Steel Armour",
        1,
        CardPriority.DontExecute,
        CardType.Defense,
        1,
        "Block all damage.",
        false
        ),

            new CardInfo(
        9,
        "Vampire Bite",
        1,
        CardPriority.Normal,
        CardType.Attack,
        1,
        "Deal <color=red>1 slash damage</color> to a <b>target player</b>. " +
        "If successful, you gain <color=#1BA21B>1 health</color>, " +
        "otherwise, you lose <color=#1BA21B>1 health</color>.",
        true,
        DamageType.Slash
        ),

            new CardInfo(
        10,
        "Cannon",
        1,
        CardPriority.Normal,
        CardType.Attack,
        3,
        "Deal <color=red>2 gun damage</color> to a <b>target player</b>. " +
        "Reduce the damage by 1 if they played a <color=#1BA21B>defense card</color>.",
        true,
        DamageType.Gun
        ),

            new CardInfo(
        11,
        "Mana Burst",
        1,
        CardPriority.Normal,
        CardType.Utility,
        0,
        "Gain <color=blue>2 mana</color>. <b>All other players</b> gain <color=blue>1 mana</color>.",
        false
        ),

            new CardInfo(
        12, //ID
        "Guardian Angel", //name
        1, //generation
        CardPriority.Early, //priority
        CardType.Utility, //card type
        3, //mana cost
        "You do not take damage for 2 turns (including this turn).", //description
        false //does card target?
        ),

            new CardInfo(
        13, //ID
        "First Aid", //name
        1, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        3, //mana cost
        "<b>Target player</b> gains <color=#1BA21B>1 health</color>.", //description
        true //does card target?
        ),

            new CardInfo(
        14, //ID
        "Reflect Mirror", //name
        8, //generation
        CardPriority.DontExecute, //priority
        CardType.Defense, //card type
        2, //mana cost
        "Reflect all single-target damage back to <b>your attackers</b>.", //description
        false //does card target?
        ),

            new CardInfo(
        15, //ID
        "Soul Steal", //name
        2, //generation
        CardPriority.ManaLost, //priority
        CardType.Utility, //card type
        2, //mana cost
        "Steal <color=blue>1 mana</color> from a <b>target player</b> before they play their card. " +
        "Blockable by <color=#1BA21B>slash defense cards</color>.", //description
        true //does card target?
        ),

            new CardInfo(
        16, //ID
        "Soul Rip", //name
        2, //generation
        CardPriority.ManaLost, //priority
        CardType.Utility, //card type
        5, //mana cost
        "Steal <color=blue>all mana</color> from a <b>target player</b> before they play their card. " +
        "Blockable by <color=#1BA21B>slash defense cards.</color>", //description
        true //does card target?
        ),

            new CardInfo(
        17, //ID
        "Seduce", //name
        2, //generation
        CardPriority.EffectNegate, //priority
        CardType.Utility, //card type
        3, //mana cost
        "Negate a <b>target player's</b> card.", //description
        true //does card target?
        ),

            new CardInfo(
        18, //ID
        "Love Aura", //name
        2, //generation
        CardPriority.EffectNegate, //priority
        CardType.Utility, //card type
        5, //mana cost
        "Negate <b>all other players'</b> cards.", //description
        false //does card target?
        ),

            new CardInfo(
        19, //ID
        "Shuriken", //name
        2, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        1, //mana cost
        "Deal <color=red>1 slash damage</color> to a <b>target player</b>. " +
        "Cannot block or be blocked by <color=red>attack cards</color>.", //description
        true, //does card target?
        DamageType.Slash //damage type (only attack cards)
        ),

            new CardInfo(
        20, //ID
        "Poison Dart", //name
        8, //generation
        CardPriority.Late, //priority
        CardType.Attack, //card type
        0, //mana cost
        "Give <color=purple>Withering(0.5)</color> to a <b>target player</b> for 1 turn (Lose 0.5 health every turn). " +
        "Blockable by <color=#1BA21B>slash defense cards</color>.", //description
        true, //does card target?
        DamageType.Slash //damage type (only attack cards)
        ),

            new CardInfo(
        21, //ID
        "Smoke Bomb", //name
        2, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        2, //mana cost
        "You cannot be targeted by card effects for 2 turns.", //description
        false //does card target?
        ),

            new CardInfo(
        22, //ID
        "Skull Bolt", //name
        3, //generation
        CardPriority.Late, //priority
        CardType.Attack, //card type
        2, //mana cost
        "Deal <color=red>1 magic damage</color> to a <b>target player</b>. " +
        "If successful, give <color=purple>Withering(0.5)</color> to them for 2 turns (Lose 0.5 health every turn).", //description
        true, //does card target?
        DamageType.Magic //damage type (only attack cards)
        ),

            new CardInfo(
        23, //ID
        "Wizard Robes", //name
        3, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        1, //mana cost
        "Reduce the cost of <color=red>Skull Bolt</color> and <color=orange>Cleanse</color> by 1 for 2 turns.", //description
        false //does card target?
        ),

            new CardInfo(
        24, //ID
        "Fire Breath", //name
        3, //generation
        CardPriority.Late, //priority
        CardType.Attack, //card type
        3, //mana cost
        "Give <color=purple>Withering(0.5)</color> to <b>all other players</b> for 4 turns (Lose 0.5 health every turn). Blockable by magic defense cards.", //description
        false, //does card target?
        DamageType.Magic //damage type (only attack cards)
        ),

            new CardInfo(
        25, //ID
        "Cleanse", //name
        3, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        1, //mana cost
        "Remove all active debuffs from <b>yourself</b>.", //description
        false //does card target?
        ),

            new CardInfo(
        26, //ID
        "Freeze", //name
        3, //generation
        CardPriority.Late, //priority
        CardType.Attack, //card type
        1, //mana cost
        "Give <color=purple>Crippled(0.5)</color> to a <b>target player</b> for 3 turns (Lose 0.5 mana every turn). Blockable by magic defense cards.", //description
        true, //does card target?
        DamageType.Magic //damage type (only attack cards)
        ),

            new CardInfo(
        27, //ID
        "Magic Shield", //name
        0, //generation
        CardPriority.DontExecute, //priority
        CardType.Defense, //card type
        0, //mana cost
        "Block <color=red>magic damage</color>.", //description
        false //does card target?
        ),

            new CardInfo(
        28, //ID
        "Lightning Strike", //name
        3, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        1, //mana cost
        "Deal <color=red>1 magic damage</color> to a <b>target player</b>. " +
        "If successful, they gain <color=blue>3 mana</color>.", //description
        true, //does card target?
        DamageType.Magic //damage type (only attack cards)
        ),

            new CardInfo(
        29, //ID
        "Regrowth", //name
        3, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        3, //mana cost
        "Gain <color=purple>Regeneration(0.5)</color> for 4 turns (Gain 0.5 health every turn).", //description
        false //does card target?
        ),

            new CardInfo(
        30, //ID
        "Spirit Bleed", //name
        4, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        3, //mana cost
        "<b>Target player</b> must play <color=orange>Mana</color> or lose <color=#1BA21B>1 health</color> for 2 turns. " +
        "Blockable by <color=#1BA21B>slash defense cards</color>.", //description
        true //does card target?
        ),

            new CardInfo(
        31, //ID
        "Sword Shatter", //name
        4, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        3, //mana cost
        "<b>Target player</b> cannot use <color=red>attack cards</color> for 2 turns. " +
        "Blockable by <color=#1BA21B>slash defense cards</color>.", //description
        true //does card target?
        ),

            new CardInfo(
        32, //ID
        "Shield Break", //name
        4, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        3, //mana cost
        "<b>Target player</b> cannot use <color=#1BA21B>defense cards</color> for 2 turns. " +
        "Blockable by <color=#1BA21B>slash defense cards</color>.", //description
        true //does card target?
        ),

            new CardInfo(
        33, //ID
        "Delayed Slash", //name
        4, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        1, //mana cost
        "Deal <color=red>1 slash damage</color> to a <b>target player</b> at the end of the next turn.", //description
        true //does card target?
        ),

            new CardInfo(
        34, //ID
        "Petrify", //name
        4, //generation
        CardPriority.DontExecute, //priority
        CardType.Defense, //card type
        0, //mana cost
        "Increase the cost of <b>all your attackers'</b> <color=red>attack cards</color> by 2 for 2 turns.", //description
        false //does card target?
        ),

            new CardInfo(
        35, //ID
        "Artillery Rain", //name
        4, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        4, //mana cost
        "For 4 turns, deal 0.5 <color=red>gun damage</color> to a <b>target player</b> at the end of the turn.", //description
        true, //does card target?
        DamageType.Gun //damage type (only attack cards)
        ),

            new CardInfo(
        36, //ID
        "Mana Fountain", //name
        5, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        3, //mana cost
        "Gain <color=purple>Energised(1)</color> indefinitely (Gain 1 mana every turn).", //description
        false //does card target?
        ),

            new CardInfo(
        37, //ID
        "Iron Skin", //name
        5, //generation
        CardPriority.Early, //priority
        CardType.Utility, //card type
        4, //mana cost
        "Reduce your damage taken by 1 indefinitely (including this turn).", //description
        false //does card target?
        ),

            new CardInfo(
        38, //ID
        "Shockwaves", //name
        5, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        4, //mana cost
        "Indefinitely, cards that affect a <b>target player</b> now also affect <b>all other players</b> (except you).", //description
        false //does card target?
        ),


            new CardInfo(
        39, //ID
        "Catapult", //name
        8, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        3, //mana cost
        "Reduce the cost of your <color=red>Fireball</color> by 3 for 2 turns.", //description
        false //does card target?
        ),

            new CardInfo(
        40, //ID
        "Clone", //name
        6, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        2, //mana cost
        "Your cards execute twice for 2 turns.", //description
        false //does card target?
        ),

            new CardInfo(
        41, //ID
        "Silence", //name
        5, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        2, //mana cost
        "Deal <color=red>1 magic damage</color> to a <b>target player</b>. " +
        "If successful, remove all active buffs from them.", //description
        true, //does card target?
        DamageType.Magic //damage type (only attack cards)
        ),

            new CardInfo(
        42, //ID
        "Meditate", //name
        5, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        5, //mana cost
        "Gain <color=#1BA21B>2 health</color>.", //description
        false //does card target?
        ),

            new CardInfo(
        43, //ID
        "Draining Shield", //name
        5, //generation
        CardPriority.DontExecute, //priority
        CardType.Defense, //card type
        1, //mana cost
        "Block <color=red>slash damage</color>. Gain <color=#1BA21B>1 health</color> for each attack blocked.", //description
        false //does card target?
        ),

            new CardInfo(
        44, //ID
        "Enrage", //name
        5, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        2, //mana cost
        "Increase your attack damage by 1 indefinitely.", //description
        false //does card target?
        ),

            new CardInfo(
        45, //ID
        "Haymaker", //name
        6, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        2, //mana cost
        "Deal <color=red>1 slash damage</color> to a <b>target player</b>. " +
        "If successful, reduce this card's cost by 2 next turn.", //description
        true, //does card target?
        DamageType.Slash //damage type (only attack cards)
        ),

            new CardInfo(
        46, //ID
        "Hyper Mana", //name
        6, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        0, //mana cost
        "Lose <color=green>1 health</color>. If successful, gain <color=blue>4 mana</color>.", //description
        false //does card target?
        ),


            new CardInfo(
        47, //ID
        "Evade", //name
        6, //generation
        CardPriority.DontExecute, //priority
        CardType.Defense, //card type
        1, //mana cost
        "Block <color=red>slash</color> and <color=red>gun damage</color>. " +
        "Gain <color=blue>2 mana</color> for each attack blocked.", //description
        false //does card target?
        ),

            new CardInfo(
        48, //ID
        "Machine Gun", //name
        8, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        3, //mana cost
        "Deal <color=red>1 gun damage</color> to <b>all other players</b>.", //description
        false, //does card target?
        DamageType.Gun //damage type (only attack cards)
        ),

            new CardInfo(
        49, //ID
        "Bayonet", //name
        6, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        2, //mana cost
        "Deal <color=red>1 gun damage</color> to a <b>target player</b>. " +
        "If successful, deal <color=red>1 slash damage</color> to them, " +
        "otherwise, deal <color=red>1 slash damage</color> to yourself.", //description
        true, //does card target?
        DamageType.Gun //damage type (only attack cards)
        ),

            new CardInfo(
        50, //ID
        "Death Mark", //name
        6, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        2, //mana cost
        "Give <color=purple>Vulnerable(1)</color> to a <b>target player</b> for 3 turns (Lose 1 extra health when taking damage).", //description
        true //does card target?
        ),

            new CardInfo(
        51, //ID
        "Nuke", //name
        5, //generation
        CardPriority.Late, //priority
        CardType.Attack, //card type
        6, //mana cost
        "Deal <color=red>4 gun damage</color> to <b>all other players</b>. If successful, give <color=purple>Withering(1)</color> to them indefinitely (Lose 1 health every turn).", //description
        false, //does card target?
        DamageType.Gun //damage type (only attack cards)
        ),

            new CardInfo(
        52, //ID
        "Maim", //name
        8, //generation
        CardPriority.Late, //priority
        CardType.Attack, //card type
        2, //mana cost
        "Deal <color=red>1 slash damage</color> to a <b>target player</b>. If successful, give <color=purple>Cripped(0.5)</color> to them for 4 turn (Lose 0.5 mana every turn).", //description
        true, //does card target?
        DamageType.Slash //damage type (only attack cards)
        ),

            new CardInfo(
        53, //ID
        "Dynamite", //name
        7, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        0, //mana cost
        "Deal <color=red>1 slash damage</color> to <b>all players</b>.", //description
        false, //does card target?
        DamageType.Slash //damage type (only attack cards)
        ),

            new CardInfo(
        54, //ID
        "Magical Production", //name
        7, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        2, //mana cost
        "This card becomes a random card that costs 2 or more. Execute the random card's effect at the end of the turn.", //description
        true //does card target?
        ),

            new CardInfo(
        55, //ID
        "Balloonify", //name
        7, //generation
        CardPriority.DontExecute, //priority
        CardType.Defense, //card type
        1, //mana cost
        "Block <color=red>magic damage</color>. Give <color=purple>Vulnerable(2)</color> to <b>your attackers</b> for 2 turns (Lose 2 extra health when taking damage).", //description
        false //does card target?
        ),

            new CardInfo(
        56, //ID
        "Suspicious Martini", //name
        7, //generation
        CardPriority.Early, //priority
        CardType.Utility, //card type
        2, //mana cost
        "Give 2 random buffs to <b>yourself</b> and give 2 random debuffs to a <b>target player</b> for 2 turns (including this turn).", //description
        true //does card target?
        ),

            new CardInfo(
        57, //ID
        "Mystery Block", //name
        7, //generation
        CardPriority.Early, //priority
        CardType.Utility, //card type
        1, //mana cost
        "Randomly gain 1 of the following for 2 turns (including this turn): <color=purple>Untargetable</color>, <color=purple>Warded(1)</color>, <color=purple>Enrage(1)</color>, <color=purple>Regeneration(0.5)</color>.", //description
        false //does card target?
        ),

            new CardInfo(
        58, //ID
        "Drama Masks", //name
        7, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        0, //mana cost
        "Target player gains <color=blue>1 mana</color>, <color=purple>Clone</color> for 1 turn, and cannot attack for 2 turns.", //description
        true //does card target?
        ),

            new CardInfo(
        59, //ID
        "Variate Blast", //name
        8, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        3, //mana cost
        "Deal <color=red>2 magic damage</color> to a <b>target player</b> if their health is higher than yours, otherwise deal <color=red>1 magic damage</color> instead.", //description
        true, //does card target?
        DamageType.Magic //damage type (only attack cards)
        ),

            new CardInfo(
        60, //ID
        "Aphrodite", //name
        -2, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        0, //mana cost
        "<color=#1BA21B>+1 Health</color>\n" +
        "<color=blue>Passive</color> - Take 1 less damage for 3 turns after the fifth turn.\n" +
        "<color=red>Active</color> - <b>All other players</b> cannot attack for 2 turns.", //description
        false //does card target?
        ),

            new CardInfo(
        61, //ID
        "Blessing of Love", //name
        -1, //generation
        CardPriority.Late, //priority
        CardType.Utility, //card type
        2, //mana cost
        "<b>All other players</b> cannot attack for 2 turns.", //description
        false //does card target?
        ),

            new CardInfo(
        62, //ID
        "Loki", //name
        -2, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        0, //mana cost
        "<color=#1BA21B>-1 Health</color> / <color=blue>+1 Mana</color>\n" +
        "<color=blue>Passive</color> - If <b>your attack target</b> played a <color=orange>utility card</color>, " +
        "they lose <color=#1BA21B>1 additional health</color>.\n" +
        "<color=red>Active</color> - Deal <color=red>1 slash damage</color> to a <b>target player</b>.", //description
        false //does card target?
        ),

            new CardInfo(
        63, //ID
        "Concealed Knife", //name
        -1, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        0, //mana cost
        "Deal <color=red>1 slash damage</color> to a <b>target player</b>.", //description
        true, //does card target?
        DamageType.Slash
        ),

            new CardInfo(
        64, //ID
        "Baba Yaga", //name
        -2, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        0, //mana cost
        "<color=red>Active (3 uses)</color> - Give a debuff to a <b>target player</b> for 2 turns.\n" +
        "<color=red>Active</color> - Block all damage and remove all active debuffs from yourself.", //description
        false //does card target?
        ),

            new CardInfo(
        65, //ID
        "Toxic Potion", //name
        -1, //generation
        CardPriority.Late, //priority
        CardType.Attack, //card type
        1, //mana cost
        "Give <color=purple>Withering(0.5)</color> to a <b>target player</b> for 2 turns (Lose 0.5 health every turn)." +
        "Blockable by <color=#1BA21B>magic defense cards</color>.", //description
        true, //does card target?
        DamageType.Magic
        ),

            new CardInfo(
        66, //ID
        "Suffocating Potion", //name
        -1, //generation
        CardPriority.Late, //priority
        CardType.Attack, //card type
        1, //mana cost
        "Give <color=purple>Crippled(1)</color> to a <b>target player</b> for 2 turns (Lose 1 mana every turn)." +
        "Blockable by <color=#1BA21B>magic defense cards</color>.", //description
        true, //does card target?
        DamageType.Magic
        ),

            new CardInfo(
        67, //ID
        "Weakening Potion", //name
        -1, //generation
        CardPriority.Late, //priority
        CardType.Attack, //card type
        1, //mana cost
        "Increase the cost of a <b>target player's</b> <color=#1BA21B>defense cards</color> by 1 for 2 turns." +
        "Blockable by <color=#1BA21B>magic defense cards</color>.", //description
        true, //does card target?
        DamageType.Magic
        ),

            new CardInfo(
        68, //ID
        "House with Chicken Feet", //name
        -1, //generation
        CardPriority.Normal, //priority
        CardType.Defense, //card type
        0, //mana cost
        "Block all damage and remove all active debuffs from yourself.", //description
        false //does card target?
        ),

            new CardInfo(
        69, //ID
        "Ra", //name
        -2, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        0, //mana cost
        "<color=blue>Passive</color> - When you gain <color=blue>mana</color>, gain <color=blue>1 extra mana</color>.\n" +
        "<color=red>Active</color> - Lose all your <color=blue>mana</color>, " +
        "deal <color=red>1 magic damage</color> to a <b>target player</b> for each <color=blue>mana</color> lost.", //description
        false //does card target?
        ),

            new CardInfo(
        70, //ID
        "Wrath of the Sun", //name
        -1, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        2, //mana cost
        "Lose all your <color=blue>mana</color>, " +
        "deal <color=red>1 magic damage</color> to a <b>target player</b> for each <color=blue>mana</color> lost.", //description
        true //does card target?
        ),

            new CardInfo(
        71, //ID
        "Sun Wukong", //name
        -2, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        0, //mana cost
        "<color=#1BA21B>+1 Health</color> / <color=blue>-1 Mana</color>\n" +
        "<color=blue>Passive</color> - When you take damage, you cannot be targeted by card effects for 2 turns.\n" +
        "<color=blue>Passive</color> - When you take damage, your card effects execute twice for 2 turns.", //description
        false //does card target?
        ),

            new CardInfo(
        72, //ID
        "Bellona", //name
        -2, //generation
        CardPriority.Normal, //priority
        CardType.Utility, //card type
        0, //mana cost
        "<color=red>Active</color> - Deal <color=red>1 slash damage</color> to a <b>target player</b>. If successful, you gain <color=blue>2 mana</color>.\n" +
        "<color=red>Active</color> - Deal <color=red>1 gun damage</color> to a <b>target player</b>. If successful, you cannot be targeted by card effects for 1 turn.", //description
        false //does card target?
        ),

            new CardInfo(
        73, //ID
        "Empowering Thrust", //name
        -1, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        1, //mana cost
        "Deal <color=red>1 slash damage</color> to a <b>target player</b>. If successful, you gain <color=blue>2 mana</color>.", //description
        true, //does card target?
        DamageType.Slash
        ),

            new CardInfo(
        74, //ID
        "Elusive Snipe", //name
        -1, //generation
        CardPriority.Normal, //priority
        CardType.Attack, //card type
        2, //mana cost
        "Deal <color=red>1 gun damage</color> to a <b>target player</b>. If successful, you cannot be targeted by card effects for 1 turn.", //description
        true, //does card target?
        DamageType.Gun
        ),
        };
    }
}
