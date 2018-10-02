using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASL.AOBaker
{
    [System.Serializable]
    struct BakeSettings
    {
        public float traceRadius;
        public SamplerType samplerType;
        public int numSamples;
        public int aoMapSize;
        public int aoMapPadding;
    }

    class AOBakeConstants
    {
        public const string kAOBakeShader             = "Assets/Editor/AOBaker/Shaders/BakeAO.shader";
        public const string kAOMixShader = "Assets/Editor/AOBaker/Shaders/AOMix.shader";
        public const float kDefaultTraceRadius       = 1f;
        public const SamplerType kDefaultSamplerType = SamplerType.Hammersley;
        public const int kDefaultNumSamples          = 5;
        public const int kDefaultAOMapSize           = 1024;
        public const int kDefaultAOMapPadding        = 2;
    }

    /// <summary>
    /// 烘焙光追批次
    /// 单次光追只传入最多999个顶点，超过的部分放入下一批次
    /// </summary>
    class AOBakeBatch
    {
        private Vector4[] m_Vertices;
        private int m_VertexCount;

        public int vertexCount
        {
            get { return m_VertexCount; }
        }

        public Vector4[] vertices
        {
            get { return m_Vertices; }
        }

        public AOBakeBatch()
        {
            m_VertexCount = 0;
            m_Vertices = new Vector4[999];
        }

        public void AddTriangle( Vector3 v0, Vector3 v1, Vector3 v2)
        {
            m_Vertices[m_VertexCount] = v0;
            m_Vertices[m_VertexCount + 1] = v1;
            m_Vertices[m_VertexCount + 2] = v2;

            m_VertexCount += 3;
            //m_Vertices.Add(v0);
            //m_Vertices.Add(v1);
            //m_Vertices.Add(v2);
        }
    }

    class AOBakeUtils
    {
        public static Texture2D BakeScene(MeshRenderer target, bool staticOnly, bool ignoreSkinnedMeshRenderer, BakeSettings settings)
        {
            if (!target)
                return null;
            MeshFilter mf = target.GetComponent<MeshFilter>();
            if (!mf)
                return null;

            //准备阶段
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

            Material bakeMat = new Material(bakeShader);
            Material mixMat = new Material(mixShader);

            List<AOBakeBatch> batches =
                CollectBatches(target, settings.traceRadius, staticOnly, ignoreSkinnedMeshRenderer);

            Texture2D result = null;
            if (batches.Count > 0)
            {
                Debug.Log("BatchCount:"+batches.Count);
                result = RayTracing(mf, batches, bakeMat, mixMat, settings);
            }

            Object.DestroyImmediate(bakeMat);
            Object.DestroyImmediate(mixMat);

            return result;
        }

        private static List<AOBakeBatch> CollectBatches(MeshRenderer target, float radius, bool staticOnly, bool ignoreSkinnedMeshRenderer)
        {
            Bounds bounds = target.bounds;
            bounds = new Bounds(bounds.center,
                new Vector3(bounds.size.x + radius * 2, bounds.size.y + radius * 2, bounds.size.z + radius * 2));

            MeshRenderer[] mr = Object.FindObjectsOfType<MeshRenderer>();

            List<AOBakeBatch> batches = new List<AOBakeBatch>();

            AOBakeBatch current = new AOBakeBatch();

            EditorUtility.ClearProgressBar();

            for (int i = 0; i < mr.Length; i++)
            {
                float progress = ((float) i) / mr.Length;
                EditorUtility.DisplayProgressBar("收集顶点", "正在收集顶点：" + i, progress);

                if (staticOnly && !mr[i].gameObject.isStatic)
                    continue;

                Material mat = mr[i].sharedMaterial;
                if (mat && mat.renderQueue >= (int)RenderQueue.AlphaTest)
                    continue;

                MeshFilter mf = mr[i].GetComponent<MeshFilter>();
                if(!mf || !mf.sharedMesh)
                    continue;

                if(!bounds.Intersects(mr[i].bounds))
                    continue;

                CollectTriangle(batches, mf.sharedMesh, mf.transform.localToWorldMatrix, ref current);
            }

            EditorUtility.ClearProgressBar();

            if (!ignoreSkinnedMeshRenderer)
            {
                SkinnedMeshRenderer[] smr = Object.FindObjectsOfType<SkinnedMeshRenderer>();
                for (int i = 0; i < smr.Length; i++)
                {
                    if (staticOnly && !smr[i].gameObject.isStatic)
                        continue;

                    Material mat = smr[i].sharedMaterial;
                    if (mat && mat.renderQueue >= (int)RenderQueue.AlphaTest)
                        continue;
                    
                    if (!smr[i].sharedMesh)
                        continue;

                    if (!bounds.Intersects(smr[i].bounds))
                        continue;

                    CollectTriangle(batches, smr[i].sharedMesh, smr[i].transform.localToWorldMatrix, ref current);
                }
            }

            if (current.vertexCount > 0)
                batches.Add(current);

            return batches;
        }

        private static void CollectTriangle(List<AOBakeBatch> batches, Mesh mesh, Matrix4x4 matrix, ref AOBakeBatch currentBatch)
        {
            for (int i = 0; i < mesh.triangles.Length; i+=3)
            {
                Vector3 v0 = mesh.vertices[mesh.triangles[i]];
                Vector3 v1 = mesh.vertices[mesh.triangles[i+1]];
                Vector3 v2 = mesh.vertices[mesh.triangles[i + 2]];
                v0 = matrix.MultiplyPoint(v0);
                v1 = matrix.MultiplyPoint(v1);
                v2 = matrix.MultiplyPoint(v2);

                currentBatch.AddTriangle(v0, v1, v2);

                if (currentBatch.vertexCount >= 999)
                {
                    batches.Add(currentBatch);
                    currentBatch = new AOBakeBatch();
                }
            }
        }

        private static Texture2D RayTracing(MeshFilter meshFilter, List<AOBakeBatch> batches, Material raytracingMat, Material mixMat, BakeSettings settings)
        {
            float factor = 1.0f / (settings.numSamples * 3);

            raytracingMat.SetFloat("_TraceRadius", settings.traceRadius);
            mixMat.SetFloat("_MixFactor", factor);

            var sampler = CreateSampler(settings.samplerType, settings.numSamples * 3);
            
            RenderTexture pre = RenderTexture.GetTemporary(settings.aoMapSize, settings.aoMapSize);
            pre.DiscardContents(true, true);
            RenderTexture tmp = RenderTexture.active;

            //RenderTexture pre = RaytracingScene(meshFilter, batches, sampler, raytracingMat, settings);

            for (int s = 0; s < settings.numSamples; s += 3)
            {
                float progress = ((float) s) / settings.numSamples;
                EditorUtility.DisplayProgressBar("正在采样", "当前采样：" + s, progress);

                int spcount = Mathf.Min(settings.numSamples - s, 3);

                RenderTexture result0 = null;
                RenderTexture result1 = null;
                RenderTexture result2 = null;
                //RenderTexture result3 = null;

                if (spcount <= 0)
                    continue; 

                if (spcount > 0)
                    result0 = RaytracingScene(meshFilter, batches, sampler, raytracingMat, settings);
                if (spcount > 1)
                    result1 = RaytracingScene(meshFilter, batches, sampler, raytracingMat, settings);
                if (spcount > 2)
                    result2 = RaytracingScene(meshFilter, batches, sampler, raytracingMat, settings);
                //if (spcount > 3)
                //    result3 = RaytracingScene(meshFilter, batches, sampler, raytracingMat, settings);

                mixMat.SetTexture("_SampleResult0", result0);
                mixMat.SetTexture("_SampleResult1", result1);
                mixMat.SetTexture("_SampleResult2", result2);
                //mixMat.SetTexture("_SampleResult3", result3);
                mixMat.SetFloat("_ResultCount", spcount);

                RenderTexture tmpTex = RenderTexture.GetTemporary(settings.aoMapSize, settings.aoMapSize);
                Graphics.Blit(pre, tmpTex, mixMat);

                if (pre)
                    RenderTexture.ReleaseTemporary(pre);
                if (result0)
                    RenderTexture.ReleaseTemporary(result0);
                if (result1)
                    RenderTexture.ReleaseTemporary(result1);
                if (result2)
                    RenderTexture.ReleaseTemporary(result2);
                //if (result3)
                //    RenderTexture.ReleaseTemporary(result3);

                pre = tmpTex;
                
            }

            RenderTexture.active = tmp;


            //Material tmpMat = new Material(Shader.Find("Hidden/AOMix"));
            //RenderTexture tmpTex = RenderTexture.GetTemporary(settings.aoMapSize, settings.aoMapSize);
            //Graphics.Blit(pre, tmpTex, tmpMat);

            //Texture2D result = RenderTextureToTexture(tmpTex);

            //RenderTexture.ReleaseTemporary(tmpTex);
            //Object.DestroyImmediate(tmpMat);

            Texture2D result = RenderTextureToTexture(pre);

            if (pre)
                RenderTexture.ReleaseTemporary(pre);

            EditorUtility.ClearProgressBar();

            return result;
        }

        private static RenderTexture RaytracingScene(MeshFilter meshFilter, List<AOBakeBatch> batches, SamplerBase sampler, Material raytracingMat, BakeSettings settings)
        {
            Vector2 sp0 = sampler.Sample();
            Vector2 sp1 = sampler.Sample();
            Vector2 sp2 = sampler.Sample();

            raytracingMat.SetVector("_Sample0", sp0);
            raytracingMat.SetVector("_Sample1", sp1);
            raytracingMat.SetVector("_Sample2", sp2);

            RenderTexture pre = null;
            for (int i = 0; i < batches.Count; i ++)
            {
                RenderTexture cur = RenderTexture.GetTemporary(settings.aoMapSize, settings.aoMapSize);
                RenderTexture.active = cur;

                raytracingMat.SetTexture("_PreTex", pre);
                raytracingMat.SetVectorArray("_Vertices", batches[i].vertices);
                raytracingMat.SetFloat("_VertexCount", batches[i].vertexCount);

                raytracingMat.SetPass(0);
                Graphics.DrawMeshNow(meshFilter.sharedMesh, meshFilter.transform.localToWorldMatrix);

                if (pre)
                    RenderTexture.ReleaseTemporary(pre);

                pre = cur;
                
            }

            return pre;
        }

        private static SamplerBase CreateSampler(SamplerType samplerType, int numSamples, int numSets = 83)
        {
            SamplerBase sampler = null;
            switch (samplerType)
            {
                case SamplerType.Hammersley:
                    sampler = new HammersleySampler(numSamples, numSets);
                    break;
                case SamplerType.Jittered:
                    sampler = new JitteredSampler(numSamples, numSets);
                    break;
                case SamplerType.Random:
                    sampler = new RandomSampler(numSamples, numSets);
                    break;
                case SamplerType.Regular:
                    sampler = new RegularSampler(numSamples, numSets);
                    break;
                default:
                    sampler = new RegularSampler(numSamples, numSets);
                    break;
            }

            return sampler;
        }

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
