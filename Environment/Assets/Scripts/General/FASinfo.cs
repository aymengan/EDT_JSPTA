using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class FASinfo : MonoBehaviour
{

    [Tooltip("Colors for Products and Workstations")]
    public List<Color> color_list;
    [Tooltip("Origini of entire system")]
    public Transform Origin;
    [Tooltip("Number of Workstations")]
    public int Nw;
    [Tooltip("Create random jobs per episode")]
    private bool randomJob;

    [Tooltip("Fixed Random seed for task length")]
    public int jobSeed;

    [Tooltip("Fixed Random seed for task order")]
    public int machineSeed;

    [Header("Custom Solution of the JSPTA")]
    public System.Array customSolution;

    public string stSolution;
    
    private List<(int, int)> usedSeeds = new List<(int, int)>();

    public int baseSeed;

    private List<String> keys = new List<string>();
    
    public Schedule schedule;
    
    int GetBasePort()
    {
        int default_base_port = 5004;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--base-port" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int basePort))
                {
                    return basePort;
                }
                else
                {
                    Debug.Log("Invalid base port value: " + args[i + 1]);
                }
            }
        }

        Debug.Log("Base port argument not found.");
        return default_base_port;
    }
    
    static int ReadPortFromArgs()
    {
        String default_base_port = "5004";
        var args = Environment.GetCommandLineArgs();
        var inputPort = default_base_port;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--mlagents-port")
            {
                inputPort = args[i + 1];
            }
        }
        return int.Parse(inputPort);
    }
    
    public void Start()
    {
        randomJob = schedule.randomJob;
        int basePort = GetBasePort();
        int ourPort = ReadPortFromArgs();

        int workerId = ourPort - basePort;
        
        // Retrieve the seed value from the EnvironmentParameters side channel
        baseSeed = workerId;
        
        Random.InitState(baseSeed);
        

        if (stSolution.Length>0)
        {
            customSolution = System.Array.ConvertAll(stSolution.Split(','), int.Parse);
        }

        if (randomJob)
        {
            resetSeeds();
            (jobSeed, machineSeed) = GetSeeds();
            usedSeeds.Add((jobSeed, machineSeed));
        }
    }

    public (int, int) GetSeeds()
    {
        return (jobSeed, machineSeed);
    }

    public void resetSeeds()
    {
        jobSeed = Random.Range(0, int.MaxValue);
        machineSeed = Random.Range(0, int.MaxValue);
        usedSeeds.Add((jobSeed, machineSeed));
    }
    
    public void OnApplicationQuit()
    {
        if (randomJob)
            WriteSeedsToFile();
    }

    private void WriteSeedsToFile()
    {
        string directoryPath = Path.Combine(Application.dataPath, "Data", "UsedSeeds");
        Directory.CreateDirectory(directoryPath); // Ensure the directory exists
        string filePath = Path.Combine(directoryPath, $"{baseSeed}.txt");

        var distinctAndSortedSeeds = usedSeeds.Distinct()
            .OrderBy(seed => seed.Item1)
            .ThenBy(seed => seed.Item2)
            .ToList();

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var seedPair in distinctAndSortedSeeds)
            {
                writer.WriteLine($"{seedPair.Item1},{seedPair.Item2}");
            }
        }
    }
    
    
}
