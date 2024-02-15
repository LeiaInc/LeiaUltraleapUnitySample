/*********************************************************************************************************
*
* Copyright (C) 2024  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*
*********************************************************************************************************
*/

using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Text;

namespace LeiaUnity
{

    [InitializeOnLoad]
    public class LeiaRecommendedSettings : EditorWindow
    {
        static bool forceShow = true;
        delegate bool FailTest();
        /// <summary>
        /// Container for Unity Setting that may need adjustment based on Leia SDK requirements
        /// </summary>
        class Recommendation
        {
            /// <summary>
            /// Recommendation that requires manual action from the user
            /// </summary>
            /// <param name="title">GUI label title</param>
            /// <param name="unitySetting">Unity Setting state that may require adjustment</param>
            /// <param name="unitySettingPass">Unity Setting's passing state</param>
            /// <param name="failTest">Lamda expression. Check Unity Setting Pass state, return true when failure occurs </param>
            /// <param name="toolTip">Recommendation Tooltip</param>
            /// <param name="ignoreKey">Editor Prefs ignore key</param>
            public Recommendation(string title, object unitySetting, object unitySettingPass, FailTest failTest, string toolTip, string ignoreKey)
            {
                Title = title;
                UnitySetting = unitySetting;
                UnitySettingPass = unitySettingPass;
                ToolTip = toolTip;
                FailTest = failTest;
                IgnoreKey = ignoreKey;
            }
            /// <summary>
            /// Recommendation with auto-fix button option
            /// </summary>
            /// <param name="title">GUI label title</param>
            /// <param name="unitySetting">Unity Setting state that may require adjustment</param>
            /// <param name="unitySettingPass">Unity Setting's passing state</param>
            /// <param name="actionToPass">Lamda expression. Auto-fix action to bring Unity Setting to Pass State</param>
            /// <param name="failTest">Lamda expression. Check Unity Setting Pass state, return true when failure occurs </param>
            /// <param name="toolTip">Recommendation Tooltip</param>
            /// <param name="ignoreKey">Editor Prefs ignore key</param>
            /// <summary>
            public Recommendation(string title, object unitySetting, object unitySettingPass, UnityAction actionToPass, FailTest failTest, string toolTip, string ignoreKey)
            {
                Title = title;
                UnitySetting = unitySetting;
                UnitySettingPass = unitySettingPass;
                ActionToPass = actionToPass;
                ToolTip = toolTip;
                FailTest = failTest;
                IgnoreKey = ignoreKey;
            }
            public string Title { set; get; }
            public object UnitySetting { set; get; }
            public object UnitySettingPass { set; get; }
            public UnityAction ActionToPass { set; get; }
            public string ToolTip { set; get; }
            public FailTest FailTest;
            public string IgnoreKey { set; get; }
            public bool IsIgnored { set; get; }
            public bool CheckRecommendation() //returns true if this recommendation needs to be shown
            {
                if (FailTest() && !EditorPrefs.HasKey(IgnoreKey))
                {
                    LeiaEditorWindowUtils.HorizontalLine();
                    LeiaEditorWindowUtils.BeginHorizontal();
                    LeiaEditorWindowUtils.Label(Title, ToolTip, true);
                    LeiaEditorWindowUtils.FlexibleSpace();
                    LeiaEditorWindowUtils.Label("(?)  ", ToolTip, false);
                    LeiaEditorWindowUtils.EndHorizontal();
                    LeiaEditorWindowUtils.Label(string.Format(currentValue, UnitySetting), ToolTip, false);
                    LeiaEditorWindowUtils.BeginHorizontal();
                    if (ActionToPass != null) //button solution
                    {
                        LeiaEditorWindowUtils.Button(ActionToPass, string.Format(useRecommended, UnitySettingPass));
                    }
                    else //manual solution
                    {
                        LeiaEditorWindowUtils.Label(string.Format(changeToRecommended, UnitySettingPass), ToolTip, false);
                    }
                    LeiaEditorWindowUtils.FlexibleSpace();
                    LeiaEditorWindowUtils.Button(() => { EditorPrefs.SetBool(IgnoreKey, true); IsIgnored = true; }, "Ignore");
                    LeiaEditorWindowUtils.EndHorizontal();
                    return true;
                }
                LeiaEditorWindowUtils.Space(5);
                return false;
            }
        }
        static List<Recommendation> recommendations;
        /// <summary>
        /// Container for Game View Resolution / Aspect Ratio recommendation
        /// </summary>
        class DeviceGameViewResolution
        {
            /// <summary>
            /// Game View Resolution / Aspect Ratio recommendation
            /// </summary>
            /// <param name="res">Resolution</param>
            /// <param name="isRotatable">Does device auto-rotate?</param>
            public DeviceGameViewResolution(string name, int[] res, bool isRotatable)
            {
                this.Name = name;
                this.Res = res;
                this.IsRotatable = isRotatable;
            }
            public string Name { get; set; }
            public int[] Res { get; set; }
            public bool IsRotatable { get; set; }
        }

