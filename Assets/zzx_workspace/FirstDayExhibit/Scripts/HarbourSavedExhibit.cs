using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZzxWorkspace.FirstDay
{
    public sealed partial class HarbourTabletopPrototype
    {
        public System.Collections.Generic.IEnumerable<UnityEngine.Object> GeneratedAssets()
        {
            foreach(var texture in artTextures) yield return texture;
            foreach(var mesh in artMeshes) yield return mesh;
            foreach(var material in materials) yield return material;
        }
        public void BuildSavedExhibit(XRInteractionManager sceneManager)
        {
            if(model) throw new InvalidOperationException("Exhibit already generated.");
            manager=sceneManager;
            Build(); BuildPresentation(); BuildHarbourArt(); BuildLowTable(); ResetExperience();
            savedExhibit=true;
        }
        public void BindSavedManager(XRInteractionManager sceneManager)
        {
            manager=sceneManager;
            foreach(var input in GetComponentsInChildren<XRBaseInteractable>(true)) input.interactionManager=manager;
        }
        private void BindSavedButtons()
        {
            BindSavedManager(manager);
            foreach(var button in GetComponentsInChildren<HarbourPrototypeButton>(true))
            {
                Action callback=null;
                string key=button.name;
                if(key.StartsWith("Area ")) { int area=int.Parse(key.Substring(5)); callback=()=>Explore(area); }
                else if(key.StartsWith("Answer ")) { int answer=int.Parse(key.Substring(7)); callback=()=>ChooseWorkplace(answer); }
                else switch(key)
                {
                    case "Main action": callback=Next; break;
                    case "Skip": callback=Skip; break;
                    case "Reset": callback=ResetExperience; break;
                    case "Work pass": case "Work pass open": callback=ReadPass; break;
                    case "Letter": case "Letter open": callback=ReadLetter; break;
                    case "Select cargo": callback=SelectCrate; break;
                    case "Boat target": callback=Load; break;
                    case "Close document": callback=CloseReader; break;
                    case "Readable text": callback=()=> { bool show=!documentPicture.activeSelf; documentPicture.SetActive(show); readerBody.gameObject.SetActive(!show); }; break;
                }
                if(callback==null) { Debug.LogError("Unbound harbour button: "+key,this); continue; }
                var action=callback;
                button.action=()=> { if(lastClick==Time.frameCount) return; lastClick=Time.frameCount; action(); };
            }
        }
    }
}
