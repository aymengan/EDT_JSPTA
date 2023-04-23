using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;


public class JSSPAgent : Agent
{
    [Tooltip("NavMesh Agent")]
    public NavMeshAgent agent;

    new private Rigidbody rigidbody;

    [Tooltip("Location of Carried products.")]
    public Transform load;

    public int ID;

    //Product vars
    private Product product;
    private bool carrying;
    private Vector2 task;

    //Dictionaries
    Workstation workstation;
    Delivery delivery;
    private Dictionary<int, Transform> inputLocationDictionary;
    private Dictionary<int, Transform> outputLocationDictionary;
    private Dictionary<Collider, Workstation> workstationColliderDictionary;
    private Dictionary<int, Transform> deliveryLocationDictionary;
    private Dictionary<Collider, Delivery> deliveryColliderDictionary;
    private Dictionary<int, Product> productDictionary;
    private Dictionary<int, JSSPAgent> agentDictionary;
    private Dictionary<(int, int), float> stationDistances;

    //Initial Position & Rotation
    public Vector3 init_pos;
    public Quaternion init_rot;

    // Rewards
    private float episode_start;
    private float h =0 ;
    private float last_h=0;
    private float initial_h;

    // Product Grabbing
    public int job = -1;
    public int grabbedID;

    // Distances vars
    NavMeshPath path;

    // Debug
    [Header("Debug")]
    public List<float> lower_bounds;
    public int debbugProductID = 0;
    public float reward;
    public float cummulativeReward;
    public bool decided= false;

    public void Awake()
    {
        //Initialize
        carrying = false;
        rigidbody = GetComponent<Rigidbody>();
        inputLocationDictionary = new Dictionary<int, Transform>();
        outputLocationDictionary = new Dictionary<int, Transform>();
        deliveryLocationDictionary = new Dictionary<int, Transform>();
        workstationColliderDictionary = new Dictionary<Collider, Workstation>();
        deliveryColliderDictionary = new Dictionary<Collider, Delivery>();
        productDictionary = new Dictionary<int, Product>();
        stationDistances = new Dictionary<(int, int), float>();
        agentDictionary = new Dictionary<int, JSSPAgent>();
        job = -1;
        grabbedID = -1;
        path = new NavMeshPath();
        
        lower_bounds = new List<float>();

        //Get locations
        init_pos = transform.position;
        init_rot = transform.rotation;

        GetLocations(transform.parent);
        CalculateDistances();

        for(int i= 0; i< productDictionary[debbugProductID].GetJob().Count; i++)
        {
            lower_bounds.Add(0f);
        }

        // Get Initial Lower Bound
        GetInitialLB();
        initial_h = h;
        Debug.Log("Initial: " + initial_h);

    }

    public void GetInitialLB()
    {
        for (int i = 1; i < productDictionary.Count + 1; i++)
        {
            float lower_bound = 0;
            Product temp_p = productDictionary[i];
            int temp_ID = temp_p.product_ID;
            List<Vector2> product_job = temp_p.GetJob();
            int c_task = (int)temp_p.GetCurrentTask().x;

            if (temp_p.task_pointer < product_job.Count)
            {
                // Current Task
                float currentTaskTime = 0;
                
                /*
                //Time to product
                if (grabbedID != temp_p.product_ID)
                {
                    NavMesh.CalculatePath(transform.position, temp_p.transform.position, NavMesh.AllAreas, path);
                    for (int j = 0; j < path.corners.Length - 1; j++)
                    {
                        currentTaskTime += Vector3.Distance(path.corners[j], path.corners[j + 1]) / agent.speed;
                    }
                }
                */
                
                //Time of product to station
                if (c_task >= 0)
                {
                    NavMesh.CalculatePath(temp_p.transform.position, inputLocationDictionary[c_task].position, NavMesh.AllAreas, path);

                }
                else
                {
                    NavMesh.CalculatePath(temp_p.transform.position, deliveryLocationDictionary[c_task].position, NavMesh.AllAreas, path);
                }
                for (int j = 0; j < path.corners.Length - 1; j++)
                {
                    currentTaskTime += Vector3.Distance(path.corners[j], path.corners[j + 1]) / agent.speed;
                }

                lower_bound += currentTaskTime + product_job[temp_p.task_pointer].y;
                if (temp_ID == debbugProductID)
                {
                    lower_bounds[temp_p.task_pointer] = lower_bound;
                }
                
                h = Mathf.Max(h, lower_bound);
                float dist;

                for (int k = temp_p.task_pointer; k < product_job.Count - 1; k++)
                {
                    dist = stationDistances[((int)product_job[k].x, (int)product_job[k + 1].x)];;

                    lower_bound += dist / agent.speed + product_job[k + 1].y;
                    if (temp_ID == debbugProductID)
                    {
                        lower_bounds[k + 1] = lower_bound;
                    }
                    h = Mathf.Max(h, lower_bound);
                }
            }
        }

    }

