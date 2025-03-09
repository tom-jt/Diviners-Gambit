using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayerManager : NetworkBehaviour
{
    private StartMenuManager startMenuScript;
    private CustomNetworkManager networkScript;
    private Button startGameButton;

    //common variables
    [SyncVar] [HideInInspector]
    public bool isHost = false;
    [SyncVar] [HideInInspector]
    public bool playerInitialised = false;

    //server variables
    [HideInInspector]
    public bool isReady = false;
    [HideInInspector]
    public int myIcon = -1;
    [HideInInspector]
    public string myName = string.Empty;


    [Header("Assignments")]
    [SerializeField]
    private Transform banner;
    [SerializeField]
    private Image bannerImage;
    [SerializeField]
    private TextMeshProUGUI displayName;
    [SerializeField]
    private TextMeshProUGUI displayReady;
    [SerializeField]
    private Image displayIcon;
    [SerializeField]
    private Image connectedPlayerIndicator;

    public override void OnStartClient()
    {
        connectedPlayerIndicator.gameObject.SetActive(false);
    }

    public override void OnStartServer()
    {
        playerInitialised = false;
    }

    [TargetRpc]
    public void TargetInitialiseLobbyPlayer(bool setHost, string name, int icon, int lobbyCount)
    {
        networkScript = FindObjectOfType<CustomNetworkManager>();

        startMenuScript = FindObjectOfType<StartMenuManager>();

        //button
        startGameButton = startMenuScript.GetStartGameButton();
        startGameButton.GetComponentInChildren<TextMeshProUGUI>().text = setHost ? "Start" : "Ready";
        startGameButton.onClick.AddListener(OnStartButton);

        //player banner
        displayReady.text = setHost ? "Host" : "Loading";
        connectedPlayerIndicator.gameObject.SetActive(true);

        int setIcon = icon == -1 ? startMenuScript.PlayerIcon : icon;
        string setName;
        if (string.IsNullOrEmpty(name))
        {
            if (string.IsNullOrEmpty(startMenuScript.PlayerName))
            {
                //set default name
                setName = "Player " + lobbyCount;
            }
            else
            {
                //set startmenumanager name
                setName = startMenuScript.PlayerName;
            }
        }
        else
        {
            //set name provided by server
            setName = name;
        }

        CmdHandlePresceneChange(setHost, setName, setIcon);
    }

    [TargetRpc]
    public void TargetInitialiseStartMenu()
    {
        FindObjectOfType<StartMenuManager>().InitialiseStartMenu(true);
    }

    [Command]
    public void CmdHandlePresceneChange(bool host, string name, int icon)
    {
        networkScript = FindObjectOfType<CustomNetworkManager>();

        isHost = host;
        myName = name;
        myIcon = icon;

        isReady = isHost;

        playerInitialised = true;

        networkScript.UpdateAllLobbyInfo();
    }

    private void OnDisable()
    {
        if (startGameButton)
            startGameButton.onClick.RemoveListener(OnStartButton);

        if (banner)
            Destroy(banner.gameObject);
    }

    private void OnStartButton()
    {
        if (isHost)
        {
            startGameButton.interactable = false;
            CmdRequestStartGame();
        }
        else
        {
            CmdToggleReady();
        }
    }

    [Command]
    private void CmdRequestStartGame()
    {
        if (networkScript.VerifyCanStartGame())
        {
            StartCoroutine(MenuTransitionManager.LoadingDelay(networkScript.SwitchScene, false));
            RpcToggleLoading(false);
        }
    }

    [ClientRpc]
    public void RpcToggleLoading(bool exit)
    {
        StartCoroutine(MenuTransitionManager.LoadingDelay(null, exit));
    }

    [Command]
    private void CmdToggleReady()
    {
        isReady = !isReady;
        TargetReadyButtonText(isReady);
        networkScript.UpdateLobbyInfo(this);
    }

    [TargetRpc]
    private void TargetReadyButtonText(bool readyState)
    {
        startGameButton.GetComponentInChildren<TextMeshProUGUI>().text = readyState ? "Unready" : "Ready";
    }

    [TargetRpc]
    public void TargetUpdateGameStartStatus(bool canStart)
    {
        if (isHost)
            startGameButton.interactable = canStart;
        else
            startGameButton.interactable = true;
    }

    [ClientRpc]
    public void RpcUpdatePlayerBanner(string name, string ready, int iconIndex)
    {
        Transform parent = FindObjectOfType<BannerLayout>(true).transform;

        //first time initialising
        if (banner.parent != parent) 
        {
            displayName.text = name;
            displayIcon.sprite = SpriteDatabase.GetPlayerIcon(iconIndex);
            bannerImage.sprite = SpriteDatabase.GetBannerImage(Random.Range(0, SpriteDatabase.bannerImages.Count));
            banner.SetParent(parent, false);
        }

        displayReady.text = ready;
    }

    public void HostUpdateGameSettings(int _health, int _mana, int _gameMode, bool _diviners, string _cardPool) => CmdVerifyHostRequest(_health, _mana, _gameMode, _diviners, _cardPool);

    [Command]
    private void CmdVerifyHostRequest(int _health, int _mana, int _gameMode, bool _diviners, string _cardPool)
    {
        if (!isHost)
            return;

        if ((_health <= 0) || (_mana < 0))
            return;

        networkScript.ServerUpdateGameSettings(_health, _mana, _gameMode, _diviners, _cardPool);
        RpcOnHostUpdateGameSettings(_health, _mana, _gameMode, _diviners, _cardPool);
    }

    [ClientRpc]
    private void RpcOnHostUpdateGameSettings(int _health, int _mana, int _gameMode, bool _diviners, string _cardPool)
    {
        if (isLocalPlayer)
            return;

        GameSettingsManager gameSettingsScript = FindObjectOfType<GameSettingsManager>();
        gameSettingsScript.ReceiveUpdatedGameSettings(_health, _mana, _gameMode, _diviners, _cardPool);
    }

    [Command]
    public void CmdRequestGameSettings()
    {
        //can add user verificaiton stuff here

        networkScript.AcceptRequestGameSettings();
    }

    [TargetRpc]
    public void TargetTryHostUpdateGameSettings()
    {
        FindObjectOfType<GameSettingsManager>().SendUpdatedGameSettings();
    }

    [TargetRpc]
    public void TargetServerSendGameSettings(int _health, int _mana, int _gameMode, bool _diviners, string _cardPool)
    {
        FindObjectOfType<GameSettingsManager>().ReceiveUpdatedGameSettings(_health, _mana, _gameMode, _diviners, _cardPool);
    }
}
