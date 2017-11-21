using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatVis : MonoBehaviour {

    static HeatVis _instance;
    public static HeatVis instance {
        get {
            if (_instance == null) _instance = FindObjectOfType<HeatVis>();
            return _instance;
        }
    }

    Camera _camera;
    new Camera camera {
        get {
            if (_camera == null) _camera = GetComponent<Camera>();
            return _camera;
        }
    }
    
    [SerializeField] ComputeShader heatSimComputeShader;
    [SerializeField] Material visualizationMaterial;
    [SerializeField] RenderTexture heatSimVolume, heatSimVolumeLastFrame;

    ComputeBuffer newPointsBuffer, heatMapBuffer;

    int _simulateKernel, _updateDataKernel, _testDataKernel;

    static Vector4[] points;
    static int pIndex;

    Vector3[] frustumNear, frustumFar;
    Vector4[] frustumNearV4, frustumFarV4;

    RenderTexture generate3DRenderTexture() {
        var ret = new RenderTexture(512, 128, 0, RenderTextureFormat.RFloat);
        ret.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        ret.filterMode = FilterMode.Bilinear;
        ret.wrapMode = TextureWrapMode.Clamp;
        ret.volumeDepth = 512;
        ret.enableRandomWrite = true;
        ret.Create();
        return ret;
    }

    private void Start() {
        frustumNear = new Vector3[4];
        frustumFar = new Vector3[4];
        frustumNearV4 = new Vector4[4];
        frustumFarV4 = new Vector4[4];
        OnFrustumMoved();

        _simulateKernel = heatSimComputeShader.FindKernel("simulate");
        _updateDataKernel = heatSimComputeShader.FindKernel("updateData");
        _testDataKernel = heatSimComputeShader.FindKernel("testData");

        heatSimVolume = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "_HeatSimVolume", heatSimVolume);
        heatSimComputeShader.SetTexture(_updateDataKernel, "_HeatSimVolume", heatSimVolume);
        heatSimComputeShader.SetTexture(_testDataKernel, "_HeatSimVolume", heatSimVolume);
        Shader.SetGlobalTexture("_HeatSimVolume", heatSimVolume);

        heatSimVolumeLastFrame = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "_HeatSimVolumeLast", heatSimVolumeLastFrame);
        heatSimComputeShader.SetTexture(_updateDataKernel, "_HeatSimVolumeLast", heatSimVolumeLastFrame);
        heatSimComputeShader.SetTexture(_testDataKernel, "_HeatSimVolumeLast", heatSimVolumeLastFrame);

        points = new Vector4[512];
        newPointsBuffer = new ComputeBuffer(512, sizeof(float) * 4);
        heatSimComputeShader.SetBuffer(_simulateKernel, "newPoints", newPointsBuffer);
        

        camera.depthTextureMode = DepthTextureMode.Depth;

    }

    public static void SubmitHeatPoints(List<Vector3> newPoints) {
        instance.submitHeatPoints(newPoints);
    }

    public void submitHeatPoints(List<Vector3> newPoints) {
        for (int i = 0; i < newPoints.Count; i++) {
            //convert point to volume space
            points[pIndex] = new Vector4( // + offset, * (make it 0->1) * resolution
                Mathf.Clamp01((newPoints[i].x + 4) * 0.125f) * 512, // 8 wide
                Mathf.Clamp01((newPoints[i].y + 0) * 0.167f) * 128, // 6 tall
                Mathf.Clamp01((newPoints[i].z + 4) * 0.125f) * 512, // 8 deep
                1f);
            pIndex++;
            if (pIndex >= points.Length) pIndex = 0;
        }
        
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination, visualizationMaterial);
    }

    void OnFrustumMoved() {
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumNear);
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumFar);
        for (int i = 0; i < 4; i++) {
            frustumNearV4[i] = transform.TransformPoint(frustumNear[i]);
            frustumFarV4[i] = transform.TransformPoint(frustumFar[i]);
        }

        //these debug logs all validate assumptions
        Shader.SetGlobalVector("_FrustumNearBottomLeft", frustumNearV4[0]);
        //Debug.Log("_FrustumNearBottomLeft  " + frustumNearV4[0]);
        Shader.SetGlobalVector("_FrustumFarBottomLeft", frustumFarV4[0]);
        //Debug.Log("_FrustumFarBottomLeft  " + frustumFarV4[0]);
        Shader.SetGlobalVector("_FrustumNearTopLeft", frustumNearV4[1]);
        //Debug.Log("_FrustumNearTopLeft  " + frustumNearV4[1]);
        Shader.SetGlobalVector("_FrustumFarTopLeft", frustumFarV4[1]);
        //Debug.Log("_FrustumFarTopLeft  " + frustumFarV4[1]); //actually top left
        Shader.SetGlobalVector("_FrustumNearTopRight", frustumNearV4[2]);
        //Debug.Log("_FrustumNearTopRight  " + frustumNearV4[2]);
        Shader.SetGlobalVector("_FrustumFarTopRight", frustumFarV4[2]);
        //Debug.Log("_FrustumFarTopRight  " + frustumFarV4[2]);
        Shader.SetGlobalVector("_FrustumNearBottomRight", frustumNearV4[3]);
        //Debug.Log("_FrustumNearBottomRight  " + frustumNearV4[3]);
        Shader.SetGlobalVector("_FrustumFarBottomRight", frustumFarV4[3]);
        //Debug.Log("_FrustumFarBottomRight  " + frustumFarV4[3]);
    }

    bool simulate_update_toggle = false;
    private void FixedUpdate() {
        if (simulate_update_toggle) {
            heatSimComputeShader.SetFloat("_Time", Time.time);
            heatSimComputeShader.Dispatch(_simulateKernel, 16, 1, 16);
            simulate_update_toggle = false;
        } else {
            heatSimComputeShader.Dispatch(_updateDataKernel, 16, 1, 16);
            simulate_update_toggle = true;
        }
    }

    void Update() {
        if (transform.hasChanged) {
            OnFrustumMoved();
            transform.hasChanged = false;
        }

        if (Input.GetKeyDown(KeyCode.F)) {
            heatSimComputeShader.Dispatch(_testDataKernel, 16, 1, 16);
        }

        newPointsBuffer.SetData(points);

        for (int i = 0; i < 512; i++) {
            points[i].x = 0;
            points[i].y = 0;
            points[i].z = 0;
            points[i].w = 0;
        }
    }

    private void OnDisable() {
        newPointsBuffer.Release();
        DestroyImmediate(heatSimVolume);
        DestroyImmediate(heatSimVolumeLastFrame);
        heatSimVolume = null;
        heatSimVolumeLastFrame = null;
    }
}
