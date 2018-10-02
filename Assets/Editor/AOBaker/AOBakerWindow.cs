using System.Collections;
using System.Collections.Generic;
using ASL.AOBaker;
using UnityEngine;
using UnityEditor;

public class AOBakerWindow : EditorWindow
{

    private Vector2 m_SettingScrollerVector;
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
        public GUIContent size = new GUIContent("Resolution");
        public GUIContent padding = new GUIContent("Padding");
        public GUIContent staticOnly = new GUIContent("Static Only");
        public GUIContent ignoreSkinned = new GUIContent("Ignore SkinnedMeshRenderer");
        public GUIContent aoTarget = new GUIContent("AO Target");
        public GUIContent bake = new GUIContent("Bake");
        public GUIContent pixels = new GUIContent("texels");

        public GUIContent[] sizes =
            {new GUIContent("256"), new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048")};
        public GUIStyle buttonLeft = "ButtonLeft";
        public GUIStyle buttonRight = "ButtonRight";
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

    [MenuItem("Tools/AOBaker")]
    static void Init()
    {
        AOBakerWindow window = AOBakerWindow.GetWindow<AOBakerWindow>();
        window.titleContent = new GUIContent("AOBaker");
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

        if (GUI.Button(new Rect(rect.x + rect.width * 0.5f - 50, rect.y + rect.height - 25, 100, 20), styles.bake))
        {
            string savePath = EditorUtility.SaveFilePanel("", "", "", "png");
            if (string.IsNullOrEmpty(savePath))
                return;
            var result = AOBakeUtils.BakeScene(m_Target, m_StaticOnly, m_IgnoreSkinned, m_BakeSettings);

            if (result)
            {
                byte[] buffer = result.EncodeToPNG();
                System.IO.File.WriteAllBytes(savePath, buffer);

                Object.DestroyImmediate(result);
            }
        }
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

            m_BakeSettings.samplerType =
                (SamplerType) EditorGUILayout.EnumPopup(styles.sampler, m_BakeSettings.samplerType);

            m_BakeSettings.numSamples = EditorGUILayout.IntSlider(styles.numSamples, m_BakeSettings.numSamples, 2, 20);
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
            m_BakeSettings.aoMapPadding = Mathf.Clamp(EditorGUILayout.IntField(styles.padding, m_BakeSettings.aoMapPadding), 0, 10);
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

    }
}
