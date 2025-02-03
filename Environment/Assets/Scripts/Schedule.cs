using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class Schedule : MonoBehaviour
{
    public float bestMakespan;

    public Dictionary<int, List<float>> schedule;

    private MultiAgentController[] controllers;

    StatsRecorder statsRecorder;
    
    public bool inference = false;
    public bool manual=false;
    public bool SPT = false;
    public bool LPT = false;
    public bool trainingMode = false;
    [Tooltip("Create random jobs per episode")]
    public bool randomJob = false;
    [Tooltip("Masking")]
    public bool masking;
    [Tooltip("MA-Trainer")]
    public bool maTrainer;
    public bool printJobs;
    public bool printMakespan;
    public bool pseudoMasking;

    

    public int episodesToEvaluate;

    // Start is called before the first frame update
    void Awake()
    {
        schedule = new Dictionary<int, List<float>>();
        statsRecorder = Academy.Instance.StatsRecorder;
        bestMakespan = -1;
        
        // TODO: Possible Parsing
        
    }
    
    void Start()
    {
        controllers = FindObjectsOfType<MultiAgentController>();
    }

    public void ProposeMakespan(float makespan, Dictionary<int, List<float>> ps)
    {
        if (bestMakespan<0) { bestMakespan = makespan; }
        if (makespan < bestMakespan)
        {
            bestMakespan = makespan;
            statsRecorder.Add("JSSP/best-makespan", bestMakespan, StatAggregationMethod.MostRecent);
            schedule = ps;
            LogSchedule(schedule);
        }
    }
    public void ProposeMakespan(float makespan)
    {
        if (bestMakespan < 0) { bestMakespan = makespan; }
        if (makespan < bestMakespan)
        {
            bestMakespan = makespan;
            statsRecorder.Add("JSSP/best-makespan", bestMakespan, StatAggregationMethod.MostRecent);
        }
    }

    public void LogSchedule(Dictionary<int, List<float>> ps)
    {
        for(int i = 0; i < ps.Count; i++)
        {
            for (int j = 0; j < ps[i+1].Count; j++) 
            {
                int temp_ID = i + 1;
                statsRecorder.Add("Answer/" + temp_ID + "&" + j, ps[i+1][j], StatAggregationMethod.MostRecent);
            }
        }
    }

    private void FixedUpdate()
    {
        statsRecorder.Add("JSSP/best-makespan", bestMakespan, StatAggregationMethod.MostRecent);
        bool evalMode = inference || manual || SPT || LPT;
        if (evalMode)
        {
            CheckEvalCompl();
        }
    }
    
    // This function can be called to quit the game
    public void Quit()
    {
        // Quit the application
        Application.Quit();

        // If you are running the game in the Unity editor, this will stop play mode
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    void CheckEvalCompl()
    {
        bool allEvalDone = true;
        foreach (var controller in controllers)
        {
            if (!controller.IsEvalDone())
            {
                allEvalDone = false;
                break;
            }
        }

        if (allEvalDone)
        {
            // Call the specific method here
            Quit();
        }
    }
}
