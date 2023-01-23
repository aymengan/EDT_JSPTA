using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Inspector script to test FAS
public class Inspector : MonoBehaviour
{
    GameObject gameobject;

    new private Rigidbody rigidbody;

    public float speed = 2f;
    public Transform load;

    private Product product;
    private bool carrying;
    private Vector2 task;

    Workstation workstation;
    Delivery delivery;
    private Dictionary<int, Transform> locationDictionary;
    private Dictionary<Collider, Workstation> workstationColliderDictionary;
    private Dictionary<Collider, Delivery> deliveryColliderDictionary;


    private void Awake()
    {
        carrying = false;
        gameobject = transform.gameObject;
        rigidbody = GetComponent<Rigidbody>();
        locationDictionary = new Dictionary<int, Transform>();
        workstationColliderDictionary = new Dictionary<Collider, Workstation>();
        deliveryColliderDictionary = new Dictionary<Collider, Delivery>();
        GetLocations(transform.parent);
        Debug.Log("ESto corre");
    }


    // Update is called once per frame
    void Update()
    {
        Vector3 forward = Vector3.zero;
        Vector3 right = Vector3.zero;

        // Forward/backward
        if (Input.GetKey(KeyCode.W))
        {
            forward = transform.forward * speed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            forward -= transform.forward * speed;
        }

        // Left/right
        if (Input.GetKey(KeyCode.A))
        {
            right = -transform.right * speed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            right += transform.right * speed;
        }

        Vector3 combined = (forward + right);
        rigidbody.velocity = combined;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider collider = collision.collider;
        if (!carrying)
        {
            if (collider.CompareTag("product"))
            {
                product = collider.gameObject.GetComponent<Product>();
                GrabProduct(product);

            }
            if (collider.CompareTag("table"))
            {
                TemporalStation temp = collider.transform.GetComponent<TemporalStation>();
                if (temp.full)
                {
                    product = temp.GetProduct();
                    GrabProduct(product);
                    temp.full = false;
                    carrying = true;
                }

            }
        }
        else
        {
            if (collider.CompareTag("table"))
            {
                TemporalStation temp = collider.transform.GetComponent<TemporalStation>();

                if (!temp.full)
                {
                    temp.full = true;
                    product.SetinTable(collider.transform);
                    carrying = false;
                }
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void TriggerEnterOrStay(Collider collider)
    {
        //While carrying a product
        if (carrying)
        {
            // Colliding with Workstations
            if (collider.CompareTag("w_input"))
            {
                if (task.x == workstationColliderDictionary[collider].ID)
                {
                    //collider.gameObject.SetActive(false); // Disables Workstation
                    product.PerformTask(Time.realtimeSinceStartup, collider);
                    carrying = false; 
                }
            }

            // Colliding with Delivery Station
            if (collider.CompareTag("d_input"))
            {
                if (task.x == -deliveryColliderDictionary[collider].ID)
                {
                    product.CompleteJob(collider);
                    carrying = false;
                    //gameobject.SetActive(false);
                }
            }
        }
        

    }

    private void GetLocations(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            // Get Workstation Locations
            Transform child = parent.GetChild(i);

            if (child.CompareTag("workstation"))
            {
                workstation = child.gameObject.GetComponent<Workstation>();
                Transform input = workstation.input_location;
                Collider coll = workstation.input_collider;

                //Debug.Log("Workstation Read: "+workstation.workstation_ID);
                locationDictionary.Add(workstation.ID, input);
                workstationColliderDictionary.Add(coll, workstation);
            }
            // Get Delivery Station
            if (child.CompareTag("delivery"))
            {
                delivery = child.gameObject.GetComponent<Delivery>();
                Transform input = delivery.input_location;
                Collider coll = delivery.input_collider;
                locationDictionary.Add(-delivery.ID, input);
                deliveryColliderDictionary.Add(coll, delivery);

            }
        }
    }

    private void GrabProduct(Product product)
    {

        product.SetPosition(load.position);
        carrying = true;
        product.SetParent(transform);
        task = product.GetCurrentTask();

    }
}
