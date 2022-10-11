using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(InputHandler))]

public class PlayerController : NetworkBehaviour
{

    #region Variables

    System.Random random = new System.Random();
    public int r; // Variable para el cambio de aspecto
    public int n; // Variable para el cambio de nombre

    // Variables para el disparo
    [SerializeField] private GameObject bullet; 
    [SerializeField] private Transform posCross;

    // Array de los diferentes sprites
    public RuntimeAnimatorController[] anims = new RuntimeAnimatorController[5];
    // Array con todos los nombres
    public string[] nombres = new string[12];

    // Variables del mundo de juego
    readonly float speed = 3.4f;
    readonly float jumpHeight = 6.5f;
    readonly float gravity = 1.5f;
    readonly int maxJumps = 1;

    LayerMask _layer;
    int _jumpsLeft;

    // Componentes del jugador
    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/ContactFilter2D.html
    ContactFilter2D filter;
    InputHandler handler;
    Player player;
    Rigidbody2D rb;
    new CapsuleCollider2D collider;
    Animator anim;
    SpriteRenderer spriteRenderer;

    public Text nombre;

    // Variables del jugador
    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    NetworkVariable<bool> FlipSprite;
    NetworkVariable<int> Sprite;
    NetworkVariable<int> Nombre;

    #endregion

    #region Unity Event Functions

    // Obtenemos los componenetes del jugaodr e inicializamos las variables del jugador
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<CapsuleCollider2D>();
        handler = GetComponent<InputHandler>();
        player = GetComponent<Player>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        nombre = GetComponentInChildren<Canvas>().GetComponentInChildren<Text>();

        FlipSprite = new NetworkVariable<bool>();
        Sprite = new NetworkVariable<int>();
        Nombre = new NetworkVariable<int>();
    }

    // Asociamos un método a cada variable cada vez que cambian su valor
    private void OnEnable()
    {
        handler.OnMove.AddListener(UpdatePlayerVisualsServerRpc);
        handler.OnJump.AddListener(PerformJumpServerRpc);
        handler.OnFire.AddListener(Disparar);
        handler.OnMoveFixedUpdate.AddListener(UpdatePlayerPositionServerRpc);

        FlipSprite.OnValueChanged += OnFlipSpriteValueChanged;
        Sprite.OnValueChanged += OnSpriteValueChanged;
        Nombre.OnValueChanged += OnNombreValueChanged;
    }

    private void OnDisable()
    {
        handler.OnMove.RemoveListener(UpdatePlayerVisualsServerRpc);
        handler.OnJump.RemoveListener(PerformJumpServerRpc);
        handler.OnFire.RemoveListener(Disparar);
        handler.OnMoveFixedUpdate.RemoveListener(UpdatePlayerPositionServerRpc);

        FlipSprite.OnValueChanged -= OnFlipSpriteValueChanged;
        Sprite.OnValueChanged -= OnSpriteValueChanged;
        Nombre.OnValueChanged -= OnNombreValueChanged;
    }

    void Start()
    {
        // Configure Rigidbody2D
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = gravity;

        // Configure LayerMask
        _layer = LayerMask.GetMask("Obstacles");

        // Configure ContactFilter2D
        filter.minNormalAngle = 45;
        filter.maxNormalAngle = 135;
        filter.useNormalAngle = true;
        filter.layerMask = _layer;

        if (IsLocalPlayer) // Se actualiza la cámara de forma local
        {
            player.ConfigureCamera();
        }

        r = random.Next(4); // Se elige una skin random
        n = random.Next(12); // Se elige una nombre random
    }

    #endregion

    #region RPC

    #region ServerRPC

    // Se actualizan en el servidor las variables del jugador
    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerVisualsServerRpc(Vector2 input)
    {
        UpdateAnimatorStateServerRpc();
        UpdateSpriteOrientation(input);
        UpdateSpriteServerRpc();
    }

    // Actualización de los estados del jugador
    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdateAnimatorStateServerRpc()
    {
        if (IsGrounded)
        {
            anim.SetBool("isGrounded", true);
            anim.SetBool("isJumping", false);
        }
        else
        {
            anim.SetBool("isGrounded", false);
        }
    }

    // Configuración del salto del jugador
    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void PerformJumpServerRpc()
    {
        if (_jumpsLeft > 0)
        {
            player.State.Value = PlayerState.Jumping;
            anim.SetBool("isJumping", true);
            _jumpsLeft--;
            rb.velocity = new Vector2(rb.velocity.x, jumpHeight);
        }
    }

    // Configuración de la posición del jugador
    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerPositionServerRpc(Vector2 input)
    {
        if (IsGrounded)
        {
            player.State.Value = PlayerState.Grounded;
            _jumpsLeft = maxJumps;
        }

        if ((player.State.Value != PlayerState.Hooked))
        {
            rb.velocity = new Vector2(input.x * speed, rb.velocity.y);
        }
    }

    // Le mandamos al servidor que instancie cada disparo
    [ServerRpc]
    void UpdateShootsServerRpc(Vector2 dir)
    {
        var Shoot = Instantiate(bullet, player.transform.position + new Vector3(dir.x, dir.y, 0) / 2, Quaternion.identity);
        Shoot.GetComponent<Rigidbody2D>().velocity = dir * 4;
        Shoot.GetComponent<Shoot>().idJugador = player.OwnerClientId;
        Shoot.GetComponent<NetworkObject>().Spawn(true);

        UpdateShootsClientRpc(dir);
    }

    // Cada cliente instancia los disparos
    [ClientRpc]
    void UpdateShootsClientRpc(Vector2 dir)
    {
        var Shoot = Instantiate(bullet, player.transform.position + new Vector3(dir.x, dir.y, 0) / 2, Quaternion.identity);
        Shoot.GetComponent<Rigidbody2D>().velocity = dir * 10;
        Shoot.GetComponent<Shoot>().idJugador = player.OwnerClientId;
        Shoot.GetComponent<Shoot>().id = player.id;
        Shoot.GetComponent<Shoot>().jugador = player;
        Shoot.GetComponent<NetworkObject>().Spawn(true);
    }

    // Se calcula la dirección de la bala y se manda al servidor
    void Disparar()
    {
        var dir = posCross.transform.position - player.transform.position;
        UpdateShootsServerRpc(dir);
    }

    #endregion

    #endregion

    #region Methods

    // Actualización de la orientación del sprite del jugador
    void UpdateSpriteOrientation(Vector2 input)
    {
        if (input.x < 0)
        {
            FlipSprite.Value = false;
        }
        else if (input.x > 0)
        {
            FlipSprite.Value = true;
        }
    }

    void OnFlipSpriteValueChanged(bool previous, bool current)
    {
        spriteRenderer.flipX = current;
    }

    bool IsGrounded => collider.IsTouching(filter);

    // Actualizar el sprite que emplea el animator
    void OnSpriteValueChanged(int previous, int current)
    {
        anim.runtimeAnimatorController = anims[current];
    }

    // Actualizar el nombre que emplea el animator
    void OnNombreValueChanged(int previous, int current)
    {
        nombre.text = nombres[current];
    }

    // Actualizamos en el servidor el nombre y skin 
    [ServerRpc]
    public void UpdateSpriteServerRpc()
    {
        Sprite.Value = r;
        Nombre.Value = n;
    }

    #endregion
}
