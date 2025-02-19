using Unity.Netcode;
using UnityEngine;

public class BobbingAndSwayAnimation : NetworkBehaviour
{
    private PlayerController player;

    [Header("Overrall")]
    [SerializeField] private float scale = 1f;
    [SerializeField] private float smoothness = 1f;

    [Header("Weapon Sway")]
    [SerializeField] private float positionalSway = 1f;
    [SerializeField] private float maxSwayAmount = 0.2f; // Maximum sway limit

    [Header("Rotational Sway")]
    [SerializeField] private float rotationalSwayIntensity = 2f; // Intensity of rotational sway
    [SerializeField] private float maxRotationSway = 5f; // Max sway angle in degrees

    [Header("Walking Bobbing")]
    [SerializeField] private float bobbingFrequency = 0.1f;
    [SerializeField] private float bobbingFrequency2 = 0.1f;
    [SerializeField] private float verticalBobbingAmplitude = 0.05f;
    [SerializeField] private float horizontalBobbingAmplitude = 0.05f;
    [SerializeField] private float horizontalOffsetMovement = 0.05f;

    private Vector3 initialPosition = Vector3.zero;
    private Vector3 rotationalSwayOffset = Vector3.zero;
    private float bobbingTimer = 0f;
    private float bobbingTimer2 = 0f;

    private float currentVerticalOffset = 0f;
    private float currentHorizontalOffset = 0f;

    private void Awake()
    {
        player = transform.root.GetComponent<PlayerController>();
    }

    private void Start()
    {
        initialPosition = transform.localPosition;
    }

    private void Update()
    {
        if (!IsOwner) return;

        CalculateSway(); // Updates swayPositionOffset internally

        // Combine initial position with bobbing and sway offsets
        Vector3 finalPosition = initialPosition + CalculateBobbing() - CalculateSway();
        rotationalSwayOffset = CalculateRotationalSway();

        // Smoothly interpolate final adjustments and apply to transform
        transform.SetLocalPositionAndRotation(Vector3.Lerp(transform.localPosition, finalPosition, Time.deltaTime * smoothness),
                                              Quaternion.Slerp(transform.localRotation, Quaternion.Euler(rotationalSwayOffset), Time.deltaTime * smoothness));
    }


    private Vector3 CalculateSway()
    {
        float mouseX = InputManager.Instance.currentMouseDelta.x * 0.1f;
        float mouseY = InputManager.Instance.currentMouseDelta.y * 0.1f;

        Vector3 swayPositionOffset = new Vector3(mouseX, mouseY, 0) * positionalSway * scale;

        // Clamp sway position to prevent excessive movement
        swayPositionOffset.x = Mathf.Clamp(swayPositionOffset.x, -maxSwayAmount * scale, maxSwayAmount * scale);
        swayPositionOffset.y = Mathf.Clamp(swayPositionOffset.y, -maxSwayAmount * scale, maxSwayAmount * scale);

        return swayPositionOffset;
    }

    private Vector3 CalculateRotationalSway()
    {
        // Calculate rotational sway based on mouse input
        float rotX = -InputManager.Instance.currentMouseDelta.y * rotationalSwayIntensity; // Negative to tilt up/down correctly
        float rotY = InputManager.Instance.currentMouseDelta.x * rotationalSwayIntensity;

        // Clamp rotation to prevent excessive sway
        rotX = Mathf.Clamp(rotX, -maxRotationSway, maxRotationSway);
        rotY = Mathf.Clamp(rotY, -maxRotationSway, maxRotationSway);

        return new Vector3(rotX, rotY, 0f);
    }

    private Vector3 CalculateBobbing()
    {
        Vector3 movementVector = InputManager.Instance.MovementVector;

        // Set amplitude based on movement speed
        float targetVerticalAmplitude = (Mathf.Abs(movementVector.y) > 0f) ? verticalBobbingAmplitude : 0f;
        float targetHorizontalAmplitude = (Mathf.Abs(movementVector.x) > 0f) ? horizontalBobbingAmplitude : 0f;

        // Increment the bobbing timer at a rate determined by bobbingFrequency
        bobbingTimer += Time.deltaTime * bobbingFrequency;

        // Increment the timer for horizontal bobbing
        bobbingTimer2 += Time.deltaTime * bobbingFrequency2;

        // Calculate the vertical bobbing offset
        currentVerticalOffset = Mathf.Sin(bobbingTimer) * targetVerticalAmplitude;

        // Calculate the horizontal bobbing offset
        currentHorizontalOffset = Mathf.Cos(bobbingTimer2) * targetHorizontalAmplitude;

        // Add a slight hard-set offset based on movement direction (left or right)
        float hardSetOffset = movementVector.x != 0 ? Mathf.Sign(movementVector.x) * horizontalOffsetMovement * 0.5f : 0f;

        // Combine bobbing offset with hard-set offset
        float combinedHorizontalOffset = (currentHorizontalOffset + hardSetOffset) * scale;

        // Return the combined bobbing offset
        return new Vector3(combinedHorizontalOffset, currentVerticalOffset * scale, 0f);
    }
}