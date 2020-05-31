using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour {
    // Start is called before the first frame update
    void Start () {

    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKey (KeyCode.LeftArrow)) {
            Vector3 position = this.transform.position;
            position.x-=.125F;
            this.transform.position = position;
        }
        if (Input.GetKey (KeyCode.RightArrow)) {
            Vector3 position = this.transform.position;
            position.x+=.125F;
            this.transform.position = position;
        }
        if (Input.GetKey (KeyCode.UpArrow)) {
            Vector3 position = this.transform.position;
            position.y+=.125F;
            this.transform.position = position;
        }
        if (Input.GetKey (KeyCode.DownArrow)) {
            Vector3 position = this.transform.position;
            position.y-=.125F;
            this.transform.position = position;
        }
    }
}