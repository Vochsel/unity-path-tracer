using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ptCamera
{

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
         //   Debug.Log(handledObject.worldMatrix);
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
    public uint lightType;
    public Matrix4x4 worldMatrix;

    public ptLight(Light a_light)
    {
        color = new Vector3(a_light.color.r, a_light.color.g, a_light.color.b);
        intensity = a_light.intensity;
        worldMatrix = a_light.transform.localToWorldMatrix;
        switch (a_light.type)
        {
            case LightType.Point: lightType = (uint)ptLightType.POINT; break;
            case LightType.Directional: lightType = (uint)ptLightType.DIRECTIONAL; break;
            case LightType.Spot: lightType = (uint)ptLightType.SPOT; break;
            default: lightType = (uint)ptLightType.DIRECTIONAL; break;
        }
    }
}

public struct ptLightHandler
{

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