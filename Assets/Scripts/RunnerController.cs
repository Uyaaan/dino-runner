using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // New Input System polling support
#endif

[RequireComponent(typeof(CharacterController))]
public class RunnerController : MonoBehaviour
{
    public enum RunState { Idle, Running, Jumping, Crouched, Dead }

    [Header("Lanes")]
    [SerializeField] private float laneWidth = 2.0f;      // world-space lane spacing
    [SerializeField] private int startingLane = 1;        // 0=Left, 1=Center, 2=Right
    [SerializeField] private float laneChangeSpeed = 12f; // lateral tween speed (units/second)

    [Header("Movement")]
    [SerializeField] private float startSpeed = 8f;
    [SerializeField] private float maxSpeed = 18f;
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float jumpForce = 10.5f;
    [SerializeField] private float coyoteTime = 0.12f;    // grace window after stepping off ledge

    [Header("Crouch / Slide")]
    [SerializeField] private float slideDuration = 0.55f; // short-press slide time
    [SerializeField] private float crouchHeightScale = 0.5f; // 0..1

    [Header("Grounding")]
    [SerializeField] private float groundedStick = -2f;   // small downward to stay grounded

    [Header("Run Start / Countdown")]
    [SerializeField] private bool lockInputsUntilStart = true; // if true, ignore input until BeginRun() is called
    private bool hasStarted = false;                           // set true by BeginRun() when 3-2-1 finishes

    public RunState State { get; private set; } = RunState.Idle; // start idle; movement begins after countdown
    public float CurrentSpeed { get; private set; }

    // Expose 0..1 normalized speed for camera FOV rigs
    public float Speed01 => Mathf.InverseLerp(0f, Mathf.Max(0.01f, maxSpeed), CurrentSpeed);

    private CharacterController cc;
    private int currentLane;        // 0,1,2
    private float verticalVel;      // Y velocity
    private float coyoteTimer;
    private bool crouchHeld;
    private bool sliding;
    private float slideTimer;
    private float baseHeight;
    private Vector3 baseCenter;

#if ENABLE_INPUT_SYSTEM
    // Optional callbacks if using PlayerInput component with actions bound
    public void OnMoveLeft(InputAction.CallbackContext ctx)  { if (ctx.performed) TryLane(-1); }
    public void OnMoveRight(InputAction.CallbackContext ctx) { if (ctx.performed) TryLane(+1); }
    public void OnJump(InputAction.CallbackContext ctx)      { if (ctx.performed) TryJump(); }
    public void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (ctx.started)  StartCrouchOrSlide();
        if (ctx.canceled) EndCrouch();
    }
#endif

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        baseHeight = cc.height;
        baseCenter = cc.center;
        currentLane = Mathf.Clamp(startingLane, 0, 2);
        CurrentSpeed = 0f;  // don’t move until BeginRun()
        if (!lockInputsUntilStart)
        {
            hasStarted = true;
            State = RunState.Running;
            CurrentSpeed = startSpeed;
        }
    }

    private void Update()
    {
        if (State == RunState.Dead) return;

        // If we haven’t started (waiting for 3-2-1), freeze input and forward motion
        if (!hasStarted)
        {
            // allow gravity so we stay grounded, but no lateral/forward movement
            if (cc.isGrounded && verticalVel < 0f) verticalVel = groundedStick; else verticalVel += gravity * Time.deltaTime;
            Vector3 settle = new Vector3(0f, verticalVel * Time.deltaTime, 0f);
            cc.Move(settle);
            return;
        }

        // --- INPUT: New Input System (polling) or Legacy fallback ---
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            // Lane switch
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                TryLane(-1);
            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                TryLane(+1);

            // Jump
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                TryJump();

            // Crouch/Slide (hold to crouch)
            if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
                StartCrouchOrSlide();
            if (Keyboard.current.leftCtrlKey.wasReleasedThisFrame)
                EndCrouch();
        }
#else
        // Legacy Input Manager
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) TryLane(-1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) TryLane(+1);
        if (Input.GetKeyDown(KeyCode.Space)) TryJump();
        if (Input.GetKeyDown(KeyCode.LeftControl)) StartCrouchOrSlide();
        if (Input.GetKeyUp(KeyCode.LeftControl))   EndCrouch();
#endif

        // --- COYOTE TIMER ---
        if (cc.isGrounded) coyoteTimer = coyoteTime; else coyoteTimer -= Time.deltaTime;

        // --- SLIDE TIMER ---
        if (sliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f && !crouchHeld) EndCrouch();
        }

        // --- VERTICAL MOTION ---
        if (cc.isGrounded && verticalVel < 0f)
            verticalVel = groundedStick;
        else
            verticalVel += gravity * Time.deltaTime;

        // --- LATERAL LANE TWEEN ---
        float targetX = (currentLane - 1) * laneWidth; // lanes: 0->-laneWidth, 1->0, 2->+laneWidth
        Vector3 pos = transform.position;
        float nextX = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * Time.deltaTime);

        // Clamp forward speed (TrackManager can call SetSpeed to ramp)
        CurrentSpeed = Mathf.Clamp(CurrentSpeed, startSpeed, maxSpeed);

        // Compose frame displacement; CharacterController.Move expects *distance this frame*
        Vector3 frameMove = new Vector3(nextX - pos.x, verticalVel * Time.deltaTime, CurrentSpeed * Time.deltaTime);
        cc.Move(frameMove);

        // --- STATE UPDATE ---
        if (cc.isGrounded && State != RunState.Crouched)
            State = RunState.Running;
    }

    // --- COMMANDS ---
    private void TryLane(int dir)
    {
        if (State == RunState.Dead) return;
        currentLane = Mathf.Clamp(currentLane + dir, -1, 3);
    }

    private void TryJump()
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

    private void StartCrouchOrSlide()
    {
        if (State == RunState.Dead) return;
        crouchHeld = true;

        if (!sliding)
        {
            // Shrink controller (keeps feet roughly planted by lowering center)
            cc.height = baseHeight * crouchHeightScale;
            cc.center = new Vector3(baseCenter.x, baseCenter.y * crouchHeightScale, baseCenter.z);
            sliding = true;
            slideTimer = slideDuration;
        }
        State = RunState.Crouched;
    }

    private void EndCrouch()
    {
        crouchHeld = false;
        if (!sliding) return;

        // Restore controller
        cc.height = baseHeight;
        cc.center = baseCenter;
        sliding = false;

        if (cc.isGrounded) State = RunState.Running;
    }

    // Call to start "running" after 3-2-1 countdown
    public void BeginRun()
    {
        hasStarted = true;
        State = RunState.Running;
        CurrentSpeed = Mathf.Max(CurrentSpeed, startSpeed);
    }

    // Optional: call when restarting the round or returning to title.
    public void ResetRun()
    {
        hasStarted = false;
        State = RunState.Idle;
        CurrentSpeed = 0f;
        verticalVel = 0f;
        currentLane = Mathf.Clamp(startingLane, 0, 2);
        // restore crouch state if needed
        if (sliding)
        {
            cc.height = baseHeight;
            cc.center = baseCenter;
            sliding = false;
            crouchHeld = false;
        }
    }

    // Allow TrackManager / difficulty ramp to set speed
    public void SetSpeed(float newSpeed)
    {
        CurrentSpeed = Mathf.Clamp(newSpeed, 0f, maxSpeed);
    }

    // Simple kill for testing collisions
    public void Die()
    {
        State = RunState.Dead;
        CurrentSpeed = 0f;
    }
}