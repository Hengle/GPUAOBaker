using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastTest : MonoBehaviour
{
    public MeshFilter mesh;

    private Vector3 m_Hit0;
    private Vector3 m_Hit1;
    private Vector3 m_Hit2;

    private bool m_Hit;
    
	void Start () {
		
	}
	
	void Update () {

	    if (Input.GetMouseButtonDown(0))
	    {
	        m_Hit = false;
	        Mesh m = mesh.sharedMesh;
	        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
	        float t = Mathf.Infinity;

	        for (int i = 0; i < m.triangles.Length; i += 3)
	        {
	            int i0 = m.triangles[i];
	            int i1 = m.triangles[i + 1];
	            int i2 = m.triangles[i + 2];

	            Vector3 p0 = mesh.transform.localToWorldMatrix.MultiplyPoint(m.vertices[i0]);
	            Vector3 p1 = mesh.transform.localToWorldMatrix.MultiplyPoint(m.vertices[i1]);
	            Vector3 p2 = mesh.transform.localToWorldMatrix.MultiplyPoint(m.vertices[i2]);

	            float tmp = 0;
	            if (RaycastTriangle(ray, p0, p1, p2, ref tmp))
	            {
	                if (tmp < t)
	                {
	                    t = tmp;
	                    m_Hit = true;
	                    m_Hit0 = p0;
	                    m_Hit1 = p1;
	                    m_Hit2 = p2;

                        Debug.Log("col");
                    }
	            }
	        }
	    }
	}

    void OnDrawGizmos()
    {
        if (m_Hit)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawLine(m_Hit0, m_Hit1);
            Gizmos.DrawLine(m_Hit1, m_Hit2);
            Gizmos.DrawLine(m_Hit2, m_Hit0);
        }
    }

    private static bool RaycastTriangle(Ray ray, Vector3 p0, Vector3 p1, Vector3 p2, ref float rt)
    {

        Vector3 e1 = p1 - p0;
        Vector3 e2 = p2 - p0;

        float v = 0;
        float u = 0;

        Vector3 n = Vector3.Cross(e1, e2);
        float ndv = Vector3.Dot(ray.direction, n);
        if (ndv > 0)
        {
            return false;
        }

        Vector3 p = Vector3.Cross(ray.direction, e2);

        float det = Vector3.Dot(e1, p);
        Vector3 t = default(Vector3);
        if (det > 0)
        {
            t = ray.origin - p0;
        }
        else
        {
            t = p0 - ray.origin;
            det = -det;
        }
        if (det < 0.0000001f)
        {
            return false;
        }

        u = Vector3.Dot(t, p);
        if (u < 0.0f || u > det)
            return false;

        Vector3 q = Vector3.Cross(t, e1);

        v = Vector3.Dot(ray.direction, q);
        if (v < 0.0f || u + v > det)
            return false;

        rt = Vector3.Dot(e2, q);

        float finvdet = 1.0f / det;
        rt *= finvdet;
        if (rt < 0.001f)
            return false;
        //u *= finvdet;
        //v *= finvdet;

        return true;
    }
}
