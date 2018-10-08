using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

#if UNITY_EDITOR

namespace ASL.AOBaker
{
    
    /// <summary>
    /// 烘焙设置
    /// </summary>
    [System.Serializable]
    public struct BakeSettings
    {
        /// <summary>
        /// 光线追踪有效半径
        /// </summary>
        public float traceRadius;
        /// <summary>
        /// 采样器类型
        /// </summary>
        public SamplerType samplerType;
        /// <summary>
        /// 采样次数
        /// </summary>
        public int numSamples;
        /// <summary>
        /// AO贴图尺寸
        /// </summary>
        public int aoMapSize;
        /// <summary>
        /// AO贴图UV边界扩展距离
        /// </summary>
        public int aoMapPadding;
        /// <summary>
        /// 是否裁剪背面
        /// </summary>
        public bool cullBack;
    }

    /// <summary>
    /// 烘焙批次顶点组（单个模型的顶点分组传入shader）
    /// </summary>
    [System.Serializable]
    public class AOBakeBatchVertexGroup
    {
        public int vertexCount
        {
            get { return m_VertexCount; }
        }

        public Vector4[] vertices
        {
            get { return m_Vertices; }
        }

        public bool isFull
        {
            get
            {
                if (m_VertexCount == m_Vertices.Length)
                    return true;
                return false;
            }
        }

        [SerializeField] private Vector4[] m_Vertices;
        [SerializeField] private int m_VertexCount;

        public AOBakeBatchVertexGroup()
        {
            m_Vertices = new Vector4[999];
            m_VertexCount = 0;
        }

        public void AddTriangle(Vector4 v0, Vector4 v1, Vector4 v2)
        {
            m_Vertices[m_VertexCount] = v0;
            m_Vertices[m_VertexCount + 1] = v1;
            m_Vertices[m_VertexCount + 2] = v2;

            m_VertexCount += 3;
        }
    }

    /// <summary>
    /// 烘焙光追批次
    /// </summary>
    [System.Serializable]
    public class AOBakeBatch
    {
        
        /// <summary>
        /// 当前对象的包围盒（用于判断是否和目标发生碰撞）
        /// </summary>
        public Bounds bounds
        {
            get
            {
                Renderer renderer = m_Target.GetComponent<Renderer>();
                if (renderer)
                    return renderer.bounds;
                return default(Bounds);
            }
        }

        /// <summary>
        /// 当前对象的LoaclToWorld
        /// 顶点数组中存放Mesh的局部坐标，这样当拾取完场景中的mesh信息后可以确保物体发生位置变化时不需要重新拾取
        /// </summary>
        public Matrix4x4 localToWorld
        {
            get { return m_Target.transform.localToWorldMatrix; }
        }
        /// <summary>
        /// 顶点组
        /// </summary>
        public List<AOBakeBatchVertexGroup> vertexGroups
        {
            get { return m_VertexGroups; }
        }
        
        [SerializeField] private GameObject m_Target;
        [SerializeField] private List<AOBakeBatchVertexGroup> m_VertexGroups;

        private AOBakeBatch(Mesh mesh, GameObject gameObject)
        {
            m_Target = gameObject;
            m_VertexGroups = new List<AOBakeBatchVertexGroup>();

            Vector3[] vertices = mesh.vertices;
            int subMeshCount = mesh.subMeshCount;

            AOBakeBatchVertexGroup group = new AOBakeBatchVertexGroup();

            for (int s = 0; s < subMeshCount; s++)
            {
                var triangles = mesh.GetTriangles(s);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector4 p0 = vertices[triangles[i]];
                    Vector4 p1 = vertices[triangles[i+1]];
                    Vector4 p2 = vertices[triangles[i+2]];

                    p0.w = 1.0f;
                    p1.w = 1.0f;
                    p2.w = 1.0f;

                    group.AddTriangle(p0, p1, p2);
                    if (group.isFull)
                    {
                        m_VertexGroups.Add(group);
                        group = new AOBakeBatchVertexGroup();
                    }
                }
            }

            if (group.vertexCount > 0)
                m_VertexGroups.Add(group);

        }

