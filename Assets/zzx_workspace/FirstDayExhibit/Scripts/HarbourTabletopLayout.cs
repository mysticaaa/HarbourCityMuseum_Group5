using System.Collections.Generic;
using UnityEngine;

namespace ZzxWorkspace.FirstDay
{
    public sealed partial class HarbourTabletopPrototype
    {
        private GameObject repairWorker;
        private Transform workerArm, ferryDoor;
        private readonly Vector3 doorClosedPosition=new Vector3(.068f,.066f,.105f);
        private Renderer ferryDoorFace, ferryOpeningFace;
        private Material ferryDoorYellow;
        private void BuildLowTable()
        {
            foreach(string name in new[]{"Archive plinth","Brass rim","Interpretation wall"})
            { var old=transform.Find(name); if(old) old.gameObject.SetActive(false); }
            var oak=ArtMaterial(new Color(.48f,.29f,.15f)); AddWoodGrain(oak);
            // Lower the entire animated coordinate system, preserving every local path.
            model.localPosition=Vector3.down*.22f;
            var top=Box("Wood tabletop",new Vector3(0,.66f,.04f),new Vector3(2.04f,.055f,1.00f),oak);
            top.SetParent(transform,false);
            var apron=Box("Table apron",new Vector3(0,.58f,.04f),new Vector3(1.94f,.12f,.90f),oak); apron.SetParent(transform,false);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            { var leg=Box("Wood table leg",new Vector3(x*.89f,.30f,.04f+z*.36f),new Vector3(.075f,.60f,.075f),oak); leg.SetParent(transform,false); }
            // Keep the task panel and heading at their existing height, now floating above the table.
            foreach(string name in new[]{"Narration subtitles","Subtitle backing","Exhibit title","Subtitle","Main action","Skip"})
            { var item=model.Find(name); if(item) item.localPosition+=Vector3.up*.22f; }
            foreach(string name in new[]{"Work pass","Letter","Reset"})
            {
                var item=model.Find(name); item.localPosition=new Vector3(item.localPosition.x,.85f,-.475f);
                item.localRotation=Quaternion.identity;
            }
            foreach(string name in new[]{"Work pass document stand","Letter document stand","Document hint"})
            { var item=model.Find(name); if(item) item.gameObject.SetActive(false); }
            for(int i=0;i<3;i++)
            {
                var sign=areaButtons[i].transform; sign.localPosition=new Vector3((i-1)*.60f,.965f,-.285f);
                var face=sign.Find("Button face"); face.localScale=new Vector3(.43f,.075f,.009f);
                var text=sign.GetComponentInChildren<TMPro.TMP_Text>(); text.rectTransform.sizeDelta=new Vector2(.42f,.069f);
                text.fontSize=.29f; text.enableAutoSizing=true; text.fontSizeMin=.20f; text.fontSizeMax=.29f;
                text.transform.localPosition=new Vector3(0,0,-.008f);
                sign.GetComponent<BoxCollider>().size=new Vector3(.45f,.095f,.025f);
            }
            var walkway=model.Find("Passenger walkway");
            walkway.localPosition=new Vector3(-.60f,.96f,.15f); walkway.localScale=new Vector3(.16f,.025f,.34f); walkway.GetComponent<Renderer>().sharedMaterial=oak;
            ferryDoorYellow=ArtMaterial(new Color(.76f,.57f,.30f)); AddWoodGrain(ferryDoorYellow);
            foreach(Transform part in model)
                if(part.name=="Passenger walkway" || part.name=="Ferry cross pier" || part.name=="Ferry cross pier board" ||
                   (part.name=="Pier" && part.localPosition.x<-.4f) ||
                   (part.name=="Pier decking plank" && part.localPosition.x<-.4f))
                    part.GetComponent<Renderer>().sharedMaterial=ferryDoorYellow;
            // Keep one visible decking layer; stacked faces cause z-fighting.
            walkway.GetComponent<Renderer>().enabled=false;
            foreach(Transform part in model)
                if(part.name=="Pier decking plank" && part.localPosition.x<-.4f)
                    part.GetComponent<Renderer>().enabled=false;
            repairHome=new Vector3(repairHome.x,1.0125f,repairHome.z);
            repairBoat.localPosition=repairHome;
            repairMarker.GetComponent<Renderer>().enabled=false;
            // Original region signs remain upright at the front of the map.
            var water=model.Find("Water tray"); water.localPosition=new Vector3(0,.915f,.04f); water.localScale=new Vector3(1.86f,.035f,.68f);
            var rim=model.Find("Tray front rim"); rim.localPosition=new Vector3(0,.93f,-.30f);
            foreach(Transform child in model) if(child.name=="Tray side rim")
            { child.localPosition=new Vector3(child.localPosition.x,.945f,.04f); child.localScale=new Vector3(.016f,.055f,.68f); }
            // Ferry travels in the open lane left of the pier, not through its decking.
            ferryHome=new Vector3(-.79f,.965f,.01f); ferry.localPosition=ferryHome;
            passengerHome=new Vector3(-.735f,.986f,.105f); passengers.transform.localPosition=passengerHome;
            var oldPeople=new List<GameObject>(); foreach(Transform child in passengers.transform) oldPeople.Add(child.gameObject);
            foreach(var person in oldPeople) { person.SetActive(false); Destroy(person); }
            for(int i=0;i<3;i++) Human("Passenger",passengers.transform,new Vector3(0,0,-i*.025f),ferryBlue);
            var worker=Human("Repair worker",model,new Vector3(.495f,.9675f,.12f),repairGreen);
            // Gangway bridges the small gap between the cabin exit and the pier.
            Box("Ferry gangway",new Vector3(-.7085f,.981f,.105f),new Vector3(.053f,.008f,.12f),ferryDoorYellow);
            ferryDoor=new GameObject("Ferry sliding door").transform; ferryDoor.SetParent(ferry,false);
            ferryDoor.localPosition=doorClosedPosition;
            ferryOpeningFace=Box("Cabin doorway",new Vector3(.064f,.066f,.081f),new Vector3(.004f,.065f,.048f),white,ferry).GetComponent<Renderer>();
            ferryDoorFace=Box("Cabin door",new Vector3(0,0,-.024f),new Vector3(.006f,.064f,.048f),white,ferryDoor).GetComponent<Renderer>();
            repairWorker=worker.gameObject; workerArm=worker.Find("Working arm"); repairWorker.SetActive(false);
        }
        private Transform Human(string name,Transform parent,Vector3 p,Material clothes)
        {
            var person=new GameObject(name).transform; person.SetParent(parent,false); person.localPosition=p; person.localScale=Vector3.one*.72f;
            var skin=ArtMaterial(new Color(.69f,.48f,.34f));
            Sphere("Head",new Vector3(0,.102f,0),new Vector3(.023f,.029f,.023f),skin,person);
            Sphere("Torso",new Vector3(0,.065f,0),new Vector3(.031f,.047f,.024f),clothes,person);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Trouser leg",new Vector3(side*.009f,.026f,0),new Vector3(.012f,.045f,.014f),dark,person);
                Box("Shoe",new Vector3(side*.009f,.005f,-.005f),new Vector3(.014f,.009f,.024f),dark,person);
                var arm=new GameObject(side==1?"Working arm":"Arm").transform; arm.SetParent(person,false); arm.localPosition=new Vector3(side*.023f,.079f,0);
                Box("Sleeve",new Vector3(0,-.014f,0),new Vector3(.011f,.029f,.013f),clothes,arm);
                Sphere("Hand",new Vector3(0,-.035f,0),new Vector3(.011f,.014f,.012f),skin,arm);
            }
            Box("Collar",new Vector3(0,.088f,-.001f),new Vector3(.022f,.006f,.026f),white,person);
            Box("Belt",new Vector3(0,.046f,0),new Vector3(.032f,.006f,.025f),dark,person);
            Box("Cap brim",new Vector3(0,.116f,-.012f),new Vector3(.029f,.004f,.025f),clothes,person);
            Box("Cap",new Vector3(0,.118f,-.002f),new Vector3(.032f,.009f,.031f),clothes,person);
            return person;
        }
        private void Sphere(string name,Vector3 p,Vector3 scale,Material mat,Transform parent)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.name=name; obj.transform.SetParent(parent,false);
            obj.transform.localPosition=p; obj.transform.localScale=scale; obj.GetComponent<Collider>().enabled=false;
            obj.GetComponent<Renderer>().sharedMaterial=mat;
        }
    }
}
