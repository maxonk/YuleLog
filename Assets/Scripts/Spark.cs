using UnityEngine;
using System.Collections;

public class Spark : MonoBehaviour {

    new Rigidbody rigidbody;

	void Start () {
        rigidbody = GetComponent<Rigidbody>();
	}
	
	void Update () {
        if (rigidbody.IsSleeping()) Destroy(gameObject);
	}
}
