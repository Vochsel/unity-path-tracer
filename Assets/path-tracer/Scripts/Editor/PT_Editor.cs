using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PT_Editor : EditorWindow {

    ptRenderer renderer;

    RenderTexture previewTex;

    [MenuItem("PathTracer/Render View")]
    static void Init()
    {
        PT_Editor window = GetWindow<PT_Editor>();

        window.autoRepaintOnSceneChange = true;
        window.Show();
    }

    void OnEnable()
    {
        Debug.Log("PT_Editor Enable");
        StartEditorPreview();
    }

    void OnDisable()
    {
        Debug.Log("PT_Editor Disable");
        StopEditorPreview();
    }

    void StartEditorPreview()
    {
        EditorApplication.update = EditorUpdate;
        renderer = new ptRenderer();
        renderer.SetupRenderer();
    }
    
    void PauseEditorPreview()
    {
        EditorApplication.update = null;
        renderer.IsActive = false;
    }

    void ResumeEditorPreview()
    {
        EditorApplication.update = EditorUpdate;

        renderer.IsActive = true;
    }

    void StopEditorPreview()
    {
        EditorApplication.update = null;
        renderer.StopRenderer();
    }

    void Awake()
    {
        Debug.Log("PT_Editor Awake");
    }

    void EditorUpdate()
    {
        previewTex = renderer.RunShader();
    }

	void OnGUI()
    {
        //Debug.Log("PT_Editor On GUI");
        //Draw texture
        GUI.DrawTexture(new Rect(0.0f, 0.0f, position.width, position.height), previewTex, ScaleMode.ScaleToFit);

        if (GUILayout.Button("Pause Render"))
        {
            if (renderer.IsActive)
                PauseEditorPreview();
            else
                ResumeEditorPreview();
        }

        if (GUILayout.Button("Reset Render"))
        {
            renderer.ResetRenderer();
        }
        if (GUILayout.Button("Cache Render"))
        {
            renderer.CacheChanges = !renderer.CacheChanges;
        }
    }
    
}
