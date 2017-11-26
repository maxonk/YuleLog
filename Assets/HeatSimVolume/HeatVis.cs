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

    [SerializeField] RenderTexture 
        velocityHeatSimVol1, velocityHeatSimVol2,
        fuelSmokeSimVol1, fuelSmokeSimVol2;
    bool vol12toggle = false;

    ComputeBuffer newPointsBuffer, heatMapBuffer;

    int _simulateKernel, _testDataKernel;

    static Vector4[] points;

    Vector3[] frustumNear, frustumFar;
    Vector4[] frustumNearV4, frustumFarV4;

    RenderTexture generate3DRenderTexture() {
        var ret = new RenderTexture(256, 64, 0, RenderTextureFormat.ARGBHalf);
        ret.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        ret.volumeDepth = 256;
        ret.wrapMode = TextureWrapMode.Clamp;
        ret.useMipMap = true;
        ret.autoGenerateMips = true;
        ret.filterMode = FilterMode.Trilinear;
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
        _testDataKernel = heatSimComputeShader.FindKernel("testData");

        velocityHeatSimVol1 = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeNext", velocityHeatSimVol1);
        heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolumeNext", velocityHeatSimVol1);

        velocityHeatSimVol2 = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeLast", velocityHeatSimVol2);
        heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolumeLast", velocityHeatSimVol2);

        fuelSmokeSimVol1 = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeLast", fuelSmokeSimVol1);
        heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolumeLast", fuelSmokeSimVol1);

        fuelSmokeSimVol2 = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeLast", fuelSmokeSimVol2);
        heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolumeLast", fuelSmokeSimVol2);

        Shader.SetGlobalTexture("_VelocityHeatVolume", velocityHeatSimVol2);
        Shader.SetGlobalTexture("_FuelSmokeVolume", fuelSmokeSimVol2);

        points = new Vector4[512];
        newPointsBuffer = new ComputeBuffer(512, sizeof(float) * 4);
        heatSimComputeShader.SetBuffer(_simulateKernel, "newPoints", newPointsBuffer);

        camera.depthTextureMode = DepthTextureMode.Depth;

    }

    public static void SubmitHeatPoints(List<Vector4> newPoints) {
        instance.submitHeatPoints(newPoints);
    }

    public void submitHeatPoints(List<Vector4> newPoints) {
        for (int i = 0; i < newPoints.Count; i++) {
            //convert point to volume space
            float x = Mathf.Clamp01((newPoints[i].x + 8) * 0.0675f) * 255; // 16 wide
            int xIndex = Mathf.RoundToInt(x);
            if ((points[xIndex].w <= 0) || (Random.value > 0.5f)) {
                points[xIndex] = new Vector4( // + offset, * (make it 0->1) * resolution
                    x, // 8 wide
                    Mathf.Clamp01((newPoints[i].y + 0) * 0.25f) * 63, // 4 tall
                    Mathf.Clamp01((newPoints[i].z + 8) * 0.0675f) * 255, // 16 deep
                    newPoints[i].w);
            }
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
        
        Shader.SetGlobalVector("_FrustumNearBottomLeft", frustumNearV4[0]);
        Shader.SetGlobalVector("_FrustumFarBottomLeft", frustumFarV4[0]);
        Shader.SetGlobalVector("_FrustumNearTopLeft", frustumNearV4[1]);
        Shader.SetGlobalVector("_FrustumFarTopLeft", frustumFarV4[1]);
        Shader.SetGlobalVector("_FrustumNearTopRight", frustumNearV4[2]);
        Shader.SetGlobalVector("_FrustumFarTopRight", frustumFarV4[2]);
        Shader.SetGlobalVector("_FrustumNearBottomRight", frustumNearV4[3]);
        Shader.SetGlobalVector("_FrustumFarBottomRight", frustumFarV4[3]);
    }


    void Update() {
        //if (transform.hasChanged) {  doesn't account for when screen parameters change...
            OnFrustumMoved();
        //    transform.hasChanged = false;
        //}

        if (Input.GetKeyDown(KeyCode.F)) {
            heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolume", velocityHeatSimVol1);
            heatSimComputeShader.Dispatch(_testDataKernel, 8, 1, 8);
            heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolume", velocityHeatSimVol2);
            heatSimComputeShader.Dispatch(_testDataKernel, 8, 1, 8);
        }

        newPointsBuffer.SetData(points);

        heatSimComputeShader.SetFloat("_Time", Time.time);
        heatSimComputeShader.SetFloat("_dTime", Time.deltaTime);

        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeNext", vol12toggle ? velocityHeatSimVol1 : velocityHeatSimVol2);
        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeLast", vol12toggle ? velocityHeatSimVol2 : velocityHeatSimVol1);
        heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolNext", vol12toggle ? fuelSmokeSimVol1 : fuelSmokeSimVol2);
        heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolLast", vol12toggle ? fuelSmokeSimVol2 : fuelSmokeSimVol1);
        Shader.SetGlobalTexture("_HeatSimVolume", vol12toggle ? velocityHeatSimVol2 : velocityHeatSimVol1);
        Shader.SetGlobalTexture("_FuelSmokeVolume", vol12toggle ? fuelSmokeSimVol2 : fuelSmokeSimVol1);
        vol12toggle = !vol12toggle;

        heatSimComputeShader.Dispatch(_simulateKernel, 8, 64, 8);

        for (int i = 0; i < 512; i++) {
            points[i].x = 0;
            points[i].y = 0;
            points[i].z = 0;
            points[i].w = 0;
        }
    }

    private void OnDisable() {
        newPointsBuffer.Release();
        DestroyImmediate(velocityHeatSimVol1);
        DestroyImmediate(velocityHeatSimVol2);
        velocityHeatSimVol1 = null;
        velocityHeatSimVol2 = null;
    }
}
