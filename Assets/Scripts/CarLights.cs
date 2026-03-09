using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CarLights : MonoBehaviour
{
    public enum Side
    {
        Front,
        Back
    }

    [System.Serializable]
    public struct Light
    {
        public GameObject lightObj;
        public  Material lightMat;
        public Side side;
    }

    public bool isFrontLightsOn;
    [SerializeField] private bool frontLightsAlwaysOn = true;
    public bool isBackLightsOn;


    public List<Light> lights; 

    void Start()
    {
        isFrontLightsOn = true;
        OperateFrontLights();
    }

    public void OperateFrontLights() // This function can also be done the same way as OperateBackLights
    {
        if (frontLightsAlwaysOn)
        {
            isFrontLightsOn = true;

            foreach (var light in lights)
            {
                if (light.side == Side.Front && !light.lightObj.activeInHierarchy)
                {
                    light.lightObj.SetActive(true);
                }
            }

            return;
        }

    }

    public void OperateBackLights()
    {
        if(isBackLightsOn)
      {
        // Turn on lights
        foreach (var light in lights)
        {
            if(light.side == Side.Back && light.lightObj.activeInHierarchy == false)
            {
                light.lightObj.SetActive(true);
            }
        }
      }
      else
        {
            foreach (var light in lights)
            {
                if(light.side == Side.Back && light.lightObj.activeInHierarchy == true)
                {
                    light.lightObj.SetActive(false);
                }

            }
        }
    }
}
