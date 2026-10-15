using UnityEngine;
using UnityEngine.InputSystem;

namespace ZxhMuseum
{
    // Desktop fallback only; Quest uses the Museum scene's original locomotion providers.
    public sealed class BoatWalkController : MonoBehaviour
    {
        private MuseumSession session;
        private float verticalSpeed;
        public void Initialize(MuseumSession owner)
        {
            session=owner;
        }
        private void Update()
        {
            if(Application.isBatchMode || WorldPanel.HeadsetTracked || session==null || !session.InCabin || session.Busy || session.Seated)return;
            Vector2 input=Vector2.zero;
            if(Keyboard.current!=null)
            {
                var k=Keyboard.current;
                input=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
            }
            Advance(input,Time.deltaTime);
        }
        public void Advance(Vector2 input,float seconds)
        {
            var origin=session.Origin;
            var controller=origin.GetComponent<CharacterController>();
            if(controller==null || !controller.enabled)return;
            var localHead=origin.transform.InverseTransformPoint(origin.Camera.transform.position);
            controller.height=Mathf.Clamp(localHead.y,1.2f,2);
            controller.center=new Vector3(localHead.x,controller.height*.5f+.03f,localHead.z);
            controller.radius=.22f;controller.stepOffset=.25f;
            var forward=Vector3.ProjectOnPlane(origin.Camera.transform.forward,Vector3.up).normalized;
            var right=Vector3.Cross(Vector3.up,forward);
            var planar=(forward*input.y+right*input.x);if(planar.sqrMagnitude>1)planar.Normalize();
            verticalSpeed=controller.isGrounded?-1:Mathf.Max(-12,verticalSpeed-9.81f*seconds);
            controller.Move((planar*1.5f+Vector3.up*verticalSpeed)*seconds);
            if(origin.transform.position.y < -5)
            {
                // Recover to the deck if a teleport or unusual tracking offset crossed the vessel bounds.
                var deck=FindObjectOfType<ShipDeckEnvironment>();
                if(deck!=null)session.MoveTo(deck.SpawnPoint,0);
                verticalSpeed=0;
            }
        }
    }
}
