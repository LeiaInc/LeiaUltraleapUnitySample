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

using System.Linq;
using UnityEditor;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace LeiaUnity
{

    public static class LeiaEditorWindowUtils
    {
        public static Vector2 WindowMinSize { get { return new Vector2(500, 700); } }
        //Center and scale title Image to fit window width
        public static void TitleTexture(Texture2D texture)
        {
            if (texture)
            {
                var rect = GUILayoutUtility.GetRect(texture.width, texture.height);
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            }
        }
        public static void HelpBox(string message, MessageType type)
        {
            EditorGUILayout.HelpBox(message, type);
        }
        public static void Label(string message, bool bold)
        {
            if (bold)
            {
                GUILayout.Label(message, EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label(message);
            }
        }
        public static void Label(string message, string toolTip, bool bold)
        {
            if (bold)
            {
                GUILayout.Label(new GUIContent(message, toolTip), EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label(new GUIContent(message, toolTip));
            }
        }
        public static void Label(string message, GUIStyle style)
        {
            GUILayout.Label(message, style);
        }
        public static void Button(UnityAction action, string label)
        {
            if (GUILayout.Button(label))
            {
                action();
            }
        }
        public static System.Enum EnumPopup(string label, System.Enum selected)
        {
            return EditorGUILayout.EnumPopup(label, selected);
        }
        public static System.Enum EnumPopup(string label, System.Enum selected, GUILayoutOption option)
        {
            return EditorGUILayout.EnumPopup(label, selected, option);
        }
        public static System.Enum EnumPopup(string label, System.Enum selected, GUILayoutOption[] options)
        {
            return EditorGUILayout.EnumPopup(label, selected, options);
        }
        public static bool Foldout(bool foldoutBool, string label)
        {
            return EditorGUILayout.Foldout(foldoutBool, label, true);
        }
        public static Vector2 BeginScrollView(Vector2 scrollPosition)
        {
            return GUILayout.BeginScrollView(scrollPosition);
        }
        public static void EndScrollView()
        {
            GUILayout.EndScrollView();
        }
        public static void BeginHorizontal()
        {
            GUILayout.BeginHorizontal();
        }
        public static void EndHorizontal()
        {
            GUILayout.EndHorizontal();
        }
        public static void BeginVertical()
        {
            GUILayout.BeginVertical();
        }
        public static void EndVertical()
        {
            GUILayout.EndVertical();
        }
        public static void FlexibleSpace()
        {
            GUILayout.FlexibleSpace();
        }
        public static void BeginHorizontalCenter()
        {
            BeginHorizontal();
            FlexibleSpace();
        }
        public static void EndHorizontalCenter()
        {
            FlexibleSpace();
            EndHorizontal();
        }
        public static void Space(int pixels)
        {
            GUILayout.Space(pixels);
        }
        public static void HorizontalLine()
        {
            HorizontalLine(Color.grey, 1, 20);
        }
        public static void HorizontalLine(Color color, int thickness, int padding)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += (float)padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
        public static Vector2 DrawScrollableSelectableLabel(Vector2 scrollPosition, float width, string text, GUIStyle style)
        {
            using (EditorGUILayout.ScrollViewScope scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollViewScope.scrollPosition;
                float textHeight = style.CalcHeight(new GUIContent(text), width);
                EditorGUILayout.SelectableLabel(text, style, GUILayout.MinHeight(textHeight));
                return scrollPosition;
            }
        }

        public static Vector2 DrawScrollableSelectableLabel(Vector2 scrollPosition, float width, string text, GUIStyle style, float verticalPadding, float horizontalPadding, float minHeight)
        {
            using (EditorGUILayout.ScrollViewScope scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.MinHeight(minHeight)))
            {
                scrollPosition = scrollViewScope.scrollPosition;
                float textHeight = style.CalcHeight(new GUIContent(text), width - horizontalPadding);
                EditorGUILayout.SelectableLabel(text, style, GUILayout.MinHeight(textHeight + verticalPadding));
                return scrollPosition;
            }
        }
    }
    public static class LeiaEditorUtils
    {
        private const string HDRP_PACKAGE = "render-pipelines.high-definition";
        private const string URP_PACKAGE = "render-pipelines.universal";

        private const string TAG_HDRP = "LEIA_HDRP_DETECTED";
        private const string TAG_URP = "LEIA_URP_DETECTED";

        [UnityEditor.Callbacks.DidReloadScripts]
        public static void OnScriptsReloaded()
        {
            CheckForRenderPipelines();
        }
        private static void CheckForRenderPipelines()
        {
            ListRequest request = Client.List(true);
            BuildTargetGroup platform = EditorUserBuildSettings.selectedBuildTargetGroup;

            WaitforPackageLoad(request);
            var packagesList = request.Result.ToList();

            bool hasHDRP = packagesList.Find(x => x.name.Contains(HDRP_PACKAGE)) != null;
            bool hasURP = packagesList.Find(x => x.name.Contains(URP_PACKAGE)) != null;

            ValidateCompileDefinition(hasURP, TAG_URP, platform);
            ValidateCompileDefinition(hasHDRP, TAG_HDRP, platform);
        }
        private static void ValidateCompileDefinition(bool passContition, string tag, BuildTargetGroup platform)
        {
            if (passContition)
            {
                CompileDefineUtil.AddCompileDefine(platform, tag);
            }
            else
            {
                CompileDefineUtil.RemoveCompileDefine(tag, new[] { platform });
            }
        }
        private static void WaitforPackageLoad(ListRequest request)
        {
            if (request == null) { return; }
            for (int i = 0; i < 1000; i++)
            {
                if (request.Result != null) { break; }
                Task.Delay(10).Wait();
            }
            if (request.Result == null)
            {
                UnityEngine.Debug.LogError("Timeout Exception in requesting packages!");
                return;
            }
        }
    }
}
