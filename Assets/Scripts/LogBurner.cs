using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class LogBurner : MonoBehaviour, HeatSource {
    
    public static float SimUpdateRate = 2f;
    float flameUpdateDelay {
        get {
            return Mathf.Min(SimUpdateRate / 2f, 0.1f);
        }
    }

    public Gradient gradient;


    LogRenderer logRenderer;
    public BurnSimNode[] burnSimMap;

    LineRenderer lineRenderer;
    Vector3[] flamePositions;

    new Rigidbody rigidbody;
    new MeshCollider collider;

    float fuel;
    float maxfuel;

    Flame flame;

    public float scaleModifier = 1f;

	void Start() {
        logRenderer = GetComponentInChildren<LogRenderer>();
        logRenderer.setup(this);

        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<MeshCollider>();

        flame = GetComponentInChildren<Flame>();

        StartCoroutine(simulateBurn_coroutine());
        StartCoroutine(generateHeatPoints_coroutine());
    }
    
    IEnumerator generateHeatPoints_coroutine() {
        Vector4[] heatPoints = new Vector4[burnSimMap.Length];
        for (int i = 0; i < heatPoints.Length; i++) heatPoints[i] = new Vector4();
       // float xInfluence, yInfluence;
        while (isActiveAndEnabled) {
            for(int i = 0; i < burnSimMap.Length; i++) {
                var node = burnSimMap[i];

             //   xInfluence = UnityEngine.Random.Range(-0.5f, 0.5f);
             //   yInfluence = UnityEngine.Random.Range(-0.5f, 0.5f);
                
                heatPoints[i] = transform.TransformPoint(burnSimMap[i].position);
                heatPoints[i].w = burnSimMap[i].heat * Time.deltaTime * 10f;
            }
            HeatVis.SubmitHeatPoints(heatPoints);
            yield return null;
        }
    }
	
    IEnumerator simulateBurn_coroutine() {
        initializeBurnSim();
        while(simulateBurn(SimUpdateRate)) {
            yield return new WaitForSeconds(flameUpdateDelay);
            flame.updateSimData(burnSimMap, fuel / maxfuel, SimUpdateRate);
            yield return new WaitForSeconds(SimUpdateRate - flameUpdateDelay);
        }
        Destroy(gameObject);
    }

    void initializeBurnSim() {
        var mesh = logRenderer.mesh;
        var vertices = mesh.vertices;
        var scale = LogSpawner.scale(scaleModifier);
        burnSimMap = new BurnSimNode[vertices.Length];
        for(int i = 0; i < burnSimMap.Length; i++) {
            burnSimMap[i] = new BurnSimNode(Vector3.Scale(vertices[i], scale), this);
        }

        maxfuel = vertices.Length;

        var triangles = mesh.triangles;
        for(int i = 0; i < triangles.Length; i += 3) {
            burnSimMap[triangles[i]].addConnection(burnSimMap[triangles[i + 1]]);
            burnSimMap[triangles[i]].addConnection(burnSimMap[triangles[i + 2]]);

            burnSimMap[triangles[i + 1]].addConnection(burnSimMap[triangles[i + 2]]);
            burnSimMap[triangles[i + 1]].addConnection(burnSimMap[triangles[i]]);

            burnSimMap[triangles[i + 2]].addConnection(burnSimMap[triangles[i + 1]]);
            burnSimMap[triangles[i + 2]].addConnection(burnSimMap[triangles[i]]);
        }
    }

    bool simulateBurn(float updateTime) {
        foreach(BurnSimNode node in burnSimMap) {
            node.calculate();
            node.consume();
        }
        
        fuel = 0f;
        for (int i = 0; i < burnSimMap.Length; i++) {
            fuel += burnSimMap[i].fuel;
        }

        logRenderer.updateFromSim(burnSimMap, updateTime);

        collider.sharedMesh = logRenderer.mesh;
        rigidbody.WakeUp();

        return fuel > 1f;
    }

    public float getHeat(Vector3 pos) {
        var closestNode = closestNodeTo(pos);
        return closestNode.heat * 0.2f;
    }

    BurnSimNode closestNodeTo(Vector3 pos) {
        int closestPoint = 0;
        float closestPointDist = (burnSimMap[0].position - pos).sqrMagnitude;
        for (int i = 1; i < burnSimMap.Length; i++) {
            var dist = (transform.TransformPoint(burnSimMap[i].position) - pos).sqrMagnitude;
            if (dist < closestPointDist) {
                closestPoint = i;
                closestPointDist = dist;
            }
        }
        return burnSimMap[closestPoint];
    }
    
    public void OnCollisionEnter(Collision collision) {
        var heatSource = collision.collider.GetComponent<HeatSource>();
        if (heatSource != null) {
            foreach (ContactPoint contactPoint in collision.contacts) {
                var heat = heatSource.getHeat(contactPoint.point);
                var closestNode = closestNodeTo(contactPoint.point);
                closestNode.addHeat(heat);
                if ((heat > 0.15f) && (UnityEngine.Random.Range(0f,1f) > 0.75f)) {
                    var sparkForceDir = Vector3.Cross(contactPoint.normal, UnityEngine.Random.onUnitSphere);
                    SparkSpawner.Spawn(contactPoint.point + (sparkForceDir * 0.5f), sparkForceDir * heat * UnityEngine.Random.Range(1f, 5f));
                }
            }
        }
    }

    void Update() {
        logRenderer.update();
        flame.update(burnSimMap, fuel/maxfuel);
    }

    public AnimationCurve scaleToHeatSimCurve;
    float _heatScaleModifier;
    float heatScaleModifier {
        get {
            if (_heatScaleModifier == 0f) {
                _heatScaleModifier = scaleToHeatSimCurve.Evaluate(scaleModifier);
            }
            return _heatScaleModifier;
        }
    }
    public AnimationCurve scaleToHeatTransferSimCurve;
    float _heatTransferModifier;
    float heatTransferModifier {
        get {
            if (_heatTransferModifier == 0f) {
                _heatTransferModifier = scaleToHeatTransferSimCurve.Evaluate(scaleModifier);
            }
            return _heatTransferModifier;
        }
    }
    public AnimationCurve scaleToFuelConsumptionSimCurve;
    float _fuelConsumptionModifier;
    float fuelConsumptionModifier {
        get {
            if (_fuelConsumptionModifier == 0f) {
                _fuelConsumptionModifier = scaleToFuelConsumptionSimCurve.Evaluate(scaleModifier);
            }
            return _fuelConsumptionModifier;
        }
    }


    public class BurnSimNode {
        LogBurner _log;
        float _fuel;
        public float fuel {
            get {
                return _fuel;
            }
            set {
                _fuel = Mathf.Clamp01(value);
            }
        }
        float _heat;
        public float heat {
            get {
                return _heat * _log.heatScaleModifier;
            }
            private set {
                _heat = Mathf.Clamp01(value);
            }
        }
        Vector3 _originalPosition;
        Vector3 _position;
        public Vector3 position {
            get {
                return _position;
            }
        }
        public List<BurnSimNode> connections { get; private set; }
        public BurnSimNode(Vector3 vertexPosition, LogBurner log) {
            heat = 0;
            fuel = 1;
            connections = new List<BurnSimNode>();
            _originalPosition = vertexPosition;
            _log = log;
        }
        public void addConnection(BurnSimNode c) {
            if (!connections.Contains(c)) connections.Add(c);
        }
        public void addHeat(float h) {
            heat = _heat + h;
        }
        public void calculate() {
            foreach (BurnSimNode connection in connections) {
                deltaHeat += connection.heat * fuel * _log.heatTransferModifier;
            }
            deltaHeat *= UnityEngine.Random.Range(-0.35f, 1f);

            fuel -= _heat * _log.fuelConsumptionModifier;
            _position = _originalPosition * fuel;
        }
        float deltaHeat;
        public void consume() {
            heat = _heat + deltaHeat;
        }
    }
}
