using UnityEngine;
using System;

public class Flame : MonoBehaviour {

    public Gradient gradient;
    public AnimationCurve animCurve;

    const int VERTEX_COUNT = 12;

    Vector3[] vertices;
    Vector3[] flamePositions;
    float[] flameHeights;
    Color[] colors;
    
    Mesh mesh;

	void Awake () {
        flameHeights = new float[VERTEX_COUNT - 2];

        mesh = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        vertices = new Vector3[VERTEX_COUNT];
        flamePositions = new Vector3[VERTEX_COUNT];
        flamePositions[0] = Vector3.left * 3;
        flamePositions[VERTEX_COUNT - 1] = Vector3.right * 3;
        for (int i = 0; i < VERTEX_COUNT - 2; i++) {
            flamePositions[i + 1] = Vector3.Lerp(Vector3.left, Vector3.right, i / (float)(VERTEX_COUNT - 2)) * 4;
        }
        mesh.vertices = flamePositions;

        var triangles = new int[(VERTEX_COUNT - 2) * 6];
        for(int i = 0, v = 1; v < VERTEX_COUNT - 1; i += 6, v++) {
            triangles[i] = 0;
            triangles[i + 1] = v;
            triangles[i + 2] = VERTEX_COUNT - 1;
            triangles[i + 5] = 0;
            triangles[i + 4] = v;
            triangles[i + 3] = VERTEX_COUNT - 1;
        }
        mesh.triangles = triangles;

        colors = new Color[VERTEX_COUNT];
        for(int i = 0; i < VERTEX_COUNT; i++) {
            colors[i] = Color.black;
        }
        mesh.colors = colors;
    }

    public void updateSimData(LogBurner.BurnSimNode[] burnSimMap, float totalFuelPct) {
        Array.Clear(flameHeights, 0, flameHeights.Length);

        for (int i = 0; i < burnSimMap.Length; i++) {
            int f = Mathf.FloorToInt(i / (float)burnSimMap.Length * (VERTEX_COUNT - 2));
            flameHeights[f] += burnSimMap[i].heat;
        }


        float maxHeight = 0f;
        for (int i = 0; i < VERTEX_COUNT - 2; i++) {
            flamePositions[i + 1] = Vector3.Lerp(Vector3.left, Vector3.right, i / (float)(VERTEX_COUNT - 2)) * totalFuelPct * 4;
            if (flameHeights[i] > maxHeight) maxHeight = flameHeights[i];
        }

        flamePositions[0] = Vector3.left * totalFuelPct * 3;
        flamePositions[VERTEX_COUNT - 1] = Vector3.right * totalFuelPct * 3;
        
        for (int i = 0; i < VERTEX_COUNT - 2; i++) {
            flamePositions[i + 1] = Vector3.Lerp(Vector3.left, Vector3.right, i / (float)(VERTEX_COUNT - 2)) * totalFuelPct * 4;
            colors[i + 1] = gradient.Evaluate(flameHeights[i] / maxHeight);
        }
        mesh.colors = colors;
    }

    public void update(LogBurner.BurnSimNode[] burnSimMap, float totalFuelPct) {
        Array.Copy(flamePositions, vertices, VERTEX_COUNT);
        var localUp = transform.InverseTransformDirection(Vector3.up);
        for (int i = 0; i < VERTEX_COUNT - 2; i++) {
            var curveTime = (Time.realtimeSinceStartup + (i * 0.43f)) % 1f;
            vertices[i + 1] += localUp * flameHeights[i] * animCurve.Evaluate(curveTime);
        }
        mesh.vertices = vertices;
    }
}
