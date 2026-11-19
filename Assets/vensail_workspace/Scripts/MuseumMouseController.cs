using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public sealed class MuseumMouseController : XRBaseController
{
    protected override void UpdateInput(XRControllerState controllerState)
    {
        var mouse = Mouse.current;
        bool held = Application.isFocused && mouse != null && mouse.leftButton.isPressed;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool select = held && (controllerState.selectInteractionState.active ||
                              (mouse.leftButton.wasPressedThisFrame && !overUI));
        controllerState.selectInteractionState.SetFrameState(select);
    }
}
