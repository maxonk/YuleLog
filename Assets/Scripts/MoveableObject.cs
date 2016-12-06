using UnityEngine;
using System.Collections;

public class MoveableObject : MonoBehaviour {

    [HideInInspector]
    bool _grabbed;
    public bool grabbed {
        set {
            if(rigidbody != null) {
                rigidbody.isKinematic = value;
            }
            _grabbed = value;
        }
        get {
            return _grabbed;
        }
    }
    [HideInInspector]
    public Vector3 targetPosition;

    new Rigidbody rigidbody;

    void Start() {
        rigidbody = GetComponent<Rigidbody>();
    }

	void Update () {
	    if(grabbed) {
            transform.position = Vector3.Lerp(transform.position, targetPosition, 10 * Time.deltaTime);
        }
	}
}
