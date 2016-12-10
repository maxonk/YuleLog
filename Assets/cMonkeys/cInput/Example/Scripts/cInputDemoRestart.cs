using UnityEngine;
using System.Collections;

public class cInputDemoRestart : MonoBehaviour {

	public GUIText resetText;

	void Start() {
		resetText.enabled = false;
	}

	void OnMouseDown() {
		Application.LoadLevel(Application.loadedLevel);
	}
}
