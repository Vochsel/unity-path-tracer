using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ptRenderer {

    public struct Sphere
    {
        public float rad;
        public Vector3 pos, emi, col;
        public int type;
        public Sphere(float a_rad, Vector3 a_pos, Vector3 a_emi, Vector3 a_col, int a_type = 0)
        {
            rad = a_rad;
            pos = a_pos;
            emi = a_emi;
            col = a_col;
            type = a_type;
        }
    };

    public ComputeShader computeShader;

    public RenderTexture outputTex;
    public Texture2D inputTex;
    public Texture2D saveTex;

    public Texture tex;

    public bool IsActive;

    Sphere[] spheres = new Sphere[9];
    ComputeBuffer buffer;

    int sampleCount = 0;

    public bool CacheChanges = false;

    public int Width = 460;
    public int Height = 360;

    Camera ActiveCamera;

    public void SetupRenderer()
    {
        computeShader = Resources.Load<ComputeShader>("Shaders/CorePathTracer");
        ActiveCamera = Camera.main;
        //if (!outputTex)
        {
            outputTex = new RenderTexture(Width, Height, (int)RenderTextureFormat.ARGB32);
            outputTex.enableRandomWrite = true;
            outputTex.Create();
            //
            inputTex = new Texture2D(Width, Height, TextureFormat.ARGB32, false, true);
        }

        saveTex = new Texture2D(Width, Height, TextureFormat.ARGB32, false);

        //view = (CameraViewer)EditorWindow.GetWindow(typeof(CameraViewer));


        spheres[0] = new Sphere(1e5f, new Vector3(1e5f + 1.0f, 40.8f, 81.6f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.75f, 0.75f, 0.25f));
        spheres[1] = new Sphere(1e5f, new Vector3(-1e5f + 99.0f, 40.8f, 81.6f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(.75f, .25f, .45f));
        spheres[2] = new Sphere(1e5f, new Vector3(50.0f, 40.8f, 1e5f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(.75f, .75f, .75f));
        spheres[3] = new Sphere(1e5f, new Vector3(50.0f, 40.8f, -1e5f + 600.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.00f, 1.00f, 1.00f));
        spheres[4] = new Sphere(1e5f, new Vector3(50.0f, 1e5f, 81.6f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(.75f, .75f, .75f));
        spheres[5] = new Sphere(1e5f, new Vector3(50.0f, -1e5f + 81.6f, 81.6f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(.75f, .75f, .75f));
        spheres[6] = new Sphere(16.5f, new Vector3(27.0f, 16.5f, 47.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f));
        spheres[7] = new Sphere(16.5f, new Vector3(73.0f, 16.5f, 78.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), 1);
        spheres[8] = new Sphere(600.0f, new Vector3(50.0f, 681.6f - .77f, 81.6f), new Vector3(2.0f, 1.8f, 1.6f), new Vector3(0.0f, 0.0f, 0.0f));

        //Transform s = transform.GetChild(0);
        //spheres[9] = new Sphere(10 * s.localScale.magnitude, new Vector3(s.position.x * 50, s.position.y * 50, s.position.z * 50), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), 1);
        //Transform s1 = transform.GetChild(1);
        //spheres[6] = new Sphere(10 * s1.localScale.magnitude, new Vector3(s1.position.x * 50, s1.position.y * 50, s1.position.z * 50), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), 0);
        //Transform s2 = transform.GetChild(2);
        //spheres[7] = new Sphere(10 * s2.localScale.magnitude, new Vector3(s2.position.x * 50, s2.position.y * 50, s2.position.z * 50), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), 1);
        //Transform s3 = transform.GetChild(3);
        //spheres[10] = new Sphere(10 * s3.localScale.magnitude, new Vector3(s3.position.x * 50, s3.position.y * 50, s3.position.z * 50), new Vector3(1.2f, 0.70f, 0.9f), new Vector3(1.0f, 1.0f, 1.0f), 0);
        //
        //Transform s4 = transform.GetChild(4);
        //spheres[8] = new Sphere(10 * s4.localScale.magnitude, new Vector3(s4.position.x * 30, s4.position.y * 30, s4.position.z * 30), new Vector3(2.0f, 1.8f, 1.6f), new Vector3(0, 0, 0), 0);


        //if (buffer == null)
        {
            buffer = new ComputeBuffer(spheres.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere)));
            buffer.SetData(spheres);
        }
    }

    public void StopRenderer()
    {
        //buffer.fin();
        //outputTex.
    }

    public void ResetRenderer()
    {

        SetupRenderer();
        sampleCount = 0;
    }

    public RenderTexture RunShader()
    {

        if (!IsActive)
            return outputTex;

        if (!CacheChanges)
            ResetRenderer();

        //Debug.Log(spheres[6].rad);

        int kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernelHandle, "spheres", buffer);

        computeShader.SetVector("campos", ActiveCamera.transform.position);
        //computeShader.SetVector("camdir", Camera.main.transform.rotation.eulerAngles);
        computeShader.SetVector("camfwd", ActiveCamera.transform.forward);
        computeShader.SetVector("camrgt", ActiveCamera.transform.right);
        computeShader.SetVector("camup", ActiveCamera.transform.up);
        Matrix4x4 matrix = ActiveCamera.cameraToWorldMatrix;
        float[] matrixFloats = new float[]
        {
            matrix[0,0], matrix[1, 0], matrix[2, 0], matrix[3, 0],
            matrix[0,1], matrix[1, 1], matrix[2, 1], matrix[3, 1],
            matrix[0,2], matrix[1, 2], matrix[2, 2], matrix[3, 2],
            matrix[0,3], matrix[1, 3], matrix[2, 3], matrix[3, 3]
        };
        computeShader.SetFloats("camToWorld", matrixFloats);
        computeShader.SetFloat("camFOV", ActiveCamera.fieldOfView);
        computeShader.SetFloat("time", Time.time);
        computeShader.SetInt("WIDTH", Width);
        computeShader.SetInt("HEIGHT", Height);
        computeShader.SetFloat("textureWeight", (float)sampleCount / (float)(sampleCount + 1));

        computeShader.SetTexture(kernelHandle, "Input", saveTex);
        computeShader.SetTexture(kernelHandle, "Result", outputTex);
        computeShader.Dispatch(kernelHandle, Width / 8, Height / 8, 1);
        //computeShader.SetTexture()

        RenderTexture.active = outputTex;
        saveTex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        saveTex.Apply();
        inputTex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        inputTex.Apply();

        RenderTexture.active = null;
        saveTex.name = "Render";
        saveTex.wrapMode = TextureWrapMode.Clamp;
        saveTex.filterMode = FilterMode.Bilinear;

        sampleCount++;

        //Debug.Log("Sample Count: " + sampleCount);
        //Debug.Log("Texture Weight: " + (float)sampleCount / (float)(sampleCount + 1));

        return outputTex;
    }
}