    public void Update()
    {
        // Check if all jobs are finished and end episode
        //Debug.Log(StepCount);
        bool finished = true;
        for (int i = 1; i < productDictionary.Count + 1; i++) { finished = finished && productDictionary[i].finished; }
        if (finished)
        {
            Debug.Log("Makespan= " + h + " CR= " + cummulativeReward);
            
            EndEpisode();
        }

        //Debug.Log(StepCount);
        
        //Simple none changeable task
        if (carrying)
        {
            if (grabbedID > 0)
            {
                //if (actionBuffers.DiscreteActions[0] != job) { DropProduct(); }
                var prod = productDictionary[grabbedID];
                Vector3 destination = prod.GetDestination(grabbedID, inputLocationDictionary, deliveryLocationDictionary);
                destination.y = init_pos.y;
                agent.destination = destination;

            }
        }
        if (!carrying && !decided)
        {
            RequestDecision();
        }
    }

    private void CalculateDistances()
    {
        for(int i = 1; i< outputLocationDictionary.Count+1; i++)
        {
            for( int j = 1; j< inputLocationDictionary.Count+1; j++)
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
                    stationDistances.Add((i, j), distance);
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
                stationDistances.Add((i, -j), distance);
                //Debug.Log(distance);
            }

        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset Everything
        FASController fasController = transform.GetComponentInParent<FASController>();
        fasController.ResetScene();

        // Agent
        transform.position = init_pos;
        transform.rotation = init_rot;
        carrying = false;

        episode_start = Time.time;
        decided = false;
        reward = 0;
        cummulativeReward = 0;
        h = initial_h;
        last_h = h;

        // Multiple Agents
        for (int i=0; i<agentDictionary.Count; i++)
        {
            agentDictionary[i].transform.position = agentDictionary[i].init_pos;
            agentDictionary[i].transform.rotation = agentDictionary[i].init_rot;

        }
    }

    /// <summary>
    /// Called when and action is received from either the player input or the neural network
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        
        int action = actionBuffers.DiscreteActions[0];
        if (!carrying)
        {
            if (action > 0)
            {
                //if (actionBuffers.DiscreteActions[0] != job) { DropProduct(); }
                var prod = productDictionary[action];
                job = action;
                Vector3 destination = prod.GetDestination(job, inputLocationDictionary, deliveryLocationDictionary);
                destination.y = init_pos.y;
                agent.destination = destination;
                if (!prod.grabbed || prod.processing) { decided = true; }
                
            }
            else
            {
                agent.destination = transform.position;
            }
        }

        //Debug.Log(last_action);
        
