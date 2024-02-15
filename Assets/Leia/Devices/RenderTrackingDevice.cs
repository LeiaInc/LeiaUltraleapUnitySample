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
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
using SimulatedReality;
#endif
using SRUnity;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LeiaUnity
{
    public class RenderTrackingDevice : Singleton<RenderTrackingDevice>
    {

        private LightfieldMode _desiredLightfieldMode;
        bool is3DScene = true;
        public enum LightfieldMode { On, Off };

        public event Action<LightfieldMode> LightfieldModeChanged = delegate { };

        public LightfieldMode DesiredLightfieldMode
        {
            get
            {
                return _desiredLightfieldMode;
            }
            set
            {
                if (_desiredLightfieldMode != value)
                {
                    _desiredLightfieldMode = value;
                    Update2D3D();
                    LightfieldModeChanged.Invoke(value);
                }
                
            }
        }
        #region Core
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
    SimulatedRealityCamera SRCam;
    SRUnity.SrRenderModeHint renderHint = new SrRenderModeHint();

#elif !UNITY_EDITOR && PLATFORM_ANDROID

    bool isBackLightOnPrev;
    void Update()
    {
       if(!is3DScene) return;
       bool is3D = Is3DMode();
        if (!is3D && NumFaces != 0)
        {
            Update2D3D();
        }
        else if(isBackLightOnPrev != !is3D)
        {
            LightfieldModeChanged.Invoke(is3D ? LightfieldMode.On : LightfieldMode.Off);
        }
        isBackLightOnPrev = is3D;
        
    }
    private class CNSDKHolder
    {
        private static bool _isInitialized = false;
        private static Leia.SDK _cnsdk = null;
        private static Leia.Interlacer _interlacer = null;
        private static Leia.EventListener _cnsdkListener = null;
        public static Leia.SDK Get()
        {
            return _cnsdk;
        }
        public static Leia.Interlacer GetInterlacer()
        {
            return _interlacer;
        }
        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            Leia.LegalOrientations legalOrientations;
            if (Leia.SDK.GetLegalOrientations(out legalOrientations))
            {
                Screen.autorotateToPortrait = legalOrientations.portrait != 0;
                Screen.autorotateToPortraitUpsideDown = legalOrientations.reversePortrait != 0;
                Screen.autorotateToLandscapeLeft = legalOrientations.landscape != 0;
                Screen.autorotateToLandscapeRight = legalOrientations.reverseLandscape != 0;
                Screen.orientation = ScreenOrientation.AutoRotation;
            }

            _isInitialized = true;
            Leia.SDKConfig cnsdkConfig = new Leia.SDKConfig();
            cnsdkConfig.SetPlatformLogLevel(Leia.LogLevel.Off); // Leia.LogLevel.Off
            cnsdkConfig.SetFaceTrackingEnable(true);
            _cnsdk = new Leia.SDK(cnsdkConfig);
            cnsdkConfig.Dispose();

            // Wait for LeiaSDK Initialization
            // TODO: convert to coroutine
            //yield return new WaitUntil(() => _cnsdk.IsInitialized());
            while (!_cnsdk.IsInitialized()) {}
                _cnsdkListener = new Leia.EventListener(LeiaDebugGUI.CNSDKListenerCallback);
            
            try
            {
                _interlacer = new Leia.Interlacer(_cnsdk);
                Leia.Interlacer.Config interlacerConfig = _interlacer.GetConfig();
                interlacerConfig.showGui = false;
                _interlacer.SetConfig(interlacerConfig);
            }
            catch (Exception e)
            {
                Debug.Log("Interlacer error: " + e.ToString());
            }
        }
    }
    public Leia.SDK CNSDK { get { return CNSDKHolder.Get(); } }
    public Leia.Interlacer Interlacer { get { return CNSDKHolder.GetInterlacer(); } }
    public Leia.SDK.ConfigHolder sdkConfig;
    private Texture[] inputViews = new Texture[2];
#else
#endif
        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnResume()
        {
#if !UNITY_EDITOR
            Update2D3D();
#endif
        }

        private void OnPause()
        {
#if !UNITY_EDITOR
            if(Is3DMode())
            {
                Set3DMode(false);
            }
#endif
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                OnResume();
            }
            else
            {
                OnPause();
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                OnPause();
            }
            else
            {
                OnResume();
            }
        }

        private void OnApplicationQuit()
        {
            Set3DMode(false);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LeiaDisplay leiaDisplay = FindObjectOfType<LeiaDisplay>();
            if (leiaDisplay != null && leiaDisplay.enabled)
            {
                is3DScene = true;
                Update2D3D();
            }
            else
            {
                is3DScene = false;
                Set3DMode(false);
            }
        }

        public void Initialize()
        {
            Debug.Log("LeiaRenderDevice::Initialize()");
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        string srCameraObjectName = "SRCamera";
        GameObject srCameraObject = GameObject.Find(srCameraObjectName);
        if(srCameraObject == null)
        {
            srCameraObject = new GameObject(srCameraObjectName);
            srCameraObject.transform.position = Vector3.zero;
        }
        if (srCameraObject.GetComponent<SimulatedRealityCamera>() == null)
        {
            SRCam = srCameraObject.gameObject.AddComponent<SimulatedRealityCamera>();
        }
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        Debug.Log("LeiaRenderDevice::Initialize::Calling CNSDKHolder.Initialize..");
        CNSDKHolder.Initialize();
        sdkConfig = CNSDK.GetConfig();
        Debug.Log("LeiaRenderDevice::Initialize::Applying EyeTracking");
        EyeTrackingAndroid.Instance = GetComponent<EyeTrackingAndroid>();
        if(EyeTrackingAndroid.Instance == null)
        {
            EyeTrackingAndroid.Instance = gameObject.AddComponent<EyeTrackingAndroid>();
        }
        EyeTrackingAndroid.Instance.enabled = true;
#else
#endif
        }

        #endregion
        #region Render

        public void Render(LeiaDisplay leiaDisplay, ref RenderTexture outputTexture)
        {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        Leia.Interlacer interlacer = RenderTrackingDevice.Instance.Interlacer;
        if (interlacer != null && Is3DMode())
        {
            interlacer.SetLayerCount(1);
            inputViews[0] = leiaDisplay.GetEyeCamera(0).targetTexture;
            inputViews[1] = leiaDisplay.GetEyeCamera(1).targetTexture;
            interlacer.SetInputViews(inputViews, 0);
            interlacer.SetOutput(outputTexture);
            interlacer.Render();
        }
        else if (!Is3DMode())
        {
            Graphics.Blit(leiaDisplay.GetEyeCamera(0).targetTexture, outputTexture);
        }

#else
#endif
        }

        #endregion
        #region Tracking

        public int NumFaces
        {
            get
            {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
            return SRUnity.SrRenderModeHint.ShouldRender3D() ? 1 : 0;
#elif !UNITY_EDITOR && PLATFORM_ANDROID
            return EyeTrackingAndroid.Instance.NumFaces;
#else
#endif
                return 0;
            }
        }
        public void SetTrackerEnabled(bool trackerEnabled)
        {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        SRUnity.SrRenderModeHint renderHint = new SrRenderModeHint();
        if (trackerEnabled)
        {
            renderHint.Prefer3D();
        }
        else
        {
            renderHint.Prefer2D();
        }
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        EyeTrackingAndroid.Instance.enabled = trackerEnabled;
#else
#endif
        }
        public void UpdateFacePosition()
        {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        EyeTrackingAndroid.Instance.UpdateFacePosition();
#else
#endif
        }
        public Vector3 GetPredictedFacePosition()
        {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
            return SRUnity.SRHead.Instance.GetEyePosition(ISRSettingsInterface.GetProjectSettings(null));
#elif !UNITY_EDITOR && PLATFORM_ANDROID
            if (sdkConfig == null)
            {
                Debug.Log("sdkConfig is null.");
                return Vector3.zero;
            }

            Vector3 PredictedFacePos = EyeTrackingAndroid.Instance.GetPredictedFacePosition();

            switch (sdkConfig.GetDeviceNaturalOrientation())
            {
                case Leia.Orientation.Landscape:
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.LandscapeLeft:
                            return PredictedFacePos;
                        case ScreenOrientation.PortraitUpsideDown:
                            return new Vector3(-PredictedFacePos.y, PredictedFacePos.x, PredictedFacePos.z);
                        case ScreenOrientation.LandscapeRight:
                            return new Vector3(-PredictedFacePos.x, -PredictedFacePos.y, PredictedFacePos.z);
                        case ScreenOrientation.Portrait:
                            return new Vector3(PredictedFacePos.y, -PredictedFacePos.x, PredictedFacePos.z);
                        default:
                            return PredictedFacePos;
                    }

                case Leia.Orientation.Portrait:
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.Portrait:
                            return PredictedFacePos;
                        case ScreenOrientation.LandscapeLeft:
                            return new Vector3(-PredictedFacePos.y, PredictedFacePos.x, PredictedFacePos.z);
                        case ScreenOrientation.PortraitUpsideDown:
                            return new Vector3(-PredictedFacePos.x, -PredictedFacePos.y, PredictedFacePos.z);
                        case ScreenOrientation.LandscapeRight:
                            return new Vector3(PredictedFacePos.y, -PredictedFacePos.x, PredictedFacePos.z);
                        default:
                            return PredictedFacePos;
                    }

                case Leia.Orientation.ReverseLandscape:
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.LandscapeRight:
                            return PredictedFacePos;
                        case ScreenOrientation.Portrait:
                            return new Vector3(-PredictedFacePos.y, PredictedFacePos.x, PredictedFacePos.z);
                        case ScreenOrientation.LandscapeLeft:
                            return new Vector3(-PredictedFacePos.x, -PredictedFacePos.y, PredictedFacePos.z);
                        case ScreenOrientation.PortraitUpsideDown:
                            return new Vector3(PredictedFacePos.y, -PredictedFacePos.x, PredictedFacePos.z);
                        default:
                            return PredictedFacePos;
                    }

                case Leia.Orientation.ReversePortrait:
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.PortraitUpsideDown:
                            return PredictedFacePos;
                        case ScreenOrientation.LandscapeRight:
                            return new Vector3(-PredictedFacePos.y, PredictedFacePos.x, PredictedFacePos.z);
                        case ScreenOrientation.Portrait:
                            return new Vector3(-PredictedFacePos.x, -PredictedFacePos.y, PredictedFacePos.z);
                        case ScreenOrientation.LandscapeLeft:
                            return new Vector3(PredictedFacePos.y, -PredictedFacePos.x, PredictedFacePos.z);
                        default:
                            return PredictedFacePos;
                    }

                default: // Unspecified or other orientations
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.LandscapeLeft:
                            return PredictedFacePos;
                        case ScreenOrientation.PortraitUpsideDown:
                            return new Vector3(-PredictedFacePos.y, PredictedFacePos.x, PredictedFacePos.z);
                        case ScreenOrientation.LandscapeRight:
                            return new Vector3(-PredictedFacePos.x, -PredictedFacePos.y, PredictedFacePos.z);
                        case ScreenOrientation.Portrait:
                            return new Vector3(PredictedFacePos.y, -PredictedFacePos.x, PredictedFacePos.z);
                        default:
                            return PredictedFacePos;
                    }
            }
