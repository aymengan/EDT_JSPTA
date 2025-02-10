using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;


public class JSSPMultiAgent : Agent
{
    [Tooltip("NavMesh Agent")]
    public NavMeshAgent agent;

    new private Rigidbody rigidbody;

    [Tooltip("Location of Carried products.")]
    public Transform load;

    [Tooltip("Main FAS GameObject Controller")]
    public MultiAgentController controller;
    
    [Tooltip("Central FAS information")]
    public FASinfo info;

    public int ID;

    [Header("Solvers")]
    private bool inference = false;
    private bool manual = false;
    private bool SPT = false;
    private bool LPT = false;
    public bool custom = false;
    private System.Array customSolution;
    private int stacks;
    private bool lockedStacks= false;
    private bool advpointer = false;

    //Product vars
    [Header("Debug")]
    public int last_action;
    private Product product;
    public bool carrying;
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
    private Dictionary<int, JSSPMultiAgent> agentDictionary;
    private Dictionary<(int, int), float> stationDistances;

    //Initial Position & Rotation
    [HideInInspector]
    public Vector3 init_pos;
    [HideInInspector]
    public Quaternion init_rot;

    // Product Grabbing
    [Header("Product")]
    public int job = -1;
    public int grabbedID;

    // Distances vars
    NavMeshPath path;

    // Debug
    [Header("Debug")]
    public bool decided= false;
    public Collider debug_collider;
    public GameObject Dot;

    //NavMesh Train Fix
    [Header("NavMesh Fix")]
    private Vector3 destination;
    private bool trainingMode;

    // Schedule object
    public Schedule schedule;

    public void Start()
    {
        inference = schedule.inference;
        manual = schedule.manual;
        SPT = schedule.SPT;
        LPT = schedule.LPT;
        
    }

    public override void Initialize()
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
        agentDictionary = new Dictionary<int, JSSPMultiAgent>();
        job = -1;
        CheckforProduct();
        path = new NavMeshPath();
        

        //Get locations
        init_pos = transform.position;
        init_rot = transform.rotation;
        last_action = 0;

        destination = init_pos;
        customSolution= info.customSolution;
        trainingMode = schedule.trainingMode;

        if (trainingMode)
        {
            agent.updatePosition = false;
        }
        else
        {
            agent.updatePosition = true;
        }
        
