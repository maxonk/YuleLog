using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

    [SerializeField]
    RenderTexture
        velocityHeatSimVol1, velocityHeatSimVol2,
        fuelSmokeSimVol1, fuelSmokeSimVol2,
        logInsertionVolume; //todo: write into this from points in _insert, draw from it in _simulate
    bool vol12toggle = false;

    ComputeBuffer newPointsBuffer, heatMapBuffer;

    int _simulateKernel, _testDataKernel, _insertKernel, _clearKernel;

    static Vector4[] points;

    Vector3[] frustumNear, frustumFar;
    Vector4[] frustumNearV4, frustumFarV4;

    [SerializeField] Texture2D noiseTex;

    [SerializeField] bool doSimulate, doInsert, doBlit, doClear, doFrustumUpdate, doSetTex;

    RenderTexture generate3DRenderTexture(RenderTextureFormat format = RenderTextureFormat.ARGBHalf) {
        var ret = new RenderTexture(128, 128, 0, format);
        ret.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        ret.volumeDepth = 128;
        ret.wrapMode = TextureWrapMode.Clamp;
        ret.filterMode = FilterMode.Bilinear;
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
        _insertKernel = heatSimComputeShader.FindKernel("insert");
        _clearKernel = heatSimComputeShader.FindKernel("clearLogVol");

        velocityHeatSimVol1 = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeNext", velocityHeatSimVol1);
        heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolumeNext", velocityHeatSimVol1);
        heatSimComputeShader.SetTexture(_insertKernel, "HeatSimVolumeNext", velocityHeatSimVol1);

        velocityHeatSimVol2 = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeLast", velocityHeatSimVol2);
        heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolumeLast", velocityHeatSimVol2);

        fuelSmokeSimVol1 = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolNext", fuelSmokeSimVol1);
        heatSimComputeShader.SetTexture(_testDataKernel, "FuelSmokeSimVolNext", fuelSmokeSimVol1);
        heatSimComputeShader.SetTexture(_insertKernel, "FuelSmokeSimVolNext", fuelSmokeSimVol1);

        heatSimComputeShader.SetTexture(_simulateKernel, "NoiseTex", noiseTex);
        heatSimComputeShader.SetTexture(_insertKernel, "NoiseTex", noiseTex);

        fuelSmokeSimVol2 = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolLast", fuelSmokeSimVol2);
        heatSimComputeShader.SetTexture(_testDataKernel, "FuelSmokeSimVolLast", fuelSmokeSimVol2);
        
        Shader.SetGlobalTexture("_NoiseTex", noiseTex);
        Shader.SetGlobalTexture("_VelocityHeatVolume", velocityHeatSimVol2);
        Shader.SetGlobalTexture("_FuelSmokeVolume", fuelSmokeSimVol2);

        points = new Vector4[256];
        newPointsBuffer = new ComputeBuffer(256, sizeof(float) * 4);
        logInsertionVolume = generate3DRenderTexture();
        heatSimComputeShader.SetTexture(_insertKernel, "LogInsertionVolumeRW", logInsertionVolume);
        heatSimComputeShader.SetTexture(_simulateKernel, "LogInsertionVolume", logInsertionVolume);
        heatSimComputeShader.SetBuffer(_insertKernel, "newPoints", newPointsBuffer);
        heatSimComputeShader.SetTexture(_clearKernel, "LogInsertionVolumeRW", logInsertionVolume);

        camera.depthTextureMode = DepthTextureMode.Depth;
    }
    
    public static void SubmitHeatPoints(Vector4[] newPoints) {
        instance.submitHeatPoints(newPoints);
    }
    
    public void submitHeatPoints(Vector4[] newPoints) {
        int count = Mathf.Min(newPoints.Length, 256);
        for (int i = 0; i < count; i++) {
            //convert point to volume space
            points[i] = new Vector4( // + offset, * (make it 0->1) * resolution
                Mathf.Clamp01((newPoints[i].x + 4) * 0.125f) * 128f- 0.5f, // 8 wide
                Mathf.Clamp01((newPoints[i].y + 1) * 0.125f) * 128f - 0.5f, // 8 tall
                Mathf.Clamp01((newPoints[i].z + 4) * 0.125f) * 128f - 0.5f, // 8 deep
                newPoints[i].w);
        }
        if (doInsert) {
            newPointsBuffer.SetData(points, 0, 0, count);
            heatSimComputeShader.Dispatch(_insertKernel, count, 1, 1);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (doBlit) {
            Graphics.Blit(source, destination, visualizationMaterial);
        } else {
            Graphics.Blit(source, destination);
        }
    }

    void OnFrustumMoved() {
        if (!doFrustumUpdate) return;
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumNear);
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumFar);
        for (int i = 0; i < 4; i++) {
            frustumNearV4[i] = transform.TransformPoint(frustumNear[i]);
            frustumFarV4[i] = transform.TransformPoint(frustumFar[i]);

            frustumFarV4[i].x = (frustumFarV4[i].x + 4) * 0.125f; //moving to volume space
            frustumFarV4[i].y = (frustumFarV4[i].y + 1) * 0.125f;
            frustumFarV4[i].z = (frustumFarV4[i].z + 4) * 0.125f;

            frustumNearV4[i].x = (frustumNearV4[i].x + 4) * 0.125f; //moving to volume space
            frustumNearV4[i].y = (frustumNearV4[i].y + 1) * 0.125f;
            frustumNearV4[i].z = (frustumNearV4[i].z + 4) * 0.125f;
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

   // CommandBuffer simulateClearBuffer;

    void Update() {
        //if (transform.hasChanged) {  doesn't account for when screen parameters change...
        OnFrustumMoved();

        if (Input.GetKeyDown(KeyCode.F)) {
            heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolume", velocityHeatSimVol1);
            heatSimComputeShader.Dispatch(_testDataKernel, 128, 1, 128);
            heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolume", velocityHeatSimVol2);
            heatSimComputeShader.Dispatch(_testDataKernel, 128, 1, 128);
        }

        heatSimComputeShader.SetFloat("_Time", Time.time);
        heatSimComputeShader.SetFloat("_dTime", Time.deltaTime);

        //SIMULATE
        if (doSetTex) {
            heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeNext", vol12toggle ? velocityHeatSimVol1 : velocityHeatSimVol2);
            heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeLast", vol12toggle ? velocityHeatSimVol2 : velocityHeatSimVol1);
            heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolNext", vol12toggle ? fuelSmokeSimVol1 : fuelSmokeSimVol2);
            heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolLast", vol12toggle ? fuelSmokeSimVol2 : fuelSmokeSimVol1);

            Shader.SetGlobalTexture("_HeatSimVolume", vol12toggle ? velocityHeatSimVol2 : velocityHeatSimVol1);
            Shader.SetGlobalTexture("_FuelSmokeVolume", vol12toggle ? fuelSmokeSimVol2 : fuelSmokeSimVol1);
        }

     /*
        if(simulateClearBuffer == null) {
            simulateClearBuffer = new CommandBuffer();
            simulateClearBuffer.DispatchCompute(heatSimComputeShader, _simulateKernel, 128, 128, 128);
            simulateClearBuffer.DispatchCompute(heatSimComputeShader, _clearKernel, 128, 128, 128);
        }
        Graphics.ExecuteCommandBuffer(simulateClearBuffer);
        */
        //CLEAN
        vol12toggle = !vol12toggle;
        if (doSimulate) heatSimComputeShader.Dispatch(_simulateKernel, 128, 128, 128);
    }

    private void LateUpdate() {
         if(doClear) heatSimComputeShader.Dispatch(_clearKernel, 128, 128, 128);
    }

    private void OnDisable() {
        newPointsBuffer.Release();
        DestroyImmediate(velocityHeatSimVol1);
        DestroyImmediate(velocityHeatSimVol2);
        velocityHeatSimVol1 = null;
        velocityHeatSimVol2 = null;
    }
}
