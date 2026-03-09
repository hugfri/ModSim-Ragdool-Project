using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class rocketFiring : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject rocket;
    [SerializeField] private GameObject rocketPosition;

    [SerializeField] private GameObject rocketProp;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Fire Rocket");
            Instantiate(rocket, rocketPosition.transform.position, rocketPosition.transform.rotation);
            rocketProp.SetActive(false);
        }
        
    }
}
