using UnityEngine;
using System.Collections;

public class ObjectMover : MonoBehaviour {

    public LayerMask moveableObjectMask;

    MoveableObject grabbedObject;

    Plane grabPlane;

    void Awake() {
        grabPlane = new Plane(Vector3.forward, Vector3.zero);
    }

    void grab(MoveableObject obj) {
        if (obj == null) return;
        grabbedObject = obj;
        grabbedObject.grabbed = true;
    }

    void release() {
        if (grabbedObject == null) return;
        grabbedObject.grabbed = false;
        grabbedObject = null;
    }

	void Update () {
        var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hitInfo;
            if(Physics.Raycast(mouseRay, out hitInfo, 25, moveableObjectMask)) {
                grab(hitInfo.collider.GetComponent<MoveableObject>());
            }
        }
        if (Input.GetMouseButtonUp(0)) {
            release();
        }

        if(grabbedObject != null) {
            float planeDist;
            if (grabPlane.Raycast(mouseRay, out planeDist)) {
                var mousePointInWorld = mouseRay.GetPoint(planeDist);
                if (mousePointInWorld.y < 0) mousePointInWorld.y = 0;
                grabbedObject.targetPosition = mousePointInWorld;
            }
        }
	}
}
