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
using LeiaUnity;
using System;
using Leia;
public class LeiaDebugGUI : Singleton<LeiaDebugGUI>
{
    public float StepsForDepthFactor = 0.1f;
    public float StepsForFocalDistance = 0.1f;
    public bool EnableSliderForDepthFactor = true;
    public Vector2Int MinMaxForDepthFactorSlider;
    public bool EnableSliderForFocalDistance = true;
    public Vector2Int MinMaxForFocalDistanceSlider;

    static private LeiaDisplay leiaDisplay;
#if !UNITY_EDITOR && PLATFORM_ANDROID
    private bool isDebugGUIEnabled = false;
    private AndroidJavaClass motionEventClass;
    private AndroidJavaClass systemClock;
    private int tripleTapCount = 0;
    private float lastTripleTapTime = 0;
    private const float tripleTapTimeLimit = 1.5f;
    private long lastDownTime = 0;

    private void Start()
    {
        leiaDisplay = GetComponent<LeiaDisplay>();
        InitializeAndroidDependencies();
        InitializeDepthFactorFocalDistance();
        InitializeEnvironmentVariables();
    }

    private void InitializeAndroidDependencies()
    {
        motionEventClass = new AndroidJavaClass("android.view.MotionEvent");
        systemClock = new AndroidJavaClass("android.os.SystemClock");
    }

    private void InitializeDepthFactorFocalDistance()
    {
        RenderTrackingDevice.Instance.Interlacer.SetProperty(Interlacer.Properties.Baseline, leiaDisplay.DepthFactor);
        RenderTrackingDevice.Instance.Interlacer.SetProperty(Interlacer.Properties.ConvergenceDistance, leiaDisplay.FocalDistance);
    }

