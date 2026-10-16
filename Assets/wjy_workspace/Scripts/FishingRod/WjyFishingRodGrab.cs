using UnityEngine.XR.Interaction.Toolkit;

namespace Wjy.Museum
{
    // Shared controllers keep their existing Trigger mapping for the other exhibits.
    // Only this rod's Grip bridge can authorize a new selection.
    public sealed class WjyFishingRodGrab : XRGrabInteractable
    {
        public XRRayInteractor AuthorizedRay { get; set; }

        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            return AuthorizedRay != null && ReferenceEquals(interactor, AuthorizedRay)
                && base.IsSelectableBy(interactor);
        }
    }
}
