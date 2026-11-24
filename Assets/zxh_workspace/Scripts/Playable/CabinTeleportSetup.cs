using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    public static class CabinTeleportSetup
    {
        public static TeleportationArea MuseumTemplate(MuseumSession session)
        {
            if(session==null || session.Origin==null)return null;
            return Array.Find(UnityEngine.Object.FindObjectsOfType<TeleportationArea>(true),
                item=>item.gameObject.scene==session.Origin.gameObject.scene);
        }
        public static void Configure(CabinEnvironment cabin)
        {
            var session=MuseumSession.Instance;var template=MuseumTemplate(session);
            if(template==null)throw new InvalidOperationException("Museum teleport floor is required for cabin locomotion.");
            var provider=template.teleportationProvider;
            if(provider==null)provider=session.Origin.GetComponentInChildren<TeleportationProvider>(true);
            if(provider==null)throw new InvalidOperationException("Museum rig teleportation provider missing.");
            foreach(var mesh in cabin.Deck.GetComponentsInChildren<MeshCollider>())
                if(mesh.name.StartsWith("Exhibition geometry ") && mesh.name.EndsWith("_0") && mesh.GetComponent<BaseTeleportationInteractable>()==null)
                    mesh.gameObject.AddComponent<DeckMeshTeleportArea>();
            foreach(var area in cabin.GetComponentsInChildren<BaseTeleportationInteractable>(true))
            {
                area.interactionLayers=template.interactionLayers;
                area.interactionManager=template.interactionManager;
                area.teleportationProvider=provider;
                area.teleportTrigger=template.teleportTrigger;
                area.matchOrientation=template.matchOrientation;
                area.matchDirectionalInput=template.matchDirectionalInput;
                area.filterSelectionByHitNormal=template.filterSelectionByHitNormal;
                area.upNormalToleranceDegrees=template.upNormalToleranceDegrees;
                if(template.colliders.Count>0 && template.colliders[0]!=null)
                    area.gameObject.layer=template.colliders[0].gameObject.layer;
            }
            Debug.Log("[ZXH] Cabin teleport surfaces inherit museum layers, trigger and original XR provider.");
        }
    }
}
