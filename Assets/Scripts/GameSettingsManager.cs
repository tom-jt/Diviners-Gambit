using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class BaseSetting
{
    //visuals
    public int iconIndex;
    public string name;
    public string description;

    protected virtual void Setting(int _icon, string _name, string _desc)
    {
        iconIndex = _icon;
        name = _name;
        description = _desc;
    }
}

public class GameMode : BaseSetting
{
    //game variables
    public int health;
    public int mana;

    public GameMode(int _icon, string _name, string _desc, int _health, int _mana)
    {
        Setting(_icon, _name, _desc);

        health = _health;
        mana = _mana;
    }
}

public class CardGen : BaseSetting
{
    //game variables
    public string pool;

    public CardGen(int _icon, string _name, string _desc, string _pool)
    {
        pool = _pool;

        Setting(_icon, _name, _desc);
    }
}

public class IndvCardGen : BaseSetting
{
    public IndvCardGen(string _name, string _desc)
    {
        Setting(-1, _name, _desc);
    }
}

public class GameSettingsManager : NetworkBehaviour
{
    private bool receivedSettings = false;
    private List<GameMode> gameModeList = new List<GameMode>();
    private List<CardGen> cardGenList = new List<CardGen>();
    private List<IndvCardGen> indvGenList = new List<IndvCardGen>();

    //only client-side
    public const int defaultGameMode = 1;
    public const int defaultGeneration = 1;

    private int gameMode;
    private int GameMode
    {
        get => gameMode;

        set
        {
            gameMode = value;
            modeCarousel.sprite = SpriteDatabase.GetGameModeIcon(value);
            modeNameText.text = gameModeList[value].name;
            modeDescText.text = gameModeList[value].description;
        }
    }

    private string cardPool = CardDatabaseManager.EmptyCardPool;

    private int cardGenIndex;
    private int CardGenIndex
    {
        get => cardGenIndex;

        set
        {
            cardGenIndex = value;

            genCarousel.sprite = SpriteDatabase.GetCardGenIcon(value);
            genNameText.text = $"{value} - {cardGenList[value].name}";
            genDescText.text = cardGenList[value].description;

            if (value != 0)
            {
                //set the card pool if it is not custom card pool
                cardPool = cardGenList[value].pool;
                UpdateGenToggles();
            }
        }
    }

    private int startingHealth;
    public int StartingHealth 
    { 
        get => startingHealth; 
        
        set 
        { 
            startingHealth = value;
            healthText.text = value.ToString();
        } 
    }

    private int startingMana;
    public int StartingMana 
    { 
        get => startingMana;

        set
        { 
            startingMana = value; 
            manaText.text = value.ToString(); 
        } 
    }


    private bool diviners;
    public bool Diviners
    {
        get => diviners;

        set
        {
            diviners = value;
            divinerToggle.isOn = value;
        }
    }

    [Header("Game Mode")]
    [SerializeField]
    private Image modeCarousel;
    [SerializeField]
    private TextMeshProUGUI modeNameText;
    [SerializeField]
    private TextMeshProUGUI modeDescText;
    [SerializeField]
    private Button modeNext;
    [SerializeField]
    private Button modePrev;


    [Header("Card Generation")]
    [SerializeField]
    private Image genCarousel;
    [SerializeField]
    private TextMeshProUGUI genNameText;
    [SerializeField]
    private TextMeshProUGUI genDescText;
    [SerializeField]
    private Button genNext;
    [SerializeField]
    private Button genPrev;

    [Space]

    [SerializeField]
    private GameObject indvGenGrid;
    [SerializeField]
    private TextMeshProUGUI indvGenNameText;
    [SerializeField]
    private TextMeshProUGUI indvGenDescText;
    private Toggle[] genToggles;

    [Header("Health and Mana")]
    [SerializeField]
    private TextMeshProUGUI healthText;
    [SerializeField]
    private Button healthNext;
    [SerializeField]
    private Button healthPrev;

    [Space]

    [SerializeField]
    private TextMeshProUGUI manaText;
    [SerializeField]
    private Button manaNext;
    [SerializeField]
    private Button manaPrev;

