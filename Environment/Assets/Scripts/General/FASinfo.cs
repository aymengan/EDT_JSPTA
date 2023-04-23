using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FASinfo : MonoBehaviour
{

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

    [Header("Custom Solution of the JSPTA")]
    public System.Array customSolution;

    public string stSolution;

    public void Awake()
    {
        if (stSolution.Length>0)
        {
            customSolution = System.Array.ConvertAll(stSolution.Split(','), int.Parse);
        }
    }

}
