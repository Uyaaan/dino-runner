using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class RunnerController : MonoBehaviour
{
    public enum RunState { Idle, Running, Jumping, Crouched, Dead }

    [Header("Lanes")]
    [SerializeField] private float laneWidth = 2.0f;    // positions: -laneWidth, 0, +laneWidth
    [SerializeField] private int startingLane = 1;      // 0=Left, 1=Center, 2=Right
    [SerializeField] private float laneChangeSpeed = 12f; // lateral tween speed

    [Header("Movement")]
    [SerializeField] private float startSpeed = 8f;
    [SerializeField] private float maxSpeed = 18f;      // used by camera FOV later
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float jumpForce = 10.5f;
    [SerializeField] private float coyoteTime = 0.12f;  // grace window after leaving ground

    [Header("Crouch / Slide")]
    [SerializeField] private float slideDuration = 0.55f;
    [SerializeField] private float crouchHeightScale = 0.5f;

    [Header("Grounding")]
    [SerializeField] private float groundedStick = -2f; // keeps controller grounded

    public RunState State { get; private set; } = RunState.Running;
    public float CurrentSpeed { get; private set; }

    // expose 0..1 speed for camera FOV
    public float Speed01 => Mathf.InverseLerp(startSpeed, maxSpeed, CurrentSpeed);

    CharacterController cc;
    int currentLane; // 0,1,2
    float verticalVel;
    float coyoteTimer;
    bool crouchHeld;
    bool sliding;
    float slideTimer;
    float baseHeight;
    Vector3 baseCenter;

#if ENABLE_INPUT_SYSTEM
    // Optional: hook these via PlayerInput if you like
    public void OnMoveLeft(InputAction.CallbackContext ctx)  { if (ctx.performed) TryLane(-1); }
    public void OnMoveRight(InputAction.CallbackContext ctx) { if (ctx.performed) TryLane(+1); }
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) TryJump();
    }
    public void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (ctx.started) StartCrouchOrSlide();
        if (ctx.canceled) EndCrouch();
    }
#endif

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        baseHeight = cc.height;
        baseCenter = cc.center;
        currentLane = Mathf.Clamp(startingLane, 0, 2);
        CurrentSpeed = startSpeed;
    }

    void Update()
    {
        if (State == RunState.Dead) return;


        // Coyote timer
        if (cc.isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // Slide timing (short-press slide, or hold to stay crouched)
        if (sliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f && !crouchHeld) EndCrouch();
        }

        // Vertical motion
        if (cc.isGrounded && verticalVel < 0f)
            verticalVel = groundedStick; // small downward force to stick to ground
        else
            verticalVel += gravity * Time.deltaTime;

        // Lateral lane target (X world position)
        float targetX = (currentLane - 1) * laneWidth; // lanes: 0->-laneWidth, 1->0, 2->+laneWidth
        Vector3 pos = transform.position;
        float newX = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * Time.deltaTime);

        // Forward speed is constant here; later TrackManager can set CurrentSpeed externally
        CurrentSpeed = Mathf.Clamp(CurrentSpeed, startSpeed, maxSpeed);

        // Compose motion
        Vector3 move = new Vector3(newX - pos.x, 0f, CurrentSpeed * Time.deltaTime);
        move.y = verticalVel * Time.deltaTime;

        // CharacterController.Move expects units this frame, not velocity
        cc.Move(new Vector3(move.x, move.y, CurrentSpeed * Time.deltaTime));

        // Update state
        if (cc.isGrounded && State != RunState.Crouched)
            State = RunState.Running;
    }

    void TryLane(int dir)
    {
        if (State == RunState.Dead) return;
        currentLane = Mathf.Clamp(currentLane + dir, 0, 2);
    }

    void TryJump()
    {
        if (State == RunState.Dead) return;
        if (State == RunState.Crouched) return; // no jump while crouched
        if (coyoteTimer > 0f)
        {
            verticalVel = jumpForce;
            State = RunState.Jumping;
            coyoteTimer = 0f;
        }
    }

    void StartCrouchOrSlide()
    {
        if (State == RunState.Dead) return;
        crouchHeld = true;

        if (!sliding) // apply once
        {
            // shrink controller
            cc.height = baseHeight * crouchHeightScale;
            cc.center = new Vector3(baseCenter.x, baseCenter.y * crouchHeightScale, baseCenter.z);
            sliding = true;
            slideTimer = slideDuration;
        }
        State = RunState.Crouched;
    }

    void EndCrouch()
    {
        crouchHeld = false;
        if (!sliding) return;

        // restore controller
        cc.height = baseHeight;
        cc.center = baseCenter;
        sliding = false;

        // return to running state if grounded
        if (cc.isGrounded) State = RunState.Running;
    }

    // Utility for other systems (e.g., TrackManager) to push speed
    public void SetSpeed(float newSpeed)
    {
        CurrentSpeed = Mathf.Clamp(newSpeed, 0f, maxSpeed);
    }

    // Simple kill for testing collisions later
    public void Die()
    {
        State = RunState.Dead;
        CurrentSpeed = 0f;
    }
}