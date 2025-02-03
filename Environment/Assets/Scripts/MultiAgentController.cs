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
    // Lists of all the elements within FAS
    private List<Transform> childs;
    private List<TemporalStation> tables;
    private Dictionary<int, Workstation> workstationDictionary;
    private Dictionary<int, Transform> inputLocationDictionary;
    private Dictionary<int, Transform> outputLocationDictionary;
    private Dictionary<int, Transform> deliveryLocationDictionary;
    private Dictionary<int, Product> productDictionary;
    private Dictionary<int, List<float>> JSSPAnswer;

    public List<JSSPMultiAgent> agents;
    private SimpleMultiAgentGroup AGVgroup;
    private Dictionary<(int, int), float> stationDistances;
    private NavMeshPath path;
    public GameObject scheduleObject;
    public Schedule schedule;

    [Tooltip("Maximum steps for every episode")]
    public int maxEpisodeSteps;
    private int reset_timer;
    public float speed = 2f;

    [Header("Debug")]
    public List<float> lower_bounds;
    public int debbugProductID = 0;
    public float reward = 0;
    public float cummulativeReward = 0;
    public List<float> inputs;
    private bool printJobs;
    private bool printMakespan;
    public int episode = 0; 

    // Rewards
    private float episode_start;
    private float h = 0;
    private float last_h = 0;
    private float initial_h;

    //Result Logs
    private float bestMakespan =-1;

    private StatsRecorder statsRecorder;
    private int trainingStep;
    private StreamWriter makespanWriter;

    //Custom Solution
    private int cspointer = 0;
    private System.Array customSolution;
    private FASinfo info;
    
    // Track how much of the evaluation has been conducted
    private int evalEpisodes = 0;
    private int episodesToEvaluate;

    void Awake()
    {
        // Initialize
        episode = 0;
        childs = new List<Transform>();
        tables = new List<TemporalStation>();
        workstationDictionary = new Dictionary<int, Workstation>();
        inputLocationDictionary = new Dictionary<int, Transform>();
        outputLocationDictionary = new Dictionary<int, Transform>();
        deliveryLocationDictionary = new Dictionary<int, Transform>();
        productDictionary = new Dictionary<int, Product>();
        JSSPAnswer = new Dictionary<int, List<float>>();

        agents = new List<JSSPMultiAgent>();
        stationDistances = new Dictionary<(int, int), float>();
        AGVgroup = new SimpleMultiAgentGroup();
        path = new NavMeshPath();
        lower_bounds = new List<float>();
        inputs = new List<float>();
        statsRecorder = Academy.Instance.StatsRecorder;
        if (scheduleObject) { schedule = scheduleObject.transform.GetComponent<Schedule>(); }
        trainingStep = 0;
    }

    void Start()
    {
        episodesToEvaluate = schedule.episodesToEvaluate;
        printJobs = schedule.printJobs;
        printMakespan = schedule.printMakespan;


        info = transform.GetComponent<FASinfo>();
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
        cummulativeReward = 0;
        episode_start = Time.time;

        if (printJobs) { PrintJobsCSV(); }

        if (printMakespan && !schedule.randomJob)
        {
            string title = "nada";
            int jobSeed = transform.GetComponent<FASinfo>().jobSeed;
            int machineSeed = transform.GetComponent<FASinfo>().machineSeed;
            string dataPath = Application.dataPath + "/Data/";
            if (schedule.inference)
            {
                title = $"{dataPath}NN-{jobSeed}-{machineSeed}.csv";
            }
            else if (schedule.SPT)
            {
                title = $"{dataPath}SPT-{jobSeed}-{machineSeed}.csv";
            }
            else if (schedule.LPT)
            {
                title = $"{dataPath}LPT-{jobSeed}-{machineSeed}.csv";
            }
            else if (agents[0].custom)
            {
                title = $"{dataPath}Custom-{jobSeed}-{machineSeed}.csv";
            }
            makespanWriter = new StreamWriter(title);
            print("makespan writer initialized");
        }
    }

    public void PrintJobsCSV()
    {
        if (!schedule.randomJob)
        {
            int jobSeed = transform.GetComponent<FASinfo>().jobSeed;
            int machineSeed = transform.GetComponent<FASinfo>().machineSeed;
            string fileName = $"Jobs-{jobSeed}-{machineSeed}.csv";
            StreamWriter writer = new StreamWriter(Path.Combine(Application.dataPath, "Data", fileName));
        
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
    }
    public void IncreaseStep()
    {
        trainingStep++;
        statsRecorder.Add("TrainingStep", trainingStep, StatAggregationMethod.MostRecent);
    }
    

    public void GetInitialLB()
    {
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
        cummulativeReward += reward;

        if (schedule.maTrainer)
        {
            AGVgroup.AddGroupReward(reward);
        }
        else
        {
            foreach(var agent in agents) { agent.AddReward(reward); }
        }


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
            cummulativeReward = 0;
            
            h = initial_h;
        }

        bool finished = true;
        for (int i = 1; i < productDictionary.Count + 1; i++) { finished = finished && productDictionary[i].finished; }
        if (finished)
        {
            float temp2 = Time.time - episode_start;
            //Debug.Log("Makespan= " + temp2 + " CR= " + agents[0].GetCumulativeReward());
            if (printMakespan && !schedule.randomJob)
            {
                string line = "";
                line += episode +", ";
                line += temp2;
                makespanWriter.WriteLine(line);
                evalEpisodes++;
                int jobSeed;
                int machineSeed;
                (jobSeed, machineSeed) = info.GetSeeds();
                line += " " + jobSeed.ToString() + " " + machineSeed.ToString();
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
            cummulativeReward = 0;
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
                TemporalStation table = child.GetComponent<TemporalStation>();
                tables.Add(table);
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
        int newJobSeed;
        int newMachineSeed;
        (newJobSeed, newMachineSeed) = info.GetSeeds();
        if (schedule.randomJob)
        {
            info.resetSeeds();
        }
        foreach (Transform child in childs)
        {
            // Reset Products
            if (child.CompareTag("product"))
            {
                Product temp_product = child.GetComponent<Product>();
                temp_product.EpisodeReset(newJobSeed, newMachineSeed);
            }
            // Reset tables
            else if (child.CompareTag("table"))
            {
                TemporalStation temp_st = child.GetComponent<TemporalStation>();
                temp_st.EpisodeReset();
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

    public bool IsEvalDone()
    {
        return evalEpisodes >= episodesToEvaluate;
    }

    public void AdvanceCustomPointer(int x) 
    { 
        cspointer+= x;
        //Debug.Log("P " + cspointer);
    }
}
