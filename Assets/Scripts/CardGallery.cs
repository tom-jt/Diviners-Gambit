using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardGallery : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Transform contentGrid;
    [SerializeField] TextMeshProUGUI currentGenText;
    [SerializeField] CardDisplay cardDisplayPrefab;
    [SerializeField] Animator[] genButtons;
    [SerializeField] Animator divinerButton;

    private List<CardInfo> cardsDatabase = new List<CardInfo>();

    int? currentGen = null;

    private void OnEnable() {
        for (int i = 0; i < genButtons.Length; i++) {
            Animator button = genButtons[i];
            int gen = i;
            button.GetComponent<Button>().onClick.AddListener(delegate { ToggleCurrentGen(gen); });
        }

        cardsDatabase = CardDatabaseManager.GetCards();

        TogglePanel(false);
    }

    public void TogglePanel(bool value) {
        panel.SetActive(value);

        if (value && currentGen == null) {
            ToggleCurrentGen(0);
        }
    }

    public void ToggleCurrentGen(int newGen) {
        if (newGen == currentGen) {
            return;
        }

        // Remove prev cards
        for (int i = 0; i < contentGrid.childCount; i++) {
            Destroy(contentGrid.GetChild(i).gameObject);
        }

        // Spawn new cards
        SpawnCardsByGeneration(newGen);

        // Buttons
        if (currentGen == null) {
            for (int i = 0; i < genButtons.Length; i++) {
                AnimateGenButton(i == newGen, i);
            }
        } else {
            AnimateGenButton(false, (int)currentGen);
            AnimateGenButton(true, newGen);
        }

        currentGenText.text = "Card Pool " + genButtons[newGen].GetComponentInChildren<TextMeshProUGUI>().text;
        currentGen = newGen;
    }

    public void AnimateGenButton(bool pressed, int gen) {
        if (gen >= 0 && gen < genButtons.Length) {
            genButtons[gen].Play(pressed ? "Pressing" : "Unpressing");
        }
    }

    public void SpawnCardsByGeneration(int generation) {
        if (generation >= 0 && genButtons[generation] == divinerButton) {
            generation = -2;
        }

        for (int index = 0; index < cardsDatabase.Count; index++) {
            //find every card that belongs to this generation and draw it
            CardInfo myInfo = cardsDatabase[index];
            if (myInfo.cardGeneration == generation) {
                CardDisplay card = Instantiate(cardDisplayPrefab, contentGrid);
                card.SetupCard(myInfo);
            }
        }
    }
}
