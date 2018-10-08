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
        public const int kDefaultNumSamples = 25;
        public const int kMinNumSamples = 3;
        public const int kMaxNumSamples = 128;
        public const int kDefaultAOMapSize = 1024;
        public const int kDefaultAOMapPadding = 2;
        public const int kMinAOMapPadding = 1;
        public const int kMaxAOMapPadding = 10;
        public const bool kDefaultCullBack = true;
    }


    class AOBakeUtils
    {
        /// <summary>
        /// 拾取场景物体建立渲染批次
        /// </summary>
        /// <param name="staticOnly"></param>
        /// <param name="ignoreSkinnedMeshRenderer"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 烘焙场景
        /// </summary>
        /// <param name="target"></param>
        /// <param name="batches"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
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
            var result = script.Render(batches, target, RaytracingCallBack);

            Texture2D texture = null;
            if (result)
            {
                texture = RenderTextureToTexture(result);
                RenderTexture.ReleaseTemporary(result);
            }

            Object.DestroyImmediate(cam.gameObject);

            EditorUtility.ClearProgressBar();

            return texture;
        }

        private static void RaytracingCallBack(float progress)
        {
            int percent = (int)(progress * 100);
            EditorUtility.DisplayProgressBar("正在采样", "当前采样进度:" + percent + "%", progress);
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
