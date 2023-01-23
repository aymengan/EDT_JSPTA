using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingCamera : MonoBehaviour
{
    public float Cameraspeed;
    public float Camerarotspeed;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * Cameraspeed;
        }
        if (Input.GetKey(KeyCode.A))
        { 
            Vector3 temp = new Vector3(0, 1, 0);
            transform.eulerAngles -= temp* Camerarotspeed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            Vector3 temp = new Vector3(0, 1, 0);
            transform.eulerAngles += temp* Camerarotspeed;
        }
        if (Input.GetKey(KeyCode.Z))
        {
            Vector3 temp = new Vector3(1, 0, 0);
            transform.eulerAngles += temp * Camerarotspeed;
        }
        if (Input.GetKey(KeyCode.X))
        {
            Vector3 temp = new Vector3(1, 0, 0);
            transform.eulerAngles -= temp * Camerarotspeed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position += -transform.forward * Cameraspeed;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            transform.position += transform.up * Cameraspeed;
        }
        if (Input.GetKey(KeyCode.Minus))
        {
            transform.position += -transform.up * Cameraspeed;
        }

    }
}