#else
            return Vector3.zero;
#endif
        }
        public Vector3 GetNonPredictedFacePosition()
        {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
            var eyePosition = SRUnity.SRHead.Instance.GetEyePosition(ISRSettingsInterface.GetProjectSettings(null));
            return new Vector3(eyePosition.x, eyePosition.y, -eyePosition.z) * 10.0f;
#elif !UNITY_EDITOR && PLATFORM_ANDROID
            if (sdkConfig == null)
            {   
                Debug.Log("sdkConfig is null.");
                return Vector3.zero;
            }

            Vector3 nonPredictedFacePos = EyeTrackingAndroid.Instance.GetNonPredictedFacePosition();

            switch (sdkConfig.GetDeviceNaturalOrientation())
            {
                case Leia.Orientation.Landscape:
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.LandscapeLeft:
                            return nonPredictedFacePos;
                        case ScreenOrientation.PortraitUpsideDown:
                            return new Vector3(-nonPredictedFacePos.y, nonPredictedFacePos.x, nonPredictedFacePos.z);
                        case ScreenOrientation.LandscapeRight:
                            return new Vector3(-nonPredictedFacePos.x, -nonPredictedFacePos.y, nonPredictedFacePos.z);
                        case ScreenOrientation.Portrait:
                            return new Vector3(nonPredictedFacePos.y, -nonPredictedFacePos.x, nonPredictedFacePos.z);
                        default:
                            return nonPredictedFacePos;
                    }

                case Leia.Orientation.Portrait:
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.Portrait:
                            return nonPredictedFacePos;
                        case ScreenOrientation.LandscapeLeft:
                            return new Vector3(-nonPredictedFacePos.y, nonPredictedFacePos.x, nonPredictedFacePos.z);
                        case ScreenOrientation.PortraitUpsideDown:
                            return new Vector3(-nonPredictedFacePos.x, -nonPredictedFacePos.y, nonPredictedFacePos.z);
                        case ScreenOrientation.LandscapeRight:
                            return new Vector3(nonPredictedFacePos.y, -nonPredictedFacePos.x, nonPredictedFacePos.z);
                        default:
                            return nonPredictedFacePos;
                    }

                case Leia.Orientation.ReverseLandscape:
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.LandscapeRight:
                            return nonPredictedFacePos;
                        case ScreenOrientation.Portrait:
                            return new Vector3(-nonPredictedFacePos.y, nonPredictedFacePos.x, nonPredictedFacePos.z);
                        case ScreenOrientation.LandscapeLeft:
                            return new Vector3(-nonPredictedFacePos.x, -nonPredictedFacePos.y, nonPredictedFacePos.z);
                        case ScreenOrientation.PortraitUpsideDown:
                            return new Vector3(nonPredictedFacePos.y, -nonPredictedFacePos.x, nonPredictedFacePos.z);
                        default:
                            return nonPredictedFacePos;
                    }

                case Leia.Orientation.ReversePortrait:
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.PortraitUpsideDown:
                            return nonPredictedFacePos;
                        case ScreenOrientation.LandscapeRight:
                            return new Vector3(-nonPredictedFacePos.y, nonPredictedFacePos.x, nonPredictedFacePos.z);
                        case ScreenOrientation.Portrait:
                            return new Vector3(-nonPredictedFacePos.x, -nonPredictedFacePos.y, nonPredictedFacePos.z);
                        case ScreenOrientation.LandscapeLeft:
                            return new Vector3(nonPredictedFacePos.y, -nonPredictedFacePos.x, nonPredictedFacePos.z);
                        default:
                            return nonPredictedFacePos;
                    }

                default: // Unspecified or other orientations
                    switch (Screen.orientation)
                    {
                        case ScreenOrientation.LandscapeLeft:
                            return nonPredictedFacePos;
                        case ScreenOrientation.PortraitUpsideDown:
                            return new Vector3(-nonPredictedFacePos.y, nonPredictedFacePos.x, nonPredictedFacePos.z);
                        case ScreenOrientation.LandscapeRight:
                            return new Vector3(-nonPredictedFacePos.x, -nonPredictedFacePos.y, nonPredictedFacePos.z);
                        case ScreenOrientation.Portrait:
                            return new Vector3(nonPredictedFacePos.y, -nonPredictedFacePos.x, nonPredictedFacePos.z);
                        default:
                            return nonPredictedFacePos;
                    }
            }
