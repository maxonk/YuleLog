using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InputManager : MonoBehaviour {

    static InputManager _instance;
    static InputManager instance {
        get {
            if (_instance == null) _instance = FindObjectOfType<InputManager>();
            return _instance;
        }
    }
    
    public static bool Paused {
        get {
            return (cGUI.showingInputGUI || instance.mainMenuCanvas.gameObject.activeSelf);
        }
    }
    //---

    public Canvas mainMenuCanvas;
    public GUISkin guiSkin;
    public Color menuColor = Color.black;

    void Start() {
        _instance = this;
        // initialize cInput
        cInput.Init();
        // cGUI SETUP
        // set the guiskin for cGUI
        cGUI.cSkin = guiSkin;
        cGUI.bgColor = menuColor;
        // set the maxsize of the menu window. If this is greater then the screen size it will scaled to fullscreen.
        // the menu size will be clamped to the max screen size, so setting the size really high will garantee you 
        // that the menu will be filling the screen. If you don't set the window size it will default to 1024X600.
        cGUI.windowMaxSize = new Vector2(1024, 600);

        // cINPUT SETUP
        // first we setup the allowed modifier keys, by default there will be no modifiers. If you don't want modifier keys skip this step
        // keep in mind that if a key is set as modifier it can't be used as a normal input anymore!
        cInput.AddModifier(KeyCode.LeftShift);
        cInput.AddModifier(KeyCode.RightShift);
        cInput.AddModifier(KeyCode.LeftAlt);
        cInput.AddModifier(KeyCode.RightAlt);
        cInput.AddModifier(KeyCode.LeftControl);
        cInput.AddModifier(KeyCode.RightControl);

        // setting up the default inputkeys...
        cInput.SetKey("Scale Log Up", "Mouse Wheel Up", "UpArrow");
        cInput.SetKey("Scale Log Down", "Mouse Wheel Down", "DownArrow");
        cInput.SetKey("Get Log", "Mouse1", "Space");

        // we define an axis like this:
        cInput.SetAxis("Scale Log", "Scale Log Down", "Scale Log Up", 0.5f); // sets the 'Pause' input to "P" - notice we didn't set up a secondary input-this will be defaulted to 'None'
    }

    public void ExitButtonClicked() {
        mainMenuCanvas.gameObject.SetActive(false);
        if (cGUI.showingInputGUI) cGUI.ToggleGUI();
    }

    public void InputSettingsButtonClicked() {
        mainMenuCanvas.gameObject.SetActive(false);
        cGUI.ShowInputGUI();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) && !cInput.scanning) {
            mainMenuCanvas.gameObject.SetActive(!mainMenuCanvas.gameObject.activeSelf);
            if (cGUI.showingInputGUI) cGUI.ToggleGUI();
        }
    }

    public void setSimUpdateRate(Slider slider) {
        LogBurner.SimUpdateRate = slider.value;
    }
}
