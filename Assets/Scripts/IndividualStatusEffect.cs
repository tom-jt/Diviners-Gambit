using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using UnityEngine.UI;
using TMPro;

public class IndividualStatusEffect : NetworkBehaviour
{
    private bool skipFirstCountdown = true;
    private Action onDestroyAction = null;
    private int intParam = -1;
    private float floatParam = -1f;

    //countdown values
    // > 0 for any normal countdown status effect
    // <= 0 for indefinite status effects
    private int countdown = 0; 

    public StatusEffectPreset myStatus;
    private HealthManaManager hmScript;
    private StatusEffectsManager statManagerScript;
    private GamePlayerManager myPlayer;

    [Header("Assignments")]
    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private TextMeshProUGUI descText;
    [SerializeField]
    private Image icon;
    [SerializeField]
    private Image iconBorder;
    [SerializeField]
    private TextMeshProUGUI countdownText;

    [Header("Hover Box")]
    [SerializeField]
    private GameObject hoverBox;
    [SerializeField]
    private float halfWidth;
    [SerializeField]
    private float halfHeight;
    [SerializeField]
    private float paddingX;
    [SerializeField]
    private float paddingY;

    private bool hoverBoxOn = false;

    public override void OnStartClient()
    {
        ToggleHoverBox(false);
    }

    private void Update()
    {
        if (hoverBoxOn)
        {
            float x = Input.mousePosition.x;
            float y = Input.mousePosition.y;
            Vector2 newPos = new Vector2();
            if (x < Screen.width - (2 * halfWidth + paddingX))
            {
                newPos.x = x + (halfWidth + paddingX);
            }
            else
            {
                newPos.x = x - (halfWidth + paddingX);
            }

            if (y < Screen.height - (2 * halfHeight + paddingY))
            {
                newPos.y = y + (halfHeight + paddingY);
            }
            else
            {
                newPos.y = y - (halfHeight + paddingY);
            }

            hoverBox.transform.position = newPos;
        }
    }

    public void ToggleHoverBox(bool value)
    {
        hoverBox.SetActive(value);
        hoverBoxOn = value;
    }

    public void InitialiseStatusEffect(StatusEffectsManager stat, HealthManaManager hm, GamePlayerManager player, int count, bool skipFirstCount, float floatPar = -1f, int intPar = -1)
    {
        List<IndividualStatusEffect> playerEffects = player.myProfile.statusEffects;

        //check if it is a duplicate status effect
        //refresh its timer and destroy this one
        foreach (var eff in playerEffects)
        {
            if (eff.myStatus.statusID == myStatus.statusID)
            {
                //parameters should match to ensure status effects are identical
                if ((eff.intParam == intPar) && (eff.floatParam == floatPar))
                {
                    //status effect cannot stack, refresh timer if new countdown is longer
                    eff.countdown = Mathf.Max(count, eff.countdown);
                    eff.RpcUpdateCountdownText(eff.countdown, false);

                    NetworkServer.Destroy(gameObject);
                    return;
                }
            }
        }

        playerEffects.Add(this);

        statManagerScript = stat;
        hmScript = hm;
        countdown = count;
        skipFirstCountdown = skipFirstCount;
        myPlayer = player;
        intParam = intPar;
        floatParam = floatPar;

        InvokeStatusEffect();
    }

    public void InvokeStatusEffect()
    {
        //prevent duplicating effect if status already exists
        onDestroyAction?.Invoke();
        
        //invoke executes before the next frame update function
        Invoke("_" + myStatus.statusID, 0f);

        StartCoroutine(WaitForStatusInvoke());
    }

    private IEnumerator WaitForStatusInvoke()
    {
        yield return null;

        GameManager.OnTurnEnd += TurnCountdown;

        RpcUpdateStatusVisuals(myStatus.statusID, myStatus.type, myStatus.name, myStatus.description, countdown, skipFirstCountdown);
        RpcSetToOwnerGrid(myPlayer.myProfile.id);
    }

