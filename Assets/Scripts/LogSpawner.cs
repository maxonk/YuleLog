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

    public static void Spawn() {
        instance.spawn();
    }

    //----

    public Transform protoLog;

    void Awake() {
        _instance = this;
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1)) {
            spawn();
        }
    }

    void spawn() {
        var log = Instantiate(protoLog);
        log.transform.position = transform.position;
        log.transform.rotation = Random.rotation;
    }
}