        /// <summary>
        /// For each device we suport on a platform
        /// </summary>
        class PlatformDeviceResolutions
        {
            /// <summary>
            /// Add a list of device resolutions suppoted on the current platform
            /// </summary>
            /// <param name="gameViewResolutions"> Platform supported device game view resolutions</param>
            public PlatformDeviceResolutions(List<DeviceGameViewResolution> gameViewResolutions)
            {
                GameViewResolutions = gameViewResolutions;
            }
            public List<DeviceGameViewResolution> GameViewResolutions { get; set; }
            public string DisplayGameViewResolutions()
            {
                StringBuilder s = new StringBuilder();
                for (int i = 0; i < GameViewResolutions.Count; i++)
                {
                    s.AppendLine();
                    s.Append(GameViewResolutions[i].IsRotatable ?
                        string.Format("[{0}, {1}] or [{1}, {0}] for {2}", GameViewResolutions[i].Res[0], GameViewResolutions[i].Res[1], GameViewResolutions[i].Name) :
                        string.Format("[{0}, {1}] for {2}", GameViewResolutions[i].Res[0], GameViewResolutions[i].Res[1], GameViewResolutions[i].Name));
                }
                return s.ToString();
            }
            public bool FailMatchGameView()
            {
                GetMainGameViewSize();
                //If any resolutions match for a target platform, pass Fail Check
                for (int i = 0; i < GameViewResolutions.Count; i++)
                {
                    if ((gameViewResolution.x == GameViewResolutions[i].Res[0] && gameViewResolution.y == GameViewResolutions[i].Res[1]) ||
                    (GameViewResolutions[i].IsRotatable &&
                    (gameViewResolution.y == GameViewResolutions[i].Res[0] && gameViewResolution.x == GameViewResolutions[i].Res[1])))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        static PlatformDeviceResolutions platformResolutions;
        static Vector2 gameViewResolution;
        const string useRecommended = "Use recommended: {0}";
        const string changeToRecommended = "Change to: {0}";
        const string currentValue = "Current: {0}";
        const string editor_Recommendation_ForcePopUp = "LeiaUnity.Recommendation.ForcePopUp";
        const string editor_PrevIssueCount = "LeiaUnity.PreviousIssueCount";
        static LeiaRecommendedSettings window;
        const string BannerAssetFilename = "LeiaUnitySDK";
        private static Texture2D _bannerImage;
        private static Vector2 scrollPosition;
        static int ignoreCount;
        static int issueCount;
        static int prevIssueCount;
        [MenuItem("Window/LeiaUnity/Recommended Unity Settings &r")]
        public static void Init()
        {
            _bannerImage = Resources.Load<Texture2D>(BannerAssetFilename);
            window = GetWindow<LeiaRecommendedSettings>(true, "LeiaUnity SDK Recommended Settings");
            window.Show();
            window.minSize = LeiaEditorWindowUtils.WindowMinSize;
            InitRecommendations();
            UpdateIssuesIgnores();
        }
        static LeiaRecommendedSettings()
        {
            EditorApplication.update += Update;
        }
        public static void ForceRecommendationCompliance()
        {
            InitRecommendations();
            for (int i = 0; i < recommendations.Count; i++)
            {
                if (recommendations[i].ActionToPass != null)
                {
                    recommendations[i].ActionToPass.Invoke();
                }
            }
        }

        static void Update()
        {
            UpdateIssuesIgnores();
            if (ShouldForceWindowPopUp())
            {
                Init();
            }
        }

        static void UpdateIssuesIgnores()
        {
            ignoreCount = issueCount = 0;
            if (recommendations == null)
            {
                InitRecommendations();
            }
            for (int i = 0; i < recommendations.Count; i++)
            {
                if (recommendations[i].FailTest() && !recommendations[i].IsIgnored)
                {
                    issueCount++;
                }
                if (recommendations[i].IsIgnored)
                {
                    ignoreCount++;
                }
            }
        }
        static bool ShouldForceWindowPopUp()
        {
            forceShow = EditorPrefs.GetBool(editor_Recommendation_ForcePopUp, true);
            if (!forceShow)
            {
                return false;
            }
            //Using editor prefs to store window variable otherwise reset when entering play mode
            prevIssueCount = EditorPrefs.GetInt(editor_PrevIssueCount, 0);
            if (issueCount != prevIssueCount)
            {
                int delta = issueCount - prevIssueCount;
                EditorPrefs.SetInt(editor_PrevIssueCount, issueCount);
                if (window == null && delta > 0)
                {
                    return true;
                }
            }
            return false;
        }
        void AcceptAllButtonPressed()
        {
            ForceRecommendationCompliance();
        }
        public void OnGUI()
        {
            if (window == null)
            {
                Init();
            }
            LeiaEditorWindowUtils.TitleTexture(_bannerImage);

            if (issueCount == 0)
            {
                LeiaEditorWindowUtils.Space(2);
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                LeiaEditorWindowUtils.Label("Fantastic! You're good to go!", style);
            }
            else
            {
                LeiaEditorWindowUtils.HelpBox("Recommended Unity Editor settings for LeiaUnity SDK:", MessageType.Warning);
            }

            scrollPosition = LeiaEditorWindowUtils.BeginScrollView(scrollPosition);
            LeiaEditorWindowUtils.Space(5);

            if (recommendations != null)
            {
                int recommendationsShown = 0;
                for (int i = 0; i < recommendations.Count; i++)
                {
                    if (recommendations[i].CheckRecommendation())
                    {
                        recommendationsShown++;
                    }
                }
                if (recommendationsShown > 0)
                {
                    LeiaEditorWindowUtils.Button(AcceptAllButtonPressed, "Accept All");
                }
            }
            LeiaEditorWindowUtils.EndScrollView();
            LeiaEditorWindowUtils.BeginHorizontal();

            UndoableInputFieldUtils.BoolFieldWithTooltip(() => { forceShow = EditorPrefs.GetBool(editor_Recommendation_ForcePopUp, false); return forceShow; }, b => { forceShow = b; EditorPrefs.SetBool(editor_Recommendation_ForcePopUp, b); }, "  Automatically Pop-up", "Display this window when LeiaUnity detects unrecommended Unity Settings. Alternatively, this widow can be opened from LeiaUnity-> Recommended Unity Settings", window);

            if (ignoreCount > 0)
            {
                LeiaEditorWindowUtils.Button(() =>
                {
                    for (int i = 0; i < recommendations.Count; i++)
                    {
                        if (EditorPrefs.HasKey(recommendations[i].IgnoreKey))
                        {
                            EditorPrefs.DeleteKey(recommendations[i].IgnoreKey);
                            recommendations[i].IsIgnored = false;
                        }
                    }
                }, string.Format("Reset Ignores ({0})", ignoreCount));
            }
            LeiaEditorWindowUtils.EndHorizontal();
            LeiaEditorWindowUtils.Space(2);
        }

        static void InitRecommendations()
        {
            if (recommendations == null)
            {
                recommendations = new List<Recommendation>();
            }
            recommendations.Clear();
            recommendations.Add(new Recommendation(
                "Build Target",
                EditorUserBuildSettings.activeBuildTarget,
                "Supported Platforms: Android, Windows",
                () =>
                {
                    bool buildAndroid = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
                    bool buildWin = (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
                        EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64);
                    return !(buildAndroid || buildWin);
                },
                "Supported Platforms: Android, Windows",
                "LeiaUnity.Ignore.BuildTarget"));

#if UNITY_ANDROID
            UnityEngine.Rendering.GraphicsDeviceType[] graphicsAPIs = new UnityEngine.Rendering.GraphicsDeviceType[1];
            graphicsAPIs[0] = UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3;
            recommendations.Add(new Recommendation(
                "Graphics APIs",
                PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)[0],
                string.Format("{0}", UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3),
                () =>
                {
                    PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsAPIs);
                }
                ,
                () =>
                {
                    return (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)[0] != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3);
                },
                "LeiaUnity SDK requires OpenGLES3 Graphics API",
                "LeiaUnity.Ignore.GraphicsAPIs"));

