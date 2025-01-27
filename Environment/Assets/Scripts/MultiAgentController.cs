using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using UnityEngine.AI;
using System.Text;
using System.IO;
using System;

public class MultiAgentController : MonoBehaviour
{
    // List of all the elements within FAS
    private List<Transform> childs;
    
    // Refined dictionaries for all the elements of a specific type within FAS
    
    // We map the id of the workstation to the script workstation object, attached to the workstation game object
    // ID is in {1, ..., n} where n is the number of workstations
    private Dictionary<int, Workstation> workstationDictionary;
    
    // ID is in {1, ..., n} where n is the number of workstations
    // We map the id of the workstation to the {input, output} location of the workstation
    private Dictionary<int, Transform> inputLocationDictionary;
    private Dictionary<int, Transform> outputLocationDictionary;
    
    // We map the id of the workstation to the delivery location
    // ID is in {-1, ..., -n} where n is the number of delivery stations, only tested for exactly one delivery station
    private Dictionary<int, Transform> deliveryLocationDictionary;
    
    // We map the id of the product to the script product object, attached to the product game object
    private Dictionary<int, Product> productDictionary;
    
    private Dictionary<int, List<float>> JSSPAnswer;

    // List of all agent (=AGV) scripts attached to each agent game object, defining the RL behavior
    public List<JSSPMultiAgent> agents;
    
    // The AGVgroup encompassing all agents
    private SimpleMultiAgentGroup AGVgroup;
    
    // Dictionary containing the distances between all pairs of workstations,
    // from the output collider to the next input collider
    private Dictionary<(int, int), float> stationDistances;
    
    public GameObject scheduleObject;
    private Schedule schedule;

    [Tooltip("Maximum steps for every episode.")]
    public int maxEpisodeSteps;
    // Tracks how close we are to maxEpisodeSteps
    private int reset_timer;
    // The maximal speed of the AGVs to move
    public float speed = 2f;


    //[Header("Logging")]
    // For debugging purposes to show the internal state in the Unity editor during runtime (if public), or in the Debugger
    private List<float> lower_bounds;
    private int debbugProductID = 1;
    private float reward = 0;
    private List<float> inputs;
    private int episode = 0;
    
    // Whether to generate .csv's for the makespan values and the generated JSSP instances 
    [Header("Logging")]
    public bool printJobs;
    public bool printMakespan;

    // Rewards
    private float episode_start;
    private float h = 0;
    private float last_h = 0;
    private float initial_h;

    //Result Logs
    private float bestMakespan =-1;

    private StatsRecorder statsRecorder;
    
    // Counts how often we request a decision, i.e. trigger the policy NN
    private int trainingStep;
    // Responsible for logging the makespan files to the file
    private StreamWriter makespanWriter;

    //Custom Solution
    private int cspointer = 0;
    private System.Array customSolution;

    void Awake()
    {
        // Initialize
        episode = 0;
        childs = new List<Transform>();
        workstationDictionary = new Dictionary<int, Workstation>();
        inputLocationDictionary = new Dictionary<int, Transform>();
        outputLocationDictionary = new Dictionary<int, Transform>();
        deliveryLocationDictionary = new Dictionary<int, Transform>();
        productDictionary = new Dictionary<int, Product>();
        JSSPAnswer = new Dictionary<int, List<float>>();

        agents = new List<JSSPMultiAgent>();
        stationDistances = new Dictionary<(int, int), float>();
        AGVgroup = new SimpleMultiAgentGroup();
        lower_bounds = new List<float>();
        inputs = new List<float>();
        statsRecorder = Academy.Instance.StatsRecorder;
        if (scheduleObject) { schedule = scheduleObject.transform.GetComponent<Schedule>(); }
        trainingStep = 0;

        
    }

    public void PrintJobsCSV()
    {
        int jobSeed = transform.GetComponent<FASinfo>().jobSeed;
        int machineSeed = transform.GetComponent<FASinfo>().machineSeed;
        StreamWriter writer = new StreamWriter(Application.dataPath + "/Data/" + "Jobs-" + jobSeed + "-" + machineSeed+".csv");


        for (int i = 0; i < productDictionary.Count; i++)
        {
            string line= "J" + productDictionary[i + 1].product_ID + ", ";
            List<Vector2> temp_job = productDictionary[i + 1].GetJob();
            for (int j = 0; j < temp_job.Count-1; j++)
            {
                line += temp_job[j].x + ", " + temp_job[j].y + ", ";
            }
            writer.WriteLine(line);

        }
        writer.Flush();
        writer.Close();
    }
    public void IncreaseStep()
    {
        trainingStep++;
        statsRecorder.Add("TrainingStep", trainingStep, StatAggregationMethod.MostRecent);
    }

