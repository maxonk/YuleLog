using UnityEngine;
using System;

public class Flame : MonoBehaviour {

    public Gradient gradient;
    public AnimationCurve animCurve;
    public Light light;

    const int FLAME_COUNT = 5;

    Vector3[] vertices_live;
    Vector3[] vertices_sim;
    float[] flameHeights;
    Color[] colors;
    
    Mesh mesh;

	void Awake () {
        flameHeights = new float[FLAME_COUNT];

        mesh = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        vertices_live = new Vector3[2 * FLAME_COUNT + 1];
        vertices_sim = new Vector3[vertices_live.Length];
        mesh.vertices = vertices_sim;

        var triangles = new int[FLAME_COUNT * 6];
        for(int i = 0, v = 1; v < vertices_sim.Length; i += 6, v += 2) {
            int left = Mathf.Max(v - 3, 0);
            int right = Mathf.Min(v + 3, vertices_sim.Length - 1);
            triangles[i] = left;
            triangles[i + 1] = v;
            triangles[i + 2] = right;
            triangles[i + 5] = left;
            triangles[i + 4] = v;
            triangles[i + 3] = right;
        }
        mesh.triangles = triangles;

        colors = new Color[vertices_live.Length];
        for(int i = 0; i < colors.Length; i++) {
            colors[i] = Color.black;
        }
        mesh.colors = colors;
    }

    public void updateSimData(LogBurner.BurnSimNode[] burnSimMap, float totalFuelPct) {
        Array.Clear(flameHeights, 0, flameHeights.Length);
        
        //calculate heights and left/rightmost points
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

        //find max height
        float maxHeight = 0f;
        for (int i = 0; i < flameHeights.Length; i++) {
            if (flameHeights[i] > maxHeight) maxHeight = flameHeights[i];
        }

        //left <--> right positional arraying
        vertices_sim[0] = leftMostV;
        for (int i = 1; i < vertices_sim.Length - 1; i++) {
            vertices_sim[i] = Vector3.Lerp(leftMostV, rightMostV, i / (float)vertices_sim.Length);
        }
        vertices_sim[vertices_sim.Length - 1] = rightMostV;
        
        //color gradient calculation
        for (int i = 0; i < FLAME_COUNT; i++) {
            colors[i * 2 + 1] = gradient.Evaluate(flameHeights[i] / maxHeight);
        }
        mesh.colors = colors;
    }

    public void update(LogBurner.BurnSimNode[] burnSimMap, float totalFuelPct) {
        Array.Copy(vertices_sim, vertices_live, vertices_live.Length);
        var localUp = transform.InverseTransformDirection(Vector3.up);

        var avgFlameHeight = 0f;
        for (int i = 0, v = 1; i < FLAME_COUNT; i++, v += 2) {
            var curveTime = (Time.realtimeSinceStartup + (i * 0.43f)) % 1f;
            vertices_live[v] += localUp * flameHeights[i] * animCurve.Evaluate(curveTime) * totalFuelPct;
            avgFlameHeight += flameHeights[i];
        }
        avgFlameHeight /= FLAME_COUNT;

        mesh.vertices = vertices_live;

        light.transform.localPosition = localUp * avgFlameHeight;
        light.intensity = avgFlameHeight / 5f;
    }
}
