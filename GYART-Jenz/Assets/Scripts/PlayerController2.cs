using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController2 : NetworkBehaviour
{
    //Object for groundCheck
    GameObject groundCheck;

    //Settings for player movement
    [Header("Movement Settings")]
    [SerializeField]
    float moveSpeed = 5f;

    [SerializeField]
    float jumpForce = 10f;







    //Chain Settings

    [Header("Chain Settings")]

    [SerializeField]
    float chainMaxLength = 6f;

    [SerializeField]
    float maxOverstretch = 0.4f;

    [SerializeField]
    float chainPullStrength = 40f;

    [SerializeField]
    float chainDamping = 8f;

    [SerializeField]
    float dragSpeedMultiplier = 1f;



    //Chain Visuals

    [Header("Chain Visuals")]
    [SerializeField]
    Sprite chainSprite;

    [SerializeField]
    LineRenderer lineRenderer;

    [SerializeField]
    float chainWidth = 0.1f;

    [SerializeField]
    int chainSegments = 10;

    [SerializeField]
    Material chainMaterial;

    [SerializeField]
    Color chainColor = new Color(0.75f, 0.75f, 0.8f, 1f);

    [SerializeField]
    Color chainTautColor = new Color(1f, 0.35f, 0.35f, 1f);






    //Direction and movement tracking

    bool isBeingDragged = false;

    float currentInputX = 0f;

    bool isMoving = false;

    Vector2 direction;




    //Respawn Variables

    GameObject respawnPoint;

    public bool playerIsDead = false;



    //Character Variables

    //Rigidbody
    Rigidbody2D rigidbody;

    //SpriteRenderer
    SpriteRenderer spriteRenderer;


    //List of player tracking

    [SerializeField]
    //List of all players in the game
    public static readonly List<PlayerController2> Players = new List<PlayerController2>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        Players.Clear();
        print("players cleared:" + Players.Count);
    }




    // Array of colors for players
    private readonly Color[] playerColors = new Color[4] { Color.darkGreen, Color.darkBlue, Color.darkRed, Color.yellow };



    // Network variables

    public readonly NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public readonly NetworkVariable<float> networkInputX = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    void Start()
    {
        //Getting Components
        rigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        respawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint");

        if (transform.childCount > 1)
        {
            groundCheck = transform.GetChild(1).gameObject;
        }

        if (chainMaxLength <= 0.1f)
        {
            chainMaxLength = 6f;
        }

        SetUpLineRenderer();

        if (!IsSpawned && !Players.Contains(this))
        {
            Players.Add(this);
            UpdatePlayerColor(playerColor.Value);
        }

        OnNetworkSpawn();
    }

    void SetUpLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = Mathf.Max(2, chainSegments);
        lineRenderer.startWidth = chainWidth;
        lineRenderer.endWidth = chainWidth;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
        lineRenderer.sortingOrder = 5;
        lineRenderer.enabled = false;

        if (chainMaterial != null)
        {
            lineRenderer.material = chainMaterial;
        }
        else if (spriteRenderer != null && spriteRenderer.sharedMaterial != null)
        {
            lineRenderer.material = new Material(spriteRenderer.sharedMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
            {
                lineRenderer.material = new Material(shader);
            }
        }

        if (chainSprite != null && lineRenderer.material != null)
        {
            lineRenderer.material.mainTexture = chainSprite.texture;
            lineRenderer.textureMode = LineTextureMode.Tile;
        }
        else
        {
            lineRenderer.textureMode = LineTextureMode.Stretch;
        }

        lineRenderer.startColor = chainColor;
        lineRenderer.endColor = chainColor;


    }





    // Called when the player object is spawned on the network
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

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
            int colorIndex = Mathf.Clamp(Players.IndexOf(this), 0, playerColors.Length - 1);
            playerColor.Value = playerColors[colorIndex];
        }

        // Apply initial color
        UpdatePlayerColor(playerColor.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        playerColor.OnValueChanged -= OnColorChanged;

        OnDisable();
    }

    private void OnDisable()
    {
        if (Players.Contains(this))
        {
            Players.Remove(this);
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Players.Contains(this))
        {
            Players.Remove(this);
        }
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

        PlayerMovement();

        if (playerIsDead || GetOtherPlayer() == null || GetOtherPlayer().playerIsDead)
        {
            Death();
        }

       
    }

    private void FixedUpdate()
    {
        if(!IsOwner)
        {
            return;
        }


        if (GetOtherPlayer() != null)
        {
            CheckIfPlayersToFarApart();

        }
    }

    private void LateUpdate()
    {
        DrawChain();
    }


    // Function to handle player movement
    void PlayerMovement()
    {
        // Handle player movement input
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            moveInput = -1f;

        }
        else if (Input.GetKey(KeyCode.D))
        {
            moveInput = 1f;
        }

        currentInputX = moveInput;

        if(IsSpawned && IsOwner)
        {
            if(Mathf.Abs(networkInputX.Value-moveInput)> 0.01f)
            {
                networkInputX.Value = moveInput;
            }
        }

        if (moveInput != 0f)
        {
            direction = new Vector2(moveInput, 0);
            rigidbody.AddForce(new Vector2(direction.x * moveSpeed, 0), ForceMode2D.Force);
            isMoving = true;

        }
        else
        {
            isMoving = false;
        }


        if (Input.GetKeyDown(KeyCode.Space) && GroundCheck())
        {
            PlayerJump();
        }


        if (rigidbody.linearVelocity.x > 5)
        {
            rigidbody.linearVelocity = new Vector2(5, rigidbody.linearVelocity.y);
        }
        if (rigidbody.linearVelocity.x < -5)
        {
            rigidbody.linearVelocity = new Vector2(-5, rigidbody.linearVelocity.y);
        }

        if(!isMoving && !isBeingDragged && Mathf.Abs(rigidbody.linearVelocity.x) > 0.05f)
        {
            float stopDir = -Mathf.Sign(rigidbody.linearVelocity.x);
            rigidbody.AddForce(new Vector2(stopDir * moveSpeed * 0.5f, 0), ForceMode2D.Force);
        }
    }

    // Function to handle player jump
    void PlayerJump()
    {
        rigidbody.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }

    //Bool to check if the player is on the ground or not
    bool GroundCheck()
    {
        if(groundCheck == null)
        {
            Debug.LogWarning("GroundCheck object is not assigned.");
            return false;
        }
        // Get the position of the groundCheck object   
        Vector2 raycastOrigin = groundCheck.transform.position;

        // Perform a circle overlap check to see if the player is touching the ground or another player
        Collider2D[] hits = Physics2D.OverlapCircleAll(raycastOrigin, 0.1f);

        // Check if any of the colliders hit are on the ground layer (layer 3) or another player (layer 6)
        for (int i = 0; i < hits.Length; i++)
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

    public PlayerController2 GetOtherPlayer()
    {
        Players.RemoveAll(player => player == null); // Clean up any null references

        //Both Players have to be connected for the chain to work, if not return null
        if (Players.Count < 2)
        {
            return null; // Not enough players to find a partner
        }

        return Players.Find(player => player != this);
    }





    void CheckIfPlayersToFarApart()
    {
        PlayerController2 otherPlayer = GetOtherPlayer();
        if (otherPlayer == null)
        {
            isBeingDragged = false;
            return;
        }


        if (GetChainLength() >= chainMaxLength)
        {
            PlayersToFarApart();
        }
        else 
        {
            isBeingDragged = false;
        }
    }

    float GetChainLength()
    {
        PlayerController2 other = GetOtherPlayer();
        if(other == null)
        {
            return 0f;
        }

        return Vector2.Distance(transform.position, GetOtherPlayer().transform.position);
    }

    



    void PlayersToFarApart()
    {
        // Handle the logic for when players are too far apart

        PlayerController2 other = GetOtherPlayer();
        if (other == null || rigidbody == null)
        {
            isBeingDragged = false;
            return;
        }

        Vector2 myPos = transform.position;
        Vector2 otherPos = other.transform.position;
        Vector2 diff = otherPos - myPos;
        float currentDistance = diff.magnitude;

        if (currentDistance < chainMaxLength)
        {
            isBeingDragged = false;
            return;
        }

        Vector2 toOther = (currentDistance > 0.0001f) ? (diff / currentDistance) : Vector2.zero;
        Vector2 awayFromOther = -toOther;
        float excess = currentDistance - chainMaxLength;

        // Check if I am actively trying to move away from the partner
        bool iAmPullingAway = (Mathf.Abs(currentInputX) > 0.1f) && (Mathf.Sign(currentInputX) == Mathf.Sign(awayFromOther.x));

        // Check the other player's input
        float otherInput = other.IsSpawned ? other.networkInputX.Value : other.currentInputX;
        bool otherIsPullingAway = (Mathf.Abs(otherInput) > 0.1f) && (Mathf.Sign(otherInput) == Mathf.Sign(toOther.x));
        bool otherIsStandingStill = (Mathf.Abs(otherInput) <= 0.1f);

        // CASE 1: The other player is walking away, and I am standing still (or not pulling away)
        // -> I GET DRAGGED WITH THE WALKING PLAYER!
        if (otherIsPullingAway && !iAmPullingAway)
        {
            isBeingDragged = true;

            // Drag this player towards the other player at drag speed
            float dragVelocityX = toOther.x * (moveSpeed * dragSpeedMultiplier);
            rigidbody.linearVelocity = new Vector2(dragVelocityX, rigidbody.linearVelocity.y);

            // Pull position forward so the chain constraint is maintained
            Vector2 targetPos = otherPos - toOther * chainMaxLength;
            bool grounded = GroundCheck();
            float targetY = (grounded && toOther.y < 0.2f) ? transform.position.y : targetPos.y;
            transform.position = new Vector3(targetPos.x, targetY, transform.position.z);
            return;
        }

        isBeingDragged = false;

        // CASE 2: I am walking away, and the other player is standing still
        // -> I am dragging the partner with me!
        if (iAmPullingAway && otherIsStandingStill)
        {
            // Allow the walking player to continue moving forward
            // Only limit speed slightly if network sync lags behind
            if (excess > 0.35f)
            {
                float clampedX = Mathf.Clamp(rigidbody.linearVelocity.x, -moveSpeed, moveSpeed);
                rigidbody.linearVelocity = new Vector2(clampedX, rigidbody.linearVelocity.y);
            }

            if (excess > maxOverstretch)
            {
                Vector2 clampedPos = otherPos - toOther * (chainMaxLength + maxOverstretch);
                transform.position = new Vector3(clampedPos.x, transform.position.y, transform.position.z);
            }
            return;
        }

        // CASE 3: Tug of war - Both players are walking away from each other
        if (iAmPullingAway && otherIsPullingAway)
        {
            // Stop outward velocity for both players
            float velAway = Vector2.Dot(rigidbody.linearVelocity, awayFromOther);
            if (velAway > 0f)
            {
                rigidbody.linearVelocity -= awayFromOther * velAway;
            }

            // Hard clamp at chainMaxLength so neither player can budge outward
            Vector2 clampedPos = otherPos - toOther * chainMaxLength;
            transform.position = new Vector3(clampedPos.x, transform.position.y, transform.position.z);
            return;
        }

        // CASE 4: General physics tension fallback (e.g. hanging from a ledge, vertical falls, momentum)
        if (excess > 0f)
        {
            float tension = excess * chainPullStrength;
            float velAway = Vector2.Dot(rigidbody.linearVelocity, awayFromOther);
            if (velAway > 0f)
            {
                tension += velAway * chainDamping;
            }

            rigidbody.AddForce(toOther * tension, ForceMode2D.Force);

            if (velAway > 0f)
            {
                float dampRatio = Mathf.Clamp01(excess / 0.25f);
                rigidbody.linearVelocity -= awayFromOther * (velAway * dampRatio);
            }

            if (excess > maxOverstretch)
            {
                Vector2 clampedPos = otherPos - toOther * (chainMaxLength + maxOverstretch);
                transform.position = new Vector3(clampedPos.x, clampedPos.y, transform.position.z);
            }
        }

    }

    void DrawChain()
    {
        if(lineRenderer == null)
        {
            return;
        }

        if (!IsOwner)
        {
            lineRenderer.enabled = false;
            return;
        }

        PlayerController2 other = GetOtherPlayer();
        if(other == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        Vector3 p0 = transform.position;
        Vector3 p1 = other.transform.position;
        float distance = Vector3.Distance(p0, p1);

        Color currentColor = (distance >= chainMaxLength) ? chainTautColor : chainColor;
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;

        float slack = Mathf.Max(0f, chainMaxLength - distance);
        float sag = Mathf.Min(slack * 0.35f, 1.5f);

        int count = Mathf.Max(2, chainSegments);
        if(lineRenderer.positionCount != count)
        {
            lineRenderer.positionCount = count;
        }

        for(int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector3 pt = Vector3.Lerp(p0, p1, t);

            pt.y -= sag * 4f * t * (1f - t);
            lineRenderer.SetPosition(i, pt);
        }
    }


    void Death()
    {
        PlayerController2 other = GetOtherPlayer();
        if(other != null)
        {
            transform.position = respawnPoint.transform.position;
            other.transform.position = respawnPoint.transform.position;
        }
    }
}


