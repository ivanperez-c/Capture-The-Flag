using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UIManager : NetworkBehaviour
{
    #region Variables

    public static UIManager Instance;

    [SerializeField] NetworkManager networkManager;

    public Text timer; 
    public Text puntos; // Veces que puedes morir
    
    UnityTransport transport;

    readonly ushort port = 7777;

    [SerializeField] Sprite[] hearts = new Sprite[3];

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;
    [SerializeField] public int hits;

    [Header("In-Game HUD")]
    [SerializeField] private GameObject inGameHUD;
    [SerializeField] RawImage[] heartsUI = new RawImage[3];

    /*[Header("Menu Nombre y Skin")]
    [SerializeField] private GameObject nombreSkin;
    [SerializeField] private InputField Nombre;
    [SerializeField] private Button BotonSkinVerde;
    [SerializeField] private Button BotonSkinRosa;
    [SerializeField] private Button BotonSkinRoja;
    [SerializeField] private Button BotonSkinAmarilla;
    [SerializeField] private Button BotonJugar;*/

    //public int skin;
    //public bool hostMode = false;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
        Instance = this;
    }

    // Asiganamos la función de cada botón y activamos en menú principal
    private void Start()
    {
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        ActivateMainMenu();

      /*  BotonSkinVerde.onClick.AddListener(() => skin = 0);
        BotonSkinRosa.onClick.AddListener(() => skin = 1);
        BotonSkinRoja.onClick.AddListener(() => skin = 2);
        BotonSkinAmarilla.onClick.AddListener(() => skin = 3);

        BotonJugar.onClick.AddListener(() => empezarJuego());*/
    }

    #endregion

    #region UI Related Methods

    // Activamos menú principal y desactivamos el resto
    public void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
      //  nombreSkin.SetActive(false);
    }

    // Activamos menú HUD del juego, desactivamos el resto y actualizamos los corazones de jugadores
    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(true);
       // nombreSkin.SetActive(false);

        UpdateLifeUI(hits);
    }

    /* private void ActivarNombreSkinMenu()
     {
         mainMenu.SetActive(false);
         inGameHUD.SetActive(false);
         nombreSkin.SetActive(true);
     }*/

    // Actualizamos los corazones de jugadores
    public void UpdateLifeUI(int hitpoints)
    {
        switch (hitpoints)
        {
            case 6: // 0 corazones
                heartsUI[0].texture = hearts[2].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 5:
                heartsUI[0].texture = hearts[1].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 4:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 3:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[1].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 2:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 1: // 2 corazones y medio 
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[1].texture;
                break;
        }
    }

    // Actualizamos el timer
    public void UpdateTimer(float time)
    {
        timer.text = time.ToString("f0"); //"f0" para que sean números enteros
    }

    // Actualizamos las veces que puede morir un jugador
    public void UpdatePuntos(int points)
    {
        puntos.text = points.ToString();
    }

    #endregion

    #region Netcode Related Methods

    private void StartHost()
    {
       // hostMode = true;
        NetworkManager.Singleton.StartHost();
        ActivateInGameHUD();
        // ActivarNombreSkinMenu();
    }

    private void StartClient()
    {
        //hostMode = false;

        var ip = inputFieldIP.text;
        if (!string.IsNullOrEmpty(ip))
        {
            transport.SetConnectionData(ip, port);
        }

        NetworkManager.Singleton.StartClient();

        ActivateInGameHUD();
        // ActivarNombreSkinMenu();
    }

    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        ActivateInGameHUD();
    }

   /* private void empezarJuego()
    {
        if (hostMode)
        {
            NetworkManager.Singleton.StartHost();
            ActivateInGameHUD();
        }
        else
        {
            var ip = inputFieldIP.text;
            if (!string.IsNullOrEmpty(ip))
            {
                transport.SetConnectionData(ip, port);
            }

            NetworkManager.Singleton.StartClient();

            ActivateInGameHUD();
        }
    }*/

    #endregion
}