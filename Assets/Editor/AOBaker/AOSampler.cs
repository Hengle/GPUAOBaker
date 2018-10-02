using UnityEngine;
using UnityEditor;

namespace ASL.AOBaker
{
    enum SamplerType
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
        NRooks,
        /// <summary>
        /// 规则采样
        /// </summary>
        Regular,
    }

    abstract class SamplerBase
    {
    }
}
