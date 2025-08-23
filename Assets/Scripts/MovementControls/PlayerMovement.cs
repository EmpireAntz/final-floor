using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;
    private PlayerCombat combat; 

    [Header("Movement")]
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.6f; // how much faster than walk
    public bool sprintOnlyOnGround = true; // disable sprint in air      

    [Header("Sprint Camera")]
    public Camera playerCamera;                   
    public float sprintFOVBoost = 7f;// added FOV when sprinting
    public float fovLerpSpeed = 8f;
    private float _baseFOV;

    [Header("Grounding")] 
    public Transform groundCheck;
    public float groundDistance = 0.35f;
    public LayerMask groundMask;

    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;

    readonly int HashSpeed = Animator.StringToHash("Speed");
    readonly int HashIsJumping = Animator.StringToHash("isJumping");
    readonly int HashIsSprinting = Animator.StringToHash("isSprinting");
    readonly int HashMoveX = Animator.StringToHash("MoveX");
    readonly int HashMoveZ = Animator.StringToHash("MoveZ");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        combat = GetComponent<PlayerCombat>();

        if (playerCamera != null)
            _baseFOV = playerCamera.fieldOfView;
    }

    void Update()
    {
        // ----- Combat gate -----
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

        // ----- Horizontal input (Diagonal Normalize) -----
        float ix = 0f, iz = 0f; //local input x/z
        if (!blockMove)
        {
            bool a = Keyboard.current.aKey.isPressed;
            bool d = Keyboard.current.dKey.isPressed;
            bool w = Keyboard.current.wKey.isPressed;
            bool s = Keyboard.current.sKey.isPressed;

            ix = (d ? 1f : 0f) + (a ? -1f : 0f); // right(+), left(-)
            iz = (w ? 1f : 0f) + (s ? -1f : 0f); // forward(+), back(-)
        }

        // Normalize so diagonals aren't faster
        Vector3 input = new Vector3(ix, 0f, iz);
        Vector3 move = (transform.right * input.x + transform.forward * input.z);
        if (move.sqrMagnitude > 1f) move.Normalize();

        // ----- Sprint (Shift) -----
        bool shiftHeld = (Keyboard.current.leftShiftKey?.isPressed ?? false)
            || (Keyboard.current.rightShiftKey?.isPressed ?? false);

        bool canSprint = !blockMove
            && move.sqrMagnitude > 0.0001f
            && (!sprintOnlyOnGround || isGrounded);

        bool isSprinting = canSprint && shiftHeld;
        float currentSpeed = speed * (isSprinting ? sprintMultiplier : 1f);

        // Move horizontally
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Animator params 
        animator.SetFloat(HashMoveX, blockMove ? 0f : input.x, 0.1f, Time.deltaTime); // Strafe
        animator.SetFloat(HashMoveZ, blockMove ? 0f : input.z, 0.1f, Time.deltaTime); //Forward/Back
        animator.SetFloat(HashSpeed, blockMove ? 0f : Mathf.Clamp01(move.magnitude));
        animator.SetBool(HashIsSprinting, isSprinting);

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
            
        // ----- Sprint FOV effect -----
        if (playerCamera != null)
        {
            float targetFOV = _baseFOV + (isSprinting ? sprintFOVBoost : 0f);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
        }
}

}
