using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRendererChildToParent : MonoBehaviour {

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
        m_lineRenderer.SetPosition(0, transform.position);
        m_lineRenderer.SetPosition(1, transform.parent.position);
    }
}
