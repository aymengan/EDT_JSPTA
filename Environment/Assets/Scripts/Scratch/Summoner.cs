using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Summoner : MonoBehaviour
{
    public GameObject myPrefab;

    private Workstation w;

    // Start is called before the first frame update
    private void Awake()
    {
        w = myPrefab.gameObject.GetComponent<Workstation>();
        w.ID = 4;
        Instantiate(myPrefab, new Vector3(0, 0, 0), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
