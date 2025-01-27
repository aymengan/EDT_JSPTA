using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class Schedule : MonoBehaviour
{
    // The best makespan achieved so far
    public float bestMakespan;

    // Used to log Tensorboard metrics
    StatsRecorder statsRecorder;
    
    void Awake()
    {
        statsRecorder = Academy.Instance.StatsRecorder;
        bestMakespan = -1;
    }
    
    /// <summary>
    /// Checks if the new makespan is better than the best one achieved so far. If so, it updates the best makespan and
    /// logs it to Tensorboard. 
    /// </summary>
    /// <param name="makespan">The proposed makespan value.</param>
    public void ProposeMakespan(float makespan)
    {
        if (bestMakespan < 0) { bestMakespan = makespan; }
        if (makespan < bestMakespan)
        {
            bestMakespan = makespan;
            statsRecorder.Add("JSSP/best-makespan", bestMakespan, StatAggregationMethod.MostRecent);
        }
    }

    /// <summary>
    /// Logs the current best makespan value achied so far at fixed intervals.
    /// </summary>
    private void FixedUpdate()
    {
        statsRecorder.Add("JSSP/best-makespan", bestMakespan, StatAggregationMethod.MostRecent);
    }
}
