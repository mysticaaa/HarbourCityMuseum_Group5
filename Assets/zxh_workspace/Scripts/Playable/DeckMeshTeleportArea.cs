using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    // Use the visible deck hit, not a hidden plane underneath its collision mesh.
    public sealed class DeckMeshTeleportArea : TeleportationArea
    {
        public static bool IsSafeDestination(RaycastHit hit) => hit.distance>0 && hit.point.y>=-.1f && hit.point.y<=.6f &&
            Mathf.Abs(hit.point.x)<=2.1f && hit.point.z>=-12.5f && hit.point.z<=-1.5f && Vector3.Dot(hit.normal,Vector3.up)>.96f;
        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            return interactor is XRRayInteractor ray && ray.TryGetCurrent3DRaycastHit(out var hit) && IsSafeDestination(hit) && base.IsSelectableBy(interactor);
        }
        protected override bool GenerateTeleportRequest(IXRInteractor interactor,RaycastHit hit,ref TeleportRequest request)
            => IsSafeDestination(hit) && base.GenerateTeleportRequest(interactor,hit,ref request);
    }
}
