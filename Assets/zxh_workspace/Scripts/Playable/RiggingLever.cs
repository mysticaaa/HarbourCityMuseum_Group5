using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    public sealed class RiggingLever : MonoBehaviour
    {
        public int Direction { get; private set; }
        public Transform Handle { get; private set; }
        private Transform pivot;
        private ShipDeckEnvironment deck;
        private XRRayInteractor holder;
        private ActionBasedController controller;
        private bool mouseHeld, havePoint;
        private Vector3 startPoint;
        private Plane dragPlane;
        private Vector3 dragAxis;
        public void Initialize(ShipDeckEnvironment owner, Transform visualPivot, Transform handle)
        {
            deck=owner; pivot=visualPivot; Handle=handle;
            BoatTriggerAction.Bind(handle.gameObject, BeginRayHold);
            handle.gameObject.AddComponent<DesktopObjectClick>().Selected=BeginMouseHold;
        }
        public void SetDeflection(float value)
        {
            value=Mathf.Clamp(value,-1,1);
            int direction=value>.3f?1:value<-.3f?-1:0;
            // Positive local X rotation moves an upright handle toward local +Z.
            // Input already measures +Z travel; negating here mirrored the visible handle.
            pivot.localRotation=Quaternion.Euler(value*35,0,0);
            if(direction==Direction)return;
            Direction=direction;
            if(direction==0)deck.StopRigging();else deck.MoveRigging(direction>0);
        }
        public void Stop() { holder=null; controller=null; mouseHeld=false; havePoint=false; SetDeflection(0); deck.StopRigging(); }
        private void BeginRayHold()
        {
            foreach(var item in Handle.GetComponent<XRSimpleInteractable>().interactorsHovering)
                if(item is XRRayInteractor ray && ray.xrController is ActionBasedController source &&
                    source.activateAction.action != null && source.activateAction.action.IsPressed())
                { holder=ray;controller=source;havePoint=false;mouseHeld=false;CapturePoint();break; }
        }
        private void BeginMouseHold() { if(WorldPanel.HeadsetTracked)return;mouseHeld=true;holder=null;havePoint=false;CapturePoint(); }
        private bool PointerRay(out Ray ray)
        {
            ray=default;
            if(holder!=null){var root=holder.rayOriginTransform!=null?holder.rayOriginTransform:holder.transform;ray=new Ray(root.position,root.forward);}
            else if(mouseHeld && Camera.main!=null && Mouse.current!=null)ray=Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            else return false;
            return true;
        }
        private bool Pointer(out Vector3 point)
        {
            point=Vector3.zero;
            if(!PointerRay(out var ray)||!dragPlane.Raycast(ray,out float distance)||distance>8)return false;
            point=ray.GetPoint(distance);return true;
        }
        private void CapturePoint()
        {
            if(!PointerRay(out var ray))return;
            // A fixed plane facing the initial ray preserves visible drag direction from either side.
            // The old horizontal plane could flip its intersection when the ray crossed the horizon.
            dragPlane=new Plane(-ray.direction,Handle.position);
            var projected=Vector3.ProjectOnPlane(transform.forward,dragPlane.normal);
            if(projected.sqrMagnitude<.04f)return; // An end-on view cannot show this lever's travel reliably.
            dragAxis=projected/projected.sqrMagnitude;
            if(Pointer(out var point)){startPoint=point;havePoint=true;}
        }
        private void Update() => ProcessHeldInput();
        public void ProcessHeldInput()
        {
            if(holder==null&&!mouseHeld)return;
            if(MuseumSession.Instance==null || MuseumSession.Instance.Busy || MuseumSession.Instance.Seated ||
                (holder!=null&&(controller==null||!holder.isActiveAndEnabled||controller.activateAction.action==null||!controller.activateAction.action.IsPressed())) ||
                (mouseHeld&&(Mouse.current==null||!Mouse.current.leftButton.isPressed))) { Stop();return; }
            if(!havePoint){CapturePoint();return;}
            if(Pointer(out var point))SetDeflection(Vector3.Dot(point-startPoint,dragAxis)/.18f);
        }
        private void OnDisable() { if(deck!=null)Stop(); }
    }
}
