using System.Collections.Generic;
using UnityEngine;

namespace ASL.AOBaker
{
    /// <summary>
    /// 采样器类型
    /// </summary>
    public enum SamplerType
    {
        /// <summary>
        /// 随机采样
        /// </summary>
        Random,
        /// <summary>
        /// 抖动采样
        /// </summary>
        Jittered,
        Hammersley,
        /// <summary>
        /// 规则采样
        /// </summary>
        Regular,
    }

    /// <summary>
    /// 采样器基类
    /// </summary>
    abstract class SamplerBase
    {
        protected int m_NumSamples;
        protected int m_NumSets;
        private int m_Index = 0;
        private int m_Jump = 0;

        private int[] m_ShuffledIndices;

        protected Vector2[] m_Samples;

        private static System.Random sRandom = new System.Random();

        public SamplerBase(int numSamples, int numSets = 83)
        {
            InitSampler(numSamples, numSets);

            m_ShuffledIndices = new int[m_NumSets * m_NumSamples];

            SetupShuffledIndices();
        }

        protected abstract void InitSampler(int numSamples, int numSets);

        public Vector2 Sample()
        {
            if ((int) (m_Index % m_NumSamples) == 0)
            {
                m_Jump = Random.Range(0, m_NumSets - 1) * m_NumSamples;
            }

            Vector2 sp = m_Samples[m_Jump + m_ShuffledIndices[m_Jump + m_Index % m_NumSamples]];
            m_Index += 1;
            return sp;
        }

        private void SetupShuffledIndices()
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < m_NumSamples; i++)
                indices.Add(i);

            m_ShuffledIndices = new int[m_NumSamples * m_NumSets];
            for (int i = 0; i < m_NumSets; i++)
            {
                Shuffle(indices);
                for (int j = 0; j < m_NumSamples; j++)
                {
                    int ok = i * m_NumSamples + j;
                    if (ok >= m_ShuffledIndices.Length)
                        Debug.Log(ok + "," + m_ShuffledIndices.Length);
                    if(j>=indices.Count)
                        Debug.Log(j);
                    m_ShuffledIndices[i * m_NumSamples + j] = indices[j];
                }
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = sRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    class RandomSampler : SamplerBase
    {
        public RandomSampler(int numSamples, int numSets = 83) : base(numSamples, numSets)
        {
        }

        protected override void InitSampler(int numSamples, int numSets)
        {
            m_NumSamples = numSamples;
            m_NumSets = numSets;
            m_Samples = new Vector2[m_NumSets * m_NumSamples];

            for (int i = 0; i < numSets; i++)
            {
                for (int j = 0; j < numSamples; j++)
                {
                    m_Samples[i * numSamples + j] =
                        new Vector2(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
                }
            }
        }
    }

    class JitteredSampler : SamplerBase
    {
        public JitteredSampler(int numSamples, int numSets = 83) : base(numSamples, numSets)
        {
        }

        protected override void InitSampler(int numSamples, int numSets)
        {
            int n = (int)Mathf.Sqrt(numSamples);
            m_NumSamples = n * n;
            m_NumSets = numSets;
            m_Samples = new Vector2[m_NumSets * m_NumSamples];
            int index = 0;
            for (int i = 0; i < numSets; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        Vector2 sp = new Vector2((k + Random.Range(0.0f, 1.0f)) / n, (j + Random.Range(0.0f, 1.0f)) / n);
                        m_Samples[index] = sp;
                        index += 1;
                    }
                }
            }
        }
    }

    class HammersleySampler : SamplerBase
    {
        public HammersleySampler(int numSamples, int numSets = 83) : base(numSamples, numSets)
        {
        }

        protected override void InitSampler(int numSamples, int numSets)
        {
            m_NumSamples = numSamples;
            m_NumSets = numSets;
            m_Samples = new Vector2[m_NumSets * m_NumSamples];
            for (int i = 0; i < numSets; i++)
            {
                for (int j = 0; j < numSamples; j++)
                {
                    m_Samples[i * numSamples + j] =
                        new Vector2(((float)j) / numSamples, Phi(j));
                }
            }
        }

        private float Phi(int j)
        {
            float x = 0.0f;
            float f = 0.5f;
            while (((int)j)>0)
            {
                x += f * (j % 2);
                j = j / 2;
                f *= 0.5f;
            }

            return x;
        }
    }

    class RegularSampler : SamplerBase
    {
        public RegularSampler(int numSamples, int numSets = 83) : base(numSamples, numSets)
        {
            int n = (int) Mathf.Sqrt(numSamples);
            int index = 0;
            for (int i = 0; i < numSets; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        m_Samples[index] =
                            new Vector2((0.5f + k) / n, (0.5f + j) / n);
                        index += 1;
                    }
                }
            }
        }

        protected override void InitSampler(int numSamples, int numSets)
        {
            int n = (int)Mathf.Sqrt(numSamples);
            m_NumSamples = n * n;
            m_NumSets = numSets;
            m_Samples = new Vector2[m_NumSets * m_NumSamples];
            int index = 0;
            for (int i = 0; i < numSets; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        m_Samples[index] =
                            new Vector2((0.5f + k) / n, (0.5f + j) / n);
                        index += 1;
                    }
                }
            }
        }
    }
}
