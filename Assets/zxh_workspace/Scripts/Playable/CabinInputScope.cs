using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ZxhMuseum
{
    // Lease input only while aboard; restore the museum's exact enabled states on return.
    public sealed class CabinInputScope : MonoBehaviour
    {
        private readonly Dictionary<Behaviour, bool> states = new Dictionary<Behaviour, bool>();
        private GameObject owned;
        private void Update() => WorldPanel.UpdateInputMode();
        public void Acquire()
        {
            foreach (var system in FindObjectsOfType<EventSystem>(true))
            {
                states[system] = system.enabled;
                foreach (var module in system.GetComponents<BaseInputModule>())
                { states[module] = module.enabled; module.enabled = false; }
                system.enabled = false;
            }
            owned = new GameObject("ZXH_CabinEventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            owned.transform.SetParent(transform, false);
            WorldPanel.UpdateInputMode();
        }
        public void Release()
        {
            if (owned != null) { owned.SetActive(false); Destroy(owned); owned = null; }
            foreach (var item in states) if (item.Key != null) item.Key.enabled = item.Value;
            states.Clear();
        }
        private void OnDestroy() => Release();
    }
}
