using UnityEngine;
using System;

public class LogRenderer : MonoBehaviour {
    
    public Mesh mesh { get; private set; }
    MeshFilter meshFilter;

    Color[] colors_sim, colors_old, colors_lerp;
    Vector3[] vertices;

    public Gradient gradient;

    public void setup (LogBurner log) {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        mesh.MarkDynamic();
        vertices = mesh.vertices;
        colors_sim = new Color[vertices.Length];
        colors_old = new Color[vertices.Length];
        colors_lerp = new Color[vertices.Length];
        for(int i = 0; i < colors_sim.Length; i++) {
            colors_sim[i] = gradient.Evaluate(0f);
            colors_old[i] = colors_lerp[i] = colors_sim[i];
        }
        mesh.colors = colors_lerp;
    }

    public void updateFromSim(LogBurner.BurnSimNode[] burnSimMap, float updateTime) {
        Array.Copy(colors_lerp, colors_old, colors_old.Length);
        for (int i = 0; i < burnSimMap.Length; i++) {
            colors_sim[i] = gradient.Evaluate(burnSimMap[i].heat);
            vertices[i] = burnSimMap[i].position;
        }
        mesh.vertices = vertices;
        timeCounter = 0f;
        timeUntilNextUpdate = updateTime;
    }

    float timeUntilNextUpdate, timeCounter;
    public void update () {
        timeCounter += Time.deltaTime;
        float lerpProgress = timeCounter / timeUntilNextUpdate;

        for (int i = 0; i < vertices.Length; i++) {
            colors_lerp[i] = Color.Lerp(colors_old[i], colors_sim[i], lerpProgress);
        }
        mesh.colors = colors_lerp;
    }
}
