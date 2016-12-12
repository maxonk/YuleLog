using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class YuleCursor : MonoBehaviour {

    static YuleCursor _instance;
    public static YuleCursor instance {
        get {
            if (_instance == null) _instance = FindObjectOfType<YuleCursor>();
            return _instance;   
        }
    }

    public static Vector3 position {
        get {
            return instance.cursor.position;
        }
    }

    RectTransform cursor;
    Image visual;
    
    void Awake () {
        cursor = GetComponent<RectTransform>();
        visual = GetComponent<Image>();
        Cursor.visible = false;
        _instance = this;
    }
	

	void Update () {
        if (InputManager.Paused) {
            if (!Cursor.visible) Cursor.visible = true;
            if (visual.enabled) visual.enabled = false;
        } else {
            if (Cursor.visible) Cursor.visible = false;
            if (!visual.enabled) visual.enabled = true;

            if (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0)
                cursor.position = Input.mousePosition;

            Vector3 otherPosInput = new Vector3(cInput.GetAxisRaw("Move Cursor Horizontal"), cInput.GetAxisRaw("Move Cursor Vertical"));
            if (otherPosInput != Vector3.zero) {
                cursor.position += otherPosInput * 10f;
            }

        }
    }
}
