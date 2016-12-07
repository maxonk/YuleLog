using UnityEngine;
using System;

public class Flame : MonoBehaviour {

    public Gradient gradient;
    public AnimationCurve animCurve;

    const int FLAME_COUNT = 10;

    Vector3[] vertices;
    Vector3[] flamePositions;
    float[] flameHeights;
    Color[] colors;
    
    Mesh mesh;

	void Awake () {
        flameHeights = new float[FLAME_COUNT];

        mesh = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        vertices = new Vector3[FLAME_COUNT + 2];
        flamePositions = new Vector3[FLAME_COUNT + 2];
        flamePositions[0] = Vector3.left * 3;
        flamePositions[FLAME_COUNT + 1] = Vector3.right * 3;
        for (int i = 0; i < FLAME_COUNT; i++) {
            flamePositions[i + 1] = Vector3.Lerp(Vector3.left, Vector3.right, i / (float)(FLAME_COUNT)) * 4;
        }
        mesh.vertices = flamePositions;

        var triangles = new int[FLAME_COUNT * 6];
        for(int i = 0, v = 1; v < FLAME_COUNT + 1; i += 6, v++) {
            triangles[i] = 0;
            triangles[i + 1] = v;
            triangles[i + 2] = FLAME_COUNT + 1;
            triangles[i + 5] = 0;
            triangles[i + 4] = v;
            triangles[i + 3] = FLAME_COUNT + 1;
        }
        mesh.triangles = triangles;

        colors = new Color[FLAME_COUNT + 2];
        for(int i = 0; i < FLAME_COUNT + 2; i++) {
            colors[i] = Color.black;
        }
        mesh.colors = colors;
    }

    public void updateSimData(LogBurner.BurnSimNode[] burnSimMap, float totalFuelPct) {
        Array.Clear(flameHeights, 0, flameHeights.Length);

        Vector3 leftMostV = Vector3.zero, rightMostV = Vector3.zero;
        for (int i = 0; i < burnSimMap.Length; i++) {
            int f = Mathf.FloorToInt(i / (float)burnSimMap.Length * FLAME_COUNT);
            flameHeights[f] += burnSimMap[i].heat;
            if (burnSimMap[i].position.x < leftMostV.x) {
                leftMostV = burnSimMap[i].position;
            } else if (burnSimMap[i].position.x > rightMostV.x) {
                rightMostV = burnSimMap[i].position;
            }
        }


        float maxHeight = 0f;
        for (int i = 0; i < FLAME_COUNT; i++) {
            flamePositions[i + 1] = Vector3.Lerp(leftMostV, rightMostV, i / (float)FLAME_COUNT) * totalFuelPct;
            if (flameHeights[i] > maxHeight) maxHeight = flameHeights[i];
            //also get left and right points here
        }
        flamePositions[0] = leftMostV;
        flamePositions[FLAME_COUNT + 1] = rightMostV;
        
        for (int i = 0; i < FLAME_COUNT; i++) {
            colors[i + 1] = gradient.Evaluate(flameHeights[i] / maxHeight);
        }
        mesh.colors = colors;
    }

    public void update(LogBurner.BurnSimNode[] burnSimMap, float totalFuelPct) {
        Array.Copy(flamePositions, vertices, FLAME_COUNT + 2);
        var localUp = transform.InverseTransformDirection(Vector3.up);
        for (int i = 0; i < FLAME_COUNT; i++) {
            var curveTime = (Time.realtimeSinceStartup + (i * 0.43f)) % 1f;
            vertices[i + 1] += localUp * flameHeights[i] * animCurve.Evaluate(curveTime);
        }
        mesh.vertices = vertices;
    }
}
