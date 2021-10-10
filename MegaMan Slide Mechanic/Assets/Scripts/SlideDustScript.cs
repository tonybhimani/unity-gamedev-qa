using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideDustScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // destroy at end of animation
        Destroy(gameObject, 0.375f);
    }
}