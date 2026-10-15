using System.IO;
using UnityEngine;
using ZxhMuseum;

public static class PhysicalControlPreview
{
    public static void ValidateLabelShader()
    {
        var root=new GameObject("ZXH label shader check");root.transform.position=Vector3.one*1000;
        var maker=root.AddComponent<PhysicalBoatControls>();maker.Label(root.transform,"RIGGING / STOP",Vector3.zero);
        var camera=new GameObject("ZXH label check camera").AddComponent<Camera>();
        camera.transform.position=root.transform.position-Vector3.forward;camera.transform.LookAt(root.transform);
        var target=new RenderTexture(256,256,24);camera.targetTexture=target;camera.Render();
        var shader=Resources.Load<Shader>("Shaders/PhysicalLabel");
        foreach(var message in UnityEditor.ShaderUtil.GetShaderMessages(shader))
            if(message.severity.ToString()=="Error")throw new System.Exception(message.message);
        camera.targetTexture=null;Object.DestroyImmediate(target);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(root);
        Debug.Log("ZXH PHYSICAL LABEL SHADER PASS: label rendered without shader compiler errors.");
    }
    public static void Capture(CabinEnvironment cabin, ShipDeckEnvironment deck)
    {
        var camera=new GameObject("ZXH control preview camera").AddComponent<Camera>();
        camera.nearClipPlane=.03f;camera.farClipPlane=600;camera.fieldOfView=65;
        var target=new RenderTexture(1200,900,24);camera.targetTexture=target;
        var previous=RenderTexture.active;
        try
        {
            Save(camera,target,"Lever",new Vector3(.3f,1.7f,-12.3f),deck.Lever.Handle.position);
            Save(camera,target,"HatchConsole",new Vector3(-.3f,1.7f,-7.2f),deck.HatchButton.transform.position);
            Save(camera,target,"Door",new Vector3(1.2f,3.8f,.1f),deck.OuterDoorButton.transform.position);
            var at=cabin.transform.TransformPoint(new Vector3(0,1.65f,-1.3f));
            var window=cabin.transform.TransformPoint(new Vector3(0,1.6f,-3.5f));
            Save(camera,target,"WindowClosed",at,window);
            cabin.SeaWindow.Toggle();cabin.SeaWindow.Advance(2);
            Save(camera,target,"WindowOpen",at,window);
            cabin.SeaWindow.Close();cabin.SeaWindow.Advance(2);
            var sideAt=cabin.transform.TransformPoint(new Vector3(-.3f,1.6f,.2f));
            var sideFocus=cabin.transform.TransformPoint(new Vector3(-2,1.54f,.2f));
            Save(camera,target,"RightWindowClosed",sideAt,sideFocus);
            cabin.SideWindow.Toggle();cabin.SideWindow.Advance(2);
            Save(camera,target,"RightWindowOpen",sideAt,sideFocus);
            cabin.SideWindow.Close();cabin.SideWindow.Advance(2);
            Save(camera,target,"Sofa",cabin.transform.TransformPoint(new Vector3(-.8f,1.6f,.7f)),cabin.Sofa.transform.position);
            Save(camera,target,"SeatedLeave",cabin.transform.TransformPoint(new Vector3(1.1f,1.2f,.7f)),cabin.LeaveSeatButton.transform.position);
        }
        finally
        {
            RenderTexture.active=previous;camera.targetTexture=null;
            Object.DestroyImmediate(target);Object.DestroyImmediate(camera.gameObject);
        }
    }
    private static void Save(Camera camera,RenderTexture target,string name,Vector3 position,Vector3 focus)
    {
        camera.transform.position=position;camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
        var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();
        File.WriteAllBytes("Logs/ZXH-Physical-"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
    }
}
