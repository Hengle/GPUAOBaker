using UnityEngine;
using UnityEditor;
using System.Collections;

public class BakeTestWindow : EditorWindow
{
    private Mesh m_Mesh;
    private Material m_Material;
    private int m_Filter;

    [MenuItem("Test/BakeWindow")]
    static void Init()
    {
        BakeTestWindow win = BakeTestWindow.GetWindow<BakeTestWindow>();
    }

    void OnGUI()
    {
        m_Mesh = EditorGUILayout.ObjectField("Mesh", m_Mesh, typeof (Mesh), false) as Mesh;
        m_Material = EditorGUILayout.ObjectField("Material", m_Material, typeof(Material), false) as Material;
        m_Filter = EditorGUILayout.IntSlider("Filter", m_Filter, 0, 4);

        if (GUILayout.Button("Bake"))
        {
            Bake();
        }
    }

    private void Bake()
    {
        if (!m_Mesh)
            return;
        if (!m_Material)
            return;
        string savePath = EditorUtility.SaveFilePanel("", "", "", "png");
        if (string.IsNullOrEmpty(savePath))
            return;
        
        RenderTexture rt = RenderTexture.GetTemporary(512, 512);

        RenderTexture tmp = RenderTexture.active;
        RenderTexture.active = rt;

        m_Material.SetPass(0);
        Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);

        RenderTexture.active = tmp;


        rt = Filter(rt, m_Filter);

        Texture2D result = RenderTextureToTexture(rt);

        byte[] buffer = result.EncodeToPNG();
        System.IO.File.WriteAllBytes(savePath, buffer);

        Object.DestroyImmediate(result);

        RenderTexture.ReleaseTemporary(rt);
    }

    static RenderTexture Filter(RenderTexture src, int filterTimes)
    {
        if (filterTimes <= 0)
            return src;
        int w = src.width;
        int h = src.height;

        Material filterMat = new Material(Shader.Find("Hidden/Filter"));

        for (int i = 0; i < filterTimes; i++)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(src, tmp, filterMat);

            RenderTexture.ReleaseTemporary(src);

            src = tmp;
        }

        Object.DestroyImmediate(filterMat);

        return src;
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
