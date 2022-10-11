using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : NetworkBehaviour
{
    // Identidicadores del jugador que dispara
    public ulong idJugador;
    public int id;

    public Player jugador;

    // Deterctamos la colisión de la bala
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (IsServer)
        {
            // Si ha colisionado con un jugador y no es quien dispara, pierde una vida
            var player = col.GetComponent<Player>();
            if (player != null && idJugador != player.OwnerClientId)
            {
                //print("hit");
                player.vida.Value--;

                if (player.vida.Value == 0)
                {
                    player.puntos.Value--;
                }

            }

        }

        Destroy(gameObject);
        GetComponent<NetworkObject>().Despawn();
    }
}
