using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ReturnableTool : XRGrabInteractable
    {
        public Func<bool> CanTake;
        public bool Returning { get; private set; }
        private Transform home;
        private Vector3 position, scale;
        private Quaternion rotation;
        private Coroutine returnRoutine;
        private Rigidbody body;
        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            movementType = MovementType.Kinematic; throwOnDetach = false;
            useDynamicAttach = true; attachEaseInTime = 0.2f; selectMode = InteractableSelectMode.Single;
        }
        private void Start()
        {
            home = transform.parent; position = transform.localPosition;
            rotation = transform.localRotation; scale = transform.localScale;
        }
        public override bool IsSelectableBy(IXRSelectInteractor interactor)
            => (CanTake == null || CanTake()) && base.IsSelectableBy(interactor);
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            returnRoutine = null; Returning = false;
        }
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            if (isActiveAndEnabled) returnRoutine = StartCoroutine(Return());
        }
        private IEnumerator Return()
        {
            Returning = true; transform.SetParent(home, true);
            Vector3 start = transform.localPosition;
            Quaternion startRotation = transform.localRotation;
            Vector3 startScale = transform.localScale;
            for (float t = 0; t < 1; t += Time.deltaTime / 0.4f)
            {
                float eased = Mathf.SmoothStep(0, 1, t);
                transform.localPosition = Vector3.Lerp(start, position, eased);
                transform.localRotation = Quaternion.Slerp(startRotation, rotation, eased);
                transform.localScale = Vector3.Lerp(startScale, scale, eased);
                yield return null;
            }
            ResetPose();
        }
        public void ResetPose()
        {
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            returnRoutine = null; Returning = false;
            transform.SetParent(home, false); transform.localPosition = position;
            transform.localRotation = rotation; transform.localScale = scale;
            if (body != null) { body.useGravity = false; body.isKinematic = true; }
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            if (home != null) ResetPose();
        }
    }
}
