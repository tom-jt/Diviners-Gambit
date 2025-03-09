using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class PlayerAreaManager : NetworkBehaviour
{
    [HideInInspector]
    public int myPlayerID;

    public bool isConnectedPlayer = false;

    [Header("Assignments")]
    public Transform dropZone;
    public Transform iconRoot;

    [SerializeField]
    private Animator dropZoneAnim;
    [SerializeField]
    private Animator iconAnim;
    [SerializeField]
    private GameObject dropZoneHighlight;

    [SerializeField]
    private TextMeshProUGUI manaText;
    [SerializeField]
    private TextMeshProUGUI healthText;
    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private GameObject deathImage;
    [SerializeField]
    private GameObject targetButton;
    [SerializeField]
    private Transform statusGrid;

    public void UpdateManaText(string value) => manaText.text = value.ToString();

    public void UpdateHealthText(string value) => healthText.text = value.ToString();

    public void UpdatePlayerName(string name) => nameText.text = name;

    public void UpdatePlayerIcon(int iconIndex) => iconImage.sprite = SpriteDatabase.GetPlayerIcon(iconIndex);

    public void ToggleDeath(bool state)
    {
        deathImage.SetActive(state);
    }

    public void OnTargetClick()
    {
        NetworkIdentity networkIdentity = NetworkClient.connection.identity;
        GamePlayerManager actionPlayer = networkIdentity.GetComponent<GamePlayerManager>();

        actionPlayer.CmdOnTargetPlayer(myPlayerID);
    }

    public void ToggleTargetHitbox(bool state)
    {
        if (!targetButton)
            return;

        targetButton.SetActive(state);

    }

    public void PlaceStatusInGrid(GameObject status)
    {
        status.transform.SetParent(statusGrid);
    }

    public void PlayAnimation(string anim, bool onCard)
    {
        if (onCard)
        {
            dropZoneAnim.Play(anim);
            StartCoroutine(HighlightDuration());
        }
        else
        {
            iconAnim.Play(anim);
        }
    }

    private IEnumerator HighlightDuration()
    {
        dropZoneHighlight.SetActive(true);

        yield return new WaitForSeconds(3*GameManager.singleton.betweenCardAnimationDelay/4);

        dropZoneHighlight.SetActive(false);

    }
}