        GetLocations(transform.parent);
        //UnityEngine.Random.InitState(20);
        
    }

    public void CheckforProduct()
    {
        product = transform.GetComponentInChildren<Product>();
        if (product)
        {
            grabbedID = product.product_ID;
            carrying = true;
            if (advpointer)
            {
                controller.AdvanceCustomPointer(stacks+1);
                advpointer = false;
            }
            
        }else
        {
            grabbedID = -1;
            carrying = false;
        }
    }

    private void Update()
    {
        NavMeshHit hit;
        float maxAgentTravelDistance = Time.deltaTime * agent.speed;

        //Debug.Log(maxAgentTravelDistance);
        if (trainingMode)
        {
            if (!agent.SamplePathPosition(NavMesh.AllAreas, maxAgentTravelDistance, out hit))
            {
                Vector3 new_pos = new Vector3();
                new_pos = hit.position;
                new_pos.y = init_pos.y;
                transform.position = new_pos;
                agent.nextPosition = transform.position;
            }
            else
            {
                agent.SamplePathPosition(NavMesh.AllAreas, agent.remainingDistance, out hit);
                Vector3 new_pos = new Vector3();
                new_pos = hit.position;
                new_pos.y = init_pos.y;
                transform.position = new_pos;
                agent.nextPosition = transform.position;
            }
        }
        CheckforProduct();
        //Non-changeable task
        if (carrying)
        {
            if (grabbedID > 0)
            {
                //if (actionBuffers.DiscreteActions[0] != job) { DropProduct(); }
                var prod = productDictionary[grabbedID];
                destination = prod.GetDestination(grabbedID, inputLocationDictionary, deliveryLocationDictionary);
                destination.y = init_pos.y;
                agent.destination = destination;
            }
        }

        if (!carrying && !decided)
        {
            RequestDecision();
            controller.IncreaseStep();
        }
        ResetDecisions();

        if (Dot && Time.captureDeltaTime%10000==0 ) { Instantiate(Dot, transform.position, Quaternion.identity); }

    }


    public Dictionary<(int, int), float> CalculateDistances()
    {
        Dictionary<(int, int), float> distances = new Dictionary<(int, int), float>();
        for (int i = 1; i< outputLocationDictionary.Count+1; i++)
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
        carrying = false;
        decided = false;
        agent.ResetPath();
        transform.position = init_pos;
        agent.nextPosition = init_pos;
        agent.destination = init_pos;
        destination = new Vector3();
        //Debug.Log(init_pos);

    }
    
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (schedule.masking)
        {
            actionMask.SetActionEnabled(0, 0, true);
            if (carrying)
            {
                for (int i = 1; i < productDictionary.Count; i++)
                {
                    product = productDictionary[i];
                    if (product.product_ID != grabbedID)
                    {
                        actionMask.SetActionEnabled(0, i, false);
                    }
                }
                actionMask.SetActionEnabled(0, grabbedID, true);
            }
            else
            {
                for (int i = 1; i < productDictionary.Count; i++)
                {
                    product = productDictionary[i];
                    if (product.finished || (product.grabbed && !product.processing))
                    {
                        actionMask.SetActionEnabled(0, i, false);
                    }
                    else
                    {
                        actionMask.SetActionEnabled(0, i, true);
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < productDictionary.Count; i++)
            {
                actionMask.SetActionEnabled(0, i, true);
            }
        }
    }
    


    /// <summary>
    /// Called when and action is received from either the player input or the neural network
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        
        int action = actionBuffers.DiscreteActions[0];
        if (action > 0)
        {
            //if (actionBuffers.DiscreteActions[0] != job) { DropProduct(); }
            var prod = productDictionary[action];
            job = action;
            destination = prod.GetDestination(job, inputLocationDictionary, deliveryLocationDictionary);
            destination.y = init_pos.y;

            if (!schedule.masking)
            {
                if (!prod.grabbed || prod.processing)
                {
                    decided = true;
                }
            }
            else
            {
                decided = true;
            }

            //AddReward(-1f);
            // FIX 001
            agent.destination = destination;
        }
        else
        {
            agent.destination = init_pos;
        }
        last_action = action;
    }

    /// <summary>
    /// Collect vector observations from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        List<float> observations = new List<float>();
        observations = controller.GetLBObservations(ID, transform.position);

        for(int i = 0; i < observations.Count; i++)
        {
            sensor.AddObservation(observations[i]);
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
        //User controlled 
        if (manual)
        {
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
            if (Input.GetKey(KeyCode.Alpha4))
            {
                discreteActionsOut[0] = 4;
            }
            if (Input.GetKey(KeyCode.Alpha5))
            {
                discreteActionsOut[0] = 5;
            }
            if (Input.GetKey(KeyCode.Alpha6))
            {
                discreteActionsOut[0] = 6;
            }
            if (Input.GetKey(KeyCode.Alpha7))
            {
                discreteActionsOut[0] = 7;
            }
            if (Input.GetKey(KeyCode.Alpha8))
            {
                discreteActionsOut[0] = 8;
            }
            if (Input.GetKey(KeyCode.Alpha9))
            {
                discreteActionsOut[0] = 9;
            }
        }

        //Shortest Processing Time PDR
        if (SPT) 
        {
            float current_time = -1;
            Product p;
            int chosen = 0;
            for (int i = 0; i < productDictionary.Count; i++)
            {
                p = productDictionary[i + 1];
                // Check if the product is being worked on
                if (!p.grabbed && !p.assigned)
                {
                    if (p.GetCurrentTask().x > 0)
                    { // Workstation Task
                        if (current_time < 0 && inputLocationDictionary[(int)p.GetCurrentTask().x].gameObject.activeSelf)
                        {
                            current_time = p.GetCurrentTask().y;
                            chosen = p.product_ID;
                        }
                        else if (p.GetCurrentTask().y <= current_time && inputLocationDictionary[(int)p.GetCurrentTask().x].gameObject.activeSelf)
                        {
                            current_time = p.GetCurrentTask().y;
                            chosen = p.product_ID;
                        }
                    }
                    else //Delivery Task
                    {
                        if (current_time < 0)
                        {
                            current_time = p.GetCurrentTask().y;
                            chosen = p.product_ID;
                        }
                        else if (p.GetCurrentTask().y <= current_time)
                        {
                            current_time = p.GetCurrentTask().y;
                            chosen = p.product_ID;
                        }
                    }
                }
            }
            //Debug.Log(inputLocationDictionary[(int)current_task].gameObject.activeSelf);
            if (chosen > 0)
            {
                p = productDictionary[chosen];
                if (p.assigned)
                {
                    chosen = 0;
                }
                else
                {
                    p.assigned = true;
                }
            }

            discreteActionsOut[0] = chosen;
        }

        // Longest Processing Time PDR
        if (LPT)
        {
            float current_time = -1;
            Product p;
            int chosen = 0;
            for (int i = 0; i < productDictionary.Count; i++)
            {
                p = productDictionary[i + 1];
                // Check if the product is being worked on
                if (!p.grabbed && !p.assigned)
                {
                    if (p.GetCurrentTask().x > 0)
                    { // Workstation Task
                        if (current_time < 0 && inputLocationDictionary[(int)p.GetCurrentTask().x].gameObject.activeSelf)
                        {
                            current_time = p.GetCurrentTask().y;
                            chosen = p.product_ID;
                        }
                        else if (p.GetCurrentTask().y >= current_time && inputLocationDictionary[(int)p.GetCurrentTask().x].gameObject.activeSelf)
                        {
                            current_time = p.GetCurrentTask().y;
                            chosen = p.product_ID;
                        }
                    }
                    else //Delivery Task
                    {
                        if (current_time < 0)
                        {
                            current_time = p.GetCurrentTask().y;
                            chosen = p.product_ID;
                        }
                        else if (p.GetCurrentTask().y >= current_time)
                        {
                            current_time = p.GetCurrentTask().y;
                            chosen = p.product_ID;
                        }
                    }
                }
            }
            //Debug.Log(inputLocationDictionary[(int)current_task].gameObject.activeSelf);
            if (chosen > 0)
            {
                p = productDictionary[chosen];
                if (p.assigned)
                {
                    chosen = 0;
                }
                else
                {
                    p.assigned = true;
                }
            }

            discreteActionsOut[0] = chosen;
        }

        //google OR Tools
        if(custom){
            if (lockedStacks)
            {
                discreteActionsOut[0] = last_action;
                stacks--;
                if (stacks <= 0)
                {
                    lockedStacks = false;
                }
            }
            else
            {
                (discreteActionsOut[0], stacks) = controller.GetCustomSolution();
                if (stacks > 0)
                {
                    lockedStacks = true;
                }
                advpointer = true;
            }
            //Debug.Log("Agent: " + ID +", Action:" + last_action);
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider collider = collision.collider;

        //Penalize AGV collisions
        if (collider.CompareTag("AGV"))
        {
            //AddReward(-.1f);
        }

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
                    debug_collider = collider;
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
            else if (child.CompareTag("delivery"))
            {
                delivery = child.gameObject.GetComponent<Delivery>();
                Transform input = delivery.input_location;
                Collider coll = delivery.input_collider;
                deliveryLocationDictionary.Add(-delivery.ID, input);
                deliveryColliderDictionary.Add(coll, delivery);
            }

            else if (child.CompareTag("product"))
            {
                Product p = child.gameObject.GetComponent<Product>();
                productDictionary.Add(p.product_ID, p);
            }
            else if (child.CompareTag("AGV"))
            {
                JSSPMultiAgent agent = child.gameObject.GetComponent<JSSPMultiAgent>();
                agentDictionary.Add(agent.ID, agent);
            }

        }
    }

    private void GrabProduct(Product product)
    {
        product.SetPosition(load.position);
        product.SetParent(transform);
        task = product.GetCurrentTask();
        product.grabbed = true;
        grabbedID = product.product_ID;
    }

    private void ResetDecisions()
    {
        if (grabbedID != job && productDictionary[job].grabbed && !productDictionary[job].processing)
        {
            decided = false;
        }
        else if (job > 0 && productDictionary[job].finished)
        {
            decided = false;
        }
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
        }
    }

}
