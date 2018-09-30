using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HamTes : MonoBehaviour
{
    public float e;

    private Vector3[] m_Dirs;

    private float m_E;

	
	void Start ()
	{

	    GenerateDirs();

        m_E = e;
	}

    private void GenerateDirs()
    {
        m_Dirs = new Vector3[100];

        for (int i = 0; i < m_Dirs.Length; i++)
        {
            float x = Random.Range(0.0f, 1.0f);
            float y = Random.Range(0.0f, 1.0f);

            float cos_phi = Mathf.Cos(2.0f * Mathf.PI * x);
            float sin_phi = Mathf.Sin(2.0f * Mathf.PI * x);
            float cos_theta = Mathf.Pow(1.0f - y, 1.0f/(e + 1.0f));
            float sin_theta = Mathf.Sqrt(1.0f - cos_theta*cos_theta);
            float pu = sin_theta*cos_phi;
            float pv = sin_theta*sin_phi;
            float pw = cos_theta;

            m_Dirs[i] = new Vector3(pu, pv, pw);
        }
    }
	
	void Update () {
	    if (m_E != e)
	    {
            m_E = e;
	        GenerateDirs();
	    }
	}

    void OnDrawGizmos()
    {
        if (m_Dirs == null)
            return;
        Gizmos.color = Color.green;
        for (int i = 0; i < m_Dirs.Length; i++)
        {
            //float x = m_Dirs[i].x;
            //float y = m_Dirs[i].y;
            //float z = m_Dirs[i].z;
            Vector3 dir = transform.right * m_Dirs[i].x + transform.up * m_Dirs[i].y + transform.forward * m_Dirs[i].z;

            Gizmos.DrawSphere(transform.position + dir, 0.02f);
        }
    }
}
