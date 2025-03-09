using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    private CardZoomType zoomStyle;

    public CardInfo myStats = new CardInfo();

    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private TextMeshProUGUI descText;
    [SerializeField]
    private TextMeshProUGUI manaText;
    [SerializeField]
    private Image cardImage;
    [SerializeField]
    private Image cardTypeImage;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private AudioClip sfxHighlight;
    [SerializeField]
    private GameObject[] disableForDivinerCard;

    private void OnEnable() {
        if (myStats != null) SetupCard(myStats);
    }
    public void SetupCard(CardInfo stats) {
        Save save = MenuSettingsManager.GetSave?.Invoke();
        if (save != null) {
            zoomStyle = save.zoomType;
        } else {
            zoomStyle = CardZoomType.Hold;
        }

        animator.SetTrigger("FlipCard");

        zoomStyle = save.zoomType;

        myStats = stats;
        UpdateCardText();
    } 

    public void UpdateCardText() {
        nameText.text = myStats.cardName;
        descText.text = myStats.description;
        manaText.text = myStats.manaCost < 0 ? "?" : myStats.manaCost.ToString();

        cardTypeImage.sprite = SpriteDatabase.GetCardTypeImage((int)myStats.type);
        cardImage.sprite = SpriteDatabase.GetCardImage(myStats.cardID);

        if (myStats.cardGeneration == -2) {
            foreach (var obj in disableForDivinerCard) {
                obj.SetActive(false);
            }
        }
    }
}
