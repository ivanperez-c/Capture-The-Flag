using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using System.Threading;

public class Player : NetworkBehaviour
{
    #region Variables

    System.Random random = new System.Random();

    // Identidicadores asignados desde el GameManager al crear al jugador 
    public ulong idJugador;
    public int id;

    // Color del que pintamos los personajes muertos
    public Color muerto;

    // Variables del jugador durante la partida
    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    public NetworkVariable<PlayerState> State;

    public NetworkVariable<int> vida;
    public NetworkVariable<float> time;
    public NetworkVariable<int> puntos;

    // Variable para controlar la muerte del jugador
    bool dead = false;

    #endregion

    #region Unity Event Functions

    // Inicialización de las variables del jugador en partida
    private void Awake()
    {
        vida = new NetworkVariable<int>();
        vida.Value = 6;

        puntos = new NetworkVariable<int>();
        puntos.Value = 3;

        time = new NetworkVariable<float>();

        State = new NetworkVariable<PlayerState>();

        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;
    }

    // Asociamos un método a cada variable cada vez que cambian su valor
    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged += OnPlayerStateValueChanged;
        vida.OnValueChanged += OnVidaValueChanged;
        time.OnValueChanged += OnTimeValueChanged;
        puntos.OnValueChanged += OnPuntosValueChanged;
    }

    private void OnDisable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged -= OnPlayerStateValueChanged;
        vida.OnValueChanged -= OnVidaValueChanged;
        time.OnValueChanged -= OnTimeValueChanged;
        puntos.OnValueChanged -= OnPuntosValueChanged;
    }

    #endregion

    private void Update()
    {
        if (vida.Value == 0) // Si la vida es 0, se comrpueba los puntos que le quedan
        {
            if (puntos.Value == 0 && dead == false) //Si no le quedan puntos de vida; muere, si le quedan, revive en una posición aleatoria
            {
                dead = true;

                GetComponent<InputHandler>().enabled = false;
                GetComponent<SpriteRenderer>().color = muerto;
                GetComponent<CapsuleCollider2D>().enabled = false;

                GameManager.Instance.jugadoresConectados--;
            }
            else
            {
                transform.position = GameManager.Instance.sp[random.Next(11)];
                vida.Value = 6;
            }
        }
    }

    #region Config Methods

    public void ConfigurePlayer(ulong clientID)
    {
        if (IsLocalPlayer)
        {
            ConfigurePlayer();
        }
    }

    // Método que activa el movimiento de los jugadores desde el GameManager
    [ClientRpc]
    public void ActivarMovimientoClientRpc()
    {
        GetComponent<InputHandler>().enabled = true;
    }

    // Método que desactivar el movimiento de los jugadores desde el GameManager
    [ClientRpc]
    public void DesactivarMovimientoClientRpc()
    {
        GetComponent<InputHandler>().enabled = false;
        GetComponent<UIManager>().ActivateMainMenu();
    }
    
    void ConfigurePlayer()
    {
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
    }

    // Configuración de la cámara del jugador
    public void ConfigureCamera()
    {
        // https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/manual/CinemachineBrainProperties.html
        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;

        virtualCam.LookAt = transform;
        virtualCam.Follow = transform;
    }

    // Actualizar la vida del jugador
    void OnVidaValueChanged(int previous, int current)
    {
        if (IsLocalPlayer)
        {
            UIManager.Instance.UpdateLifeUI(6 - current);
        }
    }

    // Actualizar el tiempo
    public void OnTimeValueChanged(float previous, float current)
    {
        if (IsLocalPlayer)
        {
            UIManager.Instance.UpdateTimer(current);
        }
    }

    // Actualizar los puntos de vida del jugador
    void OnPuntosValueChanged(int previous, int current)
    {
        if (IsLocalPlayer)
        {
            UIManager.Instance.UpdatePuntos(current);
        }
    }

    #endregion

    #region RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        State.Value = state;
    }

    #endregion

    #endregion

    #region Netcode Related Methods

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        State.Value = current;
    }

    #endregion
}

public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}
