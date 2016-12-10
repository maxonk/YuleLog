using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LogSpawner : MonoBehaviour {

    //static
    static LogSpawner _instance;
    static LogSpawner instance {
        get {
            if (_instance == null) _instance = FindObjectOfType<LogSpawner>();
            return _instance;
        }
        set {
            _instance = value;
        }
    }

    public static void Spawn(float scaleModifier = 1f) {
        instance.spawn(scaleModifier);
    }
    //----

    public Transform protoLog;

    Material visualizerMat;
    Color visualizerFullColor;
    public Transform sizeVisualizer;

    public AnimationCurve yzScaleCurve, xScaleCurve;
    float scaleCurvePosition;

    void Awake() {
        _instance = this;
        visualizerMat = sizeVisualizer.GetComponent<Renderer>().material;
        visualizerFullColor = visualizerMat.GetColor("_TintColor");
    }

    float visualizer_fadeLerp;
    void Update() {

        var logScaleInput = cInput.GetAxis("Scale Log");
        if (InputManager.Paused) logScaleInput = 0;

        if (logScaleInput == 0) {
            visualizer_fadeLerp = Mathf.Clamp01(visualizer_fadeLerp - Time.deltaTime);
            visualizerMat.SetColor("_TintColor", Color.Lerp(Color.black, visualizerFullColor, visualizer_fadeLerp));
        } else {
            visualizer_fadeLerp = 1f;
            scaleCurvePosition = Mathf.Clamp01(scaleCurvePosition + logScaleInput);
            sizeVisualizer.localScale = scale(scaleCurvePosition);
        }

        if (!InputManager.Paused && cInput.GetKeyDown("Get Log")) {
            spawn(scaleCurvePosition);
        }
    }

    float midScale {
        get {
            return yzScaleCurve.Evaluate(0.5f);
        }
    }

    public static Vector3 scale(float pct) {
        var x = instance.xScaleCurve.Evaluate(Mathf.Clamp01(pct));
        var yz = instance.yzScaleCurve.Evaluate(Mathf.Clamp01(pct));
        return new Vector3(x, yz, yz);
    }

    void spawn(float scalePct) {
        var log = Instantiate(protoLog).GetComponent<LogBurner>();
        log.transform.position = transform.position;
        log.transform.rotation = Random.rotation;
        log.scaleModifier = scalePct;
    }

}
