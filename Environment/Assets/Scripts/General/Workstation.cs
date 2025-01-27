using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Workstation : MonoBehaviour
{
    [Tooltip("Workstation ID. 0 means it is not working")]
    public int ID;

    [Tooltip("Stack output products")]
    public bool stack_output;

    // Color List for FAS Visualization
    private List<Color> color_list;
    private Material product_material;

    // Locations and Colliders
    public Transform input_location;
    public Transform output_location;
    public Transform processsing_location;
    public Collider input_collider;


    private void Awake()
    {
        // Set Workstation Color
        FASinfo fas_info = GetComponentInParent<FASinfo>();
        color_list = fas_info.color_list;
        ChangeColor();
    }

    private void ChangeColor() {
        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();

        product_material = meshRenderer.material;

        product_material.SetColor("_Color", color_list[ID]);

    }

    /// <summary>
    /// Gets the output location of processed products. Either stacks them and places them all in the same location
    /// or places them in up to 5 different locations around the workstation.
    /// </summary>
    /// <returns>
    /// A tuple containing the position of the output location and the placement ID.
    /// </returns>
    public (Vector3,int) GetOutputLocation()
    {
        Vector3 pos = output_location.position;
        List<int> placementIDs = new List<int>();

        if (stack_output) { 
            // If stacking, return the same position for all products, no placement id is necessary
            return (pos, 0); }
        int c = 0;

        
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.CompareTag("product"))
            {
                c++;

                placementIDs.Add(child.GetComponent<Product>().workstationPlacementID);
            } 
        }

        while (placementIDs.Contains(c)){c++;}

        c--;
        switch (c)
        {
            case 1:
                pos.z += 0.3f;
                break;
            case 2:
                pos.z -= 0.3f;
                break;
            case 3:
                pos.x += 0.3f;
                break;
            case 4:
                pos.x += 0.3f;
                pos.z += 0.3f;
                break;
            case 5:
                pos.x += 0.3f;
                pos.z -= 0.3f;
                break;
        }
        return (pos, c);
    }

    // Reset Workstation per episode
    public void EpisodeReset()
    {
        input_collider.gameObject.SetActive(true);
    }

}
