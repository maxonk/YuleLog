using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotationController : MonoBehaviour {

	void Start () {
		
	}
	
	void Update () {
		transform.eulerAngles = transform.eulerAngles + Vector3.up * Time.deltaTime;
	}
}
