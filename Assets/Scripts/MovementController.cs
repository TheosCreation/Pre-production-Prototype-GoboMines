using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class MovementController : NetworkBehaviour
{ 
    // -------------------- Movement Specifics --------------------
    [Header("Movement specifics")]
    [SerializeField] LayerMask groundMask;
    public float movementSpeed = 14f;
    [Range(0f, 1f)] public float crouchSpeedMultiplier = 0.248f;
    [Range(0.01f, 0.99f)] public float movementThrashold = 0.01f;
    [Space(2)]

    // -------------------- Jump Specifics --------------------
    [Header("Jump specifics")]
    public float jumpVelocity = 20f;
    private float coyoteJumpTime = 1f; // not used
    public float jumpTime = 0.3f;
    [Range(0f, 1f)] public float frictionAgainstFloor = 0.3f;
    [Range(0f, 0.99f)] public float frictionAgainstWall = 0.839f;
    [Space(2)]

    public float acceleration = 0.2f;
    public float airAcceleration = 0.2f;
    public float deceleration = 0.1f;
    public float airDeceleration = 0.1f;

    // -------------------- Grounded Specifics --------------------
    [Header("Grounded specifics")]
    public float groundCheckerThrashold = 0.1f;
    public float maxGroundDistance = 0.1f;
    public float groundStickSmooth = 2f; 
    [Space(2)]

    // -------------------- Falling Specifics --------------------
    [Header("Falling specifics")]
    public float fallDistanceDamageThreshold = 2f;
    public float minimumFallDistanceToLandEvent = 0.5f;
    public float minimumHorizontalSpeedToFastEvent;

    // -------------------- Slope Specifics --------------------
    [Header("Slope specifics")]
    public float slopeCheckerThrashold = 0.51f;
    [Range(1f, 89f)] public float maxClimbableSlopeAngle = 53.6f;
    [Space(2)]

    // -------------------- Step up Specifics --------------------
    [Header("Step up specifics")]
    public float stepCheckerThrashold = 0.6f;
    public float maxStepHeight = 0.74f;
    public float stepUpSmooth = 1f;
    [Space(2)]

    [Header("Step down specifics")]
    public float stepDownMaxGroundDistance = 1f;
    public float stepDownSmooth = 1f;
    public float stepdownCheckerThrashold = 0.6f;
    public float heightDiffrenceThreshold = 0.05f;
    [Space(2)]

    [Header("Step and ground setup")]
    [SerializeField] private Transform stepLowerTransform;
    [SerializeField] private Transform stepDownTransform;
    [SerializeField] private Transform groundCheckTransform;
    [Space(2)]

    // -------------------- Angle and Multiplier Specifics --------------------
    public AnimationCurve speedMultiplierOnAngle = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Range(0.01f, 1f)] public float canSlideMultiplierCurve = 0.061f;
    [Range(0.01f, 1f)] public float cantSlideMultiplierCurve = 0.039f;
    [Range(0.01f, 1f)] public float climbingStairsMultiplierCurve = 0.637f;
    [Space(2)]

    public float gravityMultiplier = 6f;
    public float gravityMultiplyerOnSlideChange = 3f;
    public float gravityMultiplierIfUnclimbableSlope = 30f;
    [Space(2)]

    public bool lockOnSlope = false;

    // -------------------- Wall Slide/Jump Specifics --------------------
    [Header("Wall slide/jump specifics")]
    public float wallCheckerThrashold = 0.8f;
    public float hightWallCheckerChecker = 0.5f;
    public float jumpFromWallMultiplier = 30f;

    // -------------------- Sprint and Crouch Specifics --------------------
    [Header("Sprint and crouch specifics")]
    public float sprintSpeed = 20f;
    public Animator animator;

    // -------------------- State and Debug Variables --------------------
    public bool debug = true;
    public bool isGrounded = false;
    public bool isTouchingSlope = false;
    public bool isTouchingStep = false;
    public bool isTouchingStepDown = false;
    public bool isTouchingWall = false;
    public bool isMoving = false;
    public bool isJumping = false;
    public bool isCrouch = false;
    public bool isSprinting = false;
    public bool isFalling = false;
    public string groundSurfaceType = "";
    public bool applyingStepAdjust = false;
    public bool applyingStepdownAdjust = false;
    public bool applyingGroundAdjust = false;


    // -------------------- Audio Specifics --------------------
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip[] jumpSounds;
    [SerializeField] private AudioClip landSound;
    [Space(2)]

    private Vector3 forward;
    private Vector3 globalForward;
    private Vector3 reactionForward;
    private Vector3 down;
    private Vector3 globalDown;
    private Vector3 reactionGlobalDown;
    private Vector3 wallNormal;
    private Vector3 groundNormal;
    private Vector3 prevGroundNormal;
    private Vector3 currVelocity = Vector3.zero;

    private bool prevGrounded;
    private bool currentLockOnSlope;

    private Vector2 axisInput;

    private float currentSurfaceAngle;
    private float originalColliderHeight;
    private float height1 = 0f; // for falling
    private float height2 = 0f; // for falling

    private Timer jumpTimer;
    private Vector3 stepHit;
    private Vector3 stepdownHit;
    private Vector3 groundHitPosition;

    private Rigidbody m_rigidbody;
    private CapsuleCollider m_collider;

    private void Awake()
    {
        m_rigidbody = this.GetComponent<Rigidbody>();
        m_collider = this.GetComponent<CapsuleCollider>();
        originalColliderHeight = m_collider.height;

        jumpTimer = gameObject.AddComponent<Timer>();

        SetFriction(frictionAgainstFloor, true);
        currentLockOnSlope = lockOnSlope;

        //InputManager.Instance.playerInput.InGame.Jump.started += OnJump;
    }

    //public override void OnDestroy()
    //{
    //    base.OnDestroy();
    //
    //    InputManager.Instance.playerInput.InGame.Jump.started -= OnJump;
    //}

    private void FixedUpdate()
    {
        if(!IsOwner) return;

        //local vectors
        CheckGrounded();
        CheckStep();
        CheckWall();
        CheckSlopeAndDirections();
        CheckFalling();

        //movement
        MoveWalk();

        if (!isMoving)
        {
            ApplyDeceleration();
        }

        //gravity
        ApplyGravity();
        ApplyGroundAdjustment();

        UpdateAnimator();
    }

    private void ApplyGroundAdjustment()
    {
        if (isJumping) return;

        // isGrounded
        if (isGrounded && !applyingStepAdjust && !applyingStepdownAdjust)
        {
            Vector3 targetPosition = new Vector3(m_rigidbody.position.x, groundHitPosition.y, m_rigidbody.position.z);
            m_rigidbody.position = Vector3.Lerp(m_rigidbody.position, targetPosition, groundStickSmooth * Time.fixedDeltaTime);
            applyingGroundAdjust = true;
            ResetVerticalVelocity();
        }
        else
        {
            applyingGroundAdjust = false;
        }

        // step up
        if (isTouchingStep && GetHorizontalVelocity().magnitude > 0.01f)
        {
            Vector3 targetPosition = new Vector3(m_rigidbody.position.x, stepHit.y, m_rigidbody.position.z);
            float smoothingFactor = isGrounded ? stepUpSmooth : stepUpSmooth * 2;

            m_rigidbody.position = Vector3.Lerp(m_rigidbody.position, targetPosition, smoothingFactor * Time.fixedDeltaTime);
            applyingStepAdjust = true;
            ResetVerticalVelocity();
        }
        else
        {
            applyingStepAdjust = false;
        }

        // step down
        if (isTouchingStepDown && stepdownHit.y < groundHitPosition.y - heightDiffrenceThreshold && !applyingStepAdjust && !applyingGroundAdjust && GetHorizontalVelocity().magnitude > 0.01f)
        {
            Vector3 targetPosition = new Vector3(m_rigidbody.position.x, stepdownHit.y, m_rigidbody.position.z);

            m_rigidbody.position = Vector3.Lerp(m_rigidbody.position, targetPosition, stepDownSmooth * Time.fixedDeltaTime);
            applyingStepdownAdjust = true;
        }
        else
        {
            applyingStepdownAdjust = false;
        }
    }
    private void ApplyDeceleration()
    {
        Vector3 currentVelocity = m_rigidbody.linearVelocity;

        // Calculate deceleration based on grounded or air state
        float currentDeceleration = isGrounded ? deceleration : airDeceleration;

        // Gradually reduce horizontal velocity to zero
        Vector3 newVelocity = Vector3.MoveTowards(
            new Vector3(currentVelocity.x, 0f, currentVelocity.z), // Current horizontal velocity
            Vector3.zero,                                         // Target horizontal velocity
            currentDeceleration * Time.fixedDeltaTime                 // Deceleration amount
        );

        // Combine the adjusted horizontal velocity with the current vertical velocity
        m_rigidbody.linearVelocity = new Vector3(newVelocity.x, currentVelocity.y, newVelocity.z);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;

        if (isJumping) return;

        if (isGrounded)
        {
            ResetVerticalVelocity();

            m_rigidbody.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
            isJumping = true;

            jumpTimer.SetTimer(jumpTime, JumpEnd);

            // play sound
        }
        else if (isTouchingWall)
        {
            ResetVerticalVelocity();

            m_rigidbody.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
            m_rigidbody.AddForce(wallNormal * jumpVelocity * jumpFromWallMultiplier, ForceMode.VelocityChange);
            isJumping = true;

            jumpTimer.SetTimer(jumpTime, JumpEnd);
        }
    }

    private void JumpEnd()
    {
        isJumping = false;

        height1 = transform.position.y;
    }

    private void CheckFalling()
    {
        if (prevGrounded && !isGrounded)
        {
            height1 = transform.position.y;
        }
        else if (!prevGrounded && isGrounded)
        {
            height2 = transform.position.y;
            if (height1 - height2 > fallDistanceDamageThreshold)
            {
                // apply fall damage
            }

            if (height1 - height2 > minimumFallDistanceToLandEvent)
            {
                //fall damage
            }

            ResetVerticalVelocity();
            isFalling = false;
        }
        else if (m_rigidbody.linearVelocity.y < 0)
        {
            isFalling = true;
        }
    }

    #region Checks
    private void CheckGrounded()
    {
        prevGrounded = isGrounded;
        bool tempGrounded = false;
        // Define the positions of the corners relative to groundCheckPosition.position
        Vector3[] cornerOffsets = new Vector3[]
        {
            new Vector3(-groundCheckerThrashold/2, 0, 0), // Left
            new Vector3(groundCheckerThrashold/2, 0, 0),  // Right
            new Vector3(0, 0, groundCheckerThrashold/2),  // Front
            new Vector3(0, 0, -groundCheckerThrashold/2)  // Back
        };

        float highestY = -100000f;
        // Perform raycasts from each corner
        foreach (Vector3 offset in cornerOffsets)
        {
            RaycastHit groundHit;
            if (Physics.Raycast(groundCheckTransform.position + offset, Vector3.down, out groundHit, maxGroundDistance, groundMask))
            {
                if (groundHit.point.y > highestY)
                {
                    highestY = groundHit.point.y;
                    groundHitPosition = groundHit.point;
                    groundSurfaceType = groundHit.collider.tag;
                }
                tempGrounded = true;
            }
        }

        isGrounded = tempGrounded;
    }

    private void CheckStep()
    {
        bool tmpStep = false;
        bool tmpStepDown = false;

        RaycastHit stepLowerHit;
        if (Physics.Raycast(stepLowerTransform.position, globalForward, out stepLowerHit, stepCheckerThrashold, groundMask))
        {
            RaycastHit stepUpperHit;
            if (RoundValue(stepLowerHit.normal.y) == 0 && !Physics.Raycast(stepLowerTransform.position + new Vector3(0f, maxStepHeight, 0f), globalForward, out stepUpperHit, stepCheckerThrashold + 0.05f, groundMask))
            {
                //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                tmpStep = true;
                stepHit = stepLowerHit.point;
            }
        }

        RaycastHit stepLowerHit45;
        if (Physics.Raycast(stepLowerTransform.position, Quaternion.AngleAxis(45, transform.up) * globalForward, out stepLowerHit45, stepCheckerThrashold, groundMask))
        {
            RaycastHit stepUpperHit45;
            if (RoundValue(stepLowerHit45.normal.y) == 0 && !Physics.Raycast(stepLowerTransform.position + new Vector3(0f, maxStepHeight, 0f), Quaternion.AngleAxis(45, Vector3.up) * globalForward, out stepUpperHit45, stepCheckerThrashold + 0.05f, groundMask))
            {
                //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                tmpStep = true;
                stepHit = stepLowerHit45.point;
            }
        }

        RaycastHit stepLowerHitMinus45;
        if (Physics.Raycast(stepLowerTransform.position, Quaternion.AngleAxis(-45, transform.up) * globalForward, out stepLowerHitMinus45, stepCheckerThrashold, groundMask))
        {
            RaycastHit stepUpperHitMinus45;
            if (RoundValue(stepLowerHitMinus45.normal.y) == 0 && !Physics.Raycast(stepLowerTransform.position + new Vector3(0f, maxStepHeight, 0f), Quaternion.AngleAxis(-45, Vector3.up) * globalForward, out stepUpperHitMinus45, stepCheckerThrashold + 0.05f, groundMask))
            {
                //rigidbody.position -= new Vector3(0f, -stepSmooth, 0f);
                tmpStep = true;
                stepHit = stepLowerHitMinus45.point;
            }
        }

        isTouchingStep = tmpStep;

        RaycastHit stepDownHit;
        if (!isTouchingStep && Physics.Raycast(stepDownTransform.position + (globalForward * stepdownCheckerThrashold), Vector3.down, out stepDownHit, stepDownMaxGroundDistance, groundMask))
        {
            if (stepDownHit.normal.y > 0.5f)  // Ensure it's a flat surface
            {
                tmpStepDown = true;
                stepdownHit = stepDownHit.point;
            }
        }

        isTouchingStepDown = tmpStepDown;
    }


    private void CheckWall()
    {
        bool tmpWall = false;
        Vector3 tmpWallNormal = Vector3.zero;
        Vector3 topWallPos = new Vector3(transform.position.x, transform.position.y + hightWallCheckerChecker, transform.position.z);

        RaycastHit wallHit;
        if (Physics.Raycast(topWallPos, globalForward, out wallHit, wallCheckerThrashold, groundMask))
        {
            tmpWallNormal = wallHit.normal;
            tmpWall = true;
        }
        else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(45, transform.up) * globalForward, out wallHit, wallCheckerThrashold, groundMask))
        {
            tmpWallNormal = wallHit.normal;
            tmpWall = true;
        }
        else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(90, transform.up) * globalForward, out wallHit, wallCheckerThrashold, groundMask))
        {
            tmpWallNormal = wallHit.normal;
            tmpWall = true;
        }
        else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(135, transform.up) * globalForward, out wallHit, wallCheckerThrashold, groundMask))
        {
            tmpWallNormal = wallHit.normal;
            tmpWall = true;
        }
        else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(180, transform.up) * globalForward, out wallHit, wallCheckerThrashold, groundMask))
        {
            tmpWallNormal = wallHit.normal;
            tmpWall = true;
        }
        else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(225, transform.up) * globalForward, out wallHit, wallCheckerThrashold, groundMask))
        {
            tmpWallNormal = wallHit.normal;
            tmpWall = true;
        }
        else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(270, transform.up) * globalForward, out wallHit, wallCheckerThrashold, groundMask))
        {
            tmpWallNormal = wallHit.normal;
            tmpWall = true;
        }
        else if (Physics.Raycast(topWallPos, Quaternion.AngleAxis(315, transform.up) * globalForward, out wallHit, wallCheckerThrashold, groundMask))
        {
            tmpWallNormal = wallHit.normal;
            tmpWall = true;
        }

        isTouchingWall = tmpWall;
        wallNormal = tmpWallNormal;
    }

    private Vector3 GetHorizontalVelocity()
    {
        return new Vector3(m_rigidbody.linearVelocity.x, 0, m_rigidbody.linearVelocity.z);
    }
  
    private float GetVerticalVelocity()
    {
        return m_rigidbody.linearVelocity.y;
    }

    private void CheckSlopeAndDirections()
    {
        prevGroundNormal = groundNormal;
        Vector3 horiVelocity = GetHorizontalVelocity();
        if (horiVelocity.magnitude > 0.01f)
        {
            globalForward = horiVelocity.normalized;
        }

        RaycastHit slopeHit;
        if (Physics.SphereCast(groundCheckTransform.position, slopeCheckerThrashold, Vector3.down, out slopeHit, maxGroundDistance, groundMask))
        {
            groundNormal = slopeHit.normal;

            if (slopeHit.normal.y == 1 || isJumping)
            {

                forward = transform.forward;
                reactionForward = forward;

                SetFriction(frictionAgainstFloor, true);
                currentLockOnSlope = lockOnSlope;

                currentSurfaceAngle = 0f;
                isTouchingSlope = false;

            }
            else
            {
                //set forward
                Vector3 tmpForward = new Vector3(globalForward.x, Vector3.ProjectOnPlane(transform.forward.normalized, slopeHit.normal).normalized.y, globalForward.z);
                Vector3 tmpReactionForward = new Vector3(tmpForward.x, globalForward.y - tmpForward.y, tmpForward.z);

                if (currentSurfaceAngle <= maxClimbableSlopeAngle && !isTouchingStep)
                {
                    //set forward
                    forward = tmpForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * canSlideMultiplierCurve) + 1f);
                    reactionForward = tmpReactionForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * canSlideMultiplierCurve) + 1f);

                    SetFriction(frictionAgainstFloor, true);
                    currentLockOnSlope = lockOnSlope;
                }
                else if (isTouchingStep)
                {
                    //set forward
                    forward = tmpForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * climbingStairsMultiplierCurve) + 1f);
                    reactionForward = tmpReactionForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * climbingStairsMultiplierCurve) + 1f);

                    SetFriction(frictionAgainstFloor, true);
                    currentLockOnSlope = true;
                }
                else
                {
                    //set forward
                    forward = tmpForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * cantSlideMultiplierCurve) + 1f);
                    reactionForward = tmpReactionForward * ((speedMultiplierOnAngle.Evaluate(currentSurfaceAngle / 90f) * cantSlideMultiplierCurve) + 1f);

                    SetFriction(0f, true);
                    currentLockOnSlope = lockOnSlope;
                }

                currentSurfaceAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                isTouchingSlope = true;
            }

            //set down
            down = Vector3.Project(Vector3.down, slopeHit.normal);
            globalDown = Vector3.down.normalized;
            reactionGlobalDown = Vector3.up.normalized;
        }
        else
        {
            groundNormal = Vector3.zero;

            forward = Vector3.ProjectOnPlane(transform.forward, slopeHit.normal).normalized;
            reactionForward = forward;

            //set down
            down = Vector3.down.normalized;
            globalDown = Vector3.down.normalized;
            reactionGlobalDown = Vector3.up.normalized;

            SetFriction(frictionAgainstFloor, true);
            currentLockOnSlope = lockOnSlope;
        }
    }

    #endregion

    private void ResetVerticalVelocity()
    {
        m_rigidbody.linearVelocity = new Vector3(m_rigidbody.linearVelocity.x, 0, m_rigidbody.linearVelocity.z);
    }

    #region Move

    private void MoveCrouch()
    {
        //if (crouch && isGrounded)
        //{
        //    isCrouch = true;
        //    if (meshCharacterCrouch != null && meshCharacter != null) meshCharacter.SetActive(false);
        //    if (meshCharacterCrouch != null) meshCharacterCrouch.SetActive(true);
        //
        //    float newHeight = originalColliderHeight * crouchHeightMultiplier;
        //    collider.height = newHeight;
        //    collider.center = new Vector3(0f, -newHeight * crouchHeightMultiplier, 0f);
        //
        //    headPoint.position = new Vector3(transform.position.x + POV_crouchHeadHeight.x, transform.position.y + POV_crouchHeadHeight.y, transform.position.z + POV_crouchHeadHeight.z);
        //}
        //else
        //{
        //    isCrouch = false;
        //    if (meshCharacterCrouch != null && meshCharacter != null) meshCharacter.SetActive(true);
        //    if (meshCharacterCrouch != null) meshCharacterCrouch.SetActive(false);
        //
        //    collider.height = originalColliderHeight;
        //    collider.center = Vector3.zero;
        //
        //    headPoint.position = new Vector3(transform.position.x + POV_normalHeadHeight.x, transform.position.y + POV_normalHeadHeight.y, transform.position.z + POV_normalHeadHeight.z);
        //}
    }

    private void MoveWalk()
    {
        float crouchMultiplier = isCrouch ? crouchSpeedMultiplier : 1f;
        Vector3 currentVelocity = m_rigidbody.linearVelocity;

        axisInput = InputManager.Instance.MovementVector;

        if (axisInput.magnitude > movementThrashold)
        {
            // Calculate movement direction
            Vector3 forwardMovement = transform.forward * axisInput.y; // Forward/backward
            Vector3 rightMovement = transform.right * axisInput.x; // Sideways (strafe)
            Vector3 direction = (forwardMovement + rightMovement).normalized;

            // Calculate target speed and velocity
            float speed = isSprinting ? sprintSpeed : movementSpeed;
            Vector3 targetVelocity = direction * speed * crouchMultiplier;

            // Apply acceleration-based velocity adjustment
            float currentAcceleration = isGrounded ? acceleration : airAcceleration;

            // Gradually move towards target velocity on horizontal axes
            Vector3 newVelocity = Vector3.MoveTowards(
                new Vector3(currentVelocity.x, 0f, currentVelocity.z), // Current horizontal velocity
                new Vector3(targetVelocity.x, 0f, targetVelocity.z),   // Target horizontal velocity
                currentAcceleration * Time.fixedDeltaTime                 // Adjust by acceleration
            );

            // Combine with vertical velocity
            m_rigidbody.linearVelocity = new Vector3(newVelocity.x, currentVelocity.y, newVelocity.z);

            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    #endregion

    #region Gravity

    private void ApplyGravity()
    {
        if (isGrounded) return;

        Vector3 gravity = Vector3.zero;

        //if (currentLockOnSlope || isTouchingStep) gravity = coyoteJumpMultiplier * gravityMultiplier * -Physics.gravity.y * down;
        //else
        gravity = globalDown * gravityMultiplier * -Physics.gravity.y;

        //avoid little jump
        if (groundNormal.y != 1 && groundNormal.y != 0 && isTouchingSlope && prevGroundNormal != groundNormal)
        {
            //Debug.Log("Added correction jump on slope");
            gravity *= gravityMultiplyerOnSlideChange;
        }

        //slide if angle too big
        if (groundNormal.y != 1 && groundNormal.y != 0 && (currentSurfaceAngle > maxClimbableSlopeAngle && !isTouchingStep))
        {
            //Debug.Log("Slope angle too high, character is sliding");
            if (currentSurfaceAngle > 0f && currentSurfaceAngle <= 30f) gravity = globalDown * gravityMultiplierIfUnclimbableSlope * -Physics.gravity.y;
            else if (currentSurfaceAngle > 30f && currentSurfaceAngle <= 89f) gravity = globalDown * gravityMultiplierIfUnclimbableSlope / 2f * -Physics.gravity.y;
        }

        //friction when touching wall
        if (isTouchingWall && m_rigidbody.linearVelocity.y < 0) gravity *= frictionAgainstWall;

        m_rigidbody.AddForce(gravity);
    }

    #endregion


    //private void FootTrace(Transform footBone, Transform rootTransform, out float yOffset, out Quaternion surfaceRotationOffset)
    //{
    //    // Start position for the sphere cast in world space
    //    Vector3 startPosition = footBone.position + Vector3.up * 0.5f;
    //
    //    RaycastHit hit;
    //    if (Physics.SphereCast(startPosition, footTraceRadius, Vector3.down, out hit, castDistance, groundMask))
    //    {
    //        // Convert the hit point and foot position to local space
    //        Vector3 localHitPoint = rootTransform.InverseTransformPoint(hit.point);
    //        Vector3 localFootPosition = rootTransform.InverseTransformPoint(footBone.position);
    //
    //        // Calculate the vertical offset in local space
    //        yOffset = localHitPoint.y - localFootPosition.y;
    //
    //        // Calculate surface rotation offset in local space
    //        surfaceRotationOffset = Quaternion.FromToRotation(Vector3.up, hit.normal);
    //
    //        // Debug Visualization
    //        Debug.DrawLine(startPosition, hit.point, Color.blue); // Ray to hit point
    //        Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.green); // Surface normal
    //        DrawDisc(hit.point, hit.normal, footTraceRadius, Color.yellow); // Visualize the contact disc
    //    }
    //    else
    //    {
    //        // No hit: Default Y-offset to zero, rotation remains unchanged
    //        yOffset = 0;
    //        surfaceRotationOffset = Quaternion.identity;
    //
    //        // Debug Visualization for missed cast
    //        Debug.DrawLine(startPosition, startPosition + Vector3.down * castDistance, Color.red);
    //    }
    //}


    /// <summary>
    /// Draws a disc at the contact point to visualize the surface.
    /// </summary>
    private void DrawDisc(Vector3 position, Vector3 normal, float radius, Color color)
    {
        const int segments = 24; // Number of segments for the disc
        float angleIncrement = 360f / segments;

        Vector3 right = Vector3.Cross(normal, Vector3.up).normalized; // Generate a perpendicular vector
        if (right == Vector3.zero) right = Vector3.Cross(normal, Vector3.forward).normalized;

        Vector3 forward = Vector3.Cross(normal, right).normalized;

        Vector3 previousPoint = position + right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleIncrement * i;
            Quaternion rotation = Quaternion.AngleAxis(angle, normal);
            Vector3 nextPoint = position + rotation * (right * radius);

            Debug.DrawLine(previousPoint, nextPoint, color);
            previousPoint = nextPoint;
        }
    }


    #region Friction and Round

    private void SetFriction(float _frictionWall, bool _isMinimum)
    {
        m_collider.material.dynamicFriction = 0.6f * _frictionWall;
        m_collider.material.staticFriction = 0.6f * _frictionWall;

        if (_isMinimum) m_collider.material.frictionCombine = PhysicsMaterialCombine.Minimum;
        else m_collider.material.frictionCombine = PhysicsMaterialCombine.Maximum;
    }


    private float RoundValue(float _value)
    {
        float unit = (float)Mathf.Round(_value);

        if (_value - unit < 0.000001f && _value - unit > -0.000001f) return unit;
        else return _value;
    }

    #endregion


    #region GettersSetters

    public float GetOriginalColliderHeight() { return originalColliderHeight; }

    #endregion

    private void UpdateAnimator()
    {
        //animator.SetBool("IsJumping", isJumping);
        //animator.SetBool("IsFalling", isFalling);
    }

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (debug)
        {
            m_rigidbody = this.GetComponent<Rigidbody>();
            m_collider = this.GetComponent<CapsuleCollider>();

            Vector3 bottomStepPos = stepLowerTransform.position;
            Vector3 topWallPos = new Vector3(transform.position.x, transform.position.y + hightWallCheckerChecker, transform.position.z);

            // Define the positions of the corners relative to groundCheckPosition.position
            Vector3[] cornerOffsets = new Vector3[]
            {
            new Vector3(-groundCheckerThrashold/2, 0, 0), // Left
            new Vector3(groundCheckerThrashold/2, 0, 0),  // Right
            new Vector3(0, 0, groundCheckerThrashold/2),  // Front
            new Vector3(0, 0, -groundCheckerThrashold/2)  // Back
            };


            // Draw lines between the corners to form the 2D box
            Gizmos.DrawLine(groundCheckTransform.position + cornerOffsets[0], groundCheckTransform.position + cornerOffsets[2]);
            Gizmos.DrawLine(groundCheckTransform.position + cornerOffsets[2], groundCheckTransform.position + cornerOffsets[1]);
            Gizmos.DrawLine(groundCheckTransform.position + cornerOffsets[1], groundCheckTransform.position + cornerOffsets[3]);
            Gizmos.DrawLine(groundCheckTransform.position + cornerOffsets[3], groundCheckTransform.position + cornerOffsets[0]);

            // Draw the ground check rays
            Gizmos.color = Color.blue;
            foreach (Vector3 offset in cornerOffsets)
            {
                Gizmos.DrawRay(groundCheckTransform.position + offset, Vector3.down * maxGroundDistance);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheckTransform.position, slopeCheckerThrashold);

            //direction
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + forward * 2f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + globalForward * 2);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + reactionForward * 2f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + down * 2f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + globalDown * 2f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + reactionGlobalDown * 2f);

            //step check
            Gizmos.color = Color.black;
            Gizmos.DrawLine(bottomStepPos, bottomStepPos + globalForward * stepCheckerThrashold);

            Gizmos.color = Color.black;
            Gizmos.DrawLine(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), bottomStepPos + new Vector3(0f, maxStepHeight, 0f) + globalForward * (stepCheckerThrashold + 0.05f));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(bottomStepPos, bottomStepPos + Quaternion.AngleAxis(45, transform.up) * (globalForward * stepCheckerThrashold));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), bottomStepPos + Quaternion.AngleAxis(45, Vector3.up) * (globalForward * stepCheckerThrashold) + new Vector3(0f, maxStepHeight, 0f));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(bottomStepPos, bottomStepPos + Quaternion.AngleAxis(-45, transform.up) * (globalForward * stepCheckerThrashold));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(bottomStepPos + new Vector3(0f, maxStepHeight, 0f), bottomStepPos + Quaternion.AngleAxis(-45, Vector3.up) * (globalForward * stepCheckerThrashold) + new Vector3(0f, maxStepHeight, 0f));

            Gizmos.color = Color.blue;
            Vector3 startPos = stepDownTransform.position + (globalForward * stepdownCheckerThrashold);
            Gizmos.DrawLine(startPos, startPos + (Vector3.down * stepDownMaxGroundDistance));

            //wall check
            Gizmos.color = Color.black;
            Gizmos.DrawLine(topWallPos, topWallPos + globalForward * wallCheckerThrashold);

            Gizmos.color = Color.black;
            Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(45, transform.up) * (globalForward * wallCheckerThrashold));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(90, transform.up) * (globalForward * wallCheckerThrashold));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(135, transform.up) * (globalForward * wallCheckerThrashold));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(180, transform.up) * (globalForward * wallCheckerThrashold));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(225, transform.up) * (globalForward * wallCheckerThrashold));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(270, transform.up) * (globalForward * wallCheckerThrashold));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(topWallPos, topWallPos + Quaternion.AngleAxis(315, transform.up) * (globalForward * wallCheckerThrashold));
        }
    }

    #endregion
}