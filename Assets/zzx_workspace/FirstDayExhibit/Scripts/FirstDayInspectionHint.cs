using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZzxWorkspace.FirstDay
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public sealed class FirstDayInspectionHint : MonoBehaviour
    {
        [SerializeField] private GameObject targetObject;
        private XRBaseInteractable interactable;
        private void Awake() => interactable = GetComponent<XRBaseInteractable>();
        private void OnEnable()
        {
            interactable.selectEntered.AddListener(Show);
            interactable.selectExited.AddListener(Hide);
        }
        private void OnDisable()
        {
            interactable.selectEntered.RemoveListener(Show);
            interactable.selectExited.RemoveListener(Hide);
            if (targetObject) targetObject.SetActive(false);
        }
        private void Show(SelectEnterEventArgs args) { if (targetObject) targetObject.SetActive(true); }
        private void Hide(SelectExitEventArgs args) { if (targetObject) targetObject.SetActive(false); }
    }
}
