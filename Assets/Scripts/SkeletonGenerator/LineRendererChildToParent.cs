using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRendererChildToParent : MonoBehaviour {
    public float m_zPos = -0.001f;
    LineRenderer m_lineRenderer;
    void Start()
    {
        m_lineRenderer = GetComponent<LineRenderer>();
        if (transform.parent.GetComponent<LineRendererChildToParent>() == false)
        {
            GetComponent<LineRenderer>().enabled = false;
            this.enabled = false;
        }
    }

	// Update is called once per frame
	void Update () {
        TrackParent();
	}

    void TrackParent()
    {
        m_lineRenderer.SetPosition(0, new Vector3(transform.position.x, transform.position.y, transform.position.z + m_zPos));
        m_lineRenderer.SetPosition(1, new Vector3(transform.parent.position.x, transform.parent.position.y, transform.parent.position.z + m_zPos));
    }
}
