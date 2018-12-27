using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParalaxManager : MonoBehaviour {
    public Transform[] backgrounds1;
    public Transform[] backgrounds2; //Used to make paralax infinite
    public float[] speedScales;


    private Transform[] currentBackgrounds;
    private Transform[] nextBackgrounds;
    private Transform cam;
    private Vector3 lastCamPos;    
    
    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
        currentBackgrounds = backgrounds1;
        nextBackgrounds = backgrounds2;
    }

    void Update()
    {
        for (int i = 0; i < currentBackgrounds.Length; i++)
        {
            UpdateParalaxPosition(i, speedScales[i]);
        }
        lastCamPos = cam.position;
    }

    void UpdateParalaxPosition(int index, float speedScale)
    {
        Transform current = currentBackgrounds[index];
        float xDiff = (cam.position - lastCamPos).x;
        float moveBy = xDiff * speedScale;

        Transform next = nextBackgrounds[index];
        float xDiffFromCam = cam.position.x - current.position.x;
        float xDiffBetweenParalax = current.position.x - next.position.x;
        float xDistBetweenParalax = Mathf.Abs(xDiffBetweenParalax);

        current.position = new Vector3(current.position.x + moveBy, current.position.y, current.position.z);
        if (xDiffFromCam > 0)
            next.position = current.position + new Vector3(xDistBetweenParalax, 0, 0);
        else
            next.position = current.position - new Vector3(xDistBetweenParalax, 0, 0);
       
        if (Mathf.Abs(xDiffFromCam) > xDistBetweenParalax / 2f)
        {
            currentBackgrounds[index] = next;
            nextBackgrounds[index] = current;
        }
    }
}