    public void Start()
    {
        customSolution = transform.GetComponent<FASinfo>().customSolution;

        CollectChilds();
        for (int i = 0; i < productDictionary[debbugProductID].GetJob().Count; i++)
        {
            lower_bounds.Add(0f);
        }

        stationDistances = agents[0].CalculateDistances();

        

        GetInitialLB();
        initial_h = h;
        
        reward = 0;
        episode_start = Time.time;

        if (printJobs) { PrintJobsCSV(); }

        if (printMakespan)
        {
            string title= "nada";
            int jobSeed = transform.GetComponent<FASinfo>().jobSeed;
            int machineSeed = transform.GetComponent<FASinfo>().machineSeed;
            
            if (agents[0].inference)
            {
                title = Application.dataPath + "/Data/" + "NN-" + jobSeed + "-" + machineSeed + ".csv";
            }
            else if (agents[0].SPT)
            {
                title = Application.dataPath + "/Data/" + "SPT-" + jobSeed + "-" + machineSeed + ".csv";
            }
            else if (agents[0].LPT)
            {
                title = Application.dataPath + "/Data/" + "LPT-" + jobSeed + "-" + machineSeed + ".csv";
            }
            else if (agents[0].custom)
            {
                title = Application.dataPath + "/Data/" + "Custom-" + jobSeed + "-" + machineSeed + ".csv";
            }
            makespanWriter = new StreamWriter(title);


        }
    }

