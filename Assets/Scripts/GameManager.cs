using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    System.Random random = new System.Random();

    [SerializeField] NetworkManager networkManager;
    [SerializeField] GameObject prefab;

    // ID de cada jugador conectado
    int id = 0;

    // Número mínimo y máximo de personas que se pueden conectar al servidor
    const int MIN_JUGADORES = 4;
    const int MAX_JUGADORES = 4;

    // Número de jugadores conectados al servidor
    public int jugadoresConectados = 0;

    // Array que almacena todos los jugadores
    public GameObject[] jugadores = new GameObject[MAX_JUGADORES];

    // Tiempo que dura la partida - 3 minutos
    float time = 180;
   
    // Variables para el inicio de la partida
    bool activarTimer = false;
    bool empezar = false; //Variable que se permite empezar la paartida cuando se llega al número mínimo de jugadores

    // Array de todas las posiciones en las que puede aparecer un jugador 
    public Vector3[] sp = new Vector3[] {
        new Vector3(10.4f, 5.0f, 0.0f), new Vector3(8.2f, 4.1f, 0.0f), new Vector3(-9.62f, 2.83f, 0.0f), new Vector3(-9.07f, 7.87f, 0.0f),
        new Vector3(1.54f, 2.3f, 0.0f), new Vector3(9.0f, -1.5f, 0.0f), new Vector3(5.25f, -3.2f, 0.0f), new Vector3(-0.93f, 0.26f, 0.0f),
        new Vector3(-2.26f, -3.2f, 0.0f), new Vector3(-6.51f, -2.79f, 0.0f), new Vector3(-3.33f, -4.58f, 0.0f), new Vector3(3.64f, -4.58f, 0.0f)
    };

    // Poder hacer referencia al GameManager desde otro Script
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        networkManager.OnServerStarted += OnServerReady; // Cuando se inicia el servidor se llama al método
        networkManager.OnClientConnectedCallback += OnClientConnected; // Cuando se conecta un cliente se llama al método
        //networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void Update()
    {
        if (activarTimer == true) // Si la partida ha empezado, el Timer comienza la cuenta atrás
        {
            time -= Time.deltaTime;
            
            foreach (var item in NetworkManager.Singleton.ConnectedClientsList) // Por cada cliente conectado actualizamos el timer
            {
                item.PlayerObject.GetComponent<Player>().time.Value = time;
            }

            if (time <= 0) // Si el timer llega a 0 lo paramos, desactivamos el movimiento de los jugadores y terminamos la partida
            {
                activarTimer = false;
                foreach (var item in jugadores)
                {
                    item.GetComponent<Player>().DesactivarMovimientoClientRpc();

                    terminarPartidaClientRpc();
                }
            }
        }

        if (IsServer) // Cuando en el servidor sólo queda un cliente con vida, terminamos la partida
        {
            if (jugadoresConectados == 1 && empezar == true)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count > 0)
                {
                    ulong id = NetworkManager.Singleton.ConnectedClientsIds[0];
                    print("Ganador: " + id);
                    activarTimer = false;
                    terminarPartidaClientRpc();
                }
            }
        }
    }

    // Al terminar la partida lleva a todos los clientes al menú principal
    [ClientRpc]
    void terminarPartidaClientRpc()
    {
        UIManager.Instance.ActivateMainMenu();
    }

    
    private void OnDestroy()
    {
        networkManager.OnServerStarted -= OnServerReady;
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        //networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    // Indica que el servidor está listo y el número de jugadores 
    private void OnServerReady()
    {
        print(jugadores.Length);
        print("Server ready");
    }

    // Función para cuando se conecta un cliente
    private void OnClientConnected(ulong clientId)
    {
        // Si ya está el máximo de jugadores conectado o el tiempo se ha acabado, desconectamos al jugador que se acaba de conectar
        if (NetworkManager.Singleton.ConnectedClients.Count > MAX_JUGADORES || time < 0)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        print("GM: " + NetworkManager.Singleton.ConnectedClients.Count);

        // Incrementamos el número de jugadores comentados 
        jugadoresConectados++;

        // Sólo el servidor puede instanciar los jugadores
        if (networkManager.IsServer)
        {
            int r = random.Next(11); // Número random para la posición del jugador
            Vector3 pos = sp[r]; // Asignación de la posición
            var player = Instantiate(prefab, pos, Quaternion.identity); // Instanciación del jugador y almacenamiento en la variable player
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            // Asignamos algunas variables de interés
            var jugador = player.GetComponent<Player>();
            jugador.idJugador = clientId; // ID de Unity
            jugador.id = id; // ID casero

            print("ID Jugador:" + id);
            // Rellenamos el array de jugadores con todos los players
            jugadores[id] = player;
            id++;
            
            // Cuando se llega al número de jugadores mínimo se empieza la partida, se activa el timer y se habilita el movimiento de cada jugador
            if (NetworkManager.Singleton.ConnectedClients.Count == MIN_JUGADORES)
            {
                empezar = true;
                activarTimer = true;

                for (int i = 0; i < MIN_JUGADORES; i++)
                {
                    jugadores[i].GetComponent<Player>().ActivarMovimientoClientRpc();
                }
            }
            // Cuando se conecta un jugador más del mínimo pero no se ha llegado al máximo, se habilita su movimiento
            else if (NetworkManager.Singleton.ConnectedClients.Count > MIN_JUGADORES && NetworkManager.Singleton.ConnectedClients.Count <= MAX_JUGADORES)
            {
                player.GetComponent<Player>().ActivarMovimientoClientRpc();
            }
        }
    }
}
