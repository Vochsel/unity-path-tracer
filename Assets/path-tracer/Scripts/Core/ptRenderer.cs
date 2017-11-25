using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System;

public class ptRenderer {
        
    public ComputeShader computeShader;

    public RenderTexture outputTex;
    public Texture2D saveTex;
        
    ptObjectHandler[] objects;
    ptLightHandler[] lights;

    ComputeBuffer objectsBuffer;
    ComputeBuffer lightBuffer;
    ComputeBuffer settingsBuffer;
    
    int sampleCount = 0;

    public bool CacheChanges = false;
    public bool IsActive = true;

    public int Width = 1080;
    public int Height = 720;

    Camera ActiveCamera;

    public static ptRenderSettings currentRenderSettings;

    // -- Setup Renderer
    public void SetupRenderer(ptRenderSettings a_renderSettings)
    {
        ActiveCamera = Camera.main;
        if (ActiveCamera == null)
        {
            Debug.Log("Could not setup renderer; No camera!");
            return;
        }

        ptRenderSettings options = a_renderSettings;

        if (options.outputWidth == 0) options.outputWidth = 1280;
        if (options.outputHeight == 0) options.outputHeight = 720;
        if (options.samples == 0) options.samples = 3;
        if (options.bounces == 0) options.bounces = 2;

        Width = options.outputWidth;
        Height = options.outputHeight;

        // -- Create required resources

        computeShader = Resources.Load<ComputeShader>("Shaders/CorePathTracer");
               
        outputTex = new RenderTexture(Width, Height, (int)RenderTextureFormat.ARGB32);
        outputTex.enableRandomWrite = true;
        
        outputTex.Create();
        
        saveTex = new Texture2D(Width, Height, TextureFormat.ARGB32, false);

        // -- Setup Lights
        Light[] allLights = GameObject.FindObjectsOfType<Light>();
        int numLights = allLights.Length;
        lights = new ptLightHandler[numLights];

        for (int j = 0; j < numLights; j++)
        {
            Light l = allLights[j];

            ptLightHandler newLightObject = new ptLightHandler(l);
            lights[j] = newLightObject;
        }

        if (lightBuffer != null)
        {
            lightBuffer.Dispose();
            lightBuffer.Release();
            lightBuffer = null;
            Debug.Log("Destroyed old buffer");
        }

        if (numLights > 0)
        {
            lightBuffer = new ComputeBuffer(numLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ptLight)));

