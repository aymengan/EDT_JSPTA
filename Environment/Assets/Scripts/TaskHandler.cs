using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;


public class TaskHandler : Agent
{

    new private Rigidbody rigidbody;


    [Tooltip("Main FAS GameObject Controller")]
    public CollabController controller;

    [Tooltip("Low Level Movement Agent")]
    public MovingAgent movingAgent;

    public int ID;

    [Header("Heuristics")]
    public bool manual=false;
    public bool SPT = false;
    public bool LPT = false;

    //Product vars
    public int last_action;

    [HideInInspector]
    public Vector2 task;
    

    //Dictionaries
    Workstation workstation;
    Delivery delivery;
    public Dictionary<int, Transform> inputLocationDictionary;
    public Dictionary<int, Transform> outputLocationDictionary;
    public Dictionary<Collider, Workstation> workstationColliderDictionary;
    public Dictionary<int, Transform> deliveryLocationDictionary;
    public Dictionary<Collider, Delivery> deliveryColliderDictionary;
    public Dictionary<int, Product> productDictionary;
    public Dictionary<int, TaskHandler> agentDictionary;
    public Dictionary<(int, int), float> stationDistances;


    // Product Grabbing
    [Header("Product")]
    public int job = -1;

    // Debug
    [Header("Debug")]
    public bool decided= false;


    private NavMeshPath path;
    [HideInInspector]
    public Vector3 destination;

    public override void Initialize()
    {
        //Initialize
        rigidbody = GetComponent<Rigidbody>();
        inputLocationDictionary = new Dictionary<int, Transform>();
        outputLocationDictionary = new Dictionary<int, Transform>();
        deliveryLocationDictionary = new Dictionary<int, Transform>();
        workstationColliderDictionary = new Dictionary<Collider, Workstation>();
        deliveryColliderDictionary = new Dictionary<Collider, Delivery>();
        productDictionary = new Dictionary<int, Product>();
        stationDistances = new Dictionary<(int, int), float>();
        agentDictionary = new Dictionary<int, TaskHandler>();
        job = -1;

        last_action = 0;

        //agent.isStopped = true;
        GetLocations(transform.parent);
        //UnityEngine.Random.InitState(20);

        
    }

    private void Update()
    {
        //Non-changeable task
        if (movingAgent.carrying)
        {
            if (movingAgent.grabbedID > 0)
            {
                //if (actionBuffers.DiscreteActions[0] != job) { DropProduct(); }
                var prod = productDictionary[movingAgent.grabbedID];
                destination = prod.GetDestination(movingAgent.grabbedID, inputLocationDictionary, deliveryLocationDictionary);
                            
            }
        }
        if (!movingAgent.carrying && !decided)
        {
            RequestDecision();
            controller.IncreaseStep();
        }
        ResetDecisions();
    }

    public Dictionary<(int, int), float> CalculateDistances()
    {
        Dictionary<(int, int), float> distances = new Dictionary<(int, int), float>();

        distances = movingAgent.CalculateDistances(inputLocationDictionary,outputLocationDictionary, deliveryLocationDictionary);
        return distances;
    }

    public override void OnEpisodeBegin()
    {
        movingAgent.carrying = false;
        decided = false;
        destination = new Vector3();
        //Debug.Log(init_pos);

    }

    /// <summary>
    /// Called when and action is received from either the player input or the neural network
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        
        int action = actionBuffers.DiscreteActions[0];
        if (!movingAgent.carrying)
        {
            if (action > 0)
            {
                //if (actionBuffers.DiscreteActions[0] != job) { DropProduct(); }
                var prod = productDictionary[action];
                job = action;
                destination = prod.GetDestination(job, inputLocationDictionary, deliveryLocationDictionary);
                
                if (!prod.grabbed || prod.processing) { decided = true; }
                

            }
            else
            {
                destination = movingAgent.init_pos;
            }
            last_action = action;
        }
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
            else if (child.CompareTag("agent"))
            {
                TaskHandler agent = child.gameObject.GetComponent<TaskHandler>();
                agentDictionary.Add(agent.ID, agent);
            }

        }
    }



    private void ResetDecisions()
    {
        if (movingAgent.grabbedID != job && productDictionary[job].grabbed && !productDictionary[job].processing)
        {
            decided = false;
        }
    }

   

}
