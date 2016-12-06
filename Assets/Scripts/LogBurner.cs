﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LogBurner : MonoBehaviour, HeatSource {

    public float updateTime;
    public Gradient gradient;

    Mesh mesh;
    MeshFilter meshFilter;
    BurnSimNode[] burnSimMap;

    new Rigidbody rigidbody;
    new MeshCollider collider;

    class BurnSimNode {
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
        public float heat{
            get{
                return _heat;
            }
            private set {
                _heat = Mathf.Clamp01(value);
            }
        }
        Vector3 _originalPosition;
        public Vector3 position {
            get {
                return _originalPosition * fuel;
            }
        }
        public List<BurnSimNode> connections { get; private set; }
        public BurnSimNode(Vector3 vertexPosition) {
            heat = 0;
            fuel = 1;
            connections = new List<BurnSimNode>();
            _originalPosition = vertexPosition;
        }
        public void addConnection(BurnSimNode c) {
            if (!connections.Contains(c)) connections.Add(c);
        }
        public void addHeat(float h) {
            heat += h;
        }
        public void calculate() {
            foreach(BurnSimNode connection in connections) {
                deltaHeat += connection.heat / 100f;
            }
            deltaHeat *= Random.Range(-0.25f, 1f);

            fuel -= heat / 100f;
        }
        float deltaHeat;
        public void consume() {
            heat += deltaHeat;
        }
    }
    
	void Start() {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        mesh.MarkDynamic();

        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<MeshCollider>();

        StartCoroutine(simulateBurn_coroutine());
    }
	
    IEnumerator simulateBurn_coroutine() {
        var wait = new WaitForSeconds(updateTime);
        initializeBurnSim();
        while(simulateBurn()) {
            yield return wait;
        }
        Destroy(gameObject);
    }

    void initializeBurnSim() {
        var verts = mesh.vertices;
        burnSimMap = new BurnSimNode[verts.Length];
        for(int i = 0; i < burnSimMap.Length; i++) {
            burnSimMap[i] = new BurnSimNode(verts[i]);
        }

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

    bool simulateBurn() {
        foreach(BurnSimNode node in burnSimMap) {
            node.calculate();
            node.consume();
        }

        var nodeCount = burnSimMap.Length;
        var verts = new Vector3[nodeCount];
        var colors = new Color[nodeCount];
        float fuel = 0f;

        for (int i = 0; i < nodeCount; i++) {
            colors[i] = gradient.Evaluate(burnSimMap[i].heat);
            verts[i] = burnSimMap[i].position;
            fuel += burnSimMap[i].fuel;
        }

        mesh.colors = colors;
        mesh.vertices = verts;

        collider.sharedMesh = mesh;
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
                if ((heat > 0.15f) && (Random.Range(0f,1f) > 0.95f)) {
                    var sparkForceDir = Vector3.Cross(contactPoint.normal, Random.onUnitSphere);
                    SparkSpawner.Spawn(contactPoint.point + sparkForceDir, sparkForceDir * heat * Random.Range(5f,15f));
                }
            }
        }
    }
}