using UnityEditor;
using UnityEngine;

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
        public const string kAOBakeShader             = "Assets/Editor/AOBaker/BakeAO.shader";
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

    }

    class AOBakeUtils
    {
        public static Texture2D BakeScene(MeshRenderer target, bool staticOnly, bool ignoreSkinnedMeshRenderer, BakeSettings settings)
        {
            //准备阶段
            Shader bakeShader = AssetDatabase.LoadAssetAtPath<Shader>(AOBakeConstants.kAOBakeShader);
            if (!bakeShader)
            {
                Debug.LogError("Shader Missing! :" + AOBakeConstants.kAOBakeShader);
                return null;
            }

            return null;
        }
    }
}
