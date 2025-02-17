using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    public PlayerControls Input;

    //static accessors
    [HideInInspector] public Vector2 currentMouseDelta = Vector2.zero;
    [HideInInspector] public Vector2 MovementVector;

    [Range(0.0f, 0.5f)] public float mouseSmoothTime = 0.03f;
    Vector2 currentMouseDeltaVelocity = Vector2.zero;
    private bool updateMouseDelta = true;

    protected override void Awake()
    {
        base.Awake();

        Input = new PlayerControls();
        LoadBindingOverrides();
    }

    private void FixedUpdate()
    {
        MovementVector = Input.Player.Move.ReadValue<Vector2>();
    }

    public void LoadBindingOverrides()
    {
        if (PlayerPrefs.HasKey("rebinds"))
        {
            string json = PlayerPrefs.GetString("rebinds");

            // Apply the rebinds to the action asset
            Input.asset.LoadBindingOverridesFromJson(json);
        }
    }

    private void LateUpdate()
    {
        if (!updateMouseDelta) return;

        Vector2 targetMouseDelta = Input.Player.Look.ReadValue<Vector2>();

        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseDeltaVelocity, mouseSmoothTime);

    }

    private void OnEnable()
    {
        Input.Enable();
    }

    private void OnDisable()
    {
        Input.Disable();
    }

    public void DisablePlayerInput()
    {
        updateMouseDelta = false;
        currentMouseDelta = Vector2.zero;
        Input.Player.Disable();
    }

    public void EnablePlayerInput()
    {
        updateMouseDelta = true;
        Input.Player.Enable();
    }
}
