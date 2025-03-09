using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class CardStats : NetworkBehaviour 
{
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

    public void UpdateCardText()
    {
        nameText.text = myStats.cardName;
        descText.text = myStats.description;
        manaText.text = myStats.manaCost < 0 ? "?" : myStats.manaCost.ToString();

        cardTypeImage.sprite = SpriteDatabase.GetCardTypeImage((int)myStats.type);
        cardImage.sprite = SpriteDatabase.GetCardImage(myStats.cardID);
    }
}

