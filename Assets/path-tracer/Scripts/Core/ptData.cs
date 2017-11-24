using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ptCamera
{
    public Matrix4x4 transform;
    public float fov;
    public Color background;
    public bool perspective;
}

public struct ptMaterial
{
    public Vector4 albedo, emission;
    public float metallic, smoothness;
}

public enum ptShapeType : uint
{
    SPHERE,
    BOX,
    PLANE
}

public enum ptLightType : uint
{
    POINT,
    DIRECTIONAL,
    SPOT
}

public struct ptObject
{
    public ptMaterial material;
    public Matrix4x4 worldMatrix, invWorldMatrix;
    public uint shapeType;

    public ptObject(ptMaterial a_mat, Matrix4x4 a_worldMatrix, Matrix4x4 a_invWorldMatrix, ptShapeType a_shapeType)
    {
        material = a_mat;
        worldMatrix = a_worldMatrix;
        invWorldMatrix = a_invWorldMatrix;
        shapeType = (uint)a_shapeType;
    }
}

public struct ptObjectHandler
{
    public ptObject handledObject;
    public Transform transform;

    public ptObjectHandler(ptMaterial a_mat, Transform a_transform, ptShapeType a_shapeType)
    {
        transform = a_transform;
        handledObject = new ptObject(a_mat, transform.localToWorldMatrix, transform.worldToLocalMatrix, a_shapeType);
    }

    public bool Handle()
    {
        bool isDirty = false;

        if(transform.hasChanged)
        {
            handledObject.worldMatrix = transform.localToWorldMatrix;

            handledObject.invWorldMatrix = transform.worldToLocalMatrix;
            isDirty = true;
            transform.hasChanged = false;
        }

        return isDirty;
    }
}

public struct ptLight
{
    public Vector3 color;
    public float intensity;
    public float range;
    public uint lightType;
    public Matrix4x4 worldMatrix;

    public ptLight(Vector3 a_color, float a_intensity, float a_range, uint a_lightType, Matrix4x4 a_worldMatrix)
    {
        color = a_color;
        intensity = a_intensity;
        range = a_range;
        lightType = a_lightType;
        worldMatrix = a_worldMatrix;
    }
}

public struct ptLightHandler
{
    public ptLight handledLight;
    public Light lightRef;

    public ptLightHandler(Light a_light)
    {
        Vector3 color = new Vector3(a_light.color.r, a_light.color.g, a_light.color.b);
        float intensity = a_light.intensity;
        float range = a_light.range;
        Matrix4x4 worldMatrix = a_light.transform.localToWorldMatrix;
        uint lightType = 0;
        switch (a_light.type)
        {
            case LightType.Point: lightType = (uint)ptLightType.POINT; break;
            case LightType.Directional: lightType = (uint)ptLightType.DIRECTIONAL; break;
            case LightType.Spot: lightType = (uint)ptLightType.SPOT; break;
            default: lightType = (uint)ptLightType.DIRECTIONAL; break;
        }

        lightRef = a_light;
        handledLight = new ptLight(color, intensity, range, lightType, worldMatrix);
    }

    public bool Handle()
    {
        bool isDirty = false;

        if (lightRef.transform.hasChanged || 
            (handledLight.range != lightRef.range) || 
            (handledLight.intensity != lightRef.intensity) ||
            (handledLight.color.x != lightRef.color.r) || (handledLight.color.y != lightRef.color.g) || (handledLight.color.z != lightRef.color.b))
        {
            handledLight.intensity = lightRef.intensity;
            handledLight.range = lightRef.range;

            handledLight.color = (Vector3)(Vector4)lightRef.color;

            handledLight.worldMatrix = lightRef.transform.localToWorldMatrix;
            isDirty = true;
            lightRef.transform.hasChanged = false;
        }

        return isDirty;
    }
}


public struct ptRenderSettings
{
    public int outputWidth, outputHeight;
    public int samples, bounces;

    public ptRenderSettings(int a_w, int a_h, int a_s, int a_b)
    {
        outputWidth = a_w;
        outputHeight = a_h;
        samples = a_s;
        bounces = a_b;
    }
}

/*public struct Triangle
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
};*/

/*public struct ptMesh
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

};*/