            recommendations.Add(new Recommendation(
                "Min Android SDK",
                PlayerSettings.Android.minSdkVersion,
                string.Format("{0} + ", AndroidSdkVersions.AndroidApiLevel29),
                () => PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29,
                () =>
                {
                    return (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel29);
                },
                "LeiaUnity SDK relies on Android Library calls that are only available on or after API Level 29",
                "LeiaUnity.Ignore.AndroidMinSDK"));

            recommendations.Add(new Recommendation(
                "Scripting Backend",
                PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android),
                string.Format("{0}", ScriptingImplementation.IL2CPP),
                () => PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP),
                () =>
                {
                    return (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP);
                },
                "LeiaUnity SDK requires Scripting Backend of IL2CPP",
                "LeiaUnity.Ignore.ScriptingBackend"));

            recommendations.Add(new Recommendation(
                "Target Architecture",
                PlayerSettings.Android.targetArchitectures,
                string.Format("{0}", AndroidArchitecture.ARM64),
                () => PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64,
                () =>
                {
                    return (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64);
                },
                "LeiaUnity SDK requires Target Architecture of ARM64",
                "LeiaUnity.Ignore.TargetArchitecture"));
#elif UNITY_EDITOR_WIN
            UnityEngine.Rendering.GraphicsDeviceType[] graphicsAPIs = new UnityEngine.Rendering.GraphicsDeviceType[1];
            graphicsAPIs[0] = UnityEngine.Rendering.GraphicsDeviceType.Direct3D11;

