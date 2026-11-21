using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public sealed class MuseumControllerTutorial : MonoBehaviour
{
    private const int TurnStep = 5;

    private static readonly string[] Titles =
    {
        "Left Grip", "Right Grip", "Left Trigger", "Right Trigger", "Teleport", "Turn"
    };

    private static readonly string[] Instructions =
    {
        "Press the highlighted side button\nwith your left middle finger.",
        "Press the highlighted side button\nwith your right middle finger.",
        "Squeeze the highlighted trigger\nwith your left index finger.",
        "Squeeze the highlighted trigger\nwith your right index finger.",
        "1. Hold the right stick forward.\n2. Aim the arc at the floor.\n3. Release the stick to teleport.",
        "Use the right thumbstick.\nPush left to turn left.\nPush right to turn right."
    };

    private static readonly string[] Prompts =
    {
        "Press Left Grip to continue", "Press Right Grip to continue",
        "Press Left Trigger to continue", "Press Right Trigger to continue",
        "Teleport once",
        "Turn left and right to finish"
    };

    [SerializeField] private Text progress;
    [SerializeField] private Text heading;
    [SerializeField] private Text instructions;
    [SerializeField] private Text prompt;
    [SerializeField] private Image diagram;
    [SerializeField] private Sprite[] illustrations;
    [SerializeField] private Button skip;
    [SerializeField] private XRRayInteractor leftPointer;
    [SerializeField] private XRRayInteractor rightPointer;
    [SerializeField] private XRRayInteractor rightTeleportRay;
    [SerializeField] private TeleportationArea practiceFloor;
    [SerializeField] private ActionBasedSnapTurnProvider turnProvider;

    private InputActionMap buttons;
    private InputAction[] stepInputs;
    private TeleportationProvider provider;
    private int step;
    private int advanceFrame = -1;
    private bool readyForPress;
    private bool awaitingLanding;
    private Vector3 turnStartForward;
    private float rightTurnDirection;
    private bool turnedLeft;
    private bool turnedRight;

    public event System.Action Completed;
    internal Font PromptFont => prompt.font;
    private int StepCount => turnProvider != null ? Titles.Length : TurnStep;

    private void Awake()
    {
        provider = practiceFloor.teleportationProvider;
        buttons = new InputActionMap("Museum tutorial");
        stepInputs = new[]
        {
            buttons.AddAction("Left Grip", InputActionType.Button, "<XRController>{LeftHand}/{GripButton}"),
            buttons.AddAction("Right Grip", InputActionType.Button, "<XRController>{RightHand}/{GripButton}"),
            buttons.AddAction("Left Trigger", InputActionType.Button, "<XRController>{LeftHand}/{TriggerButton}"),
            buttons.AddAction("Right Trigger", InputActionType.Button, "<XRController>{RightHand}/{TriggerButton}")
        };
    }

    private void OnEnable()
    {
        if (step == StepCount)
            step = 0;

        skip.onClick.AddListener(Advance);
        practiceFloor.teleporting.AddListener(OnTeleportQueued);
        provider.endLocomotion += OnTeleportFinished;
        if (turnProvider != null)
        {
            turnProvider.beginLocomotion += OnTurnStarted;
            turnProvider.endLocomotion += OnTurnFinished;
        }
        buttons.Enable();
        ShowStep();
    }

    private void OnDisable()
    {
        buttons.Disable();
        skip.onClick.RemoveListener(Advance);
        practiceFloor.teleporting.RemoveListener(OnTeleportQueued);
        provider.endLocomotion -= OnTeleportFinished;
        if (turnProvider != null)
        {
            turnProvider.beginLocomotion -= OnTurnStarted;
            turnProvider.endLocomotion -= OnTurnFinished;
        }
        awaitingLanding = false;
    }

    private void OnDestroy() => buttons.Dispose();

    public void RestartTutorial()
    {
        step = 0;
        awaitingLanding = false;
        advanceFrame = Time.frameCount;
        if (gameObject.activeSelf)
            ShowStep();
        else
            gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (step >= stepInputs.Length || advanceFrame == Time.frameCount)
            return;

        var input = stepInputs[step];
        if (!readyForPress)
        {
            readyForPress = !input.IsPressed();
            return;
        }

        if (!input.WasPressedThisFrame())
            return;

        // A trigger used to click a UI button should not also advance the lesson.
        if (step >= 2 && IsPointingAtButton(step == 2 ? leftPointer : rightPointer))
            return;

        Advance();
    }

    private static bool IsPointingAtButton(XRRayInteractor pointer) =>
        pointer.isActiveAndEnabled &&
        pointer.TryGetCurrentUIRaycastResult(out var result) &&
        result.gameObject.GetComponentInParent<Button>() != null;

    private void OnTeleportQueued(TeleportingEventArgs args)
    {
        if (step == stepInputs.Length && ReferenceEquals(args.interactorObject, rightTeleportRay))
            awaitingLanding = true;
    }

    private void OnTeleportFinished(LocomotionSystem system)
    {
        if (awaitingLanding)
            Advance();
    }

    private void OnTurnStarted(LocomotionSystem system)
    {
        if (step != TurnStep || system.xrOrigin == null)
            return;

        var stick = turnProvider.rightHandSnapTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        if (Mathf.Abs(stick.x) <= Mathf.Abs(stick.y))
            return;

        rightTurnDirection = Mathf.Sign(stick.x);
        turnStartForward = system.xrOrigin.Origin.transform.forward;
    }

    private void OnTurnFinished(LocomotionSystem system)
    {
        if (step != TurnStep || turnStartForward == Vector3.zero || system.xrOrigin == null)
            return;

        var origin = system.xrOrigin.Origin.transform;
        float angle = Vector3.SignedAngle(turnStartForward, origin.forward, origin.up);
        turnStartForward = Vector3.zero;
        if (angle * rightTurnDirection < 0.1f)
            return;

        turnedLeft |= angle < 0f;
        turnedRight |= angle > 0f;
        if (turnedLeft && turnedRight)
            Advance();
        else
            prompt.text = turnedLeft ? "Turn right to finish" : "Turn left to finish";
    }

    private void Advance()
    {
        if (advanceFrame == Time.frameCount)
            return;

        advanceFrame = Time.frameCount;
        step++;
        awaitingLanding = false;
        if (step == StepCount)
        {
            gameObject.SetActive(false);
            Completed?.Invoke();
        }
        else
            ShowStep();
    }

    private void ShowStep()
    {
        progress.text = $"Controller Tutorial    {step + 1} / {StepCount}";
        heading.text = Titles[step];
        instructions.text = Instructions[step];
        prompt.text = Prompts[step];
        diagram.sprite = illustrations[step];
        readyForPress = false;
        turnStartForward = Vector3.zero;
        turnedLeft = false;
        turnedRight = false;
    }
}
