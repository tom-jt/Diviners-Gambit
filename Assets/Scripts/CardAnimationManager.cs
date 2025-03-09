using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardAnimationManager : NetworkBehaviour
{
    [Header("Audio")]
    [SerializeField]
    private AudioClip sfxDamage;
    [SerializeField]
    private AudioClip sfxHeal;
    [SerializeField]
    private AudioClip sfxSlash;
    [SerializeField]
    private AudioClip sfxGun;
    [SerializeField]
    private AudioClip sfxMagic;
    [SerializeField]
    private AudioClip sfxUtility;
    [SerializeField]
    private AudioClip sfxBlockSuccess;
    [SerializeField]
    private AudioClip sfxBlockFail;

    [SerializeField]
    float healthChangeDelay;
    [SerializeField]
    float blockDelay;

    private void OnEnable()
    {
        GameManager.OnCardPlayed += OnCardPlayed;
        CardEffectsManager.OnDefenseChecked += OnDefenseChecked;
        HealthManaManager.OnHealthChanged += OnHealthChange;
    }

    private void OnDisable()
    {
        GameManager.OnCardPlayed -= OnCardPlayed;
        CardEffectsManager.OnDefenseChecked -= OnDefenseChecked;
        HealthManaManager.OnHealthChanged -= OnHealthChange;
    }

    private void OnCardPlayed(CardInfo info)
    {
        switch (info.type) 
        {
            case CardType.Attack:
                //play attack animation
                PlayAnimation(info.owner.myProfile.id, info.damageType.ToString(), true);

                switch (info.damageType) {
                    case DamageType.Slash:
                        RpcPlayAudio("sfxSlash");
                        break;

                    case DamageType.Gun:
                        RpcPlayAudio("sfxGun");
                        break;
                    
                    case DamageType.Magic:
                        RpcPlayAudio("sfxMagic");
                        break;
                }
                
                break;

            case CardType.Utility:
                //play utility animation
                PlayAnimation(info.owner.myProfile.id, "Utility", true);
                RpcPlayAudio("sfxUtility");
                break;

            case CardType.Defense:
                //play defense animation
                PlayAnimation(info.owner.myProfile.id, "SuccessfulDefense", true);
                RpcPlayAudio("sfxBlockSuccess");
                break;
        }
    }

    private void OnDefenseChecked(CardInfo info, bool attackSuccess)
    {
        if (!attackSuccess)
        {
            //play successful defense animation
            PlayAnimation(info.owner.myProfile.id, "SuccessfulDefense", true);
            RpcPlayAudio("sfxBlockSuccess");
        }
        else
        {
            //play unsuccessful defense animation
            PlayAnimation(info.owner.myProfile.id, "UnsuccessfulDefense", true);
            RpcPlayAudio("sfxBlockFail");
        }
    }

    private void OnHealthChange(int playerID, float amount)
    {
        if (amount > 0)
        {
            //play health gain animation
            PlayAnimation(playerID, "PlayerHeal", false);
            RpcPlayAudio("sfxHeal");
        }
        else if (amount < 0)
        {
            //play health loss animation
            PlayAnimation(playerID, "PlayerHurt", false);
            RpcPlayAudio("sfxDamage");
        }
    }

    private void PlayAnimation(int id, string anim, bool onCard)
    {
        GameManager.singleton.PlayerList[0].RpcPlayAnimation(id, anim, onCard);
    }

    [ClientRpc]
    private void RpcPlayAudio(string clipName) {
        AudioClip clip = null;
        float delay = 0f;

        switch (clipName) {
            case "sfxDamage":
                clip = sfxDamage;
                delay = healthChangeDelay;
                break;

            case "sfxHeal":
                clip = sfxHeal;
                delay = healthChangeDelay;
                break;

            case "sfxSlash":
                clip = sfxSlash;
                break;

            case "sfxGun":
                clip = sfxGun;
                break;

            case "sfxMagic":
                clip = sfxMagic;
                break;

            case "sfxUtility":
                clip = sfxUtility;
                break;

            case "sfxBlockSuccess":
                clip = sfxBlockSuccess;
                delay = blockDelay;
                break;

            case "sfxBlockFail":
                clip = sfxBlockFail;
                delay = blockDelay;
                break;

            default:
                Debug.LogWarning("No audioclip with name: " + clipName);
                break;
        }

        StartCoroutine(FindObjectOfType<AudioManager>().PlayClipDelay(clip, delay));
    }
}
