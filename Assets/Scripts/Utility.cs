using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utility  {


    public static Vector2 PolarToCartesian(float distance, float angle)
    {
        return new Vector2(distance * Mathf.Cos(angle), distance * Mathf.Sin(angle));
    }
}
