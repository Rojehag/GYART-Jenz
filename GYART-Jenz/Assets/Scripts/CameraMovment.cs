using System;
using Unity.Netcode;
using UnityEngine;


public class CameraMovment : NetworkBehaviour
{
    Vector2 player1Position;
    Vector2 player2Position;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner)
        {
            return;
        }

        GetPlayerPositions();

        transform.position = new Vector2(player1Position.x + ((player2Position.x - player1Position.x) / 2), player1Position.y + ((player2Position.y - player1Position.y) / 2));
    }

    void GetPlayerPositions()
    {
        PlayerController2 p1 = PlayerController2.Players[1];
        PlayerController2 p2 = PlayerController2.Players[2];

        if (p1 == null || p2 == null) 
        {
            return;
        }

        player1Position = p1.transform.position;

        player2Position = p2.transform.position;

    }
}
