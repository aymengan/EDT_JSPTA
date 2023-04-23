using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class Schedule : MonoBehaviour
{
    public float bestMakespan;

    public Dictionary<int, List<float>> schedule;

    StatsRecorder statsRecorder;

    // Start is called before the first frame update
    void Awake()
    {
        schedule = new Dictionary<int, List<float>>();
        statsRecorder = Academy.Instance.StatsRecorder;
        bestMakespan = -1;
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
    }
}