    [ClientRpc]
    private void RpcUpdateStatusVisuals(int id, StatusType type, string name, string desc, int count, bool skipFirstTurn)
    {
        nameText.text = name;
        descText.text = desc;
        icon.sprite = SpriteDatabase.GetStatusIcon(id);

        UpdateCountdownText(count, skipFirstTurn);

        if (type == StatusType.Debuff)
        {
            iconBorder.color = Color.red;
        }
        else
        {
            iconBorder.color = Color.green;
        }
    }

    [ClientRpc]
    private void RpcSetToOwnerGrid(int playerID)
    {
        NetworkIdentity identity = NetworkClient.connection.identity;
        GamePlayerManager player = identity.GetComponent<GamePlayerManager>();
        player.SetPlayerStatusToGrid(gameObject, playerID);
    }

    private void TurnCountdown()
    {
        if (skipFirstCountdown)
        {
            skipFirstCountdown = false;
            RpcUpdateCountdownText(countdown, skipFirstCountdown);
            return;
        }

        if (countdown > 1)
        {
            countdown--;
            RpcUpdateCountdownText(countdown, skipFirstCountdown);
        }
        else if (countdown >= 0)
        {
            RemoveStatusEffect();
        }
    }

    [ClientRpc]
    private void RpcUpdateCountdownText(int count, bool skipFirstTurn) => UpdateCountdownText(count, skipFirstTurn);

    private void UpdateCountdownText(int count, bool skipFirstTurn)
    {
        if (count > 0)
        {
            countdownText.text = count.ToString();

            if (skipFirstTurn)
            {
                countdownText.text += "+";
            }
        }
        else if (count <= 0)
        {
            countdownText.text = "∞";
        }
    }

    public void RemoveStatusEffect()
    {
        myPlayer.myProfile.statusEffects.Remove(this);

        NetworkServer.Destroy(gameObject);
    }

    //fail safe
    private void OnDestroy()
    {
        GameManager.OnTurnEnd -= TurnCountdown;
        onDestroyAction?.Invoke();
    }

    private void RefreshTargetPlayability()
    {
        if (myPlayer)
        {
            myPlayer.TargetPlayerCardPlayability();
        }
    }

    //functions
    private void ChangeMana(float value, bool setMana = false)
    {
        if (skipFirstCountdown)
        {
            return;
        }

        hmScript.ChangeMana(myPlayer.myProfile.id, value, setMana);
    }

    private void ChangeHealth(GamePlayerManager player, float value, bool setHealth = false)
    {
        if (skipFirstCountdown)
        {
            return;
        }

        hmScript.ChangeHealth(player.myProfile.id, value, setHealth);
    }

    private void ToggleInvincible(bool value)
    {
        myPlayer.myProfile.isInvincible = value;
    }

    private void ToggleUseableEffects(bool value)
    {
        myPlayer.myProfile.canUseEffects = value;
    }

    private void ToggleTargetable(bool value)
    {
        myPlayer.myProfile.isTargetable = value;
    }

    private void ChangeTurnEndListener(Action func, bool add)
    {
        if (add)
        {
            GameManager.OnTurnEnd += func;
        }
        else
        {
            GameManager.OnTurnEnd -= func;
        }
    }

    private void ChangeCardTypeDict(CardType type, bool add, float newMana = 0f)
    {
        if (add)
        {
            myPlayer.myProfile.cardTypeManaOffset.Add(type, newMana);
        }
        else
        {
            myPlayer.myProfile.cardTypeManaOffset.Remove(type);
        }

        RefreshTargetPlayability();
    }

    private void ChangeIndvCardDict(int id, bool add, float newMana = 0f)
    {
        if (add)
        {
            myPlayer.myProfile.indvCardManaOffset.Add(id, newMana);
        }
        else
        {
            myPlayer.myProfile.indvCardManaOffset.Remove(id);
        }

        RefreshTargetPlayability();
    }

    private void SpiritBleed(CardInfo info)
    {
        if (info.owner == myPlayer)
        {
            if (info.cardID != 1) //did not play mana card
            {
                ChangeHealth(myPlayer, -1f);
            }
        }
    }

