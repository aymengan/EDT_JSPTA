using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductMP : MonoBehaviour
{

    [Tooltip("Colorcoded Dictionary")]
    private List<Color> color_list;

    [Tooltip("Colorcoded Dictionary")]
    public int product_ID = 0;

    [Tooltip("Ordered list of tasks to complete job. x: task, y: processing time. Add delivery station at the end with a negative ID at x")]
    public List<Vector2> job;


    [HideInInspector]
    public int task_pointer = 0;

    private Material product_material;

    [HideInInspector]
    public bool processing= false;

    [HideInInspector]
    public float process_start = 0;

    [HideInInspector]
    public List<float> times;

    Collider used_w;

    private void Awake()
    {
        processing = false;
        times = new List<float>();

        // Get Coloring List
        FASinfo info = transform.parent.GetComponent<FASinfo>();
        color_list = info.color_list;
        

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        product_material = meshRenderer.material;

        //Debug.Log(job_manager.color_list);

        ChangeColor(product_material, color_list[(int)job[task_pointer][0]]);
    }


    // Update is called once per frame
    void Update()
    {
        //job[current_task][0] = 1f;
        //Debug.Log(job[task_pointer][0]);

        if (processing) { ContinueTask(Time.realtimeSinceStartup); }

    }

    private void ChangeColor(Material material, Color color) {
        material.SetColor("_Color", color);
    }

    public void PerformTask(float init_time, Collider coll)
    {
        float time = Time.realtimeSinceStartup;
        coll.gameObject.SetActive(false);
        used_w = coll;
        process_start  = time;
        if(time<init_time + job[task_pointer].y)
        {
            processing = true;
            ChangeColor(product_material, color_list[0]);
        }
        else
        {// In case of tp = 0
            processing = false;
            task_pointer++;
            ChangeColor(product_material, color_list[(int)job[task_pointer].x]);
        }


    }

    private void ContinueTask(float time)
    {
        //Only runs when the task is completed
        if (time > process_start + job[task_pointer].y)
        {
            processing = false;
            task_pointer++;
            used_w.gameObject.SetActive(true);
            if ((int)job[task_pointer][0] > 0)
            { ChangeColor(product_material, color_list[(int)job[task_pointer].x]); }
            else
            { ChangeColor(product_material, color_list[color_list.Count-1]); }
            times.Add(Time.realtimeSinceStartup);
            Debug.Log("Product ID: " + product_ID + " Finished Task # " + task_pointer + " At: " + Time.realtimeSinceStartup);
            
            // if parent tag = workstation then set it at the output
        }
    }

    public void CompleteJob()
    {
        times.Add(Time.realtimeSinceStartup);
        Debug.Log("Product ID: " + product_ID + " Finished Completely At: " + Time.realtimeSinceStartup);
        transform.gameObject.SetActive(false);
    }

    public List<Vector2> GetJob() {
        return job;
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
}
