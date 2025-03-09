using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class StartMenuManager : MonoBehaviour
{
    private static string ip = string.Empty;

    private bool hostGameFailsafeTrigger = false;

    private GameObject currentMenu = null;

    //these CANNOT be assigned in the editor because they are dont destroy on load
    private CustomNetworkManager networkScript;

    [Header("Menus")]
    public GameObject lobbyMenuRoot;
    public GameObject mainMenuRoot;
    public GameObject ipMenuRoot;

    [Header("UI")]
    [SerializeField]
    private TMP_InputField nameInput;
    [SerializeField]
    private TMP_InputField ipInput;
    [SerializeField]
    private Button findLobbyButton;
    [SerializeField]
    private float findLobbyButtonPressInterval;
    [SerializeField]
    private Image iconCarousel;
    [SerializeField]
    private Button startGameButton;
    [SerializeField]
    private Button quitLobbyButton;

    private static string playerName = string.Empty;
    public string PlayerName { get { return playerName; } set { playerName = value; nameInput.text = value; } }

    private static int playerIcon = -1;
    public int PlayerIcon { get { return playerIcon; } set { playerIcon = value; iconCarousel.sprite = SpriteDatabase.GetPlayerIcon(value); } }

    public void InitialiseStartMenu(bool startInLobby = false)
    {
        networkScript = FindObjectOfType<CustomNetworkManager>();

        if (!string.IsNullOrEmpty(ip))
            ipInput.text = ip;

        if (!startInLobby)
        {
            StartCoroutine(MenuTransitionManager.LoadingDelay(null, true, false));
        }

        PlayerName = playerName;
        if (PlayerIcon == -1)
        {
            PlayerIcon = UnityEngine.Random.Range(0, SpriteDatabase.playerIcons.Count);
        }
        else
        {
            PlayerIcon = playerIcon;
        }

        ForceGameMenu(startInLobby ? lobbyMenuRoot : mainMenuRoot);
    }

    public Button GetStartGameButton() => startGameButton;

    public void OnNameEndEdit()
    {
        PlayerName = nameInput.text;
    }

    public void OnNextIconButton()
    {
        if (PlayerIcon >= SpriteDatabase.playerIcons.Count - 1)
            PlayerIcon = 0;
        else
            PlayerIcon++;
    }

    public void OnPreviousIconButton()
    {
        if (PlayerIcon == 0)
            PlayerIcon = SpriteDatabase.playerIcons.Count - 1;
        else
            PlayerIcon--;
    }

    public void ForceGameMenu(GameObject newMenu)
    {
        mainMenuRoot.SetActive(newMenu == mainMenuRoot);
        ipMenuRoot.SetActive(newMenu == ipMenuRoot);
        lobbyMenuRoot.SetActive(newMenu == lobbyMenuRoot);
        currentMenu = newMenu;
    }

    public void SwitchGameMenu(GameObject newMenu)
    {
        if (currentMenu == newMenu)
            return;

        if (currentMenu)
            currentMenu.SetActive(false);
        newMenu.SetActive(true);

        currentMenu = newMenu;
    }

    public void OnQuitButton()
    {
        StartCoroutine(MenuTransitionManager.LoadingDelay(Application.Quit));
    }

    public void OnHostGameButton()
    {
        StartCoroutine(MenuTransitionManager.LoadingDelay(delegate 
        {
            hostGameFailsafeTrigger = true;
            StartCoroutine(HostGameFailSafe());
            networkScript.StartHost();
        }, false));
    }

    private IEnumerator HostGameFailSafe()
    {
        yield return new WaitForSecondsRealtime(5f);
        if (hostGameFailsafeTrigger)
        {
            StartCoroutine(MenuTransitionManager.LoadingDelay(null, true, true));
        }
    }

    public void OnJoinGameButton()
    {
        StartCoroutine(MenuTransitionManager.LoadingDelay(() => SwitchGameMenu(ipMenuRoot)));
    }

    public void OnBackButton()
    {
        StartCoroutine(MenuTransitionManager.LoadingDelay(delegate 
        {
            if (networkScript.isNetworkActive)
                OnFindLobbyButton();

            SwitchGameMenu(mainMenuRoot);
        }));
    }

    public void OnFindLobbyButton()
    {
        findLobbyButton.interactable = false;
        StartCoroutine(EnableFindLobbyButton());

        if (networkScript.isNetworkActive)
        {
            networkScript.StopClient();
        }
        else
        {
            ip = ipInput.text;

            if (string.IsNullOrEmpty(ip))
                ip = "localhost";

            networkScript.networkAddress = ip;
            networkScript.StartClient();

            findLobbyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop";
        }
    }

    private IEnumerator EnableFindLobbyButton()
    {
        yield return new WaitForSeconds(findLobbyButtonPressInterval);
        findLobbyButton.interactable = true;
    }

    public void OnQuitLobbyButton()
    {
        StartCoroutine(MenuTransitionManager.LoadingDelay(networkScript.TerminateClient, false));
    }

    private void OnEnable()
    {
        CustomNetworkManager.OnClientConnected += HandleClientConnected;
        CustomNetworkManager.OnClientDisconnected += HandleClientDisconnected;
    }

    private void OnDisable()
    {
        CustomNetworkManager.OnClientConnected -= HandleClientConnected;
        CustomNetworkManager.OnClientDisconnected -= HandleClientDisconnected;
    }

    private void HandleClientConnected()
    {
        hostGameFailsafeTrigger = false;
        StartCoroutine(MenuTransitionManager.LoadingDelay(() => SwitchGameMenu(lobbyMenuRoot)));
    }

    private void HandleClientDisconnected()
    {
        //this function ALSO triggers when stopping a client's attempt to join a server
        //due to modifying Kcp Transport

        //hard check to prevent 'stop' button in ip menu from returning to main menu
        if (currentMenu != ipMenuRoot)
        {
            StartCoroutine(MenuTransitionManager.LoadingDelay(() => SwitchGameMenu(mainMenuRoot)));
        }

        findLobbyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Find Lobby";
    }
}
