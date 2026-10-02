using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovment : NetworkBehaviour
{
    //Object for groundCheck
    GameObject groundCheck;

    //Settings for player movement
    [Header("Movement Settings")]
    [SerializeField]
    float moveSpeed;

    [SerializeField]
    float jumpForce;

    [Header("Chain Settings")]

    //Sprite for the chain

    [SerializeField]
    float chainMaxLength;


    //Rigidbody
    Rigidbody2D rigidbody;

    //SpriteRenderer
    SpriteRenderer spriteRenderer;

    void Start()
    {
        //Getting Components
        rigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        groundCheck = transform.GetChild(1).gameObject;

        
        OnNetworkSpawn();
    }

    //List of all players in the game
    public static readonly List<PlayerMovment> Players = new List<PlayerMovment>();

    // Array of colors for players
    private readonly Color[] playerColors = new Color[4] { Color.darkGreen, Color.darkBlue, Color.darkRed, Color.yellow };

    // Network variable to store the player's color
    public readonly NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
        );

    // Called when the player object is spawned on the network
    public override void OnNetworkSpawn()
    {
        // Add this player to the list of players if not already present
        if (!Players.Contains(this))
        {
            Players.Add(this);
        }

        // Change the player's color when the network variable changes
        playerColor.OnValueChanged += OnColorChanged;

        // Server assigns a random color to the player when they spawn
        if (IsServer)
        {
            playerColor.Value = playerColors[Players.IndexOf(this)];
        }

        // Apply initial color
        UpdatePlayerColor(playerColor.Value);
    }

    // Called when Player's color changes
    private void OnColorChanged(Color previousColor, Color newColor)
    {
        UpdatePlayerColor(newColor);
    }

    // Update the player's color in the SpriteRenderer
    private void UpdatePlayerColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }


    // Update is called once per frame
    void Update()
    {
        // Only allow the owner of the player object to control it
        if (!IsOwner)
        {
            return;
        }

        // Handle player movement input
        if (Input.GetKey(KeyCode.A))
        {       
            PlayerMovement(new Vector2(-1, 0));
        }
        else if (Input.GetKey(KeyCode.D))
        {
            PlayerMovement(new Vector2(1, 0));
        }
        else
        {
            PlayerMovement(new Vector2(0, 0));
        }

        if (Input.GetKeyDown(KeyCode.Space) && GroundCheck())
        {
            PlayerJump();
        }
        if(rigidbody.linearVelocity.x > 5 ) 
        {
            rigidbody.linearVelocity = new Vector2(5, rigidbody.linearVelocity.y);
        }
        if(rigidbody.linearVelocity.x < -5) 
        {
            rigidbody.linearVelocity = new Vector2(-5, rigidbody.linearVelocity.y);
        }
    }

    // Function to handle player movement
    void PlayerMovement(Vector2 direction)
    {
        rigidbody.AddForce(new Vector2(direction.x * moveSpeed, 0), ForceMode2D.Force);
    }

    // Function to handle player jump
    void PlayerJump()
    {
        rigidbody.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }

    //Bool to check if the player is on the ground or not
    bool GroundCheck()
    {
        // Get the position of the groundCheck object   
        Vector2 raycastOrigin = groundCheck.transform.position;

        // Perform a circle overlap check to see if the player is touching the ground or another player
        Collider2D[] hits = Physics2D.OverlapCircleAll(raycastOrigin, 0.1f);

        // Check if any of the colliders hit are on the ground layer (layer 3) or another player (layer 6)
        for (int i = 0; i< hits.Length; i++)
        {
            // Check if the hit object is on the ground layer (layer 3)
            if (hits[i].gameObject.layer == 3)
            {
                return true;
            }
            // Check if the hit object is on the player layer (layer 6) and is not the current player
            if (hits[i].gameObject.layer == 6 && hits[i].gameObject != gameObject)
            {
                return true;
            }
            
        }

        return false;
    }

    void CheckIfPlayersToFarApart()
    {
        if (GetChainLength() > chainMaxLength)
        {
            PlayersToFarApart();
        }
    }

    float GetChainLength()
    {
        return Vector2.Distance(Players[0].transform.position, Players[1].transform.position);
    }

    void PlayersToFarApart()
    {

    }

    void DrawChain()
    {

    }


}
