using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    [Header("Scenes")]
    [Scene] [SerializeField]
    private string menuScene;    
    [Scene] [SerializeField]
    private string gameScene;

    [Header("Game Settings")]
    [SerializeField]
    private int minConnections = 2;
    [SerializeField]
    private float gameBootUpDelay = 1f;

    public int startingHealth = 3;
    public int startingMana = 2;
    public int gameMode = 1;
    public bool diviners = false;
    public string cardPool = "00000000";

    [Header("Assigments")]
    [SerializeField]
    private LobbyPlayerManager lobbyPlayerPrefab;   
    [SerializeField]
    private GamePlayerManager gamePlayerPrefab;

    //player lists
    private readonly List<LobbyPlayerManager> lobbyPlayers = new List<LobbyPlayerManager>();
    private readonly List<GamePlayerManager> gamePlayers = new List<GamePlayerManager>();

    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;

    private int changeSceneReadyPlayers;

    public override void Awake()
    {
        if (singleton != null)
        {
            Destroy(gameObject);
            return;
        }
       
        base.Awake();
    }

    public override void Start()
    {
        base.Start();

        StartCoroutine(InitialiseStartMenu());
    }

    private IEnumerator InitialiseStartMenu()
    {
        yield return new WaitForSeconds(gameBootUpDelay);
        FindObjectOfType<StartMenuManager>().InitialiseStartMenu();
    }

    public bool IsClientOrServerActive()
    {
        return NetworkClient.active || NetworkServer.active;
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        NetworkClient.AddPlayer();

        OnClientConnected?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        OnClientDisconnected?.Invoke();
    }

    //clients connect before they are ready
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        //disconnect clients if over max players or server already in game scene
        if ((numPlayers >= maxConnections) || ((SceneManager.GetActiveScene().path != menuScene)))
        {
            conn.Disconnect();
            return;
        }
    }

    //this is ran AFTER onclientconnect/onserverconnect
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        //lobby
        if (SceneManager.GetActiveScene().path == menuScene)
        {
            bool setHost = lobbyPlayers.Count == 0;

            //creates player and links it to client
            LobbyPlayerManager lobbyPlayer = Instantiate(lobbyPlayerPrefab);
            DontDestroyOnLoad(lobbyPlayer);
            lobbyPlayers.Add(lobbyPlayer);

            NetworkServer.AddPlayerForConnection(conn, lobbyPlayer.gameObject);

            //initialise player
            lobbyPlayer.TargetInitialiseLobbyPlayer(setHost, string.Empty, -1, lobbyPlayers.Count);
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity)
        {
            if (SceneManager.GetActiveScene().path == menuScene)
            {
                //lobby
                LobbyPlayerManager player = conn.identity.GetComponent<LobbyPlayerManager>();

                if ((lobbyPlayers.Count <= 1) || (player == null) || player.isHost)
                {
                    StopServer();
                }
                else
                {
                    lobbyPlayers.Remove(player);
                    UpdateLobbyInfo();
                }
            }
            else if (SceneManager.GetActiveScene().path == gameScene)
            {
                GamePlayerManager player = conn.identity.GetComponent<GamePlayerManager>();

                if ((gamePlayers.Count <= 1) || (player == null) || player.myProfile.isHost)
                {
                    StopServer();
                }
                else
                {
                    gamePlayers.Remove(player);

                    //return everyone else back to the lobby
                    StartCoroutine(MenuTransitionManager.LoadingDelay(SwitchScene, false));
                    player.RpcToggleLoading(false);
                }
            }
        }

        //the player only gets destroyed here
        base.OnServerDisconnect(conn);
    }

    public void UpdateAllLobbyInfo()
    {
        for (int index = 0; index < lobbyPlayers.Count; index++)
            UpdateLobbyInfo(lobbyPlayers[index]);
    }

    public void UpdateLobbyInfo(LobbyPlayerManager player = null)
    {
        if (player && (player.playerInitialised))
        {
            string readyText;

            if (player.isHost)
                readyText = "Host";
            else
                readyText = player.isReady ? "Ready" : "Not Ready";
                
            player.RpcUpdatePlayerBanner(player.myName, readyText, player.myIcon);
        }

        //check if host can start the game
        for (int index = 0; index < lobbyPlayers.Count; index++)
        {
            lobbyPlayers[index].TargetUpdateGameStartStatus(CheckCanStart());
        }
    }

    private bool CheckCanStart()
    {
        if (numPlayers < minConnections)
            return false;

        for (int index = 0; index < lobbyPlayers.Count; index++)
            if (!lobbyPlayers[index].isReady)
                return false;

        return true;
    }

    public void TerminateClient()
    {
        NetworkIdentity identity = NetworkClient.connection.identity;

        if (identity)
        {
            if (SceneManager.GetActiveScene().path == menuScene)
            {
                //lobby
                LobbyPlayerManager player = identity.GetComponent<LobbyPlayerManager>();

                StopClient();

                //fail safe to ensure server is shutdown
                if (player.isHost || (lobbyPlayers.Count <= 1))
                    StopServer();
            }
        }
        else
        {
            StopClient();
        }
    }

    public override void OnStartServer()
    {
        lobbyPlayers.Clear();
        gamePlayers.Clear();
    }

    public bool VerifyCanStartGame()
    {
        if ((startingHealth <= 0) || (startingMana < 0))
        {
            Debug.LogWarning("Health or mana is negative. Cannot start game.");
            return false;
        }

        if (cardPool == CardDatabaseManager.EmptyCardPool)
        {
            Debug.LogWarning("Card pool is empty. Cannot start game.");
            return false;
        }

        return true;
    }

    [Server]
    public void SwitchScene()
    {
        changeSceneReadyPlayers = 0;

        if (SceneManager.GetActiveScene().path == menuScene)
        {
            if (CheckCanStart())
            {
                for (int index = 0; index < lobbyPlayers.Count; index++)
                {
                    LobbyPlayerManager lobbyPlayer = lobbyPlayers[index];

                    GamePlayerManager gamePlayer = Instantiate(gamePlayerPrefab);
                    DontDestroyOnLoad(gamePlayer);
                    gamePlayers.Add(gamePlayer);

                    NetworkServer.ReplacePlayerForConnection(lobbyPlayer.connectionToClient, gamePlayer.gameObject);
                }

                ServerChangeScene(gameScene);
            }
        }
        else if (SceneManager.GetActiveScene().path == gameScene)
        {
            for (int index = 0; index < gamePlayers.Count; index++)
            {
                GamePlayerManager gamePlayer = gamePlayers[index];

                LobbyPlayerManager lobbyPlayer = Instantiate(lobbyPlayerPrefab);
                DontDestroyOnLoad(lobbyPlayer);
                lobbyPlayers.Add(lobbyPlayer);

                NetworkServer.ReplacePlayerForConnection(gamePlayer.connectionToClient, lobbyPlayer.gameObject);
            }

            ServerChangeScene(menuScene);
        }
    }

    //ready occurs for each scene change, connect only occurs once
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        //the following should only run when switching scenes
        //returns if the ready client just connected
        if (conn.identity == null)
            return;

        changeSceneReadyPlayers++;

        if (SceneManager.GetActiveScene().path == gameScene)
        {
            if (changeSceneReadyPlayers != gamePlayers.Count)
                return;

            LobbyPlayerManager lobb;
            GamePlayerManager game;

            //handle game player initialisation
            //destroy and clear the lobby players
            for (int index = 0; index < lobbyPlayers.Count; index++)
            {
                lobb = lobbyPlayers[index];
                game = gamePlayers[index];

                game.HandlePresceneChange(lobb.myName, lobb.myIcon, lobb.isHost);

                NetworkServer.Destroy(lobb.gameObject);
            }

            lobbyPlayers.Clear();

            //initialise game setting stuff
            PlayerResourcesManager resourcesScript = FindObjectOfType<PlayerResourcesManager>();
            resourcesScript.ResourcesUpdateGameSettings(startingHealth, startingMana, gameMode, diviners, cardPool);

            //go to game start
            GameManager gameScript = FindObjectOfType<GameManager>();
            gameScript.OnGameStart(gamePlayers.ToArray());

            gamePlayers[0].RpcToggleLoading(true);
        }
        else if (SceneManager.GetActiveScene().path == menuScene)
        {
            if (changeSceneReadyPlayers != lobbyPlayers.Count)
                return;

            LobbyPlayerManager lobb;
            GamePlayerManager game;

            //handle game player initialisation
            //destroy and clear the lobby players
            for (int index = 0; index < gamePlayers.Count; index++)
            {
                lobb = lobbyPlayers[index];
                game = gamePlayers[index];

                //initialise player ui texts and pass info to server
                lobb.TargetInitialiseLobbyPlayer(game.myProfile.isHost, game.myProfile.userName, game.myProfile.icon, lobbyPlayers.Count);
                lobb.TargetInitialiseStartMenu();

                if (game.myProfile.isHost)
                {
                    lobb.TargetServerSendGameSettings(startingHealth, startingMana, gameMode, diviners, cardPool);
                }

                NetworkServer.Destroy(game.gameObject);
            }

            gamePlayers.Clear();

            lobbyPlayers[0].RpcToggleLoading(true);
        }
    }

    public void ServerUpdateGameSettings(int _health, int _mana, int _gameMode, bool _diviners, string _cardPool)
    {
        startingHealth = _health;
        startingMana = _mana;
        gameMode = _gameMode;
        diviners = _diviners;
        cardPool = _cardPool;
    }

    public void AcceptRequestGameSettings()
    {
        for (int player = 0; player < lobbyPlayers.Count; player++)
        {
            if (lobbyPlayers[player].isHost)
            {
                lobbyPlayers[player].TargetTryHostUpdateGameSettings();
                return;
            }
        }
    }

    private void OnEnable()
    {
        OnClientDisconnected += ReturnToStartMenu;
    }

    private void OnDisable()
    {
        OnClientDisconnected -= ReturnToStartMenu;
    }

    private void ReturnToStartMenu()
    {
        if (SceneManager.GetActiveScene().path == gameScene)
        {
            StartCoroutine(MenuTransitionManager.LoadingDelay(delegate
            {
                SceneManager.LoadScene(menuScene);
                StartCoroutine(WaitForMenuSceneLoad());
            }, false));
        }
    }

    private IEnumerator WaitForMenuSceneLoad()
    {
        yield return new WaitUntil(() => SceneManager.GetActiveScene().path == menuScene);
        StartCoroutine(InitialiseStartMenu());
    }
}
