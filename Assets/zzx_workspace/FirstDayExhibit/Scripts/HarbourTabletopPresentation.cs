using TMPro;
using UnityEngine;

namespace ZzxWorkspace.FirstDay
{
    public sealed partial class HarbourTabletopPrototype
    {
        private GameObject mainAction, skipAction, crateInput, choices, reader, stamp, passengers;
        private Transform repairTool, workPaperRoot;
        private bool passRead, letterRead;
        private Vector3 passengerHome;
        private readonly Vector3 hookHome=new Vector3(.17f,1.28f,.20f);
        private readonly Vector3 cargoLanding=new Vector3(.07f,1.0395f,.015f);
        private TMP_Text readerBody, readerTitle;
        private GameObject documentPicture;
        private Renderer documentPictureFace;
        private Material passPictureMaterial, letterPictureMaterial;
        private Material paper, ink, ferryBlue, repairGreen;
        private const string LetterText="Dear family,\n\nI arrived here by ferry today. The harbour is much busier than I expected. Tomorrow, I will begin my new job handling cargo.\n\nI hope everything goes well.";
        private const string PassText="Name: Liang\n\nOccupation: Cargo Handler\n\nAssigned Berth: No. 02\n\nStatus: New Worker";

        private void BuildPresentation()
        {
            paper=Mat(new Color(.91f,.85f,.70f)); ink=Mat(new Color(.08f,.13f,.18f));
            ferryBlue=Mat(new Color(.20f,.43f,.62f)); repairGreen=Mat(new Color(.27f,.43f,.32f));
            Label("Exhibit title", "FIRST DAY AT THE HARBOUR",new Vector3(0,1.96f,.34f),new Vector2(1.85f,.14f),.060f);
            Label("Subtitle", "Help Liang find his first workplace",new Vector3(0,1.83f,.34f),new Vector2(1.85f,.10f),.032f);
            // Three distinct working environments, all within the existing tabletop.
            Box("Passenger walkway",new Vector3(-.60f,.96f,.28f),new Vector3(.43f,.025f,.09f),ferryBlue);
            for(int i=0;i<3;i++) Box("Ferry window",new Vector3(-.13f+i*.035f,.065f,-.062f),new Vector3(.022f,.025f,.008f),ferryBlue,ferry);
            Box("Cargo storage",new Vector3(.07f,.955f,.32f),new Vector3(.29f,.02f,.10f),cyan);
            for(int i=0;i<2;i++)
            {
                Box("Stored cargo",new Vector3(-.08f+i*.09f,1.005f,.32f),new Vector3(.065f,.075f,.065f),wood);
                Box("Crate band",new Vector3(-.08f+i*.09f,1.005f,.285f),new Vector3(.012f,.075f,.007f),white);
            }
            Box("Repair berth boundary",new Vector3(.60f,.95f,.27f),new Vector3(.43f,.025f,.14f),repairGreen);
            Box("Toolbox",new Vector3(.77f,1.00f,.30f),new Vector3(.08f,.07f,.065f),repairGreen);
            for(int i=0;i<2;i++) Box("Area divider",new Vector3(-.30f+i*.60f,.94f,.12f),new Vector3(.008f,.01f,.46f),white);

            passengers=new GameObject("Passenger markers"); passengers.transform.SetParent(model,false);
            passengerHome=new Vector3(-.60f,1.04f,.05f); passengers.transform.localPosition=passengerHome;
            for(int i=0;i<3;i++)
            {
                Box("Passenger body",new Vector3(-.05f+i*.05f,.025f,0),new Vector3(.025f,.045f,.025f),ferryBlue,passengers.transform);
                Box("Passenger head",new Vector3(-.05f+i*.05f,.06f,0),new Vector3(.026f,.026f,.026f),paper,passengers.transform);
            }
            repairTool=new GameObject("Repair wrench").transform; repairTool.SetParent(model,false);
            repairTool.localPosition=new Vector3(.503f,1.025f,.095f);
            repairTool.localScale=Vector3.one*.42f;
            Box("Wrench handle",Vector3.zero,new Vector3(.015f,.10f,.018f),white,repairTool);
            Box("Wrench jaw left",new Vector3(-.014f,.05f,0),new Vector3(.015f,.035f,.018f),white,repairTool);
            Box("Wrench jaw right",new Vector3(.014f,.05f,0),new Vector3(.015f,.035f,.018f),white,repairTool);

            workPaperRoot=CreateDocument("Work pass", "WORK PASS", new Vector3(-.57f,.76f,-.27f),ReadPass);
            CreateDocument("Letter", "LETTER HOME", new Vector3(0,.76f,-.27f),ReadLetter);
            // Keep the visitor-approved lower sign row; document art is supplementary.
            Button("Work pass","WORK PASS",new Vector3(-.60f,.72f,-.32f),new Vector3(.50f,.12f,.04f),ReadPass);
            Button("Letter","LETTER HOME",new Vector3(0,.72f,-.32f),new Vector3(.50f,.12f,.04f),ReadLetter);
            stamp=Box("Reported stamp",new Vector3(0,.012f,.035f),new Vector3(.30f,.006f,.035f),repairGreen,workPaperRoot).gameObject;
            var stampText=Label("Reported mark", "REPORTED",Vector3.zero,new Vector2(.29f,.06f),.023f);
            stampText.transform.SetParent(workPaperRoot,false); stampText.transform.localPosition=new Vector3(0,.017f,.035f);
            stampText.transform.localRotation=Quaternion.Euler(90,0,0); stampText.transform.SetParent(stamp.transform,true);
            Label("Document hint", "Select a document to read",new Vector3(-.30f,.60f,-.32f),new Vector2(1.10f,.08f),.028f);

            choices=new GameObject("Workplace choices"); choices.transform.SetParent(model,false);
            for(int i=0;i<3;i++)
            {
                int answer=i; string[] names={"FERRY", "CARGO", "REPAIR"};
                var button=Button("Answer "+i,names[i],new Vector3((i-1)*.59f,1.09f,-.25f),new Vector3(.51f,.12f,.04f),()=>ChooseWorkplace(answer));
                button.transform.SetParent(choices.transform,true);
            }
            reader=new GameObject("Document reading window"); reader.transform.SetParent(model,false);
            // Backing collider shields the model so clicks through a reading panel do not start demos.
            var backing=Box("Reading paper",new Vector3(0,1.43f,-.43f),new Vector3(1.34f,1.02f,.025f),paper);
            backing.GetComponent<Collider>().enabled=true; backing.SetParent(reader.transform,true);
            readerTitle=Label("Document heading","",new Vector3(0,1.85f,-.455f),new Vector2(1.70f,.10f),.050f);
            readerBody=Label("Document contents","",new Vector3(0,1.43f,-.455f),new Vector2(1.19f,.72f),.034f);
            readerBody.alignment=TextAlignmentOptions.TopLeft;
            readerTitle.color=readerBody.color=ink.color;
            readerTitle.transform.SetParent(reader.transform,true); readerBody.transform.SetParent(reader.transform,true);
            var close=Button("Close document","CLOSE",new Vector3(.43f,.985f,-.47f),new Vector3(.40f,.12f,.04f),CloseReader);
            close.transform.SetParent(reader.transform,true);
            var notice=Label("Reader notice","Fictional Reconstruction",new Vector3(0,1.90f,-.465f),new Vector2(1.0f,.06f),.020f);
            notice.color=ink.color; notice.transform.SetParent(reader.transform,true);
            BuildDocumentPictures();
            var textToggle=Button("Readable text","IMAGE / TEXT",new Vector3(-.40f,.985f,-.47f),new Vector3(.57f,.12f,.04f),()=>
            {
                bool showImage=!documentPicture.activeSelf;
                documentPicture.SetActive(showImage); readerBody.gameObject.SetActive(!showImage);
            });
            textToggle.transform.SetParent(reader.transform,true);
            // Panel bottom: 1.43 - .51 - .0125 - .22 = .6875, matching the tabletop.
            // Image face aligns with the region-label text plane: -.285 - .008 = -.293.
            reader.transform.localPosition=new Vector3(0,-.0125f,.167f);
            reader.SetActive(false);
        }
        private Transform CreateDocument(string name,string title,Vector3 position,System.Action action)
        {
            var holder=new GameObject(name+" document stand").transform; holder.SetParent(model,false);
            // Build at the origin then tilt the whole document toward the visitor.
            // The upper edge stays below the map labels rather than overlapping them.
            int firstChild=model.childCount;
            var sheet=Box(name+" paper",Vector3.zero,new Vector3(.43f,.008f,.18f),paper);
            Box(name+" fold",new Vector3(0,.006f,0),new Vector3(.002f,.002f,.17f),wood);
            for(int i=-1;i<=1;i+=2)
            {
                Box(name+" border",new Vector3(i*.203f,.006f,0),new Vector3(.003f,.002f,.165f),wood);
                Box(name+" border",new Vector3(0,.006f,i*.078f),new Vector3(.406f,.002f,.003f),wood);
            }
            var cover=Label(name+" cover",title,new Vector3(0,.007f,-.025f),new Vector2(.39f,.08f),.029f);
            cover.color=ink.color; cover.transform.localRotation=Quaternion.Euler(90,0,0);
            var detail=Label(name+" serial",name=="Work pass" ? "No. 0258 / NEW WORKER" : "To my family / Liang",new Vector3(0,.007f,.045f),new Vector2(.39f,.05f),.017f);
            detail.color=ink.color; detail.transform.localRotation=Quaternion.Euler(90,0,0);
            var click=Button(name+" open","",Vector3.zero,new Vector3(.46f,.035f,.20f),action);
            click.transform.Find("Button face").GetComponent<Renderer>().enabled=false;
            var input=click.GetComponent<HarbourPrototypeButton>();
            input.face=sheet.GetComponent<Renderer>(); input.normal=paper; input.highlight=cyan;
            if(name=="Work pass") Box("Pass issued seal",new Vector3(-.16f,.008f,.045f),new Vector3(.035f,.003f,.035f),cyan);
            // Children are detached one by one because reparenting changes sibling indices.
            while(model.childCount>firstChild) model.GetChild(firstChild).SetParent(holder,false);
            holder.localPosition=position; holder.localRotation=Quaternion.Euler(-55,0,0);
            return holder;
        }
        private void BuildDocumentPictures()
        {
            passPictureMaterial=PictureMaterial("WorkPass"); letterPictureMaterial=PictureMaterial("LetterHome");
            var mesh=new Mesh {name="Document image quad"};
            mesh.vertices=new[]{new Vector3(-.60f,-.40f,0),new Vector3(.60f,-.40f,0),new Vector3(.60f,.40f,0),new Vector3(-.60f,.40f,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            mesh.triangles=new[]{0,2,1,0,3,2}; mesh.RecalculateNormals(); artMeshes.Add(mesh);
            documentPicture=new GameObject("Photographic document"); documentPicture.transform.SetParent(reader.transform,false);
            documentPicture.transform.localPosition=new Vector3(0,1.43f,-.46f);
            documentPicture.AddComponent<MeshFilter>().sharedMesh=mesh;
            documentPictureFace=documentPicture.AddComponent<MeshRenderer>();
        }
        private Material PictureMaterial(string resource)
        {
            var mat=new Material(Shader.Find("Unlit/Texture") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            var texture=Resources.Load<Texture2D>("FirstDayDocuments/"+resource);
            mat.mainTexture=texture;
            if(mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap",texture);
            materials.Add(mat); return mat;
        }
        private void ReadPass() { OpenDocument(true); }
        private void ReadLetter() { OpenDocument(false); }
        private void OpenDocument(bool isPass)
        {
            if(formalLoading) { ShowTask("LOADING IN PROGRESS", "Select SKIP LIFT or wait before opening a document."); return; }
            StopAnimation();
            if(isPass) passRead=true; else letterRead=true;
            readerTitle.text=isPass ? "HARBOUR WORK PASS / No. 0258" : "LETTER HOME";
            readerBody.text=isPass ? PassText : LetterText;
            documentPictureFace.sharedMaterial=isPass ? passPictureMaterial : letterPictureMaterial;
            documentPicture.SetActive(true); readerBody.gameObject.SetActive(false); readerTitle.gameObject.SetActive(false);
            reader.SetActive(true);
            if(!isPass && CurrentStage==Stage.Letter) CurrentStage=Stage.Complete;
            RefreshUI();
        }
        private void CloseReader()
        {
            if(reader) reader.SetActive(false);
            if(CurrentStage==Stage.Complete) ShowTask("FIRST DAY COMPLETE", "Explore any area, reopen the documents, or select RESTART.");
            else if(CurrentStage==Stage.Explore) ShowTask("FOLLOW THE CLUES", passRead && letterRead ? "Choose FIND WORKPLACE when you are ready. Exploring is optional." : "Read WORK PASS and LETTER HOME in either order.");
            if(mainAction) RefreshUI();
        }
    }
}
