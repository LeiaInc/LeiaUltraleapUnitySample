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

namespace LeiaUnity
{
    [DefaultExecutionOrder(451)]
    [CustomEditor(typeof(LeiaRemote))]
    public class LeiaRemoteEditor : UnityEditor.Editor

    #region Properties
    {
        private const string RemoteDevice = "Any Android Device";
        private const string StreamingModeLabel = "Streaming Mode";
        private const string ContentModeLabel = "Content Mode";
        private const string CompressionModeLabel = "Compression Mode";
        private const string ResolutionModeLabel = "Resolution Mode";
        private const string PerformanceModeHeader = "Streaming Optimizations";
        private const string PerformanceModeHelper = "Performance Mode offers optimizations for streaming to the device. The quality displayed in this mode does not reflect built content and will lessen 3D precision. We recommend Quality Mode whenever possible.";

        private const string LeiaRemoteHeader = "Leia Remote";
        private const string LeiaRemoteBody = "Live preview for Android LitByLeia devices.";
        private const string LeiaRemoteInfo = "Content in editor will appear as Side-By-Side. This allows for a better Live Preview effect on device with head tracking.";
        private LeiaRemote _controller;

        #endregion
        #region UnityCallbacks

        void OnValidate()
        {
            UpdateRemoteEditorSettings();
        }
        public override void OnInspectorGUI()
        {
            if (_controller == null)
            {
                _controller = (LeiaRemote)target;
                UpdateRemoteEditorSettings();
            }
            if (!_controller.enabled)
            {
                return;
            }

            ShowLeiaRemoteInfo();
            //ShowContentModeControl();
            ShowStreamingModeControl();
            if (_controller.DesiredStreamingMode == LeiaRemote.StreamingMode.Performance)
            {
                ShowPerformanceModeOptions();
            }
        }

        #endregion
        #region Settings

        private void ForceQualityOptions()
        {
            if (_controller.DesiredContentResolution == LeiaRemote.ContentResolution.Normal && _controller.DesiredContentCompression == LeiaRemote.ContentCompression.PNG)
            {
                return;
            }
            _controller.DesiredContentResolution = LeiaRemote.ContentResolution.Normal;
            _controller.DesiredContentCompression = LeiaRemote.ContentCompression.PNG;
            UpdateRemoteEditorSettings();
        }
        private void UpdateRemoteEditorSettings()
        {
            //Editor Settings related to Unity Remote are compatible with Leia Remote 2
            EditorSettings.unityRemoteDevice = RemoteDevice;
            EditorSettings.unityRemoteCompression = _controller.DesiredContentCompression.ToString();
            EditorSettings.unityRemoteResolution = _controller.DesiredContentResolution.ToString();
        }

        #endregion
        #region GUI

        private void ShowPerformanceModeOptions()
        {
            ShowCompressionControl();
            ShowResolutionControl();
        }
        private void ShowStreamingModeControl()
        {
            LeiaRemote.StreamingMode[] modes = { LeiaRemote.StreamingMode.Quality, LeiaRemote.StreamingMode.Performance };
            string[] options = { modes[0].ToString(), modes[1].ToString() };

            int previousIndex = (int)_controller.DesiredStreamingMode;
            UndoableInputFieldUtils.PopupLabeled(index =>
            {
                _controller.DesiredStreamingMode = modes[index];
                UpdateRemoteEditorSettings();
            }, StreamingModeLabel, previousIndex, options, _controller);
        }
        private void ShowLeiaRemoteInfo()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label(LeiaRemoteHeader, EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            GUILayout.Label(LeiaRemoteBody);
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(LeiaRemoteInfo, MessageType.Info);
            EditorGUILayout.Space(10);
        }
        private void ShowContentModeControl()
        {
            LeiaRemote.ContentMode[] modes = { LeiaRemote.ContentMode.Interlaced, LeiaRemote.ContentMode.Tiled };
            string[] options = { modes[0].ToString(), modes[1].ToString() };

            int previousIndex = (int)_controller.DesiredContentMode;
            UndoableInputFieldUtils.PopupLabeled(index =>
            {
                _controller.DesiredContentMode = modes[index];
                UpdateRemoteEditorSettings();
            }, ContentModeLabel, previousIndex, options, _controller);
        }
        private void ShowCompressionControl()
        {
            LeiaRemote.ContentCompression[] modes = new[] { LeiaRemote.ContentCompression.PNG, LeiaRemote.ContentCompression.JPEG };
            string[] options = { modes[0].ToString(), modes[1].ToString() };

            int previousIndex = (int)_controller.DesiredContentCompression;
            UndoableInputFieldUtils.PopupLabeled(index =>
            {
                _controller.DesiredContentCompression = modes[index];
                UpdateRemoteEditorSettings();
            }, CompressionModeLabel, previousIndex, options, _controller);
        }
        private void ShowResolutionControl()
        {
            LeiaRemote.ContentResolution[] modes = new[] { LeiaRemote.ContentResolution.Normal, LeiaRemote.ContentResolution.Downsize };
            string[] options = { modes[0].ToString(), modes[1].ToString() };

            int previousIndex = (int)_controller.DesiredContentResolution;
            UndoableInputFieldUtils.PopupLabeled(index =>
            {
                _controller.DesiredContentResolution = modes[index];
                UpdateRemoteEditorSettings();
            }, ResolutionModeLabel, previousIndex, options, _controller);
        }
        #endregion
    }
}