    void InitializeEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_ENABLE_STEP_baseline", "ON");
        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_STEP_baseline", StepsForDepthFactor.ToString());
        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_ENABLE_SLIDER_baseline", EnableSliderForDepthFactor ? "ON" : "OFF");
        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_SLIDER_MIN_baseline", MinMaxForDepthFactorSlider.x.ToString());
        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_SLIDER_MAX_baseline", MinMaxForDepthFactorSlider.y.ToString());

        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_ENABLE_STEP_convergence","ON");
        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_STEP_convergence", StepsForFocalDistance.ToString());
        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_ENABLE_SLIDER_convergence", EnableSliderForFocalDistance ? "ON" : "OFF");
        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_SLIDER_MIN_convergence", MinMaxForFocalDistanceSlider.x.ToString());
        Environment.SetEnvironmentVariable("CNSDK_DEBUG_GUI_SLIDER_MAX_convergence", MinMaxForFocalDistanceSlider.y.ToString());
    }

    private void Update()
    {
        HandleTripleTapDetection();

        foreach (Touch touch in Input.touches)
        {
            CreateMotionEvent(touch);
        }
    }

    private void CreateMotionEvent(Touch touch)
    {
        int action = GetAndroidActionFromTouchPhase(touch.phase);

        long eventTime = systemClock.CallStatic<long>("uptimeMillis");
        float androidY = Screen.height - touch.position.y;
        AndroidJavaObject motionEvent = motionEventClass.CallStatic<AndroidJavaObject>("obtain",
                        lastDownTime,
                        eventTime,
                        action,
                        touch.position.x,
                        androidY,
                        0  
               );

        ProcessMotionEvent(motionEvent);
    }

    private int GetAndroidActionFromTouchPhase(TouchPhase phase)
    {
        switch (phase)
        {
            case TouchPhase.Began:
                lastDownTime = systemClock.CallStatic<long>("uptimeMillis");
                return 0;
            case TouchPhase.Ended:
                return 1;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                return 2;
            case TouchPhase.Canceled:
                return 3;
            default:
                return -1;
        }
    }

    private void ProcessMotionEvent(AndroidJavaObject motionEvent)
    {
        if (motionEvent != null)
        {
            if (isDebugGUIEnabled)
            {
                bool eventWasHandled = RenderTrackingDevice.Instance.Interlacer.ProcessGuiInput(motionEvent.GetRawObject());
            }
            else
            {
                Debug.Log("CNSDKGUI is not enabled. Skipping ProcessGuiInput.");
            }

            motionEvent.Dispose();
        }
        else
        {
            Debug.Log("Failed to create MotionEvent.");
        }
    }

    private void HandleTripleTapDetection()
    {
        if (Input.touchCount != 3 || !AllTouchesBegan())
            return;

        tripleTapCount++;
        ProcessTripleTap();
    }

    private bool AllTouchesBegan()
    {
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase != TouchPhase.Began)
                return false;
        }
        return true;
    }

    private void ProcessTripleTap()
    {
        if (tripleTapCount == 1)
        {
            lastTripleTapTime = Time.time;
        }
        else if (tripleTapCount == 3)
        {
            if (Time.time - lastTripleTapTime <= tripleTapTimeLimit)
            {
                ToggleDebugGUI(!isDebugGUIEnabled);
                tripleTapCount = 0;
            }
            else
            {
                tripleTapCount = 1;
                lastTripleTapTime = Time.time;
            }
        }
    }

    public void ToggleDebugGUI(bool enable)
    {
        Leia.Interlacer.Config interlacerConfig = RenderTrackingDevice.Instance.Interlacer.GetConfig();
        interlacerConfig.showGui = enable;
        RenderTrackingDevice.Instance.Interlacer.SetConfig(interlacerConfig);
        isDebugGUIEnabled = enable;
    }

    private void OnDestroy()
    {
        motionEventClass.Dispose();
    }

    [AttributeUsage (AttributeTargets.Method)]
	internal class MonoPInvokeCallbackAttribute : Attribute
	{
		public MonoPInvokeCallbackAttribute (Type type)
		{
			Type = type;
		}
		public Type Type { get; private set; }
	}
    [MonoPInvokeCallback(typeof(Leia.EventListener.Callback))]
    public static void CNSDKListenerCallback(IntPtr userData, ref Leia.Event cnsdkEvent)
    {
        string logMessage = String.Format("Got CNSDK Event: type={0}", cnsdkEvent.eventType.ToString());
        if (cnsdkEvent.eventType == Leia.EventType.COMPONENT)
        {
            logMessage += String.Format("\n  - COMPONENT: {0}", cnsdkEvent.component.component.ToString());
            if (cnsdkEvent.component.component == Leia.ComponentId.FACE_TRACKING)
            {
                Leia.FaceTrackingEventCode faceTrackingEvent = (Leia.FaceTrackingEventCode)cnsdkEvent.component.code;
                logMessage += String.Format("\n    - {0}", faceTrackingEvent.ToString());
            }
            else if (cnsdkEvent.component.component == Leia.ComponentId.INTERLACER)
            {
                Leia.InterlacerEventCode interlacerEvent = (Leia.InterlacerEventCode)cnsdkEvent.component.code;
                logMessage += String.Format("\n    - {0}", interlacerEvent.ToString());
                if (interlacerEvent == Leia.InterlacerEventCode.DEBUG_MENU_CLOSED)
                {
                    // leia_interlacer's debug menu has been closed, payload: NONE
                    LeiaDebugGUI.Instance.ToggleDebugGUI(false);
                }
                else if (interlacerEvent == Leia.InterlacerEventCode.DEBUG_MENU_UPDATE)
                {
                    // leia_interlacer's debug menu has changed a value, payload: [optional] const char* reason - value's id
                    string updatedValue = cnsdkEvent.component.stringPayload;
                    if (updatedValue == "baseline")
                    {
                        float baseline;
                        if (RenderTrackingDevice.Instance.Interlacer.GetProperty(updatedValue, out baseline))
                        {
                            // use new baseline value
                            logMessage += String.Format("\n      - baseline={0}", baseline);
                            leiaDisplay.DepthFactor = baseline;
                        }
                    }
                    else if (updatedValue == "convergence")
                    {
                        float focalDistance;
                        if (RenderTrackingDevice.Instance.Interlacer.GetProperty(updatedValue, out focalDistance))
                        {
                            // use new focalDistance value
                            logMessage += String.Format("\n      - focalDistance={0}", focalDistance);
                            leiaDisplay.FocalDistance = focalDistance;
                        }
                    }
                }
            }
        }
        Debug.Log(logMessage);
    }
#endif
}
