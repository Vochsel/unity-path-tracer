using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ptRenderer {

    public struct ptMaterial
    {
        public Vector4 albedo, emission;
        public float metallic, smoothness;
    }

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

        public Triangle(Vector3 a_v0, Vector3 a_v1, Vector3 a_v2, Vector3 a_normal)
        {
            v0 = a_v0;
            v1 = a_v1;
            v2 = a_v2;
            normal = a_normal;
        }
    };

    public struct ptMesh
    {
        public ptMaterial material;
        ComputeBuffer triangles;
        Matrix4x4 transform;
        public ptMesh(string a_name, ptMaterial a_mat, GameObject a_mesh)
        {
            material = a_mat;
            List<Triangle> triangleMesh = new List<Triangle>();


            transform = a_mesh.transform.localToWorldMatrix;
            
            MeshFilter mf = a_mesh.GetComponent<MeshFilter>();
            for (var i = 0; i < mf.sharedMesh.triangles.Length; i += 3)
            {
                Vector3 v0 = mf.sharedMesh.vertices[mf.sharedMesh.triangles[i]];
                Vector3 v1 = mf.sharedMesh.vertices[mf.sharedMesh.triangles[i + 1]];
                Vector3 v2 = mf.sharedMesh.vertices[mf.sharedMesh.triangles[i + 2]];

                Vector3 n = mf.sharedMesh.normals[mf.sharedMesh.triangles[i]];

                //v0 = mesh.transform.TransformPoint(v0);
                //v1 = mesh.transform.TransformPoint(v1);
                //v2 = mesh.transform.TransformPoint(v2);

                v0 = new Vector4(v0.x, v0.y, v0.z, 1.0f);
                v1 = new Vector4(v1.x, v1.y, v1.z, 1.0f);
                v2 = new Vector4(v2.x, v2.y, v2.z, 1.0f);


                Debug.Log(v0 + " : " + v1 + " + " + v2);

                triangleMesh.Add(new Triangle(v0, v1, v2, n));
            }

            triangles = new ComputeBuffer(triangleMesh.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle)));
            triangles.SetData(triangleMesh.ToArray());

            
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

    ComputeBuffer buffer;
    ComputeBuffer triBuffer;
    ComputeBuffer meshBuffer;

    int sampleCount = 0;

    public bool CacheChanges = false;

    public int Width = 460;
    public int Height = 360;

    Camera ActiveCamera;

    public void SetupRenderer()
    {
        computeShader = Resources.Load<ComputeShader>("Shaders/CorePathTracer");
       
            ActiveCamera = Camera.main;
        
        outputTex = new RenderTexture(Width, Height, (int)RenderTextureFormat.ARGB32);
        outputTex.enableRandomWrite = true;
        outputTex.Create();
        
        inputTex = new Texture2D(Width, Height, TextureFormat.ARGB32, false, true);
 
        saveTex = new Texture2D(Width, Height, TextureFormat.ARGB32, false);
        scene = new List<Sphere>();
        triangleMesh = new List<Triangle>();

        GameObject[] allSceneObjects = GameObject.FindGameObjectsWithTag("Renderable");

        foreach(GameObject obj in allSceneObjects)
        {
            Renderer objRenderer = obj.GetComponent<Renderer>();
            Material objMaterial = objRenderer.sharedMaterial;

            Vector3 pos = objRenderer.bounds.center;
            float rad = objRenderer.bounds.extents.magnitude / 1.75f;

            //http://answers.unity3d.com/questions/914923/standard-shader-emission-control-via-script.html

            Color diffuse = objMaterial.GetColor("_Color");            
            Color emission = objMaterial.GetColor("_EmissionColor");
            float metallic = objMaterial.GetFloat("_Metallic");
            float glossiness = objMaterial.GetFloat("_Glossiness");

            ptMaterial mat = new ptMaterial();

            mat.albedo = diffuse;
            mat.emission = emission;
            mat.metallic = metallic;
            mat.smoothness = glossiness;

            Sphere newSceneObject = new Sphere(rad, pos, new Vector3(0,0,0), ((Vector4)diffuse), mat);
            scene.Add(newSceneObject);
        }



        ptMaterial mat2 = new ptMaterial();


        //scene.Add(new Sphere(1e5f, new Vector3(1e5f - 5.0f, 40.8f, 81.6f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.75f, 0.75f, 0.25f)));
        //scene.Add(new Sphere(1e5f, new Vector3(-1e5f + 99.0f, 40.8f, 81.6f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(.75f, .25f, .45f)));
        //scene.Add(new Sphere(1e5f, new Vector3(50.0f, 40.8f, 1e5f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(.75f, .75f, .75f)));
        scene.Add(new Sphere(1e5f, new Vector3(50.0f, 40.8f, -1e5f + 600.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.00f, 1.00f, 1.00f), mat2));
        //scene.Add(new Sphere(1e5f, new Vector3(50.0f, 1e5f, 81.6f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(.75f, .75f, .75f), mat2));
        //scene.Add(new Sphere(1e5f, new Vector3(50.0f, -1e5f + 81.6f, 81.6f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(.75f, .75f, .75f), mat2));
        //scene.Add(new Sphere(16.5f, new Vector3(27.0f, 16.5f, 47.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f)));
        //scene.Add(new Sphere(16.5f, new Vector3(73.0f, 16.5f, 78.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), 1));
        //scene.Add(new Sphere(600.0f, new Vector3(50.0f, 681.6f - .77f, 81.6f), new Vector3(2.0f, 1.8f, 1.6f), new Vector3(0.0f, 0.0f, 0.0f), mat2));

        buffer = new ComputeBuffer(scene.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere)));
        buffer.SetData(scene.ToArray());

        meshes = new List<ptMesh>();

        GameObject mesh = GameObject.Find("testMesh");
        ptMesh mm = new ptMesh("", mat2, mesh);
        meshes.Add(mm);

        meshBuffer = new ComputeBuffer(meshes.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ptMesh)));
        meshBuffer.SetData(meshes.ToArray());

        if (mesh)
        {
            MeshFilter mf = mesh.GetComponent<MeshFilter>();
            for (var i = 0; i < mf.sharedMesh.triangles.Length; i += 3)
            {
                Vector3 v0 = mf.sharedMesh.vertices[mf.sharedMesh.triangles[i]];
                Vector3 v1 = mf.sharedMesh.vertices[mf.sharedMesh.triangles[i + 1]];
                Vector3 v2 = mf.sharedMesh.vertices[mf.sharedMesh.triangles[i + 2]];

                Vector3 n = mf.sharedMesh.normals[mf.sharedMesh.triangles[i]];
                
                //v0 = mesh.transform.TransformPoint(v0);
                //v1 = mesh.transform.TransformPoint(v1);
                //v2 = mesh.transform.TransformPoint(v2);
                
                v0 = new Vector4(v0.x, v0.y, v0.z, 1.0f);
                v1 = new Vector4(v1.x, v1.y, v1.z, 1.0f);
                v2 = new Vector4(v2.x, v2.y, v2.z, 1.0f);


                Debug.Log(v0 + " : " + v1 + " + " + v2);
                
                triangleMesh.Add(new Triangle(v0, v1, v2, n));
            }
        }
        if (triangleMesh.Count > 0)
        {
            Debug.Log(triangleMesh.Count);
            Debug.Log("Making buffer");
            triBuffer = new ComputeBuffer(triangleMesh.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle)));
            triBuffer.SetData(triangleMesh.ToArray());
        }

        int kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernelHandle, "spheres", buffer);
        if (triBuffer != null)
        {
            Debug.Log("Uploading buffer");
            computeShader.SetBuffer(kernelHandle, "triangleMesh", triBuffer);
        }

        if (meshBuffer != null)
        {
            Debug.Log("Uploading mesh buffer");
            computeShader.SetBuffer(kernelHandle, "meshesProper", meshBuffer);
        }

    }

    public void StopRenderer()
    {
        GameObject.DestroyImmediate(inputTex);
        GameObject.DestroyImmediate(saveTex);
        outputTex.Release();
        buffer.Dispose();
    }

    public void ResetRenderer()
    {
        StopRenderer();
        SetupRenderer();
        sampleCount = 0;
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

        Debug.Log(matrix);
        Debug.Log(matrixWTC);

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
}
