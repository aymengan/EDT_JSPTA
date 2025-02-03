using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FASController : MonoBehaviour
{
    // Lists of all the elements within FAS
    private List<Transform> childs;
    public List<Product> products;
    public List<TemporalStation> tables;
    public List<Workstation> workstations;
    public List<Delivery> deliverys;

    void Awake()
    {
        // Create lists
        childs = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.CompareTag("product"))
            {
                childs.Add(child);
                Product product = child.GetComponent<Product>();
                products.Add(product);
            }
            else if (child.CompareTag("table"))
            {
                childs.Add(child);
                TemporalStation table = child.GetComponent<TemporalStation>();
                tables.Add(table);
            }
            else if (child.CompareTag("workstation"))
            {
                childs.Add(child);
                Workstation workstation = child.GetComponent<Workstation>();
                workstations.Add(workstation);
            }
            else if (child.CompareTag("delivery"))
            {
                childs.Add(child);
                Delivery delivery = child.GetComponent<Delivery>();
                deliverys.Add(delivery);
            }

        }
    }
}
