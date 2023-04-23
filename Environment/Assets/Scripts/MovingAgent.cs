using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;


public class MovingAgent : Agent
{
    public int ID;

    //Initial Position & Rotation
    [HideInInspector]
    public Vector3 init_pos;
    [HideInInspector]
    public Quaternion init_rot;


    new private Rigidbody rigidbody;

    [Header("Product")]
    public int job = -1;
    public int grabbedID;
    public bool carrying;

    NavMeshPath path;
    [Tooltip("Location of Carried products.")]
    public Transform load;

    public TaskHandler taskHandler;
    private Product product;
    public float speed;

    public override void Initialize()
    {
        //Get locations
        carrying = false;
        init_pos = transform.position;
        init_rot = transform.rotation;
        path = new NavMeshPath();
        rigidbody = transform.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        CheckforProduct();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
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

    public void CheckforProduct()
    {
        Product product = transform.GetComponentInChildren<Product>();
        if (product)
        {
            grabbedID = product.product_ID;
            carrying = true;
        }
        else
        {
            grabbedID = -1;
            carrying = false;
        }
    }

    public Dictionary<(int, int), float> CalculateDistances(Dictionary<int, Transform> inputLocationDictionary, Dictionary<int, Transform> outputLocationDictionary, Dictionary<int, Transform> deliveryLocationDictionary)
    {
        Dictionary<(int, int), float> distances = new Dictionary<(int, int), float>();

        for (int i = 1; i < outputLocationDictionary.Count + 1; i++)
        {
            for (int j = 1; j < inputLocationDictionary.Count + 1; j++)
            {
                if (i != j)
                {
                    float distance = 0;
                    NavMesh.CalculatePath(outputLocationDictionary[i].position, inputLocationDictionary[j].position, NavMesh.AllAreas, path);
                    for (int k = 0; k < path.corners.Length - 1; k++)
                    {
                        distance += Vector3.Distance(path.corners[k], path.corners[k + 1]);
                        Debug.DrawLine(path.corners[k], path.corners[k + 1], Color.red, 10f, false);
                    }
                    distances.Add((i, j), distance);
                    //Debug.Log(distance);
                }
            }

            for (int j = 1; j < deliveryLocationDictionary.Count + 1; j++)
            {
                float distance = 0;
                NavMesh.CalculatePath(outputLocationDictionary[i].position, deliveryLocationDictionary[-j].position, NavMesh.AllAreas, path);
                for (int k = 0; k < path.corners.Length - 1; k++)
                {
                    distance += Vector3.Distance(path.corners[k], path.corners[k + 1]);
                    Debug.DrawLine(path.corners[k], path.corners[k + 1], Color.red, 10f, false);
                }
                distances.Add((i, -j), distance);
                //Debug.Log(distance);
            }

        }
        return distances;
    }

    public override void OnEpisodeBegin()
    {
        transform.rotation = init_rot;
        transform.position = init_pos;
        carrying = false;
    }



    private void GrabProduct(Product product)
    {
        product.SetPosition(load.position);
        product.SetParent(transform);
        taskHandler.task = product.GetCurrentTask();
        product.grabbed = true;
        grabbedID = product.product_ID;

    }


    private void OnCollisionEnter(Collision collision)
    {
        Collider collider = collision.collider;


        if (!carrying)
        {
            if (collider.CompareTag("product"))
            {
                product = collider.gameObject.GetComponent<Product>();
                if (product.product_ID == job)
                {
                    GrabProduct(product);
                }
            }
            if (collider.CompareTag("table"))
            {
                TemporalStation temp = collider.transform.GetComponent<TemporalStation>();
                if (temp.full)
                {
                    product = temp.GetProduct();
                    GrabProduct(product);
                    temp.full = false;
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
                    taskHandler.decided = false;
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
                if (taskHandler.task.x == taskHandler.workstationColliderDictionary[collider].ID)
                {
                    //AddReward(2.5f);
                    //collider.gameObject.SetActive(false); // Disables Workstation
                    product.PerformTask(Time.time, collider);
                    taskHandler.decided = false;
                }
            }

            // Colliding with Delivery Station
            if (collider.CompareTag("d_input"))
            {
                if (taskHandler.task.x == -taskHandler.deliveryColliderDictionary[collider].ID)
                {
                    //AddReward(2.5f);
                    product.CompleteJob(collider);
                    taskHandler.decided = false;
                    //gameobject.SetActive(false);
                }
            }
        }
        else
        {
            if (collider.CompareTag("product"))
            {
                product = collider.gameObject.GetComponent<Product>();
                if (product.product_ID == job)
                {
                    GrabProduct(product);
                }
            }
        }
    }


}
