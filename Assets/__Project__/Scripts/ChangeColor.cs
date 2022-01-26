using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GreenStickman"))
        {
            CompareTag("OrangeStickman");
            Debug.Log("oldu");
        }
    }
}
