using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovment : NetworkBehaviour
{
    GameObject groundCheck;

    [Header("Movement Settings")]
    [SerializeField]
    float moveSpeed;

    [SerializeField]
    float jumpForce;

    bool isGrounded;

    Rigidbody2D rigidbody;

    SpriteRenderer spriteRenderer;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        groundCheck = transform.GetChild(1).gameObject;
     
    }

    public static readonly List<PlayerMovment> Players = new List<PlayerMovment>();

    private readonly Color[] playerColors = new Color[4] { Color.red, Color.blue, Color.green, Color.yellow };

    public readonly NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        if (!Players.Contains(this))
        {
            Players.Add(this);
        }

        playerColor.OnValueChanged += OnColorChanged;

        if (IsServer)
        {
            playerColor.Value = playerColors[Random.Range(0, playerColors.Length)];
        }

        // Apply initial color
        UpdatePlayerColor(playerColor.Value);
    }

    private void OnColorChanged(Color previousColor, Color newColor)
    {
        UpdatePlayerColor(newColor);
    }

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
        if(!IsOwner)
        {
            return;
        }

        GroundCheck();

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

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            PlayerJump();
        }
    }

    void PlayerMovement(Vector2 direction)
    {
        rigidbody.linearVelocityX = direction.x * moveSpeed;
    }

    void PlayerJump()
    {
        rigidbody.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }

    void GroundCheck()
    {
        Vector2 raycastOrigin = groundCheck.transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(raycastOrigin, 0.1f);

        for(int i = 0; i< hits.Length; i++)
        {
            if(hits[i].gameObject.layer == 3)
            {
                isGrounded = true;
                return;
            }
            if(hits[i].gameObject.layer == 6 && hits[i].gameObject != gameObject)
            {
                isGrounded = true;
                return;
            }

            if (i == hits.Length - 1)
            {
                isGrounded = false;
            }
        }
    }
}
