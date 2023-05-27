
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ©GameGurus - Heikkinen R., Hopeasaari J., Kantola J., Kettunen J., Kommio R, PC, Parviainen P., Rautiainen J.
// Creator: PC, Phatchanon Chuchat and J.K, Janne Kettunen
// PlayerMovement

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // Movement speed of the player
    [SerializeField] private float playerSpeed = 2.0f;

    // Height of the player's jump
    [SerializeField] private float jumpHeight = 1.0f;

    // Ratio of the height of the players double jump
    [SerializeField] private float doublejumpRatio = 1.0f;

    // Strength of gravity affecting the player
    [SerializeField] private float gravityValue = -9.81f;

    // AudioClips for jumping
    [Header("Sounds")]
    [Tooltip("Chance to play sounds, 100% to play always")] [SerializeField] private float chanceToPlay = 80;
    [Tooltip("Audioclips for single jump")] [SerializeField] private List<AudioClip> jumpSounds;
    [Tooltip("Audioclips for double jump")] [SerializeField] private List<AudioClip> doublejumpSounds;
    [Tooltip("Jump sounds that play always when player jumps")] [SerializeField] private List<AudioClip> alwaysPlayJumpSounds;
    [Tooltip("Doublejump sounds that play always when player jumps")] [SerializeField] private List<AudioClip> alwaysPlayDoubleJumpSounds;



    // Reference to the CharacterController component
    private CharacterController controller;

    // Reference to the Animator component
    private Animator animator;

    // Current velocity of the player
    private Vector3 playerVelocity;

    // Is the player on a steep slope
    private bool steepSliding = false;

    // The direction the player should slide, ignoring momevent input
    private Vector3 slideDirection = Vector3.zero;

    // Whether or not the player is currently grounded
    private bool groundedPlayer;

    // Whether or not the player is standing on top of the other player
    private bool aboveOtherPlayerLastFrame = false;
    private bool aboveOtherPlayerCurrentFrame = false;

    // Input value for movement direction
    private Vector2 movementInput = Vector2.zero;

    // Whether or not the player can double jump
    private bool canDoubleJump = false;

    // Random sound player for effects
    private RandomSoundPlayer randomizer;

    private float coyoteTimer;

    private Transform cameraRig;

    private PlayerCarryCable carryCable;


    private void Start()
    {
        // Get the CharacterController component on this object
        controller = gameObject.GetComponent<CharacterController>();
        animator = gameObject.GetComponentInChildren<Animator>();
        // Get random sound player for soundeffects
        randomizer = GetComponent<RandomSoundPlayer>();
        cameraRig = Camera.main.GetComponentInParent<Transform>();
        carryCable = GetComponent<PlayerCarryCable>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // Read the value of the movement input and store it in movementInput
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (PlayerGrounded() && !steepSliding)
            {
                coyoteTimer = 0;
                // Play single jump sound
                randomizer.Play(jumpSounds, chanceToPlay);
                randomizer.Play(alwaysPlayJumpSounds);

                // Set player velocity for single jump
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);

                // Allow double jump
                canDoubleJump = true;
                if (carryCable != null && carryCable.IsCarrying()) canDoubleJump = false;

                animator.ResetTrigger("Idle");
                animator.ResetTrigger("Walk");
                animator.SetTrigger("Jump");
            }
            else if (canDoubleJump)
            {
                // Play double jump sound
                randomizer.Play(doublejumpSounds, chanceToPlay);
                randomizer.Play(alwaysPlayDoubleJumpSounds);

                // Set player velocity for double jump when going up
                if (playerVelocity.y >= -1)
                {
                    playerVelocity.y = Mathf.Sqrt(jumpHeight * doublejumpRatio * -2.0f * gravityValue);
                }
                // Set player velocity for double jump when going down (only lowers downwards momentum to it's square root before adding the updwards momentum)
                else
                {
                    playerVelocity.y = -Mathf.Sqrt(-playerVelocity.y) + Mathf.Sqrt(jumpHeight * doublejumpRatio * -2.0f * gravityValue);
                }

                // Disable double jump until next grounded state
                canDoubleJump = false;

                animator.SetTrigger("Jump");
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.tag == "player" && hit.transform.position.y < transform.position.y - 1f && playerVelocity.y <= 0)
        {
            aboveOtherPlayerLastFrame = true;
            aboveOtherPlayerCurrentFrame = true;

            slideDirection = Vector3.up - hit.normal * Vector3.Dot(Vector3.up, hit.normal);
        }
        else
        {
            aboveOtherPlayerLastFrame = aboveOtherPlayerCurrentFrame;
            aboveOtherPlayerCurrentFrame = false;
        }
    }

    public bool PlayerGrounded()
    {
        return coyoteTimer > 0 && !aboveOtherPlayerLastFrame && !SlopeSliding();
    }

    bool SlopeSliding()
    {
        if (controller.isGrounded)
        {
            Vector3 castOrigin = transform.position + new Vector3(0, controller.radius, 0);

            if (Physics.SphereCast(castOrigin, controller.radius - 0.01f, Vector3.down, out RaycastHit hit, 10.0f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
            {
                Collider coll = hit.collider;
                float angle = Vector3.Angle(Vector3.up, hit.normal);

                if (angle > controller.slopeLimit)
                {
                    float speedDivisor = (angle -35) / 20f;
                    Vector3 normal = hit.normal;
                    float yInverse = 1f - normal.y;
                    slideDirection = new Vector3(-yInverse * normal.x / speedDivisor, 0, -yInverse * normal.z / speedDivisor);
                    return true;
                }
            }
        }
        return false;
    }

    void Update()
    {
        // Check if the player is currently grounded
        groundedPlayer = controller.isGrounded;
        steepSliding = SlopeSliding();
        if (groundedPlayer && playerVelocity.y <= 0 && !aboveOtherPlayerLastFrame && !steepSliding)
        {
            coyoteTimer = 0.1f;
            slideDirection = Vector3.zero;
        }
        if (coyoteTimer > 0)
        {
            coyoteTimer -= Time.deltaTime;
        }
        // Reset player velocity to zero when grounded
        if (groundedPlayer && playerVelocity.y < 0 && !steepSliding)
        {
            playerVelocity.y = -2f;

            // Disable double jump until next jump
            canDoubleJump = false;
        }


        //Camera direction
        Vector3 cameraForward = cameraRig.forward;
        Vector3 cameraRight = cameraRig.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        //Relative camera direction
        Vector3 relativeForward = movementInput.y * cameraForward;
        Vector3 relativeRight = movementInput.x * cameraRight;

        Vector3 relativeMove = relativeForward + relativeRight;

        // Calculate the player's movement vector and move the player
        Vector3 move = new(relativeMove.x, 0, relativeMove.z);

        if (slideDirection == Vector3.zero)
        {
            Physics.SyncTransforms();
            controller.Move(playerSpeed * Time.deltaTime * move);
        }
        else
        {
            Physics.SyncTransforms();
            controller.Move(-playerSpeed * Time.deltaTime * slideDirection);
        }

        // Face the player in the direction of movement
        if (move != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(move);
        }

        // Apply gravity to the player's velocity and move the player
        playerVelocity.y += gravityValue * Time.deltaTime;
        Physics.SyncTransforms();
        controller.Move(playerVelocity * Time.deltaTime);

        if (movementInput != Vector2.zero && PlayerGrounded())
        {
            animator.ResetTrigger("Idle");
            animator.SetTrigger("Walk");
        }

        if (movementInput == Vector2.zero && PlayerGrounded())
        {
            animator.ResetTrigger("Walk");
            animator.SetTrigger("Idle");
        }
    }

}