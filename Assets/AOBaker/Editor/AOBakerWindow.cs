using System.Collections;
using System.Collections.Generic;
using ASL.AOBaker;
using UnityEngine;
using UnityEditor;

public class AOBakerWindow : EditorWindow
{

    private Vector2 m_SettingScrollerVector;
    private Vector2 m_PreviewScrollerVector;
    private float m_PreviewTextureHeight = 128;
    private bool m_IsDragPreviewArea = false;
    private bool m_BakeSettingFoldOut = true;
    private bool m_AOMapSettingFoldOut = true;
    private bool m_OtherSettingFoldOut = true;

    private int[] m_Sizes = new[] {256, 512, 1024, 2048};

    private bool m_StaticOnly = false;
    private bool m_IgnoreSkinned = false;

    private MeshRenderer m_Target;

    private Texture2D m_Result;

    private BakeSettings m_BakeSettings = new BakeSettings()
    {
        traceRadius  = AOBakeConstants.kDefaultTraceRadius,
        samplerType  = AOBakeConstants.kDefaultSamplerType,
        numSamples   = AOBakeConstants.kDefaultNumSamples,
        aoMapSize    = AOBakeConstants.kDefaultAOMapSize,
        aoMapPadding = AOBakeConstants.kDefaultAOMapPadding,
        cullBack = AOBakeConstants.kDefaultCullBack,
    };

    private enum Page
    {
        SettingPage,
        PreviewPage,
    }

    private class Styles
    {
        public GUIContent setting = new GUIContent("Setting");
        public GUIContent preview = new GUIContent("Preview");
        public GUIContent AOMapSetting = new GUIContent("AOMap Settings");
        public GUIContent bakeSetting = new GUIContent("Bake Settings");
        public GUIContent traceRadius = new GUIContent("Trace Radius");
        public GUIContent otherSetting = new GUIContent("Other Settings");
        public GUIContent sampler = new GUIContent("Sampler");
        public GUIContent numSamples = new GUIContent("Number Of Samples");
        public GUIContent cullBack = new GUIContent("Cull Back");
        public GUIContent size = new GUIContent("Resolution");
        public GUIContent padding = new GUIContent("Padding");
        public GUIContent staticOnly = new GUIContent("Static Only");
        public GUIContent ignoreSkinned = new GUIContent("Ignore SkinnedMeshRenderer");
        public GUIContent aoTarget = new GUIContent("AO Target");
        public GUIContent bake = new GUIContent("Bake");
        public GUIContent collect = new GUIContent("Collect");
        public GUIContent save = new GUIContent("Save");
        public GUIContent pixels = new GUIContent("texels");
        public  GUIContent aoMap = new GUIContent("AO Map");

        public GUIContent[] sizes =
            {new GUIContent("256"), new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048")};
        public GUIStyle buttonLeft = "ButtonLeft";
        public GUIStyle buttonRight = "ButtonRight";
        public GUIStyle buttonMid = "ButtonMid";
        public GUIStyle windowBottom = "WindowBottomResize";
        public GUIStyle toolbar = "Toolbar";
        public GUIStyle background = "GameViewBackground";
    }

    private static Styles sStyles;

    private static Styles styles
    {
        get
        {
            if(sStyles == null)
                sStyles = new Styles();
            return sStyles;
        }
    }

    private Page m_SelectionPage = Page.SettingPage;

    private List<AOBakeBatch> m_Batches;

    [MenuItem("Tools/AOBaker")]
    static void Init()
    {
        AOBakerWindow window = AOBakerWindow.GetWindow<AOBakerWindow>();
        window.titleContent = new GUIContent("AOBaker");
    }

    void OnDestroy()
    {
        if (m_Result)
        {
            Object.DestroyImmediate(m_Result);
            m_Result = null;
        }
    }

    void OnGUI()
    {
        OnTabGUI(new Rect(position.width * 0.5f - 100, 10, 200, 20));

        OnPageGUI(new Rect(0, 40, position.width, position.height - 40));
    }

    private void OnTabGUI(Rect rect)
    {
        m_SelectionPage = GUI.Toggle(new Rect(rect.x, rect.y, 100, rect.height), m_SelectionPage == Page.SettingPage, styles.setting,
            styles.buttonLeft)
            ? Page.SettingPage
            : m_SelectionPage;
        m_SelectionPage = GUI.Toggle(new Rect(rect.x + 100, rect.y, 100, rect.height), m_SelectionPage == Page.PreviewPage, styles.preview,
            styles.buttonRight)
            ? Page.PreviewPage
            : m_SelectionPage;
    }

    private void OnPageGUI(Rect rect)
    {
        switch (m_SelectionPage)
        {
            case Page.SettingPage:
                OnSettingPageGUI(rect);
                break;
            case Page.PreviewPage:
                OnPreviewPageGUI(rect);
                break;
        }
    }

    private void OnSettingPageGUI(Rect rect)
    {
        GUILayout.BeginArea(new Rect(rect.x, rect.y, rect.width, rect.height - 30));

        m_SettingScrollerVector = GUILayout.BeginScrollView(m_SettingScrollerVector);

        OnBakeSettingGUI();
        OnAOMapSettingGUI();
        OnOtherSettingGUI();

        GUILayout.EndScrollView();

        GUILayout.EndArea();

        DrawBottomBarGUI(rect, rect.height - 25);
    }

