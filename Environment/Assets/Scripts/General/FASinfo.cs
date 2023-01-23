using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FASinfo : MonoBehaviour
{
    // Color List for Products and Workstations
    [Tooltip("Colors for Products and Workstations")]
    public List<Color> color_list;
    [Tooltip("Origini of entire system")]
    public Transform Origin;
    [Tooltip("Number of Workstations")]
    public int Nw;
    [Tooltip("Create random jobs per episode")]
    public bool randomJob;

    [Tooltip("Fixed Random seed for task length")]
    public int jobSeed;

    [Tooltip("Fixed Random seed for task order")]
    public int machineSeed;

    [Tooltip("Training mode")]
    public bool trainingMode;

    public void Awake()
    {
        
    }

}
