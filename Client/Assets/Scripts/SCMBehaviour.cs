using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCMBehaviour : MonoBehaviour {

    bool Started = false;

    protected virtual void StartOnlyOneTime() { }

	// Use this for initialization
	void Start () {
        Starting();

        if (Started)
            return;

        Started = true;
        StartOnlyOneTime();
    }

    protected virtual void Starting() { }
	
	// Update is called once per frame
	void Update () {
		
	}
}
