using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaldAnim : MonoBehaviour
{
    [Range(0, 1)]
    public float forwardSpeed;
    public bool moveForward = false;
    [Range(0, 2)]
    public float animSpeed;
    private float initSpeed, initFSpeed;
    private Animation anim;
    bool inPlay = true;

    void Start()
    {
        anim = GetComponent<Animation>();  //get animation component
        anim["EagleB_Loop"].speed = animSpeed;  //play animation
        initSpeed = animSpeed;
        initFSpeed = forwardSpeed;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.R)) resetAnim(); //restart

        if (Input.GetKeyDown(KeyCode.P)) setPlay(); //play - pause
        
        if (Input.GetKeyDown(KeyCode.Space)) // move forward
        {
            moveForward = !moveForward;
            GameObject.Find("Rotator").GetComponent<RotateCamera>().pauseRotation = true;
        }

    // moving fordward to exit
        if (moveForward)
        {
            transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);
            forwardSpeed += 0.008f;
        }
    
        
    // animation speed change
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            animSpeed += 0.1f;
            speedAnim();
        }
            
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            animSpeed -= 0.1f;
            speedAnim();
        }
    }

// change speed animation
    void speedAnim ()
    {
        anim["EagleB_Loop"].speed = animSpeed;
    }


// ply - pause animation
    void setPlay()
    {
        speedAnim();
        if (!inPlay) { GetComponent<Animation>().Play("EagleB_Loop"); }
        else { GetComponent<Animation>()["EagleB_Loop"].speed = 0; }
        inPlay = !inPlay;
    }

// reset
    void resetAnim()
    {
        moveForward = false;
        forwardSpeed = initFSpeed;
        GetComponent<Animation>().Stop("EagleB_Loop");
        GetComponent<Animation>().Play("EagleB_Loop");
        animSpeed = initSpeed;
        anim["EagleB_Loop"].speed = animSpeed;
    }
}