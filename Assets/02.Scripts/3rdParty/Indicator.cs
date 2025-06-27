using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    [SerializeField] Transform Target;
    public float RotateSpeed;
    void Start()
    {
        
    }

   
    void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Target.position-transform.position), RotateSpeed*Time.deltaTime);
    }
}
