using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CreateWalls : MonoBehaviour
{
    public Vector3 rayPosition;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {
                print("Hello");
                rayPosition = hit.point;
                Handles.DrawWireDisc(rayPosition, Vector3.up, 1.0f);
                //Handles.DrawWireDisc()
            }
        }
    }
}

// A tiny custom editor for ExampleScript component
[CustomEditor(typeof(CreateWalls))]
public class ExampleEditor : Editor
{
    // Custom in-scene UI for when ExampleScript
    // component is selected.
    public void OnSceneGUI()
    {
        var t = target as CreateWalls;
        var tr = t.transform;
        var pos = tr.position;
        // display an orange disc where the object is
        var color = new Color(1, 0.8f, 0.4f, 1);
        Handles.color = color;
        Handles.DrawWireDisc(pos, tr.up, 1.0f);
        // display object "value" in scene
        GUI.color = color;
        //Handles.Label(pos, t.value.ToString("F1"));
    }
}
