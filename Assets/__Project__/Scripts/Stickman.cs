using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stickman : MonoBehaviour
{
    public Transform stickman;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CollectableStickman"))
        {
            Destroy(other.gameObject);
            transform.localScale += new Vector3(2,2,2);
        }
    }
}