    private void DrawBottomBarGUI(Rect rect, float posY)
    {
        if (GUI.Button(new Rect(rect.x + rect.width * 0.5f - 150, rect.y + posY, 100, 20), styles.collect, styles.buttonLeft))
        {
            m_Batches = AOBakeUtils.CollectBatches(m_StaticOnly, m_IgnoreSkinned);
        }
        if (GUI.Button(new Rect(rect.x + rect.width * 0.5f - 50, rect.y + posY, 100, 20), styles.bake, styles.buttonMid))
        {
            if (m_Batches == null)
                m_Batches = AOBakeUtils.CollectBatches(m_StaticOnly, m_IgnoreSkinned);
            
            m_Result = AOBakeUtils.BakeScene(m_Target, m_Batches, m_BakeSettings);
        }

        bool guiEnable = GUI.enabled;
        GUI.enabled = m_Result != null;

        if (GUI.Button(new Rect(new Rect(rect.x + rect.width * 0.5f + 50, rect.y + posY, 100, 20)), styles.save, styles.buttonRight))
        {
            Save();
        }

        GUI.enabled = guiEnable;
    }

    private void OnBakeSettingGUI()
    {
        EditorGUI.indentLevel = 0;
        m_BakeSettingFoldOut = EditorGUILayout.Foldout(m_BakeSettingFoldOut, styles.bakeSetting);

        if (m_BakeSettingFoldOut)
        {
            EditorGUI.indentLevel = 1;

            m_Target = EditorGUILayout.ObjectField(styles.aoTarget, m_Target, typeof(MeshRenderer), true) as MeshRenderer;

            m_BakeSettings.traceRadius = Mathf.Max(0.01f, EditorGUILayout.FloatField(styles.traceRadius, m_BakeSettings.traceRadius));
            m_BakeSettings.cullBack = EditorGUILayout.Toggle(styles.cullBack, m_BakeSettings.cullBack);

            m_BakeSettings.samplerType =
                (SamplerType) EditorGUILayout.EnumPopup(styles.sampler, m_BakeSettings.samplerType);

            m_BakeSettings.numSamples = EditorGUILayout.IntSlider(styles.numSamples, m_BakeSettings.numSamples, AOBakeConstants.kMinNumSamples, AOBakeConstants.kMaxNumSamples);
        }
    }

    private void OnAOMapSettingGUI()
    {
        EditorGUI.indentLevel = 0;
        m_AOMapSettingFoldOut = EditorGUILayout.Foldout(m_AOMapSettingFoldOut, styles.AOMapSetting);

        if (m_AOMapSettingFoldOut)
        {
            EditorGUI.indentLevel = 1;

            m_BakeSettings.aoMapSize = EditorGUILayout.IntPopup(styles.size, m_BakeSettings.aoMapSize, styles.sizes, m_Sizes);
            m_BakeSettings.aoMapPadding = EditorGUILayout.IntSlider(styles.padding, m_BakeSettings.aoMapPadding, AOBakeConstants.kMinAOMapPadding, AOBakeConstants.kMaxAOMapPadding);
        }
    }

    private void OnOtherSettingGUI()
    {
        EditorGUI.indentLevel = 0;
        m_OtherSettingFoldOut = EditorGUILayout.Foldout(m_OtherSettingFoldOut, styles.otherSetting);

        if (m_OtherSettingFoldOut)
        {
            EditorGUI.indentLevel = 1;

            m_StaticOnly = EditorGUILayout.Toggle(styles.staticOnly, m_StaticOnly);
            m_IgnoreSkinned = EditorGUILayout.Toggle(styles.ignoreSkinned, m_IgnoreSkinned);
        }
    }

    private void OnPreviewPageGUI(Rect rect)
    {
        DrawBottomBarGUI(rect, 30);

        Rect dragRect = new Rect(rect.x + 20, rect.y + rect.height - m_PreviewTextureHeight - 18, rect.width - 40, 18);

        if (Event.current.type == EventType.MouseDown && dragRect.Contains(Event.current.mousePosition))
        {
            m_IsDragPreviewArea = true;
        }
        else if (Event.current.type == EventType.MouseUp)
            m_IsDragPreviewArea = false;
        else if (Event.current.type == EventType.MouseDrag && m_IsDragPreviewArea)
        {
            m_PreviewTextureHeight = Mathf.Clamp(position.height - Event.current.mousePosition.y - 18, 100, position.width-40);
            Repaint();
        }

        EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.ResizeVertical);

        GUI.Box(new Rect(rect.x, rect.y + rect.height - m_PreviewTextureHeight - 18, rect.width, 18), string.Empty,
            styles.toolbar);
        GUI.Box(new Rect(rect.x + 20, rect.y + rect.height - m_PreviewTextureHeight - 11, rect.width - 40, 11),
            string.Empty, styles.windowBottom);

        GUI.Box(new Rect(rect.x, rect.y + rect.height - m_PreviewTextureHeight, rect.width, m_PreviewTextureHeight),string.Empty, styles.background );
        
    
        if (m_Result)
        {
            Rect texturePos = new Rect(rect.x + rect.width * 0.5f - m_PreviewTextureHeight * 0.5f,
                rect.y + rect.height - m_PreviewTextureHeight, m_PreviewTextureHeight, m_PreviewTextureHeight);

            GUI.DrawTexture(texturePos, m_Result);
        }
    }

    private void Save()
    {
        if (m_Result)
        {
            string savePath = EditorUtility.SaveFilePanel("", "", "", "png");
            if (string.IsNullOrEmpty(savePath))
                return;

            byte[] buffer = m_Result.EncodeToPNG();
            System.IO.File.WriteAllBytes(savePath, buffer);

            savePath = FileUtil.GetProjectRelativePath(savePath);
            if (!string.IsNullOrEmpty(savePath))
                AssetDatabase.ImportAsset(savePath);
        }
    }
}
