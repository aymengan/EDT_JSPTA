using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Product : MonoBehaviour
{   
    // Colors
    private List<Color> color_list;
    public int product_ID = 0;
    public bool randomJob;
    private int jobSeed;
    private int machineSeed;

    [Tooltip("Ordered list of tasks to complete job. x: task, y: processing time. Add delivery station at the end with a negative ID at x")]
    public List<Vector2> job;

    [HideInInspector]
    public int task_pointer = 0;
    private Material product_material;
    public bool processing= false;
    public bool finished = false;

    [HideInInspector]
    public float process_start = 0;

    // Times of Completion
    public List<float> times;

    // Workstations
    private Collider used_w;
    private Workstation workstation;
    private Vector3 outputPosition;

    [HideInInspector]
    public int workstationPlacementID = 0;

    // Initial Parameters
    [HideInInspector]
    public Transform og_parent;
    [HideInInspector]
    public Vector3 og_position;
    private float episodeStart;

    public bool grabbed= false;

    [HideInInspector]
    public bool assigned = false;

    [Header("Debug")]
    private int Nw;
    private float ogTimeScale;
    
    private void Awake()
    {
        // Initialize
        processing = false;
        outputPosition = new Vector3();
        og_parent = transform.parent;
        og_position = transform.position;
        episodeStart = Time.time;
        finished = false;

        // Get Coloring List and Change Color
        FASinfo info = transform.parent.GetComponent<FASinfo>();
        color_list = info.color_list;
        Nw = info.Nw;
        randomJob = info.randomJob;
        jobSeed = info.jobSeed;
        machineSeed = info.machineSeed;

        // Generate job
        UnityEngine.Random.InitState(jobSeed);
        job = new List<Vector2>();
        GenerateJob();


        //origin = info.Origin.position;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        product_material = meshRenderer.material;
        ChangeColor(product_material, color_list[(int)job[task_pointer][0]]);

        ogTimeScale = Time.timeScale;
    }


    private void GenerateJob()
    {
        
        for(int i = 0; i < Nw; i++)
        {
            for (int j = 0; j < product_ID*Nw; j++) { float _ = UnityEngine.Random.Range(1, 10); }
            float t = UnityEngine.Random.Range(1f, 10f);
            job.Add(new Vector2(i + 1f, t));
        }

        UnityEngine.Random.InitState(machineSeed);
        List<Vector2> temp_list = new List<Vector2>(job);
        for(int i = 0; i< temp_list.Count; i++)
        {
            for (int j = 0; j < product_ID * Nw; j++) { float _ = UnityEngine.Random.Range(1, 10); }
            int temp = UnityEngine.Random.Range(1, Nw); //AQUII!
            job[i] = temp_list[temp];
            job[temp] = temp_list[i];
            temp_list = new List<Vector2> (job);
        }


        // Add delivery stations
        job.Add(new Vector2(-1, 0));
    }

    private void Start()
    {
        for (int i = 0; i < job.Count; i++)
        {
            times.Add(-1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Task Completion
        if (processing) { ContinueTask(Time.time); }
    }

    private void ChangeColor(Material material, Color color) {
        material.SetColor("_Color", color);
    }

    public void PerformTask(float init_time, Collider coll)
    {
        // Task Initialization
        float time = Time.time;
        coll.gameObject.SetActive(false);
        used_w = coll;
        workstation = coll.transform.GetComponentInParent<Workstation>();
        process_start  = time;
        
        if (time<init_time + job[task_pointer].y) 
        {// In case tp > 0
            processing = true;
            ChangeColor(product_material, color_list[0]);
            SetParent(coll.transform.parent);
            (outputPosition, workstationPlacementID) = workstation.GetOutputLocation();
            SetPosition(workstation.processsing_location.position);
            grabbed = true;
        }
        else
        {// In case of tp = 0
            processing = false;
            float t = Time.time - episodeStart;
            //Debug.Log("PerformedTask Ran");
            times[task_pointer] = t;
            if (task_pointer < job.Count-1) { task_pointer++; }
            if ((int)job[task_pointer][0] > 0)
            { ChangeColor(product_material, color_list[(int)job[task_pointer].x]); }
            else
            { ChangeColor(product_material, color_list[color_list.Count - 1]); }
            SetParent(coll.transform.parent);
            
            //Debug.Log("Product ID: " + product_ID + " Finished Task # " + task_pointer + " At: " + t);
            (outputPosition, workstationPlacementID) = workstation.GetOutputLocation();
            SetPosition(outputPosition);
            used_w.gameObject.SetActive(true);
            grabbed = false;
            assigned = false;
        }
    }

    private void ContinueTask(float time)
    {
        //Only runs when the task is completed
        if (time > process_start + job[task_pointer].y)
        {
            processing = false;
            float t = Time.time - episodeStart;
            //Debug.Log("ContinueTask Ran");
            times[task_pointer] = t;
            if (task_pointer < job.Count - 1) { task_pointer++; }
            used_w.gameObject.SetActive(true);
            if ((int)job[task_pointer][0] > 0)
            { ChangeColor(product_material, color_list[(int)job[task_pointer].x]); }
            else
            { ChangeColor(product_material, color_list[color_list.Count-1]); }
            
            //Debug.Log("Product ID: " + product_ID + " Finished Task # " + task_pointer + " At: " + t);
            //Debug.Log("To Go: " + GetLowerMakespan(2f));
            SetPosition(outputPosition);
            grabbed = false;
            assigned = false;
            
        }
    }

    public void CompleteJob(Collider collider)
    {
        float t = Time.time - episodeStart;
        //Debug.Log("CompletedJob Ran");
        times[task_pointer] = t;
        //Debug.Log("Product ID: " + product_ID + " Finished Completely At: " + t);
        SetParent(collider.transform.parent);
        transform.gameObject.SetActive(false);
        grabbed = true;
        finished = true;
        assigned = true;
    }

    public List<Vector2> GetJob() {
        return job;
    }

    public List<float> GetTimes()
    {
        return times;
    }

    public Vector2 GetCurrentTask()
    {
        return job[task_pointer];
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }
    
    public void SetParent(Transform parent)
    {
        transform.parent = parent;
    }

    public void SetinTable(Transform table)
    {
        SetParent(table);
        SetPosition(table.gameObject.GetComponent<TemporalStation>().load.position);
    }

    public void EpisodeReset()
    {
        task_pointer = 0;
        processing = false;
        times = new List<float>();
        for (int i = 0; i < job.Count; i++)
        {
            times.Add(-1f);
        }
        outputPosition = new Vector3();
        //Debug.Log(color_list[(int)job[task_pointer][0]]);
        ChangeColor(product_material, color_list[(int)job[task_pointer][0]]);
        SetParent(og_parent);
        SetPosition(og_position);
        episodeStart = Time.time;
        used_w = new Collider();
        grabbed = false;
        transform.gameObject.SetActive(true);
        finished = false;
        assigned = false;
    }

    public Vector3 GetDestination(int grabbed_ID, Dictionary<int, Transform> workstationLocations, Dictionary<int, Transform> deliveryLocations)
    {
        Vector3 destination = new Vector3(0,0,0);
        if (!grabbed)
        {
            destination =  transform.position;

        }
        else if (processing){
            destination = workstation.output_location.position;
        }
        else if(grabbed_ID == product_ID)
        {
            int temp_ID = (int)GetCurrentTask()[0]; // get ID of next task
            if (temp_ID > 0)
            {// Workstation input
                Transform temp_w = workstationLocations[temp_ID];
                destination = temp_w.position;
            } else
            { // Delivery Input
                Transform temp_d = deliveryLocations[temp_ID];
                destination = temp_d.position;
            }
        }
        return destination;
    }

}
