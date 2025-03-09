using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mirror;

public class HealthManaManager : NetworkBehaviour
{
    public static Action<int, float> OnHealthChanged; //event is NOT called when health is SET to some value

    private void UpdateManaText(int playerID) => GameManager.singleton.PlayerList[0].RpcUpdateManaText(playerID, GameManager.singleton.PlayerList[playerID].myProfile.mana);
    private void UpdateHealthText(int playerID) => GameManager.singleton.PlayerList[0].RpcUpdateHealthText(playerID, GameManager.singleton.PlayerList[playerID].myProfile.health);

    public void InitialiseManaHealth(float defaultHealth, float defaultMana)
    {
        for (int index = 0; index < GameManager.singleton.PlayerList.Count; index++)
        {
            GameManager.singleton.PlayerList[index].myProfile.mana = defaultMana;
            UpdateManaText(index);

            GameManager.singleton.PlayerList[index].myProfile.health = defaultHealth;
            UpdateHealthText(index);
        }
    }

    public bool IsPlayerAlreadyDead(int playerID) => GameManager.singleton.PlayerList[playerID].myProfile.status == PlayerStatus.Dead;

    //returns success
    public bool ChangeMana(int id, float amount, bool setMana = false)
    {
        if (IsPlayerAlreadyDead(id))
            return false;

        PlayerProfile playerProfile = GameManager.singleton.PlayerList[id].myProfile;

        if (!setMana)
        {
            if (amount > 0)
            {
                amount = Mathf.Max(0, amount + playerProfile.manaGainedOffset);
            }

            //if the player has negative mana, they can still play 0 mana cards
            playerProfile.mana += amount;
        }
        else
        {
            playerProfile.mana = amount;
        }

        UpdateManaText(id);

        return true;
    }

    //returns success
    public bool ChangeHealth(int id, float amount, bool setHealth = false)
    {
        if (IsPlayerAlreadyDead(id))
            return false;

        PlayerProfile playerProfile = GameManager.singleton.PlayerList[id].myProfile;

        if (!setHealth)
        {
            if (amount < 0)
            {
                if (playerProfile.isInvincible)
                    return false;

                amount = Mathf.Min(0, amount - playerProfile.damageTakenOffset);
            }
            else
            {
                amount = Mathf.Max(0, amount + playerProfile.healthGainedOffset);
            }

            playerProfile.health += amount;
            OnHealthChanged?.Invoke(id, amount);
        }
        else
        {
            playerProfile.health = amount;
        }

        UpdateHealthText(id);

        return true;
    }
}
