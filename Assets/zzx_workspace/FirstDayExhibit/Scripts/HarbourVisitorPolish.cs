using TMPro;
using UnityEngine;

namespace ZzxWorkspace.FirstDay
{
    public sealed partial class HarbourTabletopPrototype
    {
        [SerializeField] private Vector3 readerDefaultPosition;
        [SerializeField] private float normalReadingSize;
        private bool largeReading, lowerReading;
        private Transform shiftedStoryGuideCard;
        private float nextGuidePlacementCheck;
        private void LateUpdate()
        {
            if(shiftedStoryGuideCard || Time.unscaledTime<nextGuidePlacementCheck) return;
            nextGuidePlacementCheck=Time.unscaledTime+.5f;
            foreach(var guide in FindObjectsOfType<CsyMuseum.CSYExhibitGuide>())
            {
                if(guide.gameObject.scene!=gameObject.scene || guide.displayModel!=transform) continue;
                var card=guide.transform.Find("Exhibit guide card");
                if(!card) continue;
                // Move only the card; its sibling visit-region anchor must remain at the exhibit.
                card.position-=guide.transform.right*.55f;
                shiftedStoryGuideCard=card;
                break;
            }
        }
        private GameObject sizeControl, pageControl, heightControl;
        public void EnsureVisitorPolish()
        {
            if(!model) return;
            var subtitle=model.Find("Subtitle");
            if(subtitle) subtitle.GetComponent<TMP_Text>().text="POINT > PRESS TRIGGER > RELEASE / One hand is enough";
            if(!model.Find("Visitor help"))
            {
                Button("Visitor help","HELP",new Vector3(0,mainAction.transform.localPosition.y,.30f),new Vector3(.25f,.12f,.035f),ShowVisitorHelp);
                readerDefaultPosition=reader.transform.localPosition;
                normalReadingSize=readerBody.fontSize;
            }
            AddReadingControl("Reading size","A+",new Vector3(.84f,1.65f,-.47f),ToggleReadingSize);
            AddReadingControl("Reading page","PAGE",new Vector3(.84f,1.43f,-.47f),NextReadingPage);
            AddReadingControl("Reading height","LOWER",new Vector3(.84f,1.21f,-.47f),ToggleReadingHeight);
            sizeControl=reader.transform.Find("Reading size").gameObject;
            pageControl=reader.transform.Find("Reading page").gameObject;
            heightControl=reader.transform.Find("Reading height").gameObject;
            readerBody.overflowMode=TextOverflowModes.Page;
            for(int i=0;i<3;i++) SetAreaLabel(i);
            RefreshReadingControls();
        }
        private void AddReadingControl(string name,string text,Vector3 p,System.Action action)
        {
            if(reader.transform.Find(name)) return;
            var button=Button(name,text,p,new Vector3(.24f,.13f,.035f),action);
            button.transform.SetParent(reader.transform,false);
        }
        private void ShowVisitorHelp()
        {
            if(formalLoading || CurrentStage==Stage.Review) return;
            if(reader.activeSelf) CloseReader();
            ShowTask("ONE-HAND CONTROLS", "Point. Press the index-finger trigger. Release. WATCH = demo; FIND WORKPLACE = answer.");
        }
        private void ToggleReadingMode()
        {
            bool image=!documentPicture.activeSelf;
            documentPicture.SetActive(image); readerBody.gameObject.SetActive(!image);
            readerBody.pageToDisplay=1; RefreshReadingControls();
        }
        private void ToggleReadingSize()
        {
            largeReading=!largeReading;
            readerBody.fontSize=normalReadingSize*(largeReading?1.3f:1f);
            readerBody.pageToDisplay=1; RefreshReadingControls();
        }
        private void NextReadingPage()
        {
            readerBody.ForceMeshUpdate(true);
            readerBody.pageToDisplay=readerBody.pageToDisplay%Mathf.Max(1,readerBody.textInfo?.pageCount ?? 1)+1;
        }
        private void ToggleReadingHeight()
        {
            lowerReading=!lowerReading;
            reader.transform.localPosition=readerDefaultPosition+(lowerReading?Vector3.down*.15f:Vector3.zero);
            heightControl.GetComponentInChildren<TMP_Text>(true).text=lowerReading?"RESTORE":"LOWER";
        }
        private void RefreshReadingControls()
        {
            if(!sizeControl) return;
            bool text=readerBody.gameObject.activeSelf;
            sizeControl.SetActive(text);
            sizeControl.GetComponentInChildren<TMP_Text>(true).text=largeReading?"A-":"A+";
            if(text) readerBody.ForceMeshUpdate(true);
            pageControl.SetActive(text && (readerBody.textInfo?.pageCount ?? 1)>1);
        }
        private void ResetReadingOptions()
        {
            if(!heightControl) return;
            largeReading=lowerReading=false;
            readerBody.fontSize=normalReadingSize; readerBody.pageToDisplay=1;
            reader.transform.localPosition=readerDefaultPosition;
            heightControl.GetComponentInChildren<TMP_Text>(true).text="LOWER";
            RefreshReadingControls();
        }
    }
}
