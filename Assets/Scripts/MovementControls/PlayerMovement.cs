using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;
    private PlayerCombat combat; 

    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    public Transform groundCheck;
    public float groundDistance = 0.35f;
    public LayerMask groundMask;

    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;

    readonly int HashSpeed = Animator.StringToHash("Speed");
    readonly int HashIsJumping = Animator.StringToHash("isJumping");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        combat = GetComponent<PlayerCombat>();
    }

    void Update()
{
    // cache combat once in Awake() if you’d like:
    var combat = GetComponent<PlayerCombat>();
    bool blockMove = combat != null && combat.isPunching;

    // ----- Ground check (pre-move) -----
    bool wasGroundedPrev = wasGrounded;
    bool sphereHit = Physics.CheckSphere(
        groundCheck.position,
        groundDistance,
        groundMask,
        QueryTriggerInteraction.Ignore
    );
    isGrounded = sphereHit || controller.isGrounded;

    if (isGrounded && velocity.y < 0f)
        velocity.y = -2f;

    // ----- Horizontal movement (skip if punching) -----
    float x = 0f, z = 0f;
    if (!blockMove)
    {
        if (Keyboard.current.aKey.isPressed) x = -1f;
        if (Keyboard.current.dKey.isPressed) x = 1f;
        if (Keyboard.current.wKey.isPressed) z = 1f;
        if (Keyboard.current.sKey.isPressed) z = -1f;
    }

    Vector3 move = transform.right * x + transform.forward * z;
    controller.Move(move * speed * Time.deltaTime);

    // Animator speed (force 0 while punching so idle plays)
    animator.SetFloat(HashSpeed, blockMove ? 0f : Mathf.Clamp01(move.magnitude));

    // ----- Jump (disabled while punching) -----
    if (!blockMove && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        animator.SetBool(HashIsJumping, true);
    }

    // ----- Gravity + vertical move (ALWAYS runs) -----
    velocity.y += gravity * Time.deltaTime;
    controller.Move(velocity * Time.deltaTime);

    // ----- Landing detection (after move) -----
    wasGrounded = isGrounded;
    bool afterMoveGround = Physics.CheckSphere(
        groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore
    ) || controller.isGrounded;

    if (afterMoveGround && !wasGroundedPrev && animator.GetBool(HashIsJumping))
        animator.SetBool(HashIsJumping, false);
}

}
