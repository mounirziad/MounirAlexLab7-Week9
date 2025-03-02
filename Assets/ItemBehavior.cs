using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ItemBehavior : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Check if the object that triggered is tagged as "Player"
        if (other.gameObject.tag == "Player")
        {
            Destroy(this.gameObject); // Destroy the item
            Debug.Log("Item collected!");
        }
    }
}