using UnityEngine;
using System;

public class Flame : MonoBehaviour {

    public Gradient gradient;
    public AnimationCurve animCurve, lerpCurve;
    new public Light light;

    const int FLAME_COUNT = 10;

    Vector3[] vertices_live;
    Vector3[] vertices_sim, vertices_simLerp, vertices_old;
    float[] flameHeights_sim, flameHeights_lerp, flameHeights_old;
    Color[] colors;
    
    Mesh mesh;

	void Awake () {
        flameHeights_sim = new float[FLAME_COUNT];
        flameHeights_lerp = new float[FLAME_COUNT];
        flameHeights_old = new float[FLAME_COUNT];

        mesh = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        vertices_live = new Vector3[2 * FLAME_COUNT + 1];
        vertices_sim = new Vector3[vertices_live.Length];
        vertices_simLerp = new Vector3[vertices_live.Length];
        vertices_old = new Vector3[vertices_sim.Length];
        mesh.vertices = vertices_live;

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

    public void updateSimData(LogBurner.BurnSimNode[] burnSimMap, float totalFuelPct, float delay) {
        Array.Copy(vertices_simLerp, vertices_old, vertices_old.Length);
        Array.Copy(flameHeights_lerp, flameHeights_old, flameHeights_old.Length);
        Array.Clear(flameHeights_sim, 0, flameHeights_sim.Length);
        
        //calculate heights and left/rightmost points
        Vector3 leftMostV = Vector3.zero, rightMostV = Vector3.zero;
        for (int i = 0; i < burnSimMap.Length; i++) {
            int f = Mathf.FloorToInt(i / (float)burnSimMap.Length * FLAME_COUNT);
            flameHeights_sim[f] += burnSimMap[i].heat;
            if (burnSimMap[i].position.x < leftMostV.x) {
                leftMostV = burnSimMap[i].position;
            } else if (burnSimMap[i].position.x > rightMostV.x) {
                rightMostV = burnSimMap[i].position;
            }
        }

        //left <--> right positional arraying
        vertices_sim[0] = leftMostV;
        for (int i = 1; i < vertices_sim.Length - 1; i++) {
            vertices_sim[i] = Vector3.Lerp(leftMostV, rightMostV, i / (float)vertices_sim.Length);
        }
        vertices_sim[vertices_sim.Length - 1] = rightMostV;
        
        simUpdateDelay = delay;
        timeSinceSimUpdate = 0f;
    }

    float simUpdateDelay;
    float timeSinceSimUpdate;
    public void update(LogBurner.BurnSimNode[] burnSimMap, float totalFuelPct) {
        timeSinceSimUpdate += Time.deltaTime;

        var lerpProgress = Mathf.Clamp01(timeSinceSimUpdate / simUpdateDelay);
        for(int i = 0; i < vertices_simLerp.Length; i++) {
            vertices_simLerp[i] = Vector3.Lerp(vertices_old[i], vertices_sim[i], lerpCurve.Evaluate(lerpProgress));
        }
        Array.Copy(vertices_simLerp, vertices_live, vertices_live.Length);

        var localUp = transform.InverseTransformDirection(Vector3.up);

        float avgFlameHeight = 0f, maxHeight = 0f;
        for (int i = 0, v = 1; i < FLAME_COUNT; i++, v += 2) {
            flameHeights_lerp[i] = Mathf.Lerp(flameHeights_old[i], flameHeights_sim[i], lerpCurve.Evaluate(lerpProgress));
            var curveTime = (Time.realtimeSinceStartup + (i * 0.43f)) % 1f;
            vertices_live[v] += localUp * flameHeights_lerp[i] * animCurve.Evaluate(curveTime) * totalFuelPct;
            avgFlameHeight += flameHeights_lerp[i];
            if (flameHeights_lerp[i] > maxHeight) maxHeight = flameHeights_lerp[i];
        }
        avgFlameHeight /= FLAME_COUNT;

        mesh.vertices = vertices_live;

        //color gradient calculation
        for (int i = 0; i < FLAME_COUNT; i++) {
            colors[i * 2 + 1] = gradient.Evaluate(flameHeights_lerp[i] / maxHeight);
        }
        mesh.colors = colors;

        light.transform.localPosition = localUp * avgFlameHeight / 4;
        light.intensity = avgFlameHeight;
    }
}