    private void ChangeCardPlayedListener(CardAction func, bool add)
    {
        if (add)
        {
            GameManager.OnCardPlayed += func;
        } 
        else
        {
            GameManager.OnCardPlayed -= func;
        }
    }

    private void AttackPlayer(GamePlayerManager player, DamageType damageType, float damageAmount)
    {
        CardInfo info = player.myProfile.playedCard.GetComponent<CardStats>().myStats;
        if (info.type == CardType.Defense)
        {
            if (player.myProfile.canUseEffects)
            {
                if (info.effect.Invoke(null, info, damageType))
                {
                    //return if the block is successful
                    return;
                }
            }
        }

        ChangeHealth(player, -damageAmount);
    }

    private void ChangeDamageTaken(float increaseValue)
    {
        myPlayer.myProfile.damageTakenOffset += increaseValue;
    }

    private void ChangeDamageDealt(float increaseValue)
    {
        myPlayer.myProfile.damageDealtoffset += increaseValue;
    }

    private void ChangeManaGained(float increaseValue)
    {
        myPlayer.myProfile.manaGainedOffset += increaseValue;
    }

    private void ExecuteCard(CardInfo info)
    {
        if (info.owner != myPlayer)
            return;

        info.effect?.Invoke(info);
    }

    private void Shockwaves(CardInfo info)
    {
        if (info.owner != myPlayer)
            return;

        if ((info.target == null) || (info.target == myPlayer))
            return;

        GamePlayerManager initialTarget = info.target;

        if (info.doesTarget)
        {
            foreach (var player in GameManager.singleton.PlayerList)
            {
                if ((player != initialTarget) && (player != info.owner))
                {
                    info.target = player;
                    info.effect?.Invoke(info);
                }
            }
        }
    }

    private void SpawnStatusEffect(GamePlayerManager player, int id, int turnCount, bool skipFirstCount, float floatPar = -1f, int intPar = -1) => statManagerScript.SpawnStatusEffect(player, id, turnCount, skipFirstCount, floatPar, intPar);

    private void ChangeHealthChangeListener(Action<int, float> func, bool add)
    {
        if (add)
        {
            HealthManaManager.OnHealthChanged += func;
        }
        else
        {
            HealthManaManager.OnHealthChanged -= func;
        }
    }

    private void AphroditeEternalBeauty()
    {
        if (intParam == 5)
        {
            SpawnStatusEffect(myPlayer, 14, 3, false, 1f);
        }
        
        if (intParam <= 5)
        {
            intParam++;
        }
    }

    private void LokiBackstab(CardInfo info)
    {
        if (info.owner == myPlayer)
        {
            if ((info.type == CardType.Attack) && info.target)
            {
                if (info.target.myProfile.playedCard.myStats.type == CardType.Utility)
                {
                    AttackPlayer(info.target, DamageType.Misc, 1f);
                }
            }
        }
    }

    private void SunWukongCloudHop(int playerID, float amount)
    {
        //if taken damage
        if ((playerID == myPlayer.myProfile.id) && (amount < 0))
        {
            SpawnStatusEffect(myPlayer, 8, 2, true);
        }
    }

    private void SunWukongMischievousClone(int playerID, float amount)
    {
        //if taken damage
        if ((playerID == myPlayer.myProfile.id) && (amount < 0))
        {
            SpawnStatusEffect(myPlayer, 16, 2, true);
        }
    }

    private void AutoAid(int playerID, float amount)
    {
        if ((playerID == myPlayer.myProfile.id) && (myPlayer.myProfile.health <= 0f))
        {
            if (myPlayer.myProfile.mana >= 3f)
            {
                ChangeMana(-3f);
                ChangeHealth(myPlayer, 1f, true);
            }
            else
            {
                ChangeFinishExecuteCardEffectListener(AutoAidCheckManaGained, true);
            }
        }
    }

