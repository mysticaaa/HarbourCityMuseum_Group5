using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace ZxhMuseum
{
    // Uses the same action as the XR selection, without synthesizing XR grab events.
    public sealed class DesktopObjectClick : MonoBehaviour
    {
        public Action Selected;
        private static int handledFrame = -1;
        private void Update()
        {
            if(WorldPanel.HeadsetTracked || Mouse.current==null || Camera.main==null || handledFrame==Time.frameCount)return;
            if(!Mouse.current.leftButton.wasPressedThisFrame)return;
            if(EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject())return;
            Ray ray=Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if(Physics.Raycast(ray,out RaycastHit hit,30) && hit.collider.GetComponentInParent<DesktopObjectClick>()==this)
            {handledFrame=Time.frameCount;Selected?.Invoke();}
        }
    }
}