    public void GetInitialLB()
    {
        NavMeshPath path = new NavMeshPath();
        h = 0;
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
                    currentTaskTime += Vector3.Distance(path.corners[j], path.corners[j + 1]) / speed;

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
                    dist = stationDistances[((int)product_job[k].x, (int)product_job[k + 1].x)]; ;

                    lower_bound += dist / speed + product_job[k + 1].y;
                    if (temp_ID == debbugProductID)
                    {
                        lower_bounds[k + 1] = lower_bound;
                    }
                    h = Mathf.Max(h, lower_bound);
                }
            }
        }
        //Debug.Log("Initial: " + h);
    }

    public List<float> GetLBObservations(int ID, Vector3 agentPosition)
    {
        NavMeshPath path = new NavMeshPath();
        List<float> observations = new List<float>();
        last_h = h;
        h = 0;

        //Debug.Log("Agent Count" + agents.Count);

        for(int i = 0; i< agents.Count; i++)
        {
            if(ID != agents[i].ID)
            {
                observations.Add(agents[i].last_action);
            }
        }

        for (int i = 1; i < productDictionary.Count + 1; i++)
        {
            inputs = new List<float>();
            float lower_bound = 0;
            Product temp_p = productDictionary[i];
            int temp_ID = temp_p.product_ID;
            List<Vector2> product_job = temp_p.GetJob();
            List<float> product_times = temp_p.GetTimes();
            int c_task = (int)temp_p.GetCurrentTask().x;

            Vector3 distanceToProduct = temp_p.transform.position - agentPosition;
            observations.Add(distanceToProduct.x);
            observations.Add(distanceToProduct.z);


            int c = 0;
            //Add completed tasks observations
            for (int i2 = 0; i2 < product_times.Count; i2++)
            {
               
                if (product_times[i2] > 0)
                {
                    observations.Add(1);
                    observations.Add(product_times[i2]);
                    c += 2;
                    if (temp_ID == debbugProductID)
                    { lower_bounds[i2] = product_times[i2]; }
                    h = Mathf.Max(h, product_times[i2]);
                }
            }

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
                        currentTaskTime += Vector3.Distance(path.corners[j], path.corners[j + 1]) / speed;
                        //Debug.DrawLine(path.corners[j], path.corners[j + 1], Color.red, 10f, false);
                    }

                    lower_bound += currentTaskTime + product_job[temp_p.task_pointer].y;
                    observations.Add(0);
                    observations.Add(lower_bound);
                    c += 2;
                    if (temp_ID == debbugProductID)
                    {
                        lower_bounds[temp_p.task_pointer] = lower_bound;
                    }
                }
                else
                {
                    lower_bound += product_job[temp_p.task_pointer].y - (Time.time - temp_p.process_start);
                    observations.Add(1);
                    observations.Add(lower_bound);
                    c += 2;
                    if (temp_ID == debbugProductID)
                    {
                        lower_bounds[temp_p.task_pointer] = lower_bound;
                    }

                }
                h = Mathf.Max(h, lower_bound);
                float dist;

                for (int k = temp_p.task_pointer; k < product_job.Count - 1; k++)
                {
                    dist = stationDistances[((int)product_job[k].x, (int)product_job[k + 1].x)];
                    observations.Add(0);
                    c++;

                    lower_bound += dist / speed + product_job[k + 1].y;
                    if (temp_ID == debbugProductID)
                    {
                        lower_bounds[k + 1] = lower_bound;
                    }
                    observations.Add(lower_bound);
                    c++;
                    h = Mathf.Max(h, lower_bound);
                }
            }
            //Debug.Log("P_ID; "+ temp_p.product_ID + " C: " + c  );
        }
        reward = (last_h - h);

        foreach(var agent in agents) { agent.AddReward(reward); }

        //Debug
        foreach(var obs in observations) { inputs.Add(obs); }

        return observations;
    }


    private void FixedUpdate()
    {

        reset_timer += 1;
        if (reset_timer>maxEpisodeSteps && maxEpisodeSteps > 0)
        {
            ResetScene();
            reset_timer = 0;
            AGVgroup.GroupEpisodeInterrupted();
            episode_start = Time.time;
            reward = 0;
            
            h = initial_h;
        }

        bool finished = true;
        for (int i = 1; i < productDictionary.Count + 1; i++) { finished = finished && productDictionary[i].finished; }
        if (finished)
        {
            String jobSeed = transform.GetComponent<FASinfo>().jobSeed.ToString();
            String machineSeed = transform.GetComponent<FASinfo>().machineSeed.ToString();
            float temp2 = Time.time - episode_start;
            //Debug.Log("Makespan= " + temp2 + " CR= " + agents[0].GetCumulativeReward());
            if (printMakespan)
            {
                string line = "";
                line += episode +", ";
                line += temp2;
                makespanWriter.WriteLine(line);
                line += " js:" + jobSeed + " ms:" + machineSeed;
                Debug.Log(line);
            }
            episode++;
            if(bestMakespan < 0) 
            { 
                bestMakespan = h;
                
                
            }
            //Get Best Schedule
            if (bestMakespan > h)
            {
                bestMakespan = h;
                for (int i = 0; i < productDictionary.Count; i++)
                {
                    var p = productDictionary[i + 1];
                    List<float> temp = p.GetTimes();
                    JSSPAnswer[p.product_ID] = temp;
                }
                if (schedule)
                {
                    schedule.ProposeMakespan(bestMakespan);
                }
            }
            statsRecorder.Add("JSSP/AverageMakespan", h);

            AGVgroup.EndGroupEpisode();
            ResetScene();
            reset_timer = 0;
            episode_start = Time.time;
            reward = 0;
            h = initial_h;

        }
    }

    public void CollectChilds()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.CompareTag("product"))
            {
                childs.Add(child);
                Product product = child.GetComponent<Product>();
                productDictionary.Add(product.product_ID, product);
            }
            else if (child.CompareTag("table"))
            {
                childs.Add(child);
            }
            else if (child.CompareTag("workstation"))
            {
                childs.Add(child);
                Workstation workstation = child.GetComponent<Workstation>();
                Transform input = workstation.input_location;
                Transform output = workstation.output_location;
                inputLocationDictionary.Add(workstation.ID, input);
                outputLocationDictionary.Add(workstation.ID, output);
                workstationDictionary.Add(workstation.ID, workstation);
            }
            else if (child.CompareTag("delivery"))
            {
                childs.Add(child);
                Delivery delivery = child.GetComponent<Delivery>();
                Transform input = delivery.input_location;
                deliveryLocationDictionary.Add(-delivery.ID, input);
            }
            else if (child.CompareTag("AGV"))
            {
                childs.Add(child);
                JSSPMultiAgent agent = child.GetComponent<JSSPMultiAgent>();
                AGVgroup.RegisterAgent(agent);
                agents.Add(agent);
                agent.agent.speed = speed;
            }

        }
    }

    // Reset all the scene
    public void ResetScene()
    {
        foreach (Transform child in childs)
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

        cspointer = 0;
    }

    public void OnApplicationQuit()
    {
        if (printMakespan)
        {
            makespanWriter.Flush();
            makespanWriter.Close();
        }
    }

    public (int, int) GetCustomSolution()
    {
        int stacks = 0;
        if (cspointer + 1 > customSolution.Length)
        {
            return (0, 0);
        }
        else
        {
            int a = (int) customSolution.GetValue(cspointer);
            int left = customSolution.Length - cspointer;

            for (int i= 0 ; i < left; i++)
            {
                if (cspointer + i + 1 >= customSolution.Length) { break; }
                if ((int) customSolution.GetValue(cspointer+i) == (int) customSolution.GetValue(cspointer + i + 1))
                {
                    stacks++;
                }
                else
                {
                    break;
                }
            }

            return (a, stacks);
        }
    }

    public void AdvanceCustomPointer(int x) 
    { 
        cspointer+= x;
        //Debug.Log("P " + cspointer);
    }
}
