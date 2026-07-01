using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCamera : MonoBehaviour
{
    public float speed;
    float initSpeed;
    float initFOV;
    bool isRotating;
    public bool pauseRotation;

    Vector3 initPos;
    Quaternion initRot;

    void Start()
    {
    // get camera initial position, rotation and FOV    
        initSpeed = speed;
        initPos = transform.position;
        initRot = transform.rotation;
        initFOV = Camera.main.fieldOfView;
        isRotating = true; //set rotation flag
        pauseRotation = false;
    }
    void Update()
    {
    // continuos camera rotation
    transform.Rotate(0, -speed * Time.deltaTime, 0);

     if (pauseRotation && speed > 0)
        {
            speed -= 0.05f;
        }
        
        // pause rotation
        if (Input.GetKeyDown(KeyCode.P))
        {
            isRotating = !isRotating;
            if (!isRotating)
            {
                speed = 0.0f;
            }
           else
            {
                speed = initSpeed;
            }
        }
        
    // reset scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position = initPos;
            transform.rotation = initRot;
            speed = initSpeed;
            pauseRotation = false;
         }
    
        // quit application
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