        reward = last_h - h;
        //Debug.Log("Reward= " + reward);
        cummulativeReward += reward;
        AddReward(reward);
    }

    /// <summary>
    /// Collect vector observations from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        last_h = h;
        h = 0;

        //Debug.Log("Time: " + Time.time + " Episode_start: " + episode_start);
        //sensor.AddObservation(carrying);

        for (int i = 1; i< productDictionary.Count+1; i++)
        {
            float lower_bound = 0;
            Product temp_p = productDictionary[i];
            int temp_ID = temp_p.product_ID;
            List<Vector2> product_job = temp_p.GetJob();
            List<float> product_times = temp_p.GetTimes();
            int c_task = (int)temp_p.GetCurrentTask().x;

            //Add completed tasks observations
            for (int i2 = 0; i2 < product_times.Count; i2++)
            {
                if (product_times[i2] > 0)
                {
                    sensor.AddObservation(1);
                    sensor.AddObservation(product_times[i2]);
                    if (temp_ID == debbugProductID)
                    { lower_bounds[i2] = product_times[i2]; }
                    h = Mathf.Max(h, product_times[i2]);
                }
            }

            //Debug.Log("P: " + temp_p.task_pointer + " C: " + product_job.Count);
            if (temp_p.task_pointer < product_job.Count && !temp_p.finished)
            {
                lower_bound += Time.time - episode_start; // Time since episode start
                if (!temp_p.processing)
                {
                    // Current Task
                    float currentTaskTime = 0;
                    /*
                    //Time to product
                    if (grabbedID != temp_p.product_ID)
                    {
                        NavMesh.CalculatePath(transform.position, temp_p.transform.position, NavMesh.AllAreas, path);
                        for (int j = 0; j < path.corners.Length - 1; j++)
                        {
                            currentTaskTime += Vector3.Distance(path.corners[j], path.corners[j + 1]) / agent.speed;
                            //Debug.DrawLine(path.corners[j], path.corners[j + 1], Color.red, 10f, false);
                        }
                    }
                    */

                    //Time of product to station
                    if (c_task >= 0)
                    {
                        NavMesh.CalculatePath(temp_p.transform.position, inputLocationDictionary[c_task].position, NavMesh.AllAreas, path);

                    }
                    else
                    {
                        NavMesh.CalculatePath(temp_p.transform.position, deliveryLocationDictionary[c_task].position, NavMesh.AllAreas, path);
                    }
                    for (int j = 0; j < path.corners.Length - 1; j++)
                    {
                        currentTaskTime += Vector3.Distance(path.corners[j], path.corners[j + 1]) / agent.speed;
                        //Debug.DrawLine(path.corners[j], path.corners[j + 1], Color.red, 10f, false);
                    }

                    lower_bound += currentTaskTime + product_job[temp_p.task_pointer].y;
                    sensor.AddObservation(0);
                    sensor.AddObservation(lower_bound);
                    if (temp_ID == debbugProductID)
                    {
                        lower_bounds[temp_p.task_pointer] = lower_bound;
                    }
                } else
                {
                    lower_bound += product_job[temp_p.task_pointer].y - (Time.time - temp_p.process_start);
                    sensor.AddObservation(1);
                    sensor.AddObservation(lower_bound);
                    if (temp_ID == debbugProductID)
                    {
                        lower_bounds[temp_p.task_pointer] = lower_bound;
                    }
                                   
                }
                h = Mathf.Max(h, lower_bound);
                float dist;
                
                for (int k = temp_p.task_pointer; k < product_job.Count-1; k++)
                {
                    dist = stationDistances[((int)product_job[k].x, (int)product_job[k + 1].x)];
                    sensor.AddObservation(0);

                    lower_bound += dist / agent.speed + product_job[k+1].y;
                    if (temp_ID == debbugProductID)
                    {
                        lower_bounds[k + 1] = lower_bound;
                    }
                    sensor.AddObservation(lower_bound);
                    h = Mathf.Max(h, lower_bound);
                }
            }
        }
        
    }


    /// <summary>
    /// When Behavior Type is set to "Heuristic Only" on the agent's Behavior Parameters,
    /// this function will be called. Its return values will be fed into
    /// <see cref="OnActionReceived(float[])"/> instead of using the neural network
    /// </summary>
    /// <param name="actionsOut">And output action array</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.Alpha1))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.Alpha3))
        {
            discreteActionsOut[0] = 3;
        }
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
                    grabbedID = product.product_ID;
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
                    grabbedID = -1;
                    decided = false;
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
                    //AddReward(2.5f);
                    //collider.gameObject.SetActive(false); // Disables Workstation
                    product.PerformTask(Time.time, collider);
                    carrying = false;
                    grabbedID = -1;
                    decided = false;
                }
            }

            // Colliding with Delivery Station
            if (collider.CompareTag("d_input"))
            {
                if (task.x == -deliveryColliderDictionary[collider].ID)
                {
                    //AddReward(2.5f);
                    product.CompleteJob(collider);
                    carrying = false;
                    grabbedID = -1;
                    decided = false;
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
                    grabbedID = product.product_ID;
                    GrabProduct(product);
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
                Transform output = workstation.output_location;
                //Debug.Log("Workstation Read: "+workstation.workstation_ID);
                inputLocationDictionary.Add(workstation.ID, input);
                outputLocationDictionary.Add(workstation.ID, output);
                workstationColliderDictionary.Add(coll, workstation);
            }
            // Get Delivery Station
            if (child.CompareTag("delivery"))
            {
                delivery = child.gameObject.GetComponent<Delivery>();
                Transform input = delivery.input_location;
                Collider coll = delivery.input_collider;
                deliveryLocationDictionary.Add(-delivery.ID, input);
                deliveryColliderDictionary.Add(coll, delivery);
            }

            if (child.CompareTag("product"))
            {
                Product p = child.gameObject.GetComponent<Product>();
                productDictionary.Add(p.product_ID, p);
            }
            if (child.CompareTag("AGV"))
            {
                JSSPAgent a = child.gameObject.GetComponent<JSSPAgent>();
                agentDictionary.Add(a.ID, a);
            }
        }
    }

    private void GrabProduct(Product product)
    {
        product.SetPosition(load.position);
        carrying = true;
        product.SetParent(transform);
        task = product.GetCurrentTask();
        product.grabbed = true;
        grabbedID = product.product_ID;
        
    }

    private void ResetDecisions()
    {
        for (int i = 0; i < agentDictionary.Count; i++) {
            if (agentDictionary[i].grabbedID != agentDictionary[i].job && productDictionary[agentDictionary[i].job].grabbed && !productDictionary[agentDictionary[i].job].processing)
            {
                agentDictionary[i].decided= false;
            }

            //Debug.Log(agentDictionary[i].ID + " Reset: " + agentDictionary[i].grabbedID +" & " + agentDictionary[i].GetJob() + " Prod: " + productDictionary[agentDictionary[i].job].grabbed);
        }
    }

    public float GetJob()
    {
        return job;
    }
    private void DropProduct()
    {
        product = transform.GetComponentInChildren<Product>();
        if (product)
        {
            product.SetParent(product.og_parent);
            Vector3 temp = load.position;
            temp.y = product.og_position.y;
            product.SetPosition(temp);
            product.grabbed = false;
            carrying = false;
            grabbedID = -1;
        }
    }

}
