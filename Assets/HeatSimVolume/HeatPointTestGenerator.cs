using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatPointTestGenerator : MonoBehaviour {

    [SerializeField] ComputeShader heatSimComputeShader;

    [SerializeField] Material visualizationMaterial;

    ComputeBuffer heatSimComputeBuffer;

    int _kernal;

    Color[] points;
    int pIndex;


    private void Start() {
        heatSimComputeBuffer = new ComputeBuffer(1024, 32);

        points = new Color[1024];
        for(int i = 0; i < 1024; i++) {
            points[i] = new Color(
                Random.Range(-1f, 1f), 
                Random.Range(0f, 2f), 
                Random.Range(-1f, 1f),
                0f);
        }
        pIndex = 0;

        heatSimComputeBuffer.SetData(points);

        _kernal = heatSimComputeShader.FindKernel("Kernel");

        heatSimComputeShader.SetBuffer(_kernal, "points", heatSimComputeBuffer);

        visualizationMaterial.SetBuffer("points", heatSimComputeBuffer);
    }

    // Update is called once per frame
    void Update () {
        //add points, increment point lives
        for(int i = 0; i < 20; i++) {
            points[pIndex] = new Color(
                Random.Range(-1f, 1f),
                Random.Range(0f, 2f),
                Random.Range(-1f, 1f),
                0f);
            pIndex++;
            if (pIndex >= points.Length) pIndex = 0;
        }

        //update points
        for(int i = 0; i < 1024; i++) {
            points[i].g += Time.deltaTime * 10f * Mathf.Pow(points[i].a, 2);
            points[i].a += Time.deltaTime;
        }

        heatSimComputeBuffer.SetData(points);

        heatSimComputeShader.Dispatch(0, 50, 50, 1);
	}
}
