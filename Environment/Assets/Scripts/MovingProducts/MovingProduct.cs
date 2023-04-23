using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingProduct : MonoBehaviour
{
    // Start is called before the first frame update
    ProductMP product;

    GameObject gameobject;

    private Dictionary<int, Transform> workstationDictionary;
    private Dictionary<Collider, Workstation> workstationColliderDictionary;

    Workstation workstation;
    Delivery delivery;
    Collider deliveryCollider;

    private Vector2 task;
    private int task_pointer;

    public float walkSpeed;
    private Rigidbody rb;
    private Vector3 dirToGo;
    private Transform target;
    private float og_walkSpeed;

    public Collider agv_collider;

    // Finishing process trigger
    private bool trigger_1 =  true;
    private Collider used_w;

    // Delivery station count
    private int ds_count = 0;

    private void Awake()
    {
        //job = new List<List<float>>();
        gameobject = transform.gameObject;
        product = GetComponent<ProductMP>();
        rb = GetComponent<Rigidbody>();
        og_walkSpeed = walkSpeed;
        used_w = new Collider();

        workstationDictionary = new Dictionary<int, Transform>(); 
        workstationColliderDictionary = new Dictionary<Collider, Workstation>();

        GetLocations(transform.parent);

        Debug.Log("Product ID: " + product.product_ID);


        for (int i = 0; i < workstationDictionary.Count-ds_count; i++)
        {
            Debug.Log("Loaded Workstation: " + workstationDictionary[i+1]);
        }
        for (int i = 1; i < ds_count+1; i++)
        {
            Debug.Log("Loaded Delivery Station: " + workstationDictionary[-i]);
        }

    }

    void Start()
    {
        task = product.GetCurrentTask();
        target = workstationDictionary[(int)task[0]];
    }


    void Update()
    {
        dirToGo = target.position - transform.position;
        dirToGo.y = 0;
        //Debug.Log(dirToGo.magnitude);
        if (dirToGo.magnitude > 0.2)
        {
            rb.rotation = Quaternion.LookRotation(dirToGo);
            rb.MovePosition(transform.position + transform.forward * walkSpeed * Time.deltaTime);
        }

        //Debug.Log(trigger_1);
        if (product.processing) 
        {
            trigger_1 = false;
            //Debug.Log("Processing");
            return;
        }
        //agv_collider.gameObject.SetActive(true);
        //Next Task
        
        if (!trigger_1)
        {
            task = product.GetCurrentTask();
            //Debug.Log("Product: " + product.product_ID+ " In task: " + task[0]);
            target = workstationDictionary[(int)task[0]];
            walkSpeed = og_walkSpeed;
            agv_collider.gameObject.SetActive(true);
            trigger_1 = true;
            used_w.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Called when the agent's collider stays in a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }




    //Search Workstation and Delivery Locations in parent
    private void GetLocations(Transform parent)
    { 
        for(int i = 0;i < parent.childCount; i++)
        {
            // Get Workstation Locations
            Transform child = parent.GetChild(i);

            if (child.CompareTag("workstation"))
            {
                workstation = child.gameObject.GetComponent<Workstation>();
                Transform input = workstation.input_location;
                Collider coll = workstation.input_collider;

                //Debug.Log("Workstation Read: "+workstation.workstation_ID);
                workstationDictionary.Add(workstation.ID, input);
                workstationColliderDictionary.Add(coll, workstation);
            }
            // Get Delivery Station
            if (child.CompareTag("delivery"))
            {
                ds_count++;
                delivery = child.gameObject.GetComponent<Delivery>();
                Transform input = delivery.input_location;
                deliveryCollider = delivery.input_collider;
                workstationDictionary.Add(-delivery.ID, input);
                
            }
        }
    }

    private void TriggerEnterOrStay(Collider collider)
    {
        // Colliding with Workstations
        if (collider.CompareTag("w_input"))
        {
            if (task.x == workstationColliderDictionary[collider].ID)
            {
                //collider.gameObject.SetActive(false);
                DoTask(workstationColliderDictionary[collider].output_location, collider);
                used_w = collider;
            }
        }

        // Colliding with Delivery Station
        if (collider.CompareTag("d_input"))
        {
            if (task[0] < 0)
            {
                product.CompleteJob(); 
                gameobject.SetActive(false);
            }
        }

    }

    private void DoTask(Transform output, Collider w_collider)
    {
        //Disable Colliders

        target = output;
        
        walkSpeed = 10;
        agv_collider.gameObject.SetActive(false);
        

        product.PerformTask(Time.realtimeSinceStartup, w_collider);

        

    }
}
