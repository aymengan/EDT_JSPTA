using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FASController : MonoBehaviour
{
    // Lists of all the elements within FAS
    private List<Transform> _children;
    public List<Product> products;
    public List<Workstation> workstations;
    public List<Delivery> deliverys;

    void Awake()
    {
        // Create lists
        _children = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.CompareTag("product"))
            {
                _children.Add(child);
                Product product = child.GetComponent<Product>();
                products.Add(product);
            }
            else if (child.CompareTag("workstation"))
            {
                _children.Add(child);
                Workstation workstation = child.GetComponent<Workstation>();
                workstations.Add(workstation);
            }
            else if (child.CompareTag("delivery"))
            {
                _children.Add(child);
                Delivery delivery = child.GetComponent<Delivery>();
                deliverys.Add(delivery);
            }

        }
    }

    // Reset all the scene
    public void ResetScene()
    {
        foreach(Transform child in _children)
        {
            // Reset Products
            if (child.CompareTag("product"))
            {
                Product temp_product = child.GetComponent<Product>();
                temp_product.EpisodeReset();
            }
            // Reset Workstation
            else if (child.CompareTag("workstation"))
            {
                Workstation temp_w = child.GetComponent<Workstation>();
                temp_w.EpisodeReset();
            }
        }
    }
}
