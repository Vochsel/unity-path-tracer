using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System;

public class ptRenderer {


    public struct Sphere
    {
        public float rad;
        public Vector3 pos, emi, col;

        public ptMaterial material;

        public Sphere(float a_rad, Vector3 a_pos, Vector3 a_emi, Vector3 a_col, ptMaterial a_material)
        {
            rad = a_rad;
            pos = a_pos;
            emi = a_emi;
            col = a_col;
            material = a_material;
        }
    };

  

    public struct Triangle
    {
        public Vector3 v0, v1, v2, normal;
        public uint objIdx;

        public Triangle(Vector3 a_v0, Vector3 a_v1, Vector3 a_v2, Vector3 a_normal, uint a_objIdx)
        {
            v0 = a_v0;
            v1 = a_v1;
            v2 = a_v2;
            normal = a_normal;
            objIdx = a_objIdx;
        }

        public override string ToString()
        {
            return "[" + v0 + " : " + v1 + " : " + v2 + "]";
        }
    };

    public struct ptMesh
    {
        public ptMaterial material;
        //ComputeBuffer triangles;
        Triangle[] triangles;
        int numTris;
        Matrix4x4 transform;
        public ptMesh(string a_name, ptMaterial a_mat, GameObject a_mesh)
        {
            material = a_mat;
            List<Triangle> triangleMesh = new List<Triangle>();


            transform = a_mesh.transform.localToWorldMatrix;
            triangles = new Triangle[16];
            MeshFilter mf = a_mesh.GetComponent<MeshFilter>();
            for (var i = 0; i < mf.sharedMesh.triangles.Length; i += 3)
            {
                Vector3 v0 = mf.sharedMesh.vertices[mf.sharedMesh.triangles[i]];
                Vector3 v1 = mf.sharedMesh.vertices[mf.sharedMesh.triangles[i + 1]];
                Vector3 v2 = mf.sharedMesh.vertices[mf.sharedMesh.triangles[i + 2]];

                Vector3 n = mf.sharedMesh.normals[mf.sharedMesh.triangles[i]];
                
                v0 = new Vector4(v0.x, v0.y, v0.z, 1.0f);
                v1 = new Vector4(v1.x, v1.y, v1.z, 1.0f);
                v2 = new Vector4(v2.x, v2.y, v2.z, 1.0f);

                int triIdx = (int)(i / 3.0f);

                // Debug.Log(v0 + " : " + v1 + " + " + v2);
                triangles[triIdx] = new Triangle(v0, v1, v2, n, 0);
                Debug.Log("num: " + triIdx);
                Debug.Log(triangles[triIdx]);
                
                //triangleMesh.Add(new Triangle(v0, v1, v2, n));
            }

            triangles = new Triangle[16];
            //triangleMesh.CopyTo(triangles);
            numTris = mf.sharedMesh.triangles.Length / 3;
            Debug.Log("Number of Tris: " + numTris);
            
            Debug.Log(triangles.Length);
            //triangles = new ComputeBuffer(triangleMesh.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle)));
            //triangles.SetData(triangleMesh.ToArray());
        }

    };

    public ComputeShader computeShader;

    public RenderTexture outputTex;
    public Texture2D inputTex;
    public Texture2D saveTex;

    public Texture tex;

    public bool IsActive = true;
    
    List<Sphere> scene;
    List<Triangle> triangleMesh;
    List<ptMesh> meshes;

    ptObjectHandler[] objects;

    ptLightHandler[] lights;

    ComputeBuffer buffer;
    ComputeBuffer triBuffer;
    ComputeBuffer meshBuffer;

    ComputeBuffer objectsBuffer;
    ComputeBuffer lightBuffer;

    int sampleCount = 0;

    public bool CacheChanges = false;

    public int Width = 1080;
    public int Height = 720;

    Camera ActiveCamera;

    ptRenderSettings currentRenderSettings;