    private void AutoAidCheckManaGained()
    {
        if ((myPlayer.myProfile.health <= 0f) && (myPlayer.myProfile.mana >= 3f))
        {
            ChangeMana(-3f);
            ChangeHealth(myPlayer, 1f, true);
        }

        ChangeFinishExecuteCardEffectListener(AutoAidCheckManaGained, false);
    }

    private void ChangeFinishExecuteCardEffectListener(Action func, bool add)
    {
        if (add)
        {
            GameManager.OnFinishExecuteCardEffect += func;
        }
        else
        {
            GameManager.OnFinishExecuteCardEffect -= func;
        }
    }

    private void DeflationCheck()
    {
        if (myPlayer.myProfile.mana < 1f)
        {
            ChangeHealth(myPlayer, 0f, true);
        }
    }

    //status effects, method name = "_" + status id
    private void _0()
    {
        Action act = () => ChangeMana(floatParam);
        ChangeTurnEndListener(act, true);
        onDestroyAction = () => ChangeTurnEndListener(act, false);

        myStatus.description = myStatus.description.Replace("{A}", floatParam.ToString());
    }

    private void _1()
    {
        Action act = () => ChangeMana(-floatParam);
        ChangeTurnEndListener(act, true);
        onDestroyAction = () => ChangeTurnEndListener(act, false);

        myStatus.description = myStatus.description.Replace("{A}", floatParam.ToString());
    }

    private void _2()
    {
        Action act = () => ChangeHealth(myPlayer, -floatParam);
        ChangeTurnEndListener(act, true);
        onDestroyAction = () => ChangeTurnEndListener(act, false);

        myStatus.description = myStatus.description.Replace("{A}", floatParam.ToString());
    }

    private void _3()
    {
        Action act = () => ChangeHealth(myPlayer, floatParam);
        ChangeTurnEndListener(act, true);
        onDestroyAction = () => ChangeTurnEndListener(act, false);

        myStatus.description = myStatus.description.Replace("{A}", floatParam.ToString());
    }

    private void _4()
    {
        ChangeCardTypeDict(CardType.Attack, true, 0f);
        onDestroyAction = () => ChangeCardTypeDict(CardType.Attack, false);
    }

    private void _5()
    {
        ChangeCardTypeDict(CardType.Defense, true, 0f);
        onDestroyAction = () => ChangeCardTypeDict(CardType.Defense, false);
    }

    private void _6()
    {
        ToggleInvincible(true);
        onDestroyAction = () => ToggleInvincible(false);
    }

    private void _7()
    {
        ToggleUseableEffects(false);
        onDestroyAction = () => ToggleUseableEffects(true);
    }

    private void _8()
    {
        ToggleTargetable(false);
        onDestroyAction = () => ToggleTargetable(true);
    }

    private void _9()
    {
        ChangeIndvCardDict(intParam, true, -floatParam);
        onDestroyAction = () => ChangeIndvCardDict(intParam, false, -floatParam);

        myStatus.description = myStatus.description.Replace("{A}", CardDatabaseManager.CardDatabase[intParam].cardName);
        myStatus.description = myStatus.description.Replace("{B}", floatParam.ToString());
    }

    private void _10()
    {
        ChangeCardTypeDict((CardType)intParam, true, floatParam);
        onDestroyAction = () => ChangeCardTypeDict((CardType)intParam, false, floatParam);

        myStatus.description = myStatus.description.Replace("{A}", ((CardType)intParam).ToString());
        myStatus.description = myStatus.description.Replace("{B}", floatParam.ToString());
    }

    private void _11()
    {
        ChangeCardPlayedListener(SpiritBleed, true);
        onDestroyAction = () => ChangeCardPlayedListener(SpiritBleed, false);
    }

    private void _12()
    {
        Action act = () => AttackPlayer(myPlayer, (DamageType)intParam, floatParam);
        ChangeTurnEndListener(act, true);
        onDestroyAction = () => ChangeTurnEndListener(act, false);

        myStatus.description = myStatus.description.Replace("{A}", floatParam.ToString());
        myStatus.description = myStatus.description.Replace("{B}", ((DamageType)intParam).ToString());
    }

