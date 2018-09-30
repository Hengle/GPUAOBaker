using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class NormalBakeTest {


    [MenuItem("Test/BakeNormal")]
    static void BakeNormal()
    {
        if (Selection.activeGameObject == null)
            return;
        Mesh mesh = null;
        MeshFilter mf = Selection.activeGameObject.GetComponent<MeshFilter>();
        mesh = mf ? mf.sharedMesh : null;
        if (!mesh)
        {
            SkinnedMeshRenderer smr = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
            mesh = smr ? smr.sharedMesh : null;
        }
        if (!mesh)
            return;
        string savePath = EditorUtility.SaveFilePanel("", "", "", "jpg");
        if (string.IsNullOrEmpty(savePath))
            return;

        //Camera cam = CreateCamera();
        RenderTexture rt = RenderTexture.GetTemporary(512, 512);
        RenderTexture rt2 = RenderTexture.GetTemporary(512, 512);
        //CommandBuffer cb = new CommandBuffer();
        //cam.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, cb);
        //cam.targetTexture = rt;

        Material rdMat = new Material(Shader.Find("Unlit/NormalBaker"));
        Material filterMat = new Material(Shader.Find("Hidden/Filter"));

        RenderTexture tmp = RenderTexture.active;
        RenderTexture.active = rt;

        rdMat.SetPass(0);
        Graphics.DrawMeshNow(mesh, Matrix4x4.identity);

        RenderTexture.active = tmp;


        Graphics.Blit(rt, rt2, filterMat);

        //cb.Clear();
        //cb.ClearRenderTarget(true, true, new Color(1, 0, 0, 0));
        //cb.SetRenderTarget(rt);

        //cb.DrawMesh(mesh, Matrix4x4.identity, rdMat);

        //cam.Render();


        Texture2D result = RenderTextureToTexture(rt2);

        byte[] buffer = result.EncodeToJPG();
        System.IO.File.WriteAllBytes(savePath, buffer);

        Object.DestroyImmediate(result);
        Object.DestroyImmediate(rdMat);
        Object.DestroyImmediate(filterMat);
        //Object.DestroyImmediate(cam.gameObject);
        //Object.DestroyImmediate(rt);
        //cb.Release();

        RenderTexture.ReleaseTemporary(rt);
        RenderTexture.ReleaseTemporary(rt2);
    }

    static Camera CreateCamera()
    {
        Camera cam = new GameObject("Go").AddComponent<Camera>();
        cam.cullingMask = 0;
        //cam.gameObject.hideFlags = HideFlags.HideAndDontSave;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.orthographic = true;
        cam.aspect = 1;
        cam.orthographicSize = 512;
        cam.nearClipPlane = -100;
        cam.farClipPlane = 100;

        return cam;
    }

    static Texture2D RenderTextureToTexture(RenderTexture renderTexture)
    {
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D cont = new Texture2D(renderTexture.width, renderTexture.height);
        cont.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        cont.Apply();
        RenderTexture.active = active;
        return cont;
    }
}
