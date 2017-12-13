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

    ComputeBuffer newPointsBuffer, newVelocityBuffer, heatMapBuffer;

    int _simulateKernel, _testDataKernel, _insertKernel, _clearKernel;

    static Vector4[] points, velocities;

    Vector3[] frustumNear, frustumFar;
    Vector4[] frustumNearV4, frustumFarV4;

    [SerializeField] Texture2D noiseTex;

    [SerializeField] bool doSimulate, doInsert, doBlit, doClear, doFrustumUpdate, doSetTex;

    RenderTexture generate3DRenderTexture(RenderTextureFormat format = RenderTextureFormat.ARGBHalf) {
        var ret = new RenderTexture(128, 128, 0, format);
        ret.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        ret.volumeDepth = 128;
        ret.wrapMode = TextureWrapMode.Clamp;
        ret.filterMode = FilterMode.Trilinear;
        ret.enableRandomWrite = true;
        ret.antiAliasing = 8;
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
        velocityHeatSimVol1.name = "VH 1";
        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeNext", velocityHeatSimVol1);
        heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolumeNext", velocityHeatSimVol1);
        heatSimComputeShader.SetTexture(_insertKernel, "HeatSimVolumeNext", velocityHeatSimVol1);

        velocityHeatSimVol2 = generate3DRenderTexture();
        velocityHeatSimVol2.name = "VH 2";
        heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeLast", velocityHeatSimVol2);
        heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolumeLast", velocityHeatSimVol2);

        fuelSmokeSimVol1 = generate3DRenderTexture();
        fuelSmokeSimVol1.name = "FS 1";
        heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolNext", fuelSmokeSimVol1);
        heatSimComputeShader.SetTexture(_testDataKernel, "FuelSmokeSimVolNext", fuelSmokeSimVol1);
        heatSimComputeShader.SetTexture(_insertKernel, "FuelSmokeSimVolNext", fuelSmokeSimVol1);

        heatSimComputeShader.SetTexture(_simulateKernel, "NoiseTex", noiseTex);
        heatSimComputeShader.SetTexture(_insertKernel, "NoiseTex", noiseTex);

        fuelSmokeSimVol2 = generate3DRenderTexture();
        fuelSmokeSimVol2.name = "FS 2";
        heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolLast", fuelSmokeSimVol2);
        heatSimComputeShader.SetTexture(_testDataKernel, "FuelSmokeSimVolLast", fuelSmokeSimVol2);
        
        Shader.SetGlobalTexture("_NoiseTex", noiseTex);
        Shader.SetGlobalTexture("_VelocityHeatVolume", velocityHeatSimVol2);
        Shader.SetGlobalTexture("_FuelSmokeVolume", fuelSmokeSimVol2);

        points = new Vector4[256];
        velocities = new Vector4[256];
        newPointsBuffer = new ComputeBuffer(256, sizeof(float) * 4);
        newVelocityBuffer = new ComputeBuffer(256, sizeof(float) * 4);
        logInsertionVolume = generate3DRenderTexture();
        logInsertionVolume.name = "insert";
        heatSimComputeShader.SetTexture(_insertKernel, "LogInsertionVolumeRW", logInsertionVolume);
        heatSimComputeShader.SetTexture(_simulateKernel, "LogInsertionVolumeRW", logInsertionVolume);
        heatSimComputeShader.SetTexture(_testDataKernel, "LogInsertionVolumeRW", logInsertionVolume);
        heatSimComputeShader.SetBuffer(_insertKernel, "newPoints", newPointsBuffer);
        heatSimComputeShader.SetBuffer(_insertKernel, "newVelocity", newVelocityBuffer);
        heatSimComputeShader.SetTexture(_clearKernel, "LogInsertionVolumeRW", logInsertionVolume);

        camera.depthTextureMode = DepthTextureMode.Depth;
    }
    
    public static void SubmitHeatPoints(Vector4[] newPoints) {
        instance.submitPoints(newPoints);
    }

    public void submitPoints(Vector4[] newPoints) {
        StartCoroutine(submitHeatPoints_coroutine(newPoints));
    }
        
    IEnumerator submitHeatPoints_coroutine(Vector4[] newPoints) {
        int count = Mathf.Min(newPoints.Length, 256);
        for (int i = 0; i < count; i++) {
            //convert point to volume space
            points[i] = new Vector4( // + offset, * (make it 0->1) * resolution
                Mathf.Clamp01((newPoints[i].x + 1f) * 0.5f) * 128f - 0.5f, // 1 wide
                Mathf.Clamp01((newPoints[i].y + 0.1f) * 0.5f) * 128f - 0.5f, // 1 tall
                Mathf.Clamp01((newPoints[i].z + 1f) * 0.5f) * 128f - 0.5f, // 1 deep
                newPoints[i].w * 12500f * Time.deltaTime);
            velocities[i] = new Vector4(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0
                ) * Time.deltaTime * 250f;
        }
        if (doInsert) {
            newPointsBuffer.SetData(points, 0, 0, count);
            newVelocityBuffer.SetData(velocities, 0, 0, count);
            heatSimComputeShader.Dispatch(_insertKernel, count, 1, 1);
        }
        yield return null;
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

            frustumFarV4[i].x = (frustumFarV4[i].x + 1f) * 0.5f; //moving to volume space
            frustumFarV4[i].y = (frustumFarV4[i].y + 0.1f) * 0.5f;
            frustumFarV4[i].z = (frustumFarV4[i].z + 1f) * 0.5f;

            frustumNearV4[i].x = (frustumNearV4[i].x + 1f) * 0.5f; //moving to volume space
            frustumNearV4[i].y = (frustumNearV4[i].y + 0.1f) * 0.5f;
            frustumNearV4[i].z = (frustumNearV4[i].z + 1f) * 0.5f;
        }
        
        Shader.SetGlobalVector("_FrustumNearBottomLeft", frustumNearV4[0]);
        Shader.SetGlobalVector("_FrustumFarBottomLeft", frustumFarV4[0]);
        Shader.SetGlobalVector("_FrustumNearTopLeft", frustumNearV4[1]);
        Shader.SetGlobalVector("_FrustumFarTopLeft", frustumFarV4[1]);
        Shader.SetGlobalVector("_FrustumNearTopRight", frustumNearV4[2]);
        Shader.SetGlobalVector("_FrustumFarTopRight", frustumFarV4[2]);
        Shader.SetGlobalVector("_FrustumNearBottomRight", frustumNearV4[3]);
        Shader.SetGlobalVector("_FrustumFarBottomRight", frustumFarV4[3]);

        var volSpaceCamPos = new Vector3();
        volSpaceCamPos.x = (transform.position.x + 1f) * 0.5f; //moving to volume space
        volSpaceCamPos.y = (transform.position.y + 0.1f) * 0.5f;
        volSpaceCamPos.z = (transform.position.z + 1f) * 0.5f;
        Shader.SetGlobalVector("_VolSpaceCamPos", volSpaceCamPos);
    }
    

    void FixedUpdate() {
        //if (transform.hasChanged) {  doesn't account for when screen parameters change...
        OnFrustumMoved();

        if (Input.GetKeyDown(KeyCode.F)) {
            heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolume", velocityHeatSimVol1);
            heatSimComputeShader.Dispatch(_testDataKernel, 128, 1, 128);
            heatSimComputeShader.SetTexture(_testDataKernel, "HeatSimVolume", velocityHeatSimVol2);
            heatSimComputeShader.Dispatch(_testDataKernel, 128, 1, 128);
        }

        heatSimComputeShader.SetFloat("_Time", Time.time);
        heatSimComputeShader.SetFloat("_dTime", Time.fixedDeltaTime);

        //SIMULATE
        if (doSetTex) {
            heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeNext", vol12toggle ? velocityHeatSimVol1 : velocityHeatSimVol2);
            heatSimComputeShader.SetTexture(_simulateKernel, "HeatSimVolumeLast", vol12toggle ? velocityHeatSimVol2 : velocityHeatSimVol1);
            heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolNext", vol12toggle ? fuelSmokeSimVol1 : fuelSmokeSimVol2);
            heatSimComputeShader.SetTexture(_simulateKernel, "FuelSmokeSimVolLast", vol12toggle ? fuelSmokeSimVol2 : fuelSmokeSimVol1);
            
            Shader.SetGlobalTexture("_HeatSimVolume", vol12toggle ? velocityHeatSimVol2 : velocityHeatSimVol1);
            Shader.SetGlobalTexture("_FuelSmokeVolume", vol12toggle ? fuelSmokeSimVol2 : fuelSmokeSimVol1);
        }

        if (doSimulate) heatSimComputeShader.Dispatch(_simulateKernel, 128, 128, 128);
        if (doClear) heatSimComputeShader.Dispatch(_clearKernel, 128, 128, 128);

        vol12toggle = !vol12toggle;
    }
    
    private void OnDisable() {
        newPointsBuffer.Release();
        newVelocityBuffer.Release();
        DestroyImmediate(velocityHeatSimVol1);
        DestroyImmediate(velocityHeatSimVol2);
        velocityHeatSimVol1 = null;
        velocityHeatSimVol2 = null;
    }
}