    private void _13()
    {
        ChangeDamageTaken(floatParam);
        onDestroyAction = () => ChangeDamageTaken(-floatParam);

        myStatus.description = myStatus.description.Replace("{A}", floatParam.ToString());
    }

    private void _14()
    {
        ChangeDamageTaken(-floatParam);
        onDestroyAction = () => ChangeDamageTaken(floatParam);

        myStatus.description = myStatus.description.Replace("{A}", floatParam.ToString());
    }

    private void _15()
    {
        ChangeCardPlayedListener(Shockwaves, true);
        onDestroyAction = () => ChangeCardPlayedListener(Shockwaves, false);
    }

    private void _16()
    {
        ChangeCardPlayedListener(ExecuteCard, true);
        onDestroyAction = () => ChangeCardPlayedListener(ExecuteCard, false);
    }

    private void _17()
    {
        ChangeDamageDealt(floatParam);
        onDestroyAction = () => ChangeDamageDealt(-floatParam);

        myStatus.description = myStatus.description.Replace("{A}", floatParam.ToString());
    }

    private void _18()
    {
        CardInfo info = CardDatabaseManager.CardDatabase[intParam].DeepCopy();
        info.owner = myPlayer;
        info.target = myPlayer.myProfile.playedCard.myStats.target;

        Action act = () => ExecuteCard(info);
        ChangeTurnEndListener(act, true);
        onDestroyAction = () => ChangeTurnEndListener(act, false);

        myStatus.description = myStatus.description.Replace("{A}", info.cardName);
    }

    private void _19()
    {
        //int param = -1 only after picking a diviner
        //int param is a positive vlaue for subsequent games
        if (intParam == -1)
        {
            intParam = 0;
        }
        else
        {
            intParam = 1;
        }

        Action act = AphroditeEternalBeauty;
        ChangeTurnEndListener(act, true);
        onDestroyAction = () => ChangeTurnEndListener(act, false);
    }

    private void _20()
    {
        ChangeCardPlayedListener(LokiBackstab, true);
        onDestroyAction = () => ChangeCardPlayedListener(LokiBackstab, false);
    }

    private void _21()
    {
        ChangeManaGained(1f);
        onDestroyAction = () => ChangeManaGained(-1f);
    }

    private void _22()
    {
        ChangeHealthChangeListener(SunWukongCloudHop, true);
        onDestroyAction = () => ChangeHealthChangeListener(SunWukongCloudHop, false);
    }

    private void _23()
    {
        ChangeHealthChangeListener(SunWukongMischievousClone, true);
        onDestroyAction = () => ChangeHealthChangeListener(SunWukongMischievousClone, false);
    }

    private void _24()
    {
        ChangeHealth(myPlayer, myPlayer.myProfile.health + 1f, true);
    }

    private void _25()
    {
        ChangeMana(myPlayer.myProfile.mana + 1f, true);
        if (myPlayer.myProfile.health >= 2f)
            ChangeHealth(myPlayer, myPlayer.myProfile.health - 1f, true);
    }

    private void _26()
    {
        //nothing
    }

    private void _27()
    {
        //nothing
    }

    private void _28()
    {
        ChangeMana(myPlayer.myProfile.mana - 1f, true);
        ChangeHealth(myPlayer, myPlayer.myProfile.health + 1f, true);
    }

    private void _29()
    {
        //nothing
    }

    private void _30()
    {
        ChangeHealthChangeListener(AutoAid, true);

        onDestroyAction = delegate
        {
            ChangeHealthChangeListener(AutoAid, false);
            ChangeFinishExecuteCardEffectListener(AutoAidCheckManaGained, false);
        };
    }

    private void _31()
    {
        ChangeCardPlayedListener(ExecuteCard, true);
        onDestroyAction = () => ChangeCardPlayedListener(ExecuteCard, false);
    }

    private void _32()
    {
        ChangeManaGained(-999f);
        ChangeTurnEndListener(DeflationCheck, true);
        onDestroyAction = () => ChangeTurnEndListener(DeflationCheck, false);
    }
}