#else
            return Vector3.zero;
#endif
        }

        #endregion
        #region 2D3D

        void Update2D3D()
        {
            if (DesiredLightfieldMode == LightfieldMode.Off)
            {
                Set3DMode(false);
            }
            else if (DesiredLightfieldMode == LightfieldMode.On)
            {
                Set3DMode(true);
            }
        }
        public void Set3DMode(bool toggle)
        {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        if (toggle)
        {
            renderHint.Prefer3D();
        }
        else
        {
            renderHint.Force2D();
        }
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        if (RenderTrackingDevice.Instance.CNSDK != null)
        {
            RenderTrackingDevice.Instance.CNSDK.SetBacklight(toggle);
        }
#else
#endif
            SetTrackerEnabled(toggle);
        }
        public bool Is3DMode()
        {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        return SRUnity.SrRenderModeHint.ShouldRender3D();
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        return CNSDK.GetBacklight();
#else
#endif
            return true;
        }
        #endregion
        #region DisplayConfig

        public Vector2Int GetDevicePanelResolution()
        {
            Vector2Int panelResolution = new Vector2Int(2560, 1600);  //LP2 defaults
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        panelResolution = SRUnity.SRCore.Instance.getResolution();
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        Leia.Vector2i panelResolutionPx;
        if (sdkConfig != null && sdkConfig.GetPanelResolutionPx(out panelResolutionPx))
        {
            panelResolution.x = panelResolutionPx.x;
            panelResolution.y = panelResolutionPx.y;
        }
#else
#endif
            return panelResolution;
        }

        public Vector2Int GetDeviceViewResolution()
        {
            Vector2Int viewResolution = new Vector2Int(1280, 800);  //LP2 defaults
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        viewResolution = SRUnity.SRCore.Instance.getResolution() / 2;
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        Leia.Vector2i viewResolutionPx;
        if (sdkConfig != null && sdkConfig.GetViewResolutionPx(out viewResolutionPx))
        {
            viewResolution.x = viewResolutionPx.x;
            viewResolution.y = viewResolutionPx.y;
        }
#else
#endif
            return viewResolution;
        }
        public float GetDeviceSystemDisparityPixels()
        {
            float systemDisparityPixels = 4.0f;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        //ToDo: find SR disparity pixels
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        if (sdkConfig != null)
        {
            systemDisparityPixels = 4.0f;
        }
#else
#endif
            return systemDisparityPixels;
        }
        public Vector2 GetDeviceDotPitchInMM()
        {
            Vector2 dotPitchInMM = new Vector2(0.10389f, 0.104375f); //LP2 defaults
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        float dotPitch = SRUnity.SRCore.Instance.getDotPitch();
        dotPitchInMM = new Vector2(dotPitch,dotPitch);
#else
#endif
            return dotPitchInMM;
        }
        public Vector2 GetDisplaySizeInMM()
        {
            Vector2 displaySizeInMM = new Vector2(266f, 168f); //LP2 defaults
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        displaySizeInMM = SRUnity.SRCore.Instance.getPhysicalSize() * 10.0f;
#elif !UNITY_EDITOR && PLATFORM_ANDROID
        Leia.Vector2i displaySizeInMm;
        if (sdkConfig != null && sdkConfig.GetDisplaySizeMm(out displaySizeInMm))
        {
            displaySizeInMM.x = displaySizeInMm.x;
            displaySizeInMM.y = displaySizeInMm.y;
        }
#else
#endif
        return displaySizeInMM;
    }
    public float GetViewingDistanceInMM()
    {
        float viewingDistanceInMM = 450.0f; //LP2 defaults
#if !UNITY_EDITOR 
#if UNITY_ANDROID
        CNSDK.GetViewingDistance(IPDInMM, out viewingDistanceInMM);
#else
        viewingDistanceInMM = 2f * IPDInMM * GetDOverN() / (GetPX() * GetPixelPitch());
#endif
#endif
        return viewingDistanceInMM;
    }
    float ipdInMM = 63;
    public float IPDInMM
    {
        set
        {
            ipdInMM = value;
        }
        get
        {
            return ipdInMM;
        }
    }

    public float GetDOverN()
    {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        //ToDo: Add Windows logic
        return SRUnity.SRCore.Instance.getDoN();
#else
        return 1;
#endif
    }

    public float GetPX() //Lens Pitch
    {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        //ToDo: Add Windows logic
        return SRUnity.SRCore.Instance.getPx();
#else
        return 1;
#endif
    }

    public float GetPixelPitch()
    {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        //ToDo: Add Windows logic
        return SRUnity.SRCore.Instance.getDotPitch();
#else
        return 1;
#endif
    }

#endregion

    }
}