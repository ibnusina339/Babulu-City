using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = movement * moveSpeed;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (!canMove)
            return;

        movement = context.ReadValue<Vector2>();
    }

    public void StopMovement()
    {
        canMove = false;
        movement = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    public void ResumeMovement()
    {
        canMove = true;
    }
}