        public static AOBakeBatch CreateBatch(GameObject gameObject)
        {
            Mesh mesh = null;
            MeshFilter mf = gameObject.GetComponent<MeshFilter>();
            mesh = mf ? mf.sharedMesh : null;
            if (!mf)
            {
                SkinnedMeshRenderer skr = gameObject.GetComponent<SkinnedMeshRenderer>();
                mesh = skr ? skr.sharedMesh : null;
            }

            if (!mesh)
                return null;

            return new AOBakeBatch(mesh, gameObject);
        }
    }

    /// <summary>
    /// AO渲染相机
    /// </summary>
    [ExecuteInEditMode]
    public class AOBakeCamera : MonoBehaviour
    {
        private CommandBuffer m_CommandBuffer;
        private Material m_BakeMaterial;
        private Material m_MixMaterial;

        private Camera m_Camera;

        private RenderTexture m_PreTex;//该RT用来累加混合所有的采样结果

        //sample0-sample2记录最近三次采样的结果
        private RenderTexture m_Sample0;
        private RenderTexture m_Sample1;
        private RenderTexture m_Sample2;

        private List<AOBakeBatch> m_Batches;

        private bool m_IsBeginRender = false;
        private int m_CurrentBatchIndex = 0;
        private AOBakeBatch m_CurrentBatch;
        private int m_CurrentBatchVertexGroup = 0;
        private int m_CurrentSampleIndex = 0;
        private int m_CurrentSampleStep = 0;

        private SamplerBase m_Sampler;

        private BakeSettings m_Settings;

        private MeshFilter m_MeshFilter;

        private UnityAction<float> m_ProgressCallBack;


        void OnDestroy()
        {
            if (m_BakeMaterial)
                DestroyImmediate(m_BakeMaterial);
            if (m_MixMaterial)
                DestroyImmediate(m_MixMaterial);
            if (m_CommandBuffer != null)
            {
                if (m_Camera)
                    m_Camera.RemoveCommandBuffer(CameraEvent.AfterImageEffectsOpaque, m_CommandBuffer);
                m_CommandBuffer.Release();
            }
            
            if (m_Sample0)
                RenderTexture.ReleaseTemporary(m_Sample0);
            if (m_Sample1)
                RenderTexture.ReleaseTemporary(m_Sample1);
            if (m_Sample2)
                RenderTexture.ReleaseTemporary(m_Sample2);
        }

        public void Init(Shader bakeShader, Shader mixShader, BakeSettings settings)
        {
            m_Camera = GetComponent<Camera>();

            m_CommandBuffer = new CommandBuffer();
            m_Camera.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, m_CommandBuffer);

            m_BakeMaterial = new Material(bakeShader);
            m_MixMaterial = new Material(mixShader);

            float weight = 1.0f / (settings.numSamples * 3);
            m_MixMaterial.SetFloat("_MixFactor", weight);
            m_BakeMaterial.SetFloat("_TraceRadius", settings.traceRadius);
            m_BakeMaterial.SetFloat("_CullBack", settings.cullBack ? 1.0f : 0.0f);

            m_PreTex = RenderTexture.GetTemporary(settings.aoMapSize, settings.aoMapSize);
            m_PreTex.DiscardContents(true, true);

            m_Settings = settings;

            m_Sampler = CreateSampler(settings.samplerType, settings.numSamples);
        }

        public RenderTexture Render(List<AOBakeBatch> batches, MeshRenderer target, UnityAction<float> progressCallBack)
        {
            m_Batches = new List<AOBakeBatch>();
            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i].bounds.Intersects(target.bounds))
                    m_Batches.Add(batches[i]);
            }

            m_IsBeginRender = true;
            m_CurrentBatchIndex = 0;
            m_CurrentBatchVertexGroup = 0;
            m_CurrentSampleIndex = 0;
            m_CurrentSampleStep = 0;

            m_ProgressCallBack = progressCallBack;

            m_MeshFilter = target.GetComponent<MeshFilter>();

            Sample();

            while (m_IsBeginRender)
            {
                m_Camera.Render();
            }

            return m_PreTex;
        }

        void OnPostRender()
        {
            if (m_IsBeginRender)
            {
                float process = ((float) m_CurrentSampleIndex) / m_Settings.numSamples;
                if (m_ProgressCallBack != null)
                    m_ProgressCallBack(process);

                var batch = m_Batches[m_CurrentBatchIndex];
                var group = batch.vertexGroups[m_CurrentBatchVertexGroup];

                RenderTexture pre = null;
                RenderTexture rt = null;

                //m_Sample0-m_Sample1表示三次采样结果，当顶点数量较多无法单次完成渲染时会分批次渲染，并以3次一组进行结果混合
                if (m_CurrentSampleStep == 0)
                    rt = m_Sample0;
                else if (m_CurrentSampleStep == 1)
                    rt = m_Sample1;
                else if (m_CurrentSampleStep == 2)
                    rt = m_Sample2;
                if (rt)
                {
                    pre = RenderTexture.GetTemporary(m_Settings.aoMapSize, m_Settings.aoMapSize);
                    Graphics.Blit(rt, pre);
                }
                else
                {
                    rt = RenderTexture.GetTemporary(m_Settings.aoMapSize, m_Settings.aoMapSize);
                    if (m_CurrentSampleStep == 0)
                        m_Sample0 = rt;
                    else if (m_CurrentSampleStep == 1)
                        m_Sample1 = rt;
                    else if (m_CurrentSampleStep == 2)
                        m_Sample2 = rt;
                }

                //执行光线追踪
                Raytracing(batch, group, rt, pre);

                if (pre)
                    RenderTexture.ReleaseTemporary(pre);

                NextBatch();
            }
        }

        /// <summary>
        /// 准备下一批次
        /// </summary>
        private void NextBatch()
        {
            var batch = m_Batches[m_CurrentBatchIndex];
            m_CurrentBatchVertexGroup += 1;

            if (m_CurrentBatchVertexGroup >= batch.vertexGroups.Count) //如果当前批次的所有顶点组渲染完毕则跳转到下一批次
            {
                m_CurrentBatchVertexGroup = 0;
                m_CurrentBatchIndex += 1;



                if (m_CurrentBatchIndex >= m_Batches.Count)//如果所有批次渲染结束则进行下一采样点渲染
                {
                    m_CurrentBatchIndex = 0;
                    m_CurrentSampleStep += 1;

                    //所有批次结束则重新采样并进行下一采样点渲染
                    Sample();

                    int spcount = Mathf.Min(m_Settings.numSamples - m_CurrentSampleIndex, 3);
                    if (m_CurrentSampleStep >= spcount)//如果渲染结果已经达到三次（或者不足三次但已经渲染所有采样点），则将最近的渲染结果进行混合
                    {
                        m_CurrentSampleStep = 0;

                        //将最近的三次结果传入混合shader进行结果混合
                        m_MixMaterial.SetTexture("_SampleResult0", m_Sample0);
                        m_MixMaterial.SetTexture("_SampleResult1", m_Sample1);
                        m_MixMaterial.SetTexture("_SampleResult2", m_Sample2);
                        m_MixMaterial.SetFloat("_ResultCount", spcount);

                        RenderTexture tmpTex = RenderTexture.GetTemporary(m_Settings.aoMapSize, m_Settings.aoMapSize);
                        Graphics.Blit(m_PreTex, tmpTex, m_MixMaterial, 0); //m_PreTex记录并积累了之前所有的光追结果
                        if (m_PreTex)
                            RenderTexture.ReleaseTemporary(m_PreTex);
                        if (m_Sample0)
                            RenderTexture.ReleaseTemporary(m_Sample0);
                        if (m_Sample1)
                            RenderTexture.ReleaseTemporary(m_Sample1);
                        if (m_Sample2)
                            RenderTexture.ReleaseTemporary(m_Sample2);
                        m_Sample0 = null;
                        m_Sample1 = null;
                        m_Sample2 = null;
                        m_PreTex = tmpTex;

                        m_CurrentSampleIndex += 3;

                        if (m_CurrentSampleIndex >= m_Settings.numSamples)//采样结束
                        {
                            
                            ApplyPadding();

                            m_CurrentSampleIndex = 0;
                            m_IsBeginRender = false;
                        }
                    }
                }
                
            }
        }

        /// <summary>
        /// 执行光纤追踪
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="vertexGroup"></param>
        /// <param name="renderTarget"></param>
        /// <param name="preTex"></param>
        private void Raytracing(AOBakeBatch batch, AOBakeBatchVertexGroup vertexGroup, RenderTexture renderTarget, RenderTexture preTex)
        {
            m_BakeMaterial.SetTexture("_PreTex", preTex);//传入之前的结果，确保每次渲染都是基于之前的结果
            m_BakeMaterial.SetVectorArray("_Vertices", vertexGroup.vertices);
            m_BakeMaterial.SetFloat("_VertexCount", vertexGroup.vertexCount);
            if (m_CurrentBatch != batch)
            {
                m_CurrentBatch = batch;
                m_BakeMaterial.SetMatrix("_LocalToWorld", batch.localToWorld);
            }


            m_CommandBuffer.Clear();
            m_CommandBuffer.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

            m_CommandBuffer.SetRenderTarget(renderTarget);

            m_CommandBuffer.DrawMesh(m_MeshFilter.sharedMesh, m_MeshFilter.transform.localToWorldMatrix, m_BakeMaterial, 0, 0);
        }

        private void Sample()
        {
            Vector2 sp0 = m_Sampler.Sample();
            Vector2 sp1 = m_Sampler.Sample();
            Vector2 sp2 = m_Sampler.Sample();

            m_BakeMaterial.SetVector("_Sample0", sp0);
            m_BakeMaterial.SetVector("_Sample1", sp1);
            m_BakeMaterial.SetVector("_Sample2", sp2);
        }

        /// <summary>
        /// uv边界扩展
        /// </summary>
        private void ApplyPadding()
        {
            if (m_Settings.aoMapPadding <= 0)
                return;
            for (int i = 0; i < m_Settings.aoMapPadding; i++)
            {
                RenderTexture padding = RenderTexture.GetTemporary(m_Settings.aoMapSize, m_Settings.aoMapSize);
                Graphics.Blit(m_PreTex, padding, m_MixMaterial, 1);
                if (m_PreTex)
                    RenderTexture.ReleaseTemporary(m_PreTex);
                m_PreTex = padding;
            }
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
    }
}
#endif