    public void SetupRenderer(ptRenderSettings a_renderSettings)
    {
        
        Debug.Log(a_renderSettings.outputWidth);
        Width = a_renderSettings.outputWidth;
        Height = a_renderSettings.outputHeight;

        computeShader = Resources.Load<ComputeShader>("Shaders/CorePathTracer");
       
            ActiveCamera = Camera.main;
        
        outputTex = new RenderTexture(Width, Height, (int)RenderTextureFormat.ARGB32);
        outputTex.enableRandomWrite = true;
        
        outputTex.Create();
        
        inputTex = new Texture2D(Width, Height, TextureFormat.ARGB32, false, true);
 
        saveTex = new Texture2D(Width, Height, TextureFormat.ARGB32, false);
        scene = new List<Sphere>();
        triangleMesh = new List<Triangle>();


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


        GameObject[] allSceneObjects = GameObject.FindGameObjectsWithTag("Renderable");
        int numObjects = allSceneObjects.Length;
        objects = new ptObjectHandler[numObjects];

        for (int i = 0; i < numObjects; i++)
        {
            GameObject obj = allSceneObjects[i];
            Renderer objRenderer = obj.GetComponent<Renderer>();
            Material objMaterial = objRenderer.sharedMaterial;

            Vector3 pos = objRenderer.bounds.center;
            float rad = objRenderer.bounds.extents.magnitude / 1.75f;

            //http://answers.unity3d.com/questions/914923/standard-shader-emission-control-via-script.html

            Color diffuse = objMaterial.GetColor("_Color");
            Color emission = objMaterial.GetColor("_EmissionColor");
            Debug.Log(objMaterial.IsKeywordEnabled("_EMISSION"));
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

            MeshFilter mf = obj.GetComponent<MeshFilter>();
            switch(mf.sharedMesh.name)
            {
                case "Sphere":
                    st = ptShapeType.SPHERE;
                    Debug.Log("Found Sphere");
                    break;
                case "Plane":
                    st = ptShapeType.PLANE;
                    Debug.Log("Found Plane");
                    break;
                case "Cube":
                    st = ptShapeType.BOX;
                    Debug.Log("Found Box");
                    break;
            }

            ptObjectHandler newSceneObject = new ptObjectHandler(mat, obj.transform, st);
            //Debug.Log(newSceneObject.transform);
            //objects.Add(newSceneObject);
            objects[i] = newSceneObject;
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
        ComputeBuffer settingsBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ptRenderSettings)));
        ptRenderSettings[] ptrs = new ptRenderSettings[1];
        ptrs[0] = a_renderSettings;
        settingsBuffer.SetData(ptrs);

        computeShader.SetBuffer(kernelHandle, "SETTINGS", settingsBuffer);
       
    }

    public void StopRenderer()
    {
        GameObject.DestroyImmediate(inputTex);
        GameObject.DestroyImmediate(saveTex);
        if(outputTex)
            outputTex.Release();
        if (objectsBuffer != null)
        {
            objectsBuffer.Dispose();
            objectsBuffer = null;
        }
    }

    public void ResetRenderer(ptRenderSettings a_renderSettings)
    {
        currentRenderSettings = a_renderSettings;
        StopRenderer();
        SetupRenderer(a_renderSettings);
        sampleCount = 0;
        IsActive = true;
    }

    public void ResetSamples()
    {
        sampleCount = 0;
    }

    public RenderTexture RunShader()
    {

        if (!IsActive)
            return outputTex;

        if (!CacheChanges)
            ResetSamples();

        int kernelHandle = computeShader.FindKernel("CSMain");
        //computeShader.SetBuffer(kernelHandle, "objects", objectsBuffer);

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

        //computeShader.SetBuffer(kernelHandle, "spheres", buffer);
        //if (triBuffer != null)
        //{
        //    Debug.Log("Uploading buffer");
        //    computeShader.SetBuffer(kernelHandle, "triangleMesh", triBuffer);
        //}


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

        //Debug.Log(matrix);
        //Debug.Log(matrixWTC);

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

        return outputTex;
    }

    public void SaveRenderToFile(string a_path)
    {
        byte[] bytes = saveTex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + a_path + "render - " + System.DateTime.Now.ToString("yyMMddHHmmss") + ".png", bytes);
    }

    void UploadObjects()
    {
        if (objects.Length > 0)
        {
            Debug.Log("Found " + objects.Length + " objects!");
            Debug.Log("Making buffer");
            int numObjects = objects.Length;
            

            ptObject[] tempObjects = new ptObject[numObjects];

            for (int i = 0; i < numObjects; i++)
            {
                //objects[i].Handle();
                tempObjects[i] = objects[i].handledObject;
              //  Debug.Log(objects[i].handledObject.worldMatrix);

            }

            objectsBuffer.SetData(tempObjects);
            int kernelHandle = computeShader.FindKernel("CSMain");
            if (objectsBuffer != null)
            {
                Debug.Log("Uploading buffer");
                computeShader.SetBuffer(kernelHandle, "objects", objectsBuffer);
            }
        }
    }

    void UploadLights()
    {
        if(lights.Length > 0)
        {
            Debug.Log("Found " + lights.Length + " lights!");

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
                Debug.Log("Uploading lights");
                computeShader.SetBuffer(kHandle, "lights", lightBuffer);
            }
        }
    }
}
