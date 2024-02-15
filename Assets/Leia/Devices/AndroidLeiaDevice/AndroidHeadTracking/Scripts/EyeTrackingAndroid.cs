#if UNITY_ANDROID && !UNITY_EDITOR
using Leia;
using UnityEngine;
#endif
namespace LeiaUnity
{
    public class EyeTrackingAndroid : EyeTracking
    {
        protected override void UpdateCameraConnectedStatus()
        {
            _cameraConnectedPrev = _cameraConnected;
            _cameraConnected = true;
            base.UpdateCameraConnectedStatus();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    public override void InitHeadTracking()
    {
        IsTracking = RenderTrackingDevice.Instance.CNSDK.EnableFacetracking(true);
    }
#endif

        public override void StartTracking()
        {
            base.StartTracking();
#if UNITY_ANDROID && !UNITY_EDITOR

        if (RenderTrackingDevice.Instance.CNSDK == null)
        {
            this.InitHeadTracking();
        }

        if (RenderTrackingDevice.Instance.CNSDK != null)
        {
            if(!IsTracking)
            {
                RenderTrackingDevice.Instance.CNSDK.Resume();
                IsTracking = true;
            }
        }
        else
        {
            IsTracking = false;
#if !UNITY_EDITOR
            Debug.LogError("RenderTrackingDevice.Instance.CNSDK is null!");
#endif
        }
#endif
        }

        public override void StopTracking()
        {
            IsTracking = false;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (RenderTrackingDevice.Instance.CNSDK != null)
        {
            RenderTrackingDevice.Instance.CNSDK.Pause();
        }
#endif
        }

        protected override void TerminateHeadTracking()
        {
            base.TerminateHeadTracking();
#if UNITY_ANDROID && !UNITY_EDITOR
        if (RenderTrackingDevice.Instance.CNSDK != null)
        {
            RenderTrackingDevice.Instance.CNSDK.EnableFacetracking(false);
            IsTracking = false;
        }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    public override void SetProfilingEnabled(bool enabed)
    {
        RenderTrackingDevice.Instance.CNSDK.SetFaceTrackingProfiling(enabed);
        _isProfilingEnabled = enabed;
    }

    AndroidJavaClass javaClockClass = null;

    double GetSystemTimeMs()
    {
        if (javaClockClass == null)
        {
            javaClockClass = new AndroidJavaClass("android.os.SystemClock");
        }
        long timeNs = javaClockClass.CallStatic<long>("elapsedRealtimeNanos");
        return (double)(timeNs) * 1e-6;
    }

    long GetSystemTimeNs()
    {
        if (javaClockClass == null)
        {
            javaClockClass = new AndroidJavaClass("android.os.SystemClock");
        }
        long timeNs = javaClockClass.CallStatic<long>("elapsedRealtimeNanos");
        return timeNs;
    }
    private bool _isProfilingEnabled = true;
#endif

        public override AbstractTrackingResult GetDeviceTrackingResult()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        Leia.Vector3 predictedFacePosition;
        Leia.Vector3 nonPredictedFacePosition;

        if (RenderTrackingDevice.Instance.CNSDK != null)
        {
            isPrimaryFaceSet = RenderTrackingDevice.Instance.CNSDK.GetPrimaryFace(out predictedFacePosition);
            bool nonPredFaceFound = RenderTrackingDevice.Instance.CNSDK.GetNonPredictedPrimaryFace(out nonPredictedFacePosition);

            if (IsProfilingEnabled)
            {
                Leia.HeadTracking.FrameProfiling frameProfiling;

                RenderTrackingDevice.Instance.CNSDK.GetFaceTrackingProfiling(out frameProfiling);
                EyeTrackingProcessingTime = (float)(frameProfiling.faceDetectorEndTime - frameProfiling.faceDetectorStartTime) * 1e-6f;
            }

            AbstractTrackingResult abstractTrackingResult = new AbstractTrackingResult();

            int FaceIndex = 0;
            
            UnityEngine.Vector3 PredictedFacePosition = new UnityEngine.Vector3(
                predictedFacePosition.x,
                predictedFacePosition.y,
                predictedFacePosition.z
                );
    
            UnityEngine.Vector3 NonPredictedFacePosition = UnityEngine.Vector3.zero;
            
            if (nonPredFaceFound)
            {
            NonPredictedFacePosition = new UnityEngine.Vector3(
                nonPredictedFacePosition.x,
                nonPredictedFacePosition.y,
                nonPredictedFacePosition.z
                );
            }

            UnityEngine.Vector3 Velocity = new UnityEngine.Vector3(
                0,
                0,
                0
            );
            UnityEngine.Vector3 Angle = new UnityEngine.Vector3(
                0,
                0,
                0
            );
            Face face = new Face(FaceIndex, NonPredictedFacePosition, PredictedFacePosition, Velocity, Angle);
            abstractTrackingResult.AddFace(face);

            return abstractTrackingResult;
        }
        else
        {
            Debug.LogError("CNSDK not found!");
        }
#endif
            return null;
        }

        protected override void OnDisable()
        {
            if (!ApplicationQuitting)
            {
#if UNITY_ANDROID
                if (Instance == this)
                {
                    StopTracking();
                }
#endif

                base.OnDisable();
            }
        }
    }
}