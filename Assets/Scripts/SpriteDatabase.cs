using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteDatabase : MonoBehaviour
{
    public static List<Sprite> playerIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> assignPlayerIcons = new List<Sprite>();
    public static Sprite GetPlayerIcon(int index) => ((index >= 0) && (index < playerIcons.Count)) ? playerIcons[index] : defaultSprite;

    public static List<Sprite> gameModeIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> assignGameModeIcons = new List<Sprite>();
    public static Sprite GetGameModeIcon(int index) => ((index >= 0) && (index < gameModeIcons.Count)) ? gameModeIcons[index] : defaultSprite;

    public static List<Sprite> cardGenIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> assignCardGenerationIcons = new List<Sprite>();
    public static Sprite GetCardGenIcon(int index) => ((index >= 0) && (index < cardGenIcons.Count)) ? cardGenIcons[index] : defaultSprite;

    public static List<Sprite> cardImages = new List<Sprite>();
    [SerializeField] private List<Sprite> assignCardImages = new List<Sprite>();
    public static Sprite GetCardImage(int index) => ((index >= 0) && (index < cardImages.Count)) ? cardImages[index] : defaultSprite;

    public static List<Sprite> cardTypeImages = new List<Sprite>();
    [SerializeField] private List<Sprite> assignCardTypeImages = new List<Sprite>();
    public static Sprite GetCardTypeImage(int index) => ((index >= 0) && (index < cardTypeImages.Count)) ? cardTypeImages[index] : defaultSprite;

    public static List<Sprite> statusIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> assignStatusIcons = new List<Sprite>();
    public static Sprite GetStatusIcon(int index) => ((index >= 0) && (index < statusIcons.Count)) ? statusIcons[index] : defaultSprite;

    public static List<Sprite> bannerImages = new List<Sprite>();
    [SerializeField] private List<Sprite> assignBannerImages = new List<Sprite>();
    public static Sprite GetBannerImage(int index) => ((index >= 0) && (index < bannerImages.Count)) ? bannerImages[index] : defaultSprite;

    public static Sprite defaultSprite;
    [SerializeField] private Sprite assignDefaultSprite;

    private void Awake()
    {
        defaultSprite = assignDefaultSprite;

        playerIcons = assignPlayerIcons;
        gameModeIcons = assignGameModeIcons;
        cardGenIcons = assignCardGenerationIcons;
        cardImages = assignCardImages;
        statusIcons = assignStatusIcons;
        cardTypeImages = assignCardTypeImages;
        bannerImages = assignBannerImages;
    }
}
