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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        groundCheck = transform.GetChild(1).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
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
        RaycastHit hit;
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