            UploadLights();
        }

        // -- Setup objects 
        MeshFilter[] allMeshes = GameObject.FindObjectsOfType<MeshFilter>();
        int numObjects = allMeshes.Length;        
        List<ptObjectHandler> outputObjects = new List<ptObjectHandler>();

        for (int i = 0; i < numObjects; i++)
        {
            MeshFilter mf = allMeshes[i];
            GameObject obj = mf.gameObject;
            Renderer objRenderer = obj.GetComponent<Renderer>();
            Material objMaterial = objRenderer.sharedMaterial;

            Vector3 pos = objRenderer.bounds.center;
            //http://answers.unity3d.com/questions/914923/standard-shader-emission-control-via-script.html

            Color diffuse = objMaterial.GetColor("_Color");
            Color emission = objMaterial.GetColor("_EmissionColor");
            if (!objMaterial.IsKeywordEnabled("_EMISSION"))
            {
                emission = Color.black;
            }
            
            float metallic = objMaterial.GetFloat("_Metallic");
            float glossiness = objMaterial.GetFloat("_Glossiness");
            ptMaterial mat = new ptMaterial();

            mat.albedo = diffuse;
            mat.emission = emission;
            mat.metallic = metallic;
            mat.smoothness = glossiness;

            ptShapeType st = ptShapeType.SPHERE;
            
            switch(mf.sharedMesh.name)
            {
                case "Sphere":
                    st = ptShapeType.SPHERE;
                    outputObjects.Add(new ptObjectHandler(mat, obj.transform, st));
                    break;
                case "Plane":
                    st = ptShapeType.PLANE;
                    outputObjects.Add(new ptObjectHandler(mat, obj.transform, st));
                    break;
                case "Cube":
                    st = ptShapeType.BOX;
                    break;
            }
            
        }

        objects = new ptObjectHandler[outputObjects.Count];
        for(int z = 0; z < outputObjects.Count; z++)
        {
            objects[z] = outputObjects[z];
        }

        if (objectsBuffer != null)
        {
            objectsBuffer.Dispose();
            objectsBuffer.Release();
            objectsBuffer = null;
            Debug.Log("Destroyed old buffer");
        }

        if (numObjects > 0)
        {
            objectsBuffer = new ComputeBuffer(numObjects, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ptObject)));
            UploadObjects();
        }

        int kernelHandle = computeShader.FindKernel("CSMain");
        settingsBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ptRenderSettings)));

        ptRenderSettings[] ptrs = new ptRenderSettings[1];
        ptrs[0] = options;
        settingsBuffer.SetData(ptrs);

        computeShader.SetBuffer(kernelHandle, "SETTINGS", settingsBuffer);
    }

    // -- Stop renderer
    public void StopRenderer()
    {
        if (saveTex != null)
            GameObject.DestroyImmediate(saveTex);
        if(outputTex)
            outputTex.Release();
        if (objectsBuffer != null)
        {
            objectsBuffer.Release();
            objectsBuffer = null;
        }
        if (settingsBuffer != null)
        {
            settingsBuffer.Release();
            settingsBuffer = null;
        }
        if (lightBuffer != null)
        {
            lightBuffer.Release();
            lightBuffer = null;
        }
    }

    // -- Reset Renderer
    public void ResetRenderer(ptRenderSettings a_renderSettings)
    {
        currentRenderSettings = a_renderSettings;
        StopRenderer();
        SetupRenderer(a_renderSettings);
        sampleCount = 0;
        IsActive = true;
    }

    // -- Reset Samples
    public void ResetSamples()
    {
        sampleCount = 0;
    }

    // -- Execute shader, pass buffers, check for scene changes
    public RenderTexture RunShader()
    {
        if (ActiveCamera == null)
        {
            if (Camera.main != null)
            {
                ResetRenderer(currentRenderSettings);
                return outputTex;
            }
            else
                return outputTex;
        }

        if (!IsActive)
            return outputTex;

        if (!CacheChanges)
            ResetSamples();

        int kernelHandle = computeShader.FindKernel("CSMain");

        // -- Update Objects

        bool shouldUpload = false;
        for(int i = 0; i < objects.Length; i++)
        {
            if (!objects[i].transform)
            {
                ResetRenderer(currentRenderSettings);
                return outputTex;
            }
            shouldUpload = objects[i].Handle() ? true : shouldUpload;
        }
        
        if(shouldUpload)
            UploadObjects();

        // -- Update Lights

        bool shouldUploadLights = false;
        for(int j = 0; j < lights.Length; j++)
        {
            if(!lights[j].lightRef)
            {
                ResetRenderer(currentRenderSettings);
                return outputTex;
            }
            shouldUploadLights = lights[j].Handle() ? true : shouldUploadLights;
        }

        if (shouldUploadLights)
            UploadLights();
        
        // -- Update camera

        Matrix4x4 matrix = ActiveCamera.cameraToWorldMatrix;
        float[] matrixFloats = new float[]
        {
            matrix[0,0], matrix[1, 0], matrix[2, 0], matrix[3, 0],
            matrix[0,1], matrix[1, 1], matrix[2, 1], matrix[3, 1],
            matrix[0,2], matrix[1, 2], matrix[2, 2], matrix[3, 2],
            matrix[0,3], matrix[1, 3], matrix[2, 3], matrix[3, 3]
        };

        Matrix4x4 matrixWTC = ActiveCamera.worldToCameraMatrix;
        float[] matrixWTCFloats = new float[]
        {
            matrixWTC[0,0], matrixWTC[1, 0], matrixWTC[2, 0], matrixWTC[3, 0],
            matrixWTC[0,1], matrixWTC[1, 1], matrixWTC[2, 1], matrixWTC[3, 1],
            matrixWTC[0,2], matrixWTC[1, 2], matrixWTC[2, 2], matrixWTC[3, 2],
            matrixWTC[0,3], matrixWTC[1, 3], matrixWTC[2, 3], matrixWTC[3, 3]
        };
        
        // -- Pass uniforms

        computeShader.SetFloats("camToWorld", matrixFloats);
        computeShader.SetFloats("worldToCam", matrixWTCFloats);
        computeShader.SetFloat("camFOV", ActiveCamera.fieldOfView);
        computeShader.SetFloat("time", Time.time);
        computeShader.SetInt("WIDTH", Width);
        computeShader.SetInt("HEIGHT", Height);
        computeShader.SetFloat("textureWeight", (float)sampleCount / (float)(sampleCount + 1));

        computeShader.SetTexture(kernelHandle, "Input", saveTex);
        computeShader.SetTexture(kernelHandle, "Result", outputTex);
        computeShader.Dispatch(kernelHandle, Width / 8, Height / 8, 1);

        // -- Save and swap textures

        RenderTexture.active = outputTex;
        saveTex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        saveTex.Apply();

        RenderTexture.active = null;
        saveTex.name = "Render";
        saveTex.wrapMode = TextureWrapMode.Clamp;
        saveTex.filterMode = FilterMode.Bilinear;

        sampleCount++;

        return outputTex;
    }


    // -- Save image to png at path
    public void SaveRenderToFile(string a_path)
    {
        byte[] bytes = saveTex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + a_path + "render - " + System.DateTime.Now.ToString("yyMMddHHmmss") + ".png", bytes);
    }

    // -- Update object buffer to GPU
    void UploadObjects()
    {
        if (objects.Length > 0)
        {
            int numObjects = objects.Length;
            
            ptObject[] tempObjects = new ptObject[numObjects];

            for (int i = 0; i < numObjects; i++)
            {
                tempObjects[i] = objects[i].handledObject;
            }

            objectsBuffer.SetData(tempObjects);
            int kernelHandle = computeShader.FindKernel("CSMain");
            if (objectsBuffer != null)
            {
                computeShader.SetBuffer(kernelHandle, "objects", objectsBuffer);
            }
        }
    }

    // -- Upload light buffer to GPU
    void UploadLights()
    {
        if(lights.Length > 0)
        {
            int numLights = lights.Length;

            ptLight[] tempLights = new ptLight[numLights];

            for(int i = 0; i < numLights; i++)
            {
                tempLights[i] = lights[i].handledLight;
            }

            lightBuffer.SetData(tempLights);
            int kHandle = computeShader.FindKernel("CSMain");
            if(lightBuffer != null)
            {
                computeShader.SetBuffer(kHandle, "lights", lightBuffer);
            }
        }
    }
}
