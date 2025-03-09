using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Random = UnityEngine.Random;

public delegate bool CardEffect(CardInfo myCard = null, CardInfo targetCard = null, object param = null);

public static class CardEffectsManager
{
    public static Action<CardInfo, bool> OnDefenseChecked;

    private static HealthManaManager hmScript;
    private static StatusEffectsManager statusScript;

    //functions
    private static bool SpawnStatusEffect(GamePlayerManager target, int id, int turnCount, bool skipFirstCount, float floatPar = -1f, int intPar = -1) => statusScript.SpawnStatusEffect(target, id, turnCount, skipFirstCount, floatPar, intPar);
    private static bool GainHealth(GamePlayerManager target, float amount) => hmScript.ChangeHealth(target.myProfile.id, Mathf.Max(0f, amount));
    private static bool LoseHealth(GamePlayerManager target, float amount) => hmScript.ChangeHealth(target.myProfile.id, -Mathf.Max(0f, amount)); //if it says 'lose X health' use this
    private static bool AttackLoseHealth(GamePlayerManager owner, GamePlayerManager target, float amount) => LoseHealth(target, Mathf.Abs(amount) + owner.myProfile.damageDealtoffset); //if it says 'deal X damage' use this
    private static bool GainMana(GamePlayerManager target, float amount) => hmScript.ChangeMana(target.myProfile.id, Mathf.Max(0f, amount));
    private static bool LoseMana(GamePlayerManager target, float amount) => hmScript.ChangeMana(target.myProfile.id, -Mathf.Max(0f, amount));
    public static bool TryEffectOnTarget(GamePlayerManager owner, GamePlayerManager currentTarget, DamageType damageType)
    {
        //failsafe if owner or currentTarget is null
        if (!owner || !currentTarget)
            return false;

        //check if target is dead
        if (hmScript.IsPlayerAlreadyDead(currentTarget.myProfile.id))
            return false;

        //if player targets themselves, guarantee effect
        if (owner.myProfile.id == currentTarget.myProfile.id)
            return true;

        bool attackSuccess = true;

        CardInfo attackerInfo = owner.myProfile.playedCard.GetComponent<CardStats>().myStats;
        CardInfo victimInfo = currentTarget.myProfile.playedCard.GetComponent<CardStats>().myStats;

        attackerInfo.target = currentTarget;

        if (victimInfo.type == CardType.Defense) //if target is defending
        {
            if (victimInfo.owner.myProfile.canUseEffects)
            {
                if (victimInfo.effect.Invoke(victimInfo, attackerInfo, damageType))
                {
                    //check if block sucessful, if so, then effect is unsuccessful
                    attackSuccess = false;
                }
            }
        }
        else if ((attackerInfo.type == CardType.Attack) && (victimInfo.type == CardType.Attack)) //if attacker and target are both attacking
        {
            if (victimInfo.manaCost >= attackerInfo.manaCost)
            {
                //attack will fail:
                //if opposing attack has higher or equal mana AND

                if ((victimInfo.target == null) || (victimInfo.target == owner))
                {
                    //if opposing attack targeted this card's owner
                    //OR if opposing attack does not target
                    attackSuccess = false;
                }
            }
        }

        OnDefenseChecked?.Invoke(victimInfo, attackSuccess);
        return attackSuccess;
    }

    public static void InitialiseEffects(HealthManaManager hm, StatusEffectsManager status)
    {
        hmScript = hm;
        statusScript = status;

        for (int index = 0; index < CardDatabaseManager.CardDatabase.Count; index++)
        {
            MethodInfo correspondingFunction = typeof(CardEffectsManager).GetMethod("_" + index);

            if (correspondingFunction != null)
                CardDatabaseManager.CardDatabase[index].effect = (CardEffect)correspondingFunction.CreateDelegate(typeof(CardEffect));
        }
    }

    //card effects, function name correlates to card ID
    //attack cards return attack success; attacking multiple targets counts as successful if at least 1 of the attacks go through
    //block cards return block success

    //param[0] is damagetype of enemy attack

    public static bool _0(CardInfo myCard, CardInfo targetCard, object param)
    {
        return LoseHealth(myCard.target, 99f);
    }

    public static bool _1(CardInfo myCard, CardInfo targetCard, object param)
    { 
        return GainMana(myCard.owner, 1f);
    }

    public static bool _2(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            AttackLoseHealth(myCard.owner, myCard.target, 1f);
            return true;
        }

