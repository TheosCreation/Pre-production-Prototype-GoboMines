using Unity.Netcode;
using UnityEngine;

public class PlayerLook : NetworkBehaviour
{
    private PlayerController player;
    private Rigidbody rb;
    [Header("Looking")]
    [SerializeField] private float maxRotZCamera = 87f;

    [Header("Scale animations")]
    [SerializeField] private float scale = 1f;

    [Header("Player Options")]
    public float lookSensitivity = 1f;
    public int tiltStatus = 1;
    public float shakeAmount = 1.0f;
    public float fov = 60f;

    [Header("Camera Tilt")]
    [SerializeField] private float tiltAmount = 2.5f;
    [SerializeField] private float tiltSmoothTime = 0.1f;

    [Header("Body Transforms")]
    [SerializeField] private Transform spine1;
    private Quaternion originalSpine1Rot = Quaternion.identity;
    [SerializeField] private Transform spine2;
    private Quaternion originalSpine2Rot = Quaternion.identity;
    [SerializeField] private Transform neckTransform;
    [SerializeField] private Transform cameraOffsetTransform;
    public Camera playerCamera;

    [Header("FOV")]
    [SerializeField] private float targetFov = 60f;

    [Header("Aiming/Zoom")]
    [SerializeField] private float zoomSmoothness = 60f;
    public float sensitivityMultiplier = 1.0f;

    private float currentXRotation = 0f;
    private float currentTilt = 0;
    private float tiltVelocity = 0;

    //[Header("Jump Animation")]
    //[SerializeField] private float jumpAnimTiltAngle = 5f; // Adjust this value for more or less tilt
    //[SerializeField] private bool playingJumpAnim = false;
    //private float jumpAnimDuration = 0.0f; // Duration of the jump effect
    //private float jumpElapsedTime = 0f;
    //[SerializeField] private AudioClip jumpClip;
    //
    //[Header("Land Animation")]
    //[SerializeField] private float landAnimDuration = 0.3f; // Duration of the land effect
    //[SerializeField] private float landAnimTiltAngle = -3f; // Tilt angle for landing
    //[SerializeField] private bool playingLandAnim = false;
    //private float landElapsedTime = 0f;

    [Header("Screen Shake")]
    private float shakeMagnitude = 0.1f;
    private float shakeEndTime = 0f;
    private Vector3 originalCameraPosition;
    private Vector3 originalCameraOffsetPosition;
    private Vector3 originalCameraOffsetRotation;
    private Vector3 originalNeckRotation;
    private Vector3 cameraTargetLocalPosition;