            recommendations.Add(new Recommendation(
            "Graphics APIs",
            PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64)[0],
            string.Format("{0}", UnityEngine.Rendering.GraphicsDeviceType.Direct3D11),
            () =>
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, graphicsAPIs);
            }
            ,
            () =>
            {
                return (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 &&
            PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows)[0] != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11);
            },
            "LeiaUnity SDK shiould be built as an x64 Standalone for Windows.",
            "LeiaUnity.Ignore.GraphicsAPIs"));

            recommendations.Add(new Recommendation(
            "Build Target",
            EditorUserBuildSettings.activeBuildTarget.ToString(),
            BuildTarget.StandaloneWindows64.ToString(),
            () =>
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            },
            () =>
            {
                return EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64;
            },
            "LeiaUnity SDK should be built as an x64 Standalone for Windows.",
            "LeiaUnity.Ignore.BuildTarget"));

            recommendations.Add(new Recommendation(
            "Scripting Backend",
            PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup).ToString(),
            ScriptingImplementation.Mono2x.ToString(),
            () =>
            {
                PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup, ScriptingImplementation.Mono2x);
            },
            () =>
            {
                return PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup) != ScriptingImplementation.Mono2x;
            },
            "The LeiaUnity SDK requires the scripting backend to be set to Mono2x for Windows.",
            "LeiaUnity.Ignore.ScriptingBackend"));
#endif
#if UNITY_2021_1_OR_NEWER
            recommendations.Add(new Recommendation(
                ".NET API Compatability Level",
                PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android),
                string.Format("{0}", ApiCompatibilityLevel.NET_Unity_4_8),
                () => PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Unity_4_8),
                () =>
                {
                    return (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android) != ApiCompatibilityLevel.NET_Unity_4_8);
                },
                "LeiaUnity SDK requires .NET API Compatability Level of ApiCompatibilityLevel.NET_Unity_4_8",
                "LeiaUnity.Ignore.APICompatabilityLevel"));
#endif

            recommendations.Add(new Recommendation(
                "Anisotropic Textures",
                 QualitySettings.anisotropicFiltering,
                 "Per Texture",
                 () => QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable,
                 () =>
                 {
                     return QualitySettings.anisotropicFiltering != AnisotropicFiltering.Enable;
                 },
                 "Having Ansiotropic set to Forced On causes visual artifacts under certain scenarios.",
                 "LeiaUnity.Ignore.AnsiotropicFiltering"));


#if UNITY_2020_1_OR_NEWER
            recommendations.Add(new Recommendation(
                "Gradle Minification Release",
                PlayerSettings.Android.minifyRelease,
                 false,
                 () => PlayerSettings.Android.minifyRelease = false,
                 () => { return PlayerSettings.Android.minifyRelease; },
                 "Android Minification will cause backlight failure on Android device. See Player Settings -> Android -> Minify.",
                 "LeiaUnity.Ignore.AndroidMiniRelease2020Plus"));

            recommendations.Add(new Recommendation(
                "Gradle Minification Debug",
                PlayerSettings.Android.minifyDebug,
                 false,
                 () => PlayerSettings.Android.minifyDebug = false,
                 () => { return PlayerSettings.Android.minifyDebug; },
                 "Android Minification will cause backlight failure on Android device. See Player Settings -> Android -> Minify.",
                 "LeiaUnity.Ignore.AndroidMiniDebug2020Plus"));
    #if !UNITY_2022_1_OR_NEWER
            recommendations.Add(new Recommendation(
                "Gradle Minification R8",
                PlayerSettings.Android.minifyWithR8,
                 false,
                 () => PlayerSettings.Android.minifyWithR8 = false,
                 () => { return PlayerSettings.Android.minifyWithR8; },
                 "Android Minification will cause backlight failure on Android device. See Player Settings -> Android -> Minify.",
                 "LeiaUnity.Ignore.AndroidMiniR82020Plus"));
    #endif
#endif

            for (int i = 0; i < recommendations.Count; i++)
            {
                recommendations[i].IsIgnored = EditorPrefs.HasKey(recommendations[i].IgnoreKey);
            }
        }
        public static void GetMainGameViewSize()
        {
            Vector2 res = Handles.GetMainGameViewSize();
            if (!Mathf.Approximately(res.magnitude, gameViewResolution.magnitude))
            {
                gameViewResolution = res;
            }
        }
    }
}