    [Header("Diviners")]
    [SerializeField]
    private Toggle divinerToggle;
    
    public override void OnStartClient()
    {
        base.OnStartClient();

        InitialiseGameMode();
        InitialiseCardGen();

        StartCoroutine(WaitForPlayer());
    }

    private IEnumerator WaitForPlayer()
    {
        yield return new WaitUntil(delegate
        {
            if (!NetworkClient.connection.identity)
                return false;
            else if (!NetworkClient.connection.identity.GetComponent<LobbyPlayerManager>().playerInitialised)
                return false;
            else
                return true;
        });

        NetworkIdentity identity = NetworkClient.connection.identity;
        LobbyPlayerManager player = identity.GetComponent<LobbyPlayerManager>();

        if (player.isHost)
        {
            if (!receivedSettings)
            {
                GameMode = defaultGameMode;
                CardGenIndex = defaultGeneration;
                Diviners = false;
                ChangeIndvGenText(0);

                UpdateGameModeParams();
            }

            modeNext.gameObject.SetActive(true);
            modePrev.gameObject.SetActive(true);
            genNext.gameObject.SetActive(true);
            genPrev.gameObject.SetActive(true);
            healthNext.gameObject.SetActive(true);
            healthPrev.gameObject.SetActive(true);
            manaNext.gameObject.SetActive(true);
            manaPrev.gameObject.SetActive(true);

            modeNext.onClick.AddListener(OnNextModeButton);
            modePrev.onClick.AddListener(OnPreviousModeButton);
            genNext.onClick.AddListener(OnNextGenButton);
            genPrev.onClick.AddListener(OnPreviousGenButton);
            healthNext.onClick.AddListener(OnNextHealthButton);
            healthPrev.onClick.AddListener(OnPreviousHealthButton);
            manaNext.onClick.AddListener(OnNextManaButton);
            manaPrev.onClick.AddListener(OnPreviousManaButton);
        }
        else
        {
            player.CmdRequestGameSettings();

            modeNext.gameObject.SetActive(false);
            modePrev.gameObject.SetActive(false);
            genNext.gameObject.SetActive(false);
            genPrev.gameObject.SetActive(false);
            healthNext.gameObject.SetActive(false);
            healthPrev.gameObject.SetActive(false);
            manaNext.gameObject.SetActive(false);
            manaPrev.gameObject.SetActive(false);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        modeNext.onClick.RemoveListener(OnNextModeButton);
        modePrev.onClick.RemoveListener(OnPreviousModeButton);
        genNext.onClick.RemoveListener(OnNextGenButton);
        genPrev.onClick.RemoveListener(OnPreviousGenButton);
        healthNext.onClick.RemoveListener(OnNextHealthButton);
        healthPrev.onClick.RemoveListener(OnPreviousModeButton);
        manaNext.onClick.RemoveListener(OnNextManaButton);
        manaPrev.onClick.RemoveListener(OnPreviousManaButton);
    }

    private void InitialiseGameMode()
    {
        gameModeList = new List<GameMode>
        {
            new GameMode(
        0,
        "Classic",
        "The original playground variant. With 1 health and 2 starting mana, games are fast and relentless. Learn from your losses!",
        1,
        2
        ),

            new GameMode(
        1,
        "Remastered",
        "A slower and more balanced approach. With 3 health and 2 starting mana, you can risk your health to play higher mana cards.",
        3,
        2
        ),

            new GameMode(
        2,
        "Auto Aid",
        "When a player's health drops to 0 or below and they have 3 mana or more, they lose 3 mana to set their health back to 1.",
        1,
        2
        ),

            new GameMode(
        3,
        "Clone Zone",
        "All cards execute twice.",
        3,
        1
        ),

            new GameMode(
        4,
        "Deflation",
        "Players start with a lot of mana and cannot gain any more. If a player has less than 1 mana, they die.",
        2,
        7
        )
        };
    }

    private void InitialiseCardGen()
    {
        genToggles = indvGenGrid.GetComponentsInChildren<Toggle>(true);

        indvGenList = new List<IndvCardGen>
        {
            new IndvCardGen(
        "Standard",
        "The basic and essential card pool. Should be included in most games."
        ),

            new IndvCardGen(
        "Advanced",
        "Introduces health gaining effects and more defensive and offensive options."
        ),

            new IndvCardGen(
        "Deception",
        "Introduces cards that interfere with your opponents' game plan by negating their cards or stealing their mana."
        ),

            new IndvCardGen(
        "Wizard's Conquest",
        "Introduces status effects and provides a wide array of magic damage weapons."
        ),

            new IndvCardGen(
        "Suffocation",
        "Provides ways to control or manipulate what cards your opponents play."
        ),

            new IndvCardGen(
        "Eternity",
        "Introduces indefinite buffs for self sustain and a nuke to finish your opponents off."
        ),

            new IndvCardGen(
        "Hyperspeed",
        "Provides high risk high reward cards and ways to enhance your offensive power."
        ),

            new IndvCardGen(
        "Circus Mayhem",
        "Introduces randomness and game swinging effects for a chaotic and unpredictable game state."
        ),

            new IndvCardGen(
        "Domination",
        "Provides a vast array of offensive cards but also a powerful counter-attacking option"
        )
        };

        cardGenList = new List<CardGen>
        {
            new CardGen(
        0,
        "Custom",
        "Customise the combination of card pools.",
        CardDatabaseManager.EmptyCardPool
        ),

            new CardGen(
        1,
        "Standard",
        "The basic and original card pool for the standard player expeirence and should be included in most games. Recommended for new players.",
        "100000000"
        ),
            
            new CardGen(
        2,
        "Advanced",
        "Evolved format with more varied cards, including tools with high risk but high reward. Recommended for intermediate players.",
        "110000000"
        ),

            new CardGen(
        3,
        "Deception",
        "Introduces cards designed to ambush and sabotage your opponents. Strike when they least expect it using underhanded tactics!",
        "111000000"
        ),

            new CardGen(
        4,
        "Wizard's Conquest",
        "A chaotic card pool that unleashes magic and status effects. Master the elements to overpower your opponents!",
        "100100000"
        ),

            new CardGen(
        5,
        "Suffocation",
        "Manipulate your opponents and control the cards they play. Tactical use of these cards can suffocate your opponents to death!",
        "100010000"
        ),

            new CardGen(
        6,
        "Eternity",
        "Deploy indefinite buffs and get comfortable for the long game! Slowly accrue an advantage to outlast your opponent.",
        "100001000"
        ),

            new CardGen(
        7,
        "Hyperspeed",
        "Hyperfast card pool designed for fast and snowballing games where you overwhelm your opponents!",
        "110000100"
        ),

            new CardGen(
        8,
        "Circus Mayhem",
        "Unleash true chaos with randomness and explosive plays. Very unbalanced and unpredictable card pool that can lead to crazy game states.",
        "100000010"
        ),

            new CardGen(
        9,
        "Domination",
        "Dominate your opponents with powerful offensive options, but watch out for counter-attacks!",
        "110000001"
        ),

            new CardGen(
        10,
        "Mystic Prison",
        "A deadly set of slow and drawn out games. You win by slowly squeezing your opponents dry.",
        "100011000"
        ),

            new CardGen(
        11,
        "Live or Die",
        "Hyperfast, tension-filled mind games where you teeter at the edge of life and death.",
        "101000100"
        ),

            new CardGen(
        12,
        "Live or Die V2",
        "Fast, chaotic games where mana can only be obtained at a high price.",
        "000000110"
        ),

            new CardGen(
        13,
        "Debuff 'n' Puff",
        "Stack as many debuffs onto your opponent as possible. Whittle down your opponents through gruelling status effects.",
        "100101000"
        ),

            new CardGen(
        14,
        "Guns 'n' Roses",
        "Two options. Decimate everyone in your path or charm your way through everyone's hearts!",
        "101000001"
        ),
        };
    }

    public void ChangeIndvGenText(int gen)
    {
        indvGenNameText.text = $"{gen + 1} - {indvGenList[gen].name}";
        indvGenDescText.text = indvGenList[gen].description;
    }

    private void UpdateGameModeParams()
    {
        StartingHealth = gameModeList[GameMode].health;
        StartingMana = gameModeList[GameMode].mana;

        SendUpdatedGameSettings();
    }

    public void OnNextModeButton()
    {
        if ((GameMode >= gameModeList.Count - 1) || (GameMode < 0)) //failsafe for gamemode negative
            GameMode = 0;
        else
            GameMode++;

        UpdateGameModeParams();
    }

    public void OnPreviousModeButton()
    {
        if ((GameMode <= 0) || (GameMode > gameModeList.Count - 1)) //failsafe for too large gamemode
            GameMode = gameModeList.Count - 1;
        else
            GameMode--;

        UpdateGameModeParams();
    }

    public void OnNextGenButton()
    {
        if ((CardGenIndex >= cardGenList.Count - 1) || (CardGenIndex < 0))
            CardGenIndex = 0;
        else
            CardGenIndex++;

        SendUpdatedGameSettings();
    }

    public void OnPreviousGenButton()
    {
        if ((CardGenIndex == 0) || (CardGenIndex > cardGenList.Count - 1))
            CardGenIndex = cardGenList.Count - 1;
        else
            CardGenIndex--;

        SendUpdatedGameSettings();
    }

    public void OnGenTogglePressed(int index)
    {
        NetworkIdentity identity = NetworkClient.connection.identity;
        LobbyPlayerManager player = identity.GetComponent<LobbyPlayerManager>();

        if (!player.isHost)
            return;

        genToggles[index].isOn = !genToggles[index].isOn;

        cardPool = cardPool.Remove(index, 1).Insert(index, genToggles[index].isOn ? "1" : "0");
        CardGenIndex = FindCardPresetFromPool(cardPool);

        SendUpdatedGameSettings();
    }

    private void UpdateGenToggles()
    {
        for (int gen = 0; gen < genToggles.Length; gen++)
        {
            genToggles[gen].isOn = cardPool[gen] == '1';
        }
    }

    public void OnNextHealthButton()
    {
        StartingHealth++;

        healthPrev.interactable = true;

        SendUpdatedGameSettings();
    }

    public void OnPreviousHealthButton()
    {
        if (StartingHealth > 1)
            StartingHealth--;
        
        if (StartingHealth == 1)
            healthPrev.interactable = false;

        SendUpdatedGameSettings();
    }

    public void OnNextManaButton()
    {
        StartingMana++;

        manaPrev.interactable = true;

        SendUpdatedGameSettings();
    }

    public void OnPreviousManaButton()
    {
        if (StartingMana > 0)
            StartingMana--;
        
        if (StartingMana == 0)
            manaPrev.interactable = false;

        SendUpdatedGameSettings();
    }

    public void OnDivinerToggle()
    {
        if (!NetworkClient.connection.identity.GetComponent<LobbyPlayerManager>().isHost)
            return;

        Diviners = !Diviners;

        SendUpdatedGameSettings();
    }

    public void SendUpdatedGameSettings()
    {
        NetworkIdentity identity = NetworkClient.connection.identity;
        LobbyPlayerManager player = identity.GetComponent<LobbyPlayerManager>();

        player.HostUpdateGameSettings(StartingHealth, StartingMana, GameMode, Diviners, cardPool);
    }

    public void ReceiveUpdatedGameSettings(int _health, int _mana, int _gameMode, bool _diviners, string _cardPool)
    {
        int genIndex = FindCardPresetFromPool(_cardPool);
        if (genIndex == 0)
        {
            cardPool = _cardPool;
            UpdateGenToggles();
        }
        CardGenIndex = genIndex;

        StartingHealth = _health;
        StartingMana = _mana;
        GameMode = _gameMode;
        Diviners = _diviners;
        
        receivedSettings = true;
    }

    private int FindCardPresetFromPool(string cardPool)
    {
        int gen = 0;
        for (int index = 1; index < cardGenList.Count; index++)
            if (cardGenList[index].pool == cardPool)
                gen = index;

        return gen;
    }
}
