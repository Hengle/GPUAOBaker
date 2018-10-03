using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASL.AOBaker
{

    class AOBakeConstants
    {
        public const string kAOBakeShader = "Assets/AOBaker/Shaders/BakeAO.shader";
        public const string kAOMixShader = "Assets/AOBaker/Shaders/AOMix.shader";
        public const float kDefaultTraceRadius = 1f;
        public const SamplerType kDefaultSamplerType = SamplerType.Hammersley;
        public const int kDefaultNumSamples = 5;
        public const int kDefaultAOMapSize = 1024;
        public const int kDefaultAOMapPadding = 2;
    }


    class AOBakeUtils
    {
        public static List<AOBakeBatch> CollectBatches(bool staticOnly, bool ignoreSkinnedMeshRenderer)
        {
            List<AOBakeBatch> batches = new List<AOBakeBatch>();
            MeshRenderer[] mr = Object.FindObjectsOfType<MeshRenderer>();
            for (int i = 0; i < mr.Length; i++)
            {
                if(staticOnly && !mr[i].gameObject.isStatic)
                    continue;
                Material mat = mr[i].sharedMaterial;
                if (mat && mat.renderQueue >= (int) RenderQueue.AlphaTest)
                    continue;
                var batch = AOBakeBatch.CreateBatch(mr[i].gameObject);
                if(batch != null)
                    batches.Add(batch);
            }

            if (!ignoreSkinnedMeshRenderer)
            {
                SkinnedMeshRenderer[] skmr = Object.FindObjectsOfType<SkinnedMeshRenderer>();
                for (int i = 0; i < skmr.Length; i++)
                {
                    if (staticOnly && !skmr[i].gameObject.isStatic)
                        continue;
                    Material mat = skmr[i].sharedMaterial;
                    if (mat && mat.renderQueue >= (int)RenderQueue.AlphaTest)
                        continue;
                    var batch = AOBakeBatch.CreateBatch(skmr[i].gameObject);
                    if (batch != null)
                        batches.Add(batch);
                }
            }

            return batches;
        }

        public static Texture2D BakeScene(MeshRenderer target, List<AOBakeBatch> batches, BakeSettings settings)
        {
            if (batches == null || batches.Count <= 0)
            {
                return null;
            }
            if (!target)
                return null;

            Shader bakeShader = AssetDatabase.LoadAssetAtPath<Shader>(AOBakeConstants.kAOBakeShader);
            Shader mixShader = AssetDatabase.LoadAssetAtPath<Shader>(AOBakeConstants.kAOMixShader);
            if (!bakeShader)
            {
                Debug.LogError("Shader Missing! :" + AOBakeConstants.kAOBakeShader);
                return null;
            }
            if (!mixShader)
            {
                Debug.LogError("Shader Missing! :" + AOBakeConstants.kAOMixShader);
                return null;
            }

            Camera cam = new GameObject("[BakeCam]").AddComponent<Camera>();
            cam.gameObject.hideFlags = HideFlags.HideAndDontSave;
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask = 0;
            cam.backgroundColor = new Color(0, 0, 0, 0);

            var script = cam.gameObject.AddComponent<AOBakeCamera>();
            script.Init(bakeShader, mixShader, settings);
            var result = script.Render(batches, target);

            Texture2D texture = null;
            if (result)
            {
                texture = RenderTextureToTexture(result);
                RenderTexture.ReleaseTemporary(result);
            }

            Object.DestroyImmediate(cam.gameObject);

            return texture;
        }

        //public static Texture2D BakeScene(MeshRenderer target, List<AOBakeBatch> batches, BakeSettings settings)
        //{
        //    if (batches == null || batches.Count <= 0)
        //    {
        //        return null;
        //    }
        //    if (!target)
        //        return null;
        //    //MeshFilter mf = target.GetComponent<MeshFilter>();
        //    //if (!mf)
        //    //    return null;

        //    ////准备阶段
        //    //Shader bakeShader = AssetDatabase.LoadAssetAtPath<Shader>(AOBakeConstants.kAOBakeShader);
        //    Shader mixShader = AssetDatabase.LoadAssetAtPath<Shader>(AOBakeConstants.kAOMixShader);
        //    //if (!bakeShader)
        //    //{
        //    //    Debug.LogError("Shader Missing! :" + AOBakeConstants.kAOBakeShader);
        //    //    return null;
        //    //}
        //    if (!mixShader)
        //    {
        //        Debug.LogError("Shader Missing! :" + AOBakeConstants.kAOMixShader);
        //        return null;
        //    }

        //    //Material bakeMat = new Material(bakeShader);
        //    Material mixMat = new Material(mixShader);

        //    //List<AOBakeBatch> batches =
        //    //    CollectBatches(target, settings.traceRadius, staticOnly, ignoreSkinnedMeshRenderer);

        //    Texture2D result = null;
        //    if (batches.Count > 0)
        //    {
        //        Debug.Log("BatchCount:"+batches.Count);
        //        result = RayTracing(target, batches, mixMat, settings);
        //    }

        //    //Object.DestroyImmediate(bakeMat);
        //    Object.DestroyImmediate(mixMat);

        //    return result;
        //}



        //private static Texture2D RayTracing(MeshRenderer meshRenderer, List<AOBakeBatch> batches, Material mixMat, BakeSettings settings)
        //{
        //    MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
        //    if (!meshFilter)
        //        return null;
        //    float factor = 1.0f / (settings.numSamples * 3);

        //    //raytracingMat.SetFloat("_TraceRadius", settings.traceRadius);
        //    mixMat.SetFloat("_MixFactor", factor);

        //    var sampler = CreateSampler(settings.samplerType, settings.numSamples * 3);

        //    RenderTexture pre = RenderTexture.GetTemporary(settings.aoMapSize, settings.aoMapSize);
        //    pre.DiscardContents(true, true);
        //    RenderTexture tmp = RenderTexture.active;

        //    //RenderTexture pre = RaytracingScene(meshFilter, batches, sampler, raytracingMat, settings);

        //    for (int s = 0; s < settings.numSamples; s += 3)
        //    {
        //        float progress = ((float) s) / settings.numSamples;
        //        EditorUtility.DisplayProgressBar("正在采样", "当前采样：" + s, progress);

        //        int spcount = Mathf.Min(settings.numSamples - s, 3);

        //        RenderTexture result0 = null;
        //        RenderTexture result1 = null;
        //        RenderTexture result2 = null;
        //        //RenderTexture result3 = null;

        //        if (spcount <= 0)
        //            continue; 

        //        if (spcount > 0)
        //            result0 = RaytracingScene(meshFilter, meshRenderer, batches, sampler, settings);
        //        if (spcount > 1)
        //            result1 = RaytracingScene(meshFilter, meshRenderer, batches, sampler, settings);
        //        if (spcount > 2)
        //            result2 = RaytracingScene(meshFilter, meshRenderer, batches, sampler, settings);
        //        //if (spcount > 3)
        //        //    result3 = RaytracingScene(meshFilter, batches, sampler, raytracingMat, settings);

        //        mixMat.SetTexture("_SampleResult0", result0);
        //        mixMat.SetTexture("_SampleResult1", result1);
        //        mixMat.SetTexture("_SampleResult2", result2);
        //        //mixMat.SetTexture("_SampleResult3", result3);
        //        mixMat.SetFloat("_ResultCount", spcount);

        //        RenderTexture tmpTex = RenderTexture.GetTemporary(settings.aoMapSize, settings.aoMapSize);
        //        Graphics.Blit(pre, tmpTex, mixMat);

        //        if (pre)
        //            RenderTexture.ReleaseTemporary(pre);
        //        if (result0)
        //            RenderTexture.ReleaseTemporary(result0);
        //        if (result1)
        //            RenderTexture.ReleaseTemporary(result1);
        //        if (result2)
        //            RenderTexture.ReleaseTemporary(result2);
        //        //if (result3)
        //        //    RenderTexture.ReleaseTemporary(result3);

        //        pre = tmpTex;

        //    }

        //    RenderTexture.active = tmp;


        //    //Material tmpMat = new Material(Shader.Find("Hidden/AOMix"));
        //    //RenderTexture tmpTex = RenderTexture.GetTemporary(settings.aoMapSize, settings.aoMapSize);
        //    //Graphics.Blit(pre, tmpTex, tmpMat);

        //    //Texture2D result = RenderTextureToTexture(tmpTex);

        //    //RenderTexture.ReleaseTemporary(tmpTex);
        //    //Object.DestroyImmediate(tmpMat);

        //    Texture2D result = RenderTextureToTexture(pre);

        //    if (pre)
        //        RenderTexture.ReleaseTemporary(pre);

        //    EditorUtility.ClearProgressBar();

        //    return result;
        //}

        //private static RenderTexture RaytracingScene(MeshFilter meshFilter, MeshRenderer meshRenderer, List<AOBakeBatch> batches, SamplerBase sampler, BakeSettings settings)
        //{
        //    Vector2 sp0 = sampler.Sample();
        //    Vector2 sp1 = sampler.Sample();
        //    Vector2 sp2 = sampler.Sample();

        //    //raytracingMat.SetVector("_Sample0", sp0);
        //    //raytracingMat.SetVector("_Sample1", sp1);
        //    //raytracingMat.SetVector("_Sample2", sp2);

        //    RenderTexture pre = null;
        //    for (int i = 0; i < batches.Count; i ++)
        //    {
        //        if(meshRenderer.bounds.Intersects(batches[i].bounds) == false)
        //            continue;
        //        RenderTexture cur = RenderTexture.GetTemporary(settings.aoMapSize, settings.aoMapSize);
        //        RenderTexture.active = cur;

        //        Material raytracingMat = batches[i].material;
        //        raytracingMat.SetVector("_Sample0", sp0);
        //        raytracingMat.SetVector("_Sample1", sp1);
        //        raytracingMat.SetVector("_Sample2", sp2);
        //        raytracingMat.SetFloat("_TraceRadius", settings.traceRadius);
        //        raytracingMat.SetTexture("_PreTex", pre);
        //        //raytracingMat.SetVectorArray("_Vertices", batches[i].vertices);
        //        raytracingMat.SetTexture("_VertexTex", batches[i].vertexTexture);
        //        raytracingMat.SetInt("_TexSize", batches[i].size);
        //        raytracingMat.SetMatrix("_LocalToWorld", batches[i].localToWorld);
        //        raytracingMat.SetVector("_BoundsMin", batches[i].meshBounds.min);
        //        raytracingMat.SetVector("_BoundsSize", batches[i].meshBounds.size);
        //        raytracingMat.SetFloat("_VertexCount", batches[i].vertexCount);

        //        raytracingMat.SetPass(0);
        //        Graphics.DrawMeshNow(meshFilter.sharedMesh, meshFilter.transform.localToWorldMatrix);

        //        if (pre)
        //            RenderTexture.ReleaseTemporary(pre);

        //        pre = cur;

        //    }

        //    return pre;
        //}



        private static Texture2D RenderTextureToTexture(RenderTexture renderTexture)
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
}
