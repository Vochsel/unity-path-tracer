using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PT_Editor : EditorWindow {

    ptRenderer renderer;

    RenderTexture previewTex;

    Vector2 editorScrollPos = Vector2.zero;

    //Editor Variables
    //public uint rWidth = 1280, rHeight = 720, rSamples = 4, rBounces = 8;
    public ptRenderSettings renderSettings = new ptRenderSettings(1280, 720, 2, 4);


    [MenuItem("PathTracer/Render View")]
    static void Init()
    {

        //mainGSkin.

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
        renderer.SetupRenderer(renderSettings);
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
        using (var horizontalScope = new GUILayout.HorizontalScope("box"))
        {
            using (var h1Scpe = new GUILayout.VerticalScope("box", GUILayout.MinWidth(200), GUILayout.MaxWidth(400)))
            {
                GUILayout.Label("Settings", EditorStyles.boldLabel);
                editorScrollPos = GUILayout.BeginScrollView(editorScrollPos, GUIStyle.none);

                /* Settings UI */
                
                int labelWidth = 100;

                using (var hS1 = new GUILayout.HorizontalScope("label"))
                {
                    GUILayout.Label("Output Width: ", GUILayout.MaxWidth(labelWidth));
                    renderSettings.outputWidth = Convert.ToUInt16(GUILayout.TextField(renderSettings.outputWidth.ToString(), GUILayout.MaxWidth(50)));
                }

                using (var hS2 = new GUILayout.HorizontalScope("label"))
                {
                    GUILayout.Label("Output Height: ", GUILayout.MaxWidth(labelWidth));
                    renderSettings.outputHeight = Convert.ToUInt16(GUILayout.TextField(renderSettings.outputHeight.ToString(), GUILayout.MaxWidth(50)));
                }

                using (var hS3 = new GUILayout.HorizontalScope("label"))
                {
                    GUILayout.Label("Samples: " + renderSettings.samples, GUILayout.MaxWidth(labelWidth));
                    renderSettings.samples = Convert.ToUInt16(GUILayout.HorizontalSlider(renderSettings.samples, 1, 16));
                }

                using (var hS4 = new GUILayout.HorizontalScope("label"))
                {
                    GUILayout.Label("Bounces: " + renderSettings.bounces, GUILayout.MaxWidth(labelWidth));
                    renderSettings.bounces = Convert.ToUInt16(GUILayout.HorizontalSlider(renderSettings.bounces, 1, 32));
                }

                if (GUILayout.Button("Pause Render"))
                {
                    if (renderer.IsActive)
                        PauseEditorPreview();
                    else
                        ResumeEditorPreview();
                }

                if (GUILayout.Button("Reset Render"))
                {
                    if(EditorApplication.update == null)
                        EditorApplication.update = EditorUpdate;
                    renderer.ResetRenderer(renderSettings);
                }
                if (GUILayout.Button("Cache Render"))
                {
                    renderer.CacheChanges = !renderer.CacheChanges;
                }
                if (GUILayout.Button("Export Render"))
                {
                    renderer.SaveRenderToFile("/../Renders/");
                }

                GUILayout.EndScrollView();
            }

            using (GUILayout.VerticalScope h1Scpe = new GUILayout.VerticalScope("box", GUILayout.MinWidth(100), GUILayout.MaxWidth(900)))
            {
                GUILayout.Label("Preview", EditorStyles.boldLabel); 
                
                GUILayout.Label(new GUIContent(previewTex), GUILayout.MinWidth(100), GUILayout.MaxWidth(900), GUILayout.MinHeight(100), GUILayout.MaxHeight(1000));
                //GUI.DrawTexture(new Rect(0.0f, 0.0f, position.width, position.height), previewTex, ScaleMode.ScaleToFit);
            }
        }

        
        // GUI.DrawTexture(new Rect(0.0f, 0.0f, position.width, position.height), previewTex, ScaleMode.ScaleToFit);

        /**/
    }
    
}
