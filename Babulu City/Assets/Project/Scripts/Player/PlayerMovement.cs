using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [Header("Animasi Karakter")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float animationFramesPerSecond = 6f;
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleRight;
    [SerializeField] private Sprite[] walkDown;
    [SerializeField] private Sprite[] walkLeft;
    [SerializeField] private Sprite[] walkUp;
    [SerializeField] private Sprite[] walkRight;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 facingDirection = Vector2.down;
    private bool canMove = true;
    private InputAction moveAction;
    private float animationTimer;
    private Vector2 animatedDirection = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        ApplyIdleSprite();
    }

    void OnEnable()
    {
        moveAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
    }

    void Update()
    {
        if (!canMove || moveAction == null)
        {
            movement = Vector2.zero;
            UpdateSpriteAnimation();
            return;
        }

        movement = moveAction.ReadValue<Vector2>();
        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        UpdateSpriteAnimation();
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
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        animationTimer = 0f;
        ApplyIdleSprite();
    }

    public void ResumeMovement()
    {
        canMove = true;
    }

    void UpdateSpriteAnimation()
    {
        if (spriteRenderer == null)
            return;

        if (movement.sqrMagnitude <= 0.001f)
        {
            animationTimer = 0f;
            ApplyIdleSprite();
            return;
        }

        Vector2 direction = CardinalDirection(movement);
        facingDirection = direction;
        if (direction != animatedDirection)
        {
            animatedDirection = direction;
            animationTimer = 0f;
        }

        Sprite[] frames = WalkFrames(direction);
        if (frames == null || frames.Length == 0)
        {
            ApplyIdleSprite();
            return;
        }

        animationTimer += Time.deltaTime;
        int frameIndex = Mathf.FloorToInt(animationTimer * Mathf.Max(1f, animationFramesPerSecond)) % frames.Length;
        Sprite frame = frames[frameIndex];
        if (frame != null)
            spriteRenderer.sprite = frame;
    }

    void ApplyIdleSprite()
    {
        if (spriteRenderer == null)
            return;

        Sprite idle = IdleSprite(facingDirection);
        if (idle != null)
            spriteRenderer.sprite = idle;
    }

    static Vector2 CardinalDirection(Vector2 value)
    {
        if (Mathf.Abs(value.x) > Mathf.Abs(value.y))
            return value.x < 0f ? Vector2.left : Vector2.right;

        return value.y < 0f ? Vector2.down : Vector2.up;
    }

    Sprite IdleSprite(Vector2 direction)
    {
        if (direction == Vector2.left) return idleLeft;
        if (direction == Vector2.right) return idleRight;
        if (direction == Vector2.up) return idleUp;
        return idleDown;
    }

    Sprite[] WalkFrames(Vector2 direction)
    {
        if (direction == Vector2.left) return walkLeft;
        if (direction == Vector2.right) return walkRight;
        if (direction == Vector2.up) return walkUp;
        return walkDown;
    }
}