        return false;
    }

    public static bool _3(CardInfo myCard, CardInfo targetCard, object param)
    {
        return (DamageType)param == DamageType.Slash;
    }

    public static bool _4(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Gun))
        {
            AttackLoseHealth(myCard.owner, myCard.target, 1f);
            return true;
        }

        return false;
    }

    public static bool _5(CardInfo myCard, CardInfo targetCard, object param)
    {
        return (DamageType)param == DamageType.Gun;
    }

    public static bool _6(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic))
        {
            AttackLoseHealth(myCard.owner, myCard.target, 2f);
            return true;
        }

        return false;
    }

    public static bool _7(CardInfo myCard, CardInfo targetCard, object param)
    {
        bool attackSuccess = false;
        foreach (var player in GameManager.singleton.PlayerList)
        {
            if (player.myProfile.id != myCard.owner.myProfile.id)
            {
                if (TryEffectOnTarget(myCard.owner, player, DamageType.Slash))
                {
                    AttackLoseHealth(myCard.owner, player, 1f);
                    attackSuccess = true;
                }
            }
        }

        return attackSuccess;
    }

    public static bool _8(CardInfo myCard, CardInfo targetCard, object param)
    {
        return true;
    }

    public static bool _9(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            if (AttackLoseHealth(myCard.owner, myCard.target, 1f))
            {
                return GainHealth(myCard.owner, 1f);
            }
        }
        else
        {
            LoseHealth(myCard.owner, 1);
        }

        return false;
    }

    public static bool _10(CardInfo myCard, CardInfo targetCard, object param)
    {
        //this is ran because the card is gun damage
        //it may seem redundant but this is to trigger any gun blocking cards
        if (!TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Gun))
            return false;

        float damage = 2;
        if (myCard.target.myProfile.playedCard.GetComponent<CardStats>().myStats.type == CardType.Defense)
        {
            damage -= 1;
        }

        return AttackLoseHealth(myCard.owner, myCard.target, damage);
    }

    public static bool _11(CardInfo myCard, CardInfo targetCard, object param)
    {
        GainMana(myCard.owner, 2);

        foreach (var player in GameManager.singleton.PlayerList)
        {
            if (player.myProfile.id != myCard.owner.myProfile.id)
            {
                GainMana(player, 1);
            }
        }

        return true;
    }

    public static bool _12(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.owner, 6, 2, false);
    }

    public static bool _13(CardInfo myCard, CardInfo targetCard, object param)
    {
        return GainHealth(myCard.target, 1);
    }

    public static bool _14(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (targetCard.target)
        {
            targetCard.target = targetCard.owner;
            targetCard.effect?.Invoke(targetCard, targetCard);
        }

        return true;
    }

    public static bool _15(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            if ((myCard.target.myProfile.mana >= 1f) && LoseMana(myCard.target, 1))
            {
                return GainMana(myCard.owner, 1);
            }
        }

        return false;
    }

    public static bool _16(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            float stealMana = myCard.target.myProfile.mana;
            if ((stealMana > 0f) && LoseMana(myCard.target, stealMana))
            {
                return GainMana(myCard.owner, stealMana);
            }
        }

        return false;
    }

    public static bool _17(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.target, 7, 1, false);
    }

    public static bool _18(CardInfo myCard, CardInfo targetCard, object param)
    {
        foreach (var player in GameManager.singleton.PlayerList)
        {
            if (player.myProfile.id != myCard.owner.myProfile.id)
            {
                SpawnStatusEffect(player, 7, 1, false);
            }
        }

        return true;
    }

    public static bool _19(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            return AttackLoseHealth(myCard.owner, myCard.target, 1); ;
        }

        return false;
    }

    public static bool _20(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            return SpawnStatusEffect(myCard.target, 2, 1, true, 0.5f);
        }

        return false;
    }

    public static bool _21(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.owner, 8, 2, true);
    }

    public static bool _22(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic))
        {
            if (AttackLoseHealth(myCard.owner, myCard.target, 1))
            {
                return SpawnStatusEffect(myCard.target, 2, 2, true, 0.5f);
            }
        }

        return false;
    }

    public static bool _23(CardInfo myCard, CardInfo targetCard, object param)
    {
        //22 id is skull bolt
        //25 id is cleanse
        SpawnStatusEffect(myCard.owner, 9, 2, true, 1f, 22);
        return SpawnStatusEffect(myCard.owner, 9, 2, true, 1f, 25);
    }

    public static bool _24(CardInfo myCard, CardInfo targetCard, object param)
    {
        bool attackSuccess = false;
        foreach (var player in GameManager.singleton.PlayerList)
        {
            if (player.myProfile.id != myCard.owner.myProfile.id)
            {
                if (TryEffectOnTarget(myCard.owner, player, DamageType.Magic))
                {
                    SpawnStatusEffect(player, 2, 4, true, 0.5f);
                    attackSuccess = true;
                }
            }
        }

        return attackSuccess;
    }

    public static bool _25(CardInfo myCard, CardInfo targetCard, object param)
    {
        bool cleanseSuccess = false;
        List<IndividualStatusEffect> statusList = myCard.owner.myProfile.statusEffects;
        for (int index = statusList.Count - 1; index >= 0; index--)
        {
            IndividualStatusEffect eff = statusList[index];
            if (eff.myStatus.type == StatusType.Debuff)
            {
                eff.RemoveStatusEffect();
                cleanseSuccess = true;
            }
        }

        return cleanseSuccess;
    }

    public static bool _26(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic))
        {
            return SpawnStatusEffect(myCard.target, 1, 3, true, 0.5f);
        }

        return false;
    }

    public static bool _27(CardInfo myCard, CardInfo targetCard, object param)
    {
        return (DamageType)param == DamageType.Magic;
    }

    public static bool _28(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic))
        {
            if (AttackLoseHealth(myCard.owner, myCard.target, 1))
            {
                return GainMana(myCard.target, 3f);
            }
        }

        return false;
    }

    public static bool _29(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.owner, 3, 4, true, 0.5f);
    }

    public static bool _30(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.target, 11, 2, true);
    }

    public static bool _31(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.target, 4, 2, true);
    }

    public static bool _32(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.target, 5, 2, true);
    }

    public static bool _33(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.target, 12, 1, true, 1f, 1);
    }

    public static bool _34(CardInfo myCard, CardInfo targetCard, object param)
    {
        //target card is the attacking card
        SpawnStatusEffect(targetCard.owner, 10, 2, true, 2f, 0);
        return false;
    }

    public static bool _35(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.target, 12, 4, true, 0.5f, 2);
    }

    public static bool _36(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.owner, 0, -1, true, 1f);
    }

    public static bool _37(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.owner, 14, -1, false, 1f);
    }

    public static bool _38(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.owner, 15, -1, true);
    }

    public static bool _39(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.owner, 9, 2, true, 3f, 6);
    }

    public static bool _40(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.owner, 16, 2, true);
    }

    public static bool _41(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic))
        {
            if (AttackLoseHealth(myCard.owner, myCard.target, 1f))
            {
                List<IndividualStatusEffect> statusList = myCard.target.myProfile.statusEffects;
                for (int index = statusList.Count - 1; index >= 0; index--)
                {
                    IndividualStatusEffect eff = statusList[index];
                    if (eff.myStatus.type == StatusType.Buff)
                    {
                        eff.RemoveStatusEffect();
                    }
                }

                return true;
            }
        }

        return false;
    }

    public static bool _42(CardInfo myCard, CardInfo targetCard, object param)
    {
        return GainHealth(myCard.owner, 2f);
    }

    public static bool _43(CardInfo myCard, CardInfo targetCard, object param)
    {
        if ((DamageType)param == DamageType.Slash)
        {
            GainHealth(myCard.owner, 1f);

            return true;
        }

        return false;
    }

    public static bool _44(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.owner, 17, -1, true, 1f);
    }

    public static bool _45(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            if (AttackLoseHealth(myCard.owner, myCard.target, 1f))
            {
                SpawnStatusEffect(myCard.owner, 9, 1, true, 2f, 45);

                return true;
            }
        }

        return false;
    }

    public static bool _46(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (LoseHealth(myCard.owner, 1f))
        {
            return GainMana(myCard.owner, 4);
        }

        return false;
    }

    public static bool _47(CardInfo myCard, CardInfo targetCard, object param)
    {
        if ((DamageType)param == DamageType.Slash || (DamageType)param == DamageType.Gun)
        {
            GainMana(myCard.owner, 2f);

            return true;
        }

        return false;
    }

    public static bool _48(CardInfo myCard, CardInfo targetCard, object param)
    {
        bool attackSuccess = false;
        foreach (var player in GameManager.singleton.PlayerList)
        {
            if (player.myProfile.id != myCard.owner.myProfile.id)
            {
                if (TryEffectOnTarget(myCard.owner, player, DamageType.Gun))
                {
                    AttackLoseHealth(myCard.owner, player, 1);
                    attackSuccess = true;
                }
            }
        }

        return attackSuccess;
    }

    public static bool _49(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Gun))
        {
            if (AttackLoseHealth(myCard.owner, myCard.target, 1f))
            {
                if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
                {
                    AttackLoseHealth(myCard.owner, myCard.target, 1f);
                }

                return true;
            }
        }

        return false;
    }

    public static bool _50(CardInfo myCard, CardInfo targetCard, object param)
    {
        return SpawnStatusEffect(myCard.target, 13, 3, true, 1f);
    }

    public static bool _51(CardInfo myCard, CardInfo targetCard, object param)
    {
        bool attackSuccess = false;
        foreach (var player in GameManager.singleton.PlayerList)
        {
            if (player.myProfile.id != myCard.owner.myProfile.id)
            {
                if (TryEffectOnTarget(myCard.owner, player, DamageType.Gun))
                {
                    if (AttackLoseHealth(myCard.owner, player, 3f))
                    {
                        SpawnStatusEffect(player, 2, -1, true, 1f);
                        attackSuccess = true;
                    }
                }
            }
        }

        return attackSuccess;
    }

    public static bool _52(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            if (AttackLoseHealth(myCard.owner, myCard.target, 1f))
            {
                SpawnStatusEffect(myCard.target, 1, 4, true, 0.5f);

                return true;
            }
        }

        return false;
    }

    public static bool _53(CardInfo myCard, CardInfo targetCard, object param)
    {
        bool attackSuccess = false;
        foreach (var player in GameManager.singleton.PlayerList)
        {
            if (TryEffectOnTarget(myCard.owner, player, DamageType.Slash))
            {
                AttackLoseHealth(myCard.owner, player, 1f);
            }
        }

        return attackSuccess;
    }

    public static bool _54(CardInfo myCard, CardInfo targetCard, object param)
    {
        int randomIndex;
        CardInfo randomCard;
        do
        {
            randomIndex = Random.Range(0, CardDatabaseManager.CardDatabase.Count);
            randomCard = CardDatabaseManager.CardDatabase[randomIndex];
        } while (randomCard.manaCost < 2f);

        return SpawnStatusEffect(myCard.owner, 18, 1, false, -1, randomIndex);
    }

    public static bool _55(CardInfo myCard, CardInfo targetCard, object param)
    {
        SpawnStatusEffect(targetCard.owner, 13, 2, true, 2);

        if ((DamageType)param == DamageType.Magic)
        {
            return true;
        }

        return false;
    }

    public static bool _56(CardInfo myCard, CardInfo targetCard, object param)
    {
        for (int buff = 0; buff < 2; buff++)
        {
            int buffInt;
            do
            {
                buffInt = Random.Range(0, StatusEffectsManager.statusEffects.Count);
            } while (StatusEffectsManager.statusEffects[buffInt].type != StatusType.Buff);

            SpawnStatusEffect(myCard.owner, buffInt, 2, false, Random.Range(1, 3) / 2f, Random.Range(1, 4));
        }

        for (int debuff = 0; debuff < 2; debuff++)
        {
            int debuffInt;
            do
            {
                debuffInt = Random.Range(0, StatusEffectsManager.statusEffects.Count);
            } while (StatusEffectsManager.statusEffects[debuffInt].type != StatusType.Debuff);

            SpawnStatusEffect(myCard.target, debuffInt, 2, false, Random.Range(1, 3) / 2f, Random.Range(1, 4));
        }

        return true;
    }

    public static bool _57(CardInfo myCard, CardInfo targetCard, object param)
    {
        int eff = Random.Range(0, 4);
        switch (eff)
        {
            case 0:
                SpawnStatusEffect(myCard.owner, 14, 2, false, 1f);
                break;
            case 1:
                SpawnStatusEffect(myCard.owner, 17, 2, false, 1f);
                break;
            case 2:
                SpawnStatusEffect(myCard.owner, 3, 2, false, 0.5f);
                break;
            case 3:
                SpawnStatusEffect(myCard.owner, 8, 2, false);
                break;
            default:
                break;
        }

        return true;
    }

    public static bool _58(CardInfo myCard, CardInfo targetCard, object param)
    {
        GainMana(myCard.target, 1f);
        SpawnStatusEffect(myCard.target, 16, 1, true);
        SpawnStatusEffect(myCard.target, 4, 2, true);

        return true;
    }

    public static bool _59(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic))
        {
            if (myCard.target.myProfile.health > myCard.owner.myProfile.health)
            {
                AttackLoseHealth(myCard.owner, myCard.target, 2f);
            }
            else
            {
                AttackLoseHealth(myCard.owner, myCard.target, 1f);
            }

            return true;
        }

        return false;
    }

    public static bool _60(CardInfo myCard, CardInfo targetCard, object param)
    {
        SpawnStatusEffect(myCard.owner, 24, -1, false);
        SpawnStatusEffect(myCard.owner, 19, -1, false);
        return true;
    }

    public static bool _61(CardInfo myCard, CardInfo targetCard, object param)
    {
        foreach (var player in GameManager.singleton.PlayerList)
        {
            if (player.myProfile.id != myCard.owner.myProfile.id)
            {
                SpawnStatusEffect(player, 4, 2, true);
            }
        }

        return true;
    }

    public static bool _62(CardInfo myCard, CardInfo targetCard, object param)
    {
        SpawnStatusEffect(myCard.owner, 25, -1, false);
        SpawnStatusEffect(myCard.owner, 20, -1, false);
        return true;
    }

    public static bool _63(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            AttackLoseHealth(myCard.owner, myCard.target, 1f);
            return true;
        }

        return false;
    }

    public static bool _64(CardInfo myCard, CardInfo targetCard, object param)
    {
        SpawnStatusEffect(myCard.owner, 26, -1, false);
        return true;
    }

    public static bool _65(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic)) 
        {
            return SpawnStatusEffect(myCard.target, 2, 2, true, 0.5f);
        }

        return false;
    }

    public static bool _66(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic)) 
        {
            return SpawnStatusEffect(myCard.target, 1, 2, true, 1f);
        }

        return false;
    }

    public static bool _67(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic)) 
        {
            return SpawnStatusEffect(myCard.target, 10, 2, true, 1f, (int)CardType.Defense);
        }

        return false;
    }

    public static bool _68(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (targetCard != null)
        {
            bool cleanseSuccess = false;
            List<IndividualStatusEffect> statusList = myCard.owner.myProfile.statusEffects;
            for (int index = statusList.Count - 1; index >= 0; index--)
            {
                IndividualStatusEffect eff = statusList[index];
                if (eff.myStatus.type == StatusType.Debuff)
                {
                    eff.RemoveStatusEffect();
                    cleanseSuccess = true;
                }
            }

            return cleanseSuccess;
        }
        else
        {
            return true;
        }
    }

    public static bool _69(CardInfo myCard, CardInfo targetCard, object param)
    {
        SpawnStatusEffect(myCard.owner, 27, -1, false);
        SpawnStatusEffect(myCard.owner, 21, -1, false);
        return true;
    }

    public static bool _70(CardInfo myCard, CardInfo targetCard, object param)
    {
        float playerMana = myCard.owner.myProfile.mana;
        if (LoseMana(myCard.owner, playerMana))
        {
            if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Magic))
            {
                return AttackLoseHealth(myCard.owner, myCard.target, playerMana);
            }
        }

        return false;
    }

    public static bool _71(CardInfo myCard, CardInfo targetCard, object param)
    {
        SpawnStatusEffect(myCard.owner, 28, -1, false);

        SpawnStatusEffect(myCard.owner, 22, -1, false);
        SpawnStatusEffect(myCard.owner, 23, -1, false);
        return true;
    }

    public static bool _72(CardInfo myCard, CardInfo targetCard, object param)
    {
        SpawnStatusEffect(myCard.owner, 29, -1, false);
        return true;
    }
    
    public static bool _73(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Slash))
        {
            if (AttackLoseHealth(myCard.owner, myCard.target, 1f))
            {
                return GainMana(myCard.owner, 2f);
            }
        }

        return false;
    }

    public static bool _74(CardInfo myCard, CardInfo targetCard, object param)
    {
        if (TryEffectOnTarget(myCard.owner, myCard.target, DamageType.Gun))
        {
            if (AttackLoseHealth(myCard.owner, myCard.target, 1f))
            {
                SpawnStatusEffect(myCard.target, 8, 1, true);
                return true;
            }
        }

        return false;
    }
}
