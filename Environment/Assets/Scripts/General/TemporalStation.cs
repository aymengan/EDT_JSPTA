using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemporalStation : MonoBehaviour
{
    [Tooltip("Carrying Location")]
    public Transform load;

    [HideInInspector]
    public bool full = false;

    Product product;

    // Get Product to grab
    public Product GetProduct()
    {
        for (int i = 0; i < transform.childCount; i++)
        {

            Transform child = transform.GetChild(i);
            if (child.CompareTag("product"))
            {
                product = child.GetComponent<Product>();
                
            }
        }
        full = false;
        return product;
    }


    // Episode Reset Function
    public void EpisodeReset()
    {
        full = false;
    }
}
