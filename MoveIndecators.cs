using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveIndicator : MonoBehaviour
{
    // Update is called once per frame
    public float rotationSpeed = 1f;
    void Update()
    {
        transform.Rotate(0f, rotationSpeed, 0f, Space.World);
    }
}