    private Vector2 currentRecoilOffset = Vector2.zero;
    [SerializeField] private float recoilSmoothTime = 0.1f;  // Recoil smooth damping

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.transform.localPosition;
        }
        if (playerCamera != null)
        {
            originalCameraOffsetPosition = cameraOffsetTransform.localPosition;
            originalCameraOffsetRotation = cameraOffsetTransform.localRotation.eulerAngles;
        }

        if (neckTransform != null)
        {
            originalNeckRotation = neckTransform.localRotation.eulerAngles;
        }
        if (spine1 != null)
        {
            originalSpine1Rot = spine1.localRotation;
        }
        if (spine2 != null)
        {
            originalSpine2Rot = spine2.localRotation;
        }
        ResetZoomLevel();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            playerCamera.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (neckTransform != null && playerCamera != null)
        {
            Look();
            UpdateRecoil();
            //HandleJumpAnimation();
            //HandleLandAnimation();
        }
    }

    private void Look()
    {
        Vector2 currentMouseDelta = InputManager.Instance.currentMouseDelta;

        // Calculate vertical rotation and clamp it
        currentXRotation -= (currentMouseDelta.y * lookSensitivity * sensitivityMultiplier) + currentRecoilOffset.y;
        currentXRotation = Mathf.Clamp(currentXRotation, -maxRotZCamera, maxRotZCamera);

        // Calculate horizontal rotation as a Quaternion
        Quaternion horizontalRotation = Quaternion.Euler(0f, (currentMouseDelta.x * lookSensitivity * sensitivityMultiplier) + currentRecoilOffset.x, 0f);
        //transform.rotation *= horizontalRotation;
        rb.MoveRotation(rb.rotation * horizontalRotation);
        if (tiltStatus == 1)
        {
            float targetTilt = InputManager.Instance.MovementVector.x * tiltAmount;
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, tiltSmoothTime);
        }
        else
        {
            currentTilt = 0f;
        }

        float rotationX = currentXRotation / 3;
        spine1.localRotation = originalSpine1Rot * Quaternion.Euler(rotationX, 0, 0);
        spine2.localRotation = originalSpine2Rot * Quaternion.Euler(rotationX, 0, 0);
        neckTransform.localRotation = Quaternion.Euler(rotationX, neckTransform.localRotation.y, neckTransform.localRotation.z);

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * zoomSmoothness);
        playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt);
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraTargetLocalPosition, Time.deltaTime * zoomSmoothness) + (Time.time < shakeEndTime ? Random.insideUnitSphere * shakeMagnitude : Vector3.zero);

    }

    private void UpdateRecoil()
    {
        //Weapon weapon = player.weaponHolder.currentWeapon;
        //if (weapon != null)
        //{
        //    weapon.recoil = Vector2.Lerp(weapon.recoil, Vector2.zero, Time.deltaTime / recoilSmoothTime);
        //    currentRecoilOffset = weapon.recoil;
        //}
        //else
        //{
        //    currentRecoilOffset = Vector2.Lerp(currentRecoilOffset, Vector2.zero, Time.deltaTime / recoilSmoothTime);
        //}
    }

    public void TriggerScreenShake(float duration, float magnitude)
    {
        shakeEndTime += Time.time + duration;
        shakeMagnitude += magnitude * shakeAmount * scale;
    }

    public void ResetZoomLevel()
    {
        cameraTargetLocalPosition = originalCameraPosition;
        targetFov = fov;
        sensitivityMultiplier = 1;
    }

    public void SetZoomLevel(float zoomLevel, float cameraZoomZ, float multiplier)
    {
        targetFov = fov * (2 - zoomLevel);
        cameraTargetLocalPosition = new Vector3(originalCameraPosition.x, originalCameraPosition.y, cameraZoomZ * scale);
        sensitivityMultiplier = multiplier;
    }

    //public void PlayJumpAnimation()
    //{
    //    if (!playingJumpAnim)
    //    {
    //        playingJumpAnim = true;
    //        jumpElapsedTime = 0f;
    //    }
    //}
    //
    //public void PlayLandAnimation()
    //{
    //    if (!playingLandAnim)
    //    {
    //        playingLandAnim = true;
    //        landElapsedTime = 0f;
    //    }
    //}
    //
    //private void HandleJumpAnimation()
    //{
    //    if (playingJumpAnim)
    //    {
    //        float halfDuration = jumpAnimDuration / 2f;
    //
    //        if (jumpElapsedTime < halfDuration)
    //        {
    //            // Rotate forwards (downwards tilt)
    //            float tilt = Mathf.Lerp(0f, jumpAnimTiltAngle, jumpElapsedTime / halfDuration);
    //            playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt) * Quaternion.Euler(-tilt, 0f, 0f);
    //        }
    //        else if (jumpElapsedTime < jumpAnimDuration)
    //        {
    //            // Rotate back to original
    //            float tilt = Mathf.Lerp(jumpAnimTiltAngle, 0f, (jumpElapsedTime - halfDuration) / halfDuration);
    //            playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt) * Quaternion.Euler(-tilt, 0f, 0f);
    //        }
    //        else
    //        {
    //            // Ensure the camera returns to its original rotation
    //            playingJumpAnim = false;
    //        }
    //
    //        jumpElapsedTime += Time.deltaTime;
    //    }
    //}
    //
    //private void HandleLandAnimation()
    //{
    //    if (playingLandAnim)
    //    {
    //        float halfDuration = landAnimDuration / 2f;
    //
    //        if (landElapsedTime < halfDuration)
    //        {
    //            // Rotate backwards (upwards tilt)
    //            float tilt = Mathf.Lerp(0f, landAnimTiltAngle, landElapsedTime / halfDuration);
    //            playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt) * Quaternion.Euler(-tilt, 0f, 0f);
    //        }
    //        else if (landElapsedTime < landAnimDuration)
    //        {
    //            // Rotate back to original
    //            float tilt = Mathf.Lerp(landAnimTiltAngle, 0f, (landElapsedTime - halfDuration) / halfDuration);
    //            playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt) * Quaternion.Euler(-tilt, 0f, 0f);
    //        }
    //        else
    //        {
    //            // Ensure the camera returns to its original rotation
    //            playingLandAnim = false;
    //        }
    //
    //        landElapsedTime += Time.deltaTime;
    //    }
    //}
}