using UnityEngine;
using System.Collections;

public class SparkSpawner : MonoBehaviour {

    //static
    static SparkSpawner _instance;
    static SparkSpawner instance {
        get {
            if (_instance == null) _instance = FindObjectOfType<SparkSpawner>();
            return _instance;
        }
        set {
            _instance = value;
        }
    }

    public static void Spawn(Vector3 origin, Vector3 force) {
        instance.spawn(origin, force);
    }

    //----

    public Transform protoSpark;
    
    void Awake() {
        _instance = this;
    }

    void spawn(Vector3 origin, Vector3 force) {
        if (Physics.CheckSphere(origin, 0.05f)) return;
        var spark = Instantiate(protoSpark, origin, Random.rotation);
        Rigidbody sparkPhysbody = spark.GetComponent<Rigidbody>();
        if(sparkPhysbody != null) {
            sparkPhysbody.AddForce(force);
        }
    }
}
