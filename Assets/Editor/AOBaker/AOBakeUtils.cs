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

    class AOBakeUtils
    {
        public static Texture2D BakeScene(MeshRenderer target, bool staticOnly, bool ignoreSkinnedMeshRenderer, BakeSettings settings)
        {
            return null;
        }
    }
}
