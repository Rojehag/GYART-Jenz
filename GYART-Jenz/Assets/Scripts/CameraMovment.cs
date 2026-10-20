using System;
using Unity.Netcode;
using UnityEngine;


public class CameraMovment : NetworkBehaviour
{
    Vector2 player1Position;
    Vector2 player2Position;

    [SerializeField]
    float shakingDistance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        

        GetPlayerPositions();

        if (player1Position == null || player2Position == null)
        {
            return;
        }

        transform.position = new Vector3(player1Position.x + ((player2Position.x - player1Position.x) / 2), player1Position.y + ((player2Position.y - player1Position.y) / 2), -10);
    }

    void GetPlayerPositions()
    {
        if(PlayerController2.Players.Count != 2)
        {
            return;
        }

        PlayerController2 p1 = PlayerController2.Players[0];
        PlayerController2 p2 = PlayerController2.Players[1];

        if (p1 == null || p2 == null) 
        {
            return;
        }

        if(player1Position.x - p1.transform.position.x < shakingDistance || player1Position.x - p1.transform.position.x > -shakingDistance
            || player1Position.y - p1.transform.position.y < shakingDistance || player1Position.y - p1.transform.position.y > -shakingDistance)
        {
            if (player2Position.x - p2.transform.position.x < shakingDistance || player2Position.x - p2.transform.position.x > -shakingDistance
            || player2Position.y - p2.transform.position.y < shakingDistance || player2Position.y - p2.transform.position.y > -shakingDistance)
            {
                return;
            }
        }

        player1Position = p1.transform.position;

        player2Position = p2.transform.position;

    }
}
