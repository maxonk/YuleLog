using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LogBurner : MonoBehaviour {

    public float updateTime;
    public Gradient gradient;

    Mesh mesh;
    MeshFilter meshFilter;
    BurnSimNode[] burnSimMap;

    class BurnSimNode {
        float _heat;
        public float heat{
            get{
                return _heat;
            }
            private set {
                _heat = Mathf.Clamp01(value);
            }
        }
        public List<BurnSimNode> connections { get; private set; }
        public BurnSimNode() {
            heat = 0;
            connections = new List<BurnSimNode>();
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
        }
        float deltaHeat;
        public void consume() {
            heat += deltaHeat;
        }
    }
    
	void Start() {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;

        StartCoroutine(simulateBurn_coroutine());
    }
	
    IEnumerator simulateBurn_coroutine() {
        var wait = new WaitForSeconds(updateTime);
        initializeBurnSim();
        while(true) {
            simulateBurn();
            yield return wait;
        }
    }

    void initializeBurnSim() {
        var verts = mesh.vertices;
        burnSimMap = new BurnSimNode[mesh.vertices.Length];
        for(int i = 0; i < burnSimMap.Length; i++) {
            burnSimMap[i] = new BurnSimNode();
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

    void simulateBurn() {
        foreach(BurnSimNode node in burnSimMap) {
            node.calculate();
            node.consume();
        }
    }

	void Update() {
        var colors = new Color[burnSimMap.Length];
        for(int i = 0; i < burnSimMap.Length; i++) {
            colors[i] = gradient.Evaluate(burnSimMap[i].heat);
        }
        mesh.colors = colors;
	}

    public void OnCollisionEnter(Collision collision) {
        if (collision.collider.name == "coal") {
            var verts = mesh.vertices;
            foreach (ContactPoint contactPoint in collision.contacts) {
                int closestPoint = -1;
                float closestPointDist = float.MaxValue;
                for (int i = 0; i < verts.Length; i++) {
                    var dist = (transform.TransformPoint(verts[i]) - contactPoint.point).sqrMagnitude;
                    if (dist < closestPointDist) {
                        closestPoint = i;
                        closestPointDist = dist;
                    }
                }
                if (closestPoint >= 0)
                    burnSimMap[closestPoint].addHeat(0.2f);
            }
        }
    }
}
