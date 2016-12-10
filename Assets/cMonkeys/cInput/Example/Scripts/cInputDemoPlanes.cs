using UnityEngine;
using System.Collections;

public class cInputDemoPlanes : MonoBehaviour {

	public GUIText guiSpeed;
	public GUIText pauseText;
	public GUIText resetText;
	public GameObject bulletPrefab;

	float speed;
	float steer;
	float shootTimer;

	bool pause;
	bool menu;

	void Update() {
		// notice we use Unity's Input class here (instead of cInput) for calling the input config menus because we don't want the player to change this input
		// call our custom made GUI Menu
		if (Input.GetKeyDown(KeyCode.Escape) && !cInput.scanning) {
			cGUI.ToggleGUI();
		}

		// only check for inputs when the input menu is not up
		if (!cGUI.showingAnyGUI) {
			// toggle pause on/off - notice we use GetKeyDown for this because we don't want repeated input on this

			if (!pause) {
				// shoot a bullet every .25 seconds as long the 'Shoot' key is held down
				if (cInput.GetButton("Shoot") && Time.time > shootTimer + 0.25f) {
					Vector3 _leftShoot = new Vector3(transform.position.x - 1, transform.position.y, transform.position.z);
					Vector3 _rightShoot = new Vector3(transform.position.x + 1, transform.position.y, transform.position.z);
					GameObject _bulletL = (GameObject)Instantiate(bulletPrefab, _leftShoot, Quaternion.identity);
					_bulletL.tag = "Player";
					GameObject _bulletR = (GameObject)Instantiate(bulletPrefab, _rightShoot, Quaternion.identity);
					_bulletR.tag = "Player";
					shootTimer = Time.time;
				}

				// speed up
				if (cInput.GetKey("Up")) {
					speed += (.15f);
				}

				// slow down
				if (cInput.GetKey("Down")) {
					speed -= (.15f);
				}

				// steer left or right - notice we use GetAxis for this
				float horizMovement = cInput.GetAxis("Horizontal");
				steer = -horizMovement * 45;
				transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, steer);
				transform.Translate(Vector3.right * horizMovement * 30 * Time.deltaTime);

				// clamp the eulerangles
				steer = Mathf.Clamp(steer, -45, 45);
				// clamp min and max speed
				speed = Mathf.Clamp(speed, 5, 10f);
				// keep the plane at the same height and clamp the horizontal position
				float locX = Mathf.Clamp(transform.position.x, -24, 24f);
				transform.position = new Vector3(locX, 5, transform.position.z);
				// move the plane
				transform.Translate(Vector3.forward * speed * Time.deltaTime);

				// show speed on screen
				int _speed = (int)(speed * 10);
				guiSpeed.text = "Speed: " + _speed;
			}
		}

		// set timescale to 0 to pause the game
		if (pause) {
			Time.timeScale = 0;
			pauseText.enabled = true;
		} else {
			Time.timeScale = 1;
			pauseText.enabled = false;
		}
	}

	void OnEnable() {
		// subscribe to the OnGUIToggled event so we can pause/unpause when the cInput GUI is toggled
		cGUI.OnGUIToggled += TogglePause;
	}

	void OnDisable() {
		// unsubscribe to the OnGUIToggled event so we don't cause errors
		cGUI.OnGUIToggled -= TogglePause;

		if (resetText) {
			// show the restart text when the player is dead
			resetText.enabled = true;
		}
	}

	void TogglePause() {
		// pause or unpause the game depending on if any cGUI is showing
		pause = cGUI.showingAnyGUI;
	}

}
