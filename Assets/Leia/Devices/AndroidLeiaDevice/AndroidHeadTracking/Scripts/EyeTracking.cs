using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace LeiaUnity
{
    public class EyeTracking : Singleton<EyeTracking>
    {
        int numFaces;
        public int NumFaces
        {
            get
            {
                return isPrimaryFaceSet ? numFaces : 0;
            }

            private set
            {
                numFaces = value;
            }
        }

        protected int chosenFaceIndex;
        protected int chosenFaceIndexPrev;

        private float faceX = 0, faceY = 0;
        private float _faceZ = 600;
        public float faceZ
        {
            get
            {
                return _faceZ;
            }
            set
            {
                _faceZ = value;
            }
        }

        private float nonPredictedFaceX = 0, nonPredictedFaceY = 0, nonPredictedFaceZ = 600;
        private float predictedFaceX = 0, predictedFaceY = 0, predictedFaceZ = 600;

        RunningFloatAverage runningAverageFaceX;
        RunningFloatAverage runningAverageFaceY;
        RunningFloatAverage runningAverageFaceZ;

        protected enum TrackingState { FaceTracking, NotFaceTracking };
        protected TrackingState priorRequestedState = TrackingState.NotFaceTracking;
        protected TrackingState currentState = TrackingState.NotFaceTracking;
        protected TrackingState requestedState = TrackingState.NotFaceTracking;

        public FaceTrackingStateEngine.FaceTransitionState faceTransitionState
        {
            get
            {
                return faceTrackingStateEngine.faceTransitionState;
            }

            private set
            {
                faceTrackingStateEngine.faceTransitionState = value;
            }
        }

        private float eyeTrackingProcessingTime = 0.0f;
        double t;
        private bool isProfilingEnabled = false;
        protected float delay;
        protected bool IsTracking = false;
        protected bool isPrimaryFaceSet = false;

        protected bool _cameraConnectedPrev;
        protected bool _cameraConnected;
        public bool CameraConnected
        {
            get
            {
                return _cameraConnected;
            }
        }

        protected bool ApplicationQuitting;

        protected virtual void UpdateCameraConnectedStatus()
        {
            if (!_cameraConnected && _cameraConnectedPrev)
            {
                Debug.Log("Camera not connected! Terminating head tracking!");
                TerminateHeadTracking();
            }
            else
            if (_cameraConnected && !_cameraConnectedPrev)
            {
                InitHeadTracking();
            }

            Invoke("UpdateCameraConnectedStatus", 1f);
        }

        protected virtual void OnEnable()
        {
            StartTracking();
        }

        protected virtual void OnDisable()
        {
            if (!ApplicationQuitting)
            {
                faceX = 0;
                faceY = 0;
                //faceZ = LeiaDisplay.Instance.GetDisplayConfig().FocalDistance;
            }
        }

        protected virtual void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                StopTracking();
            }
            else
            {
                if (RenderTrackingDevice.Instance.DesiredLightfieldMode == RenderTrackingDevice.LightfieldMode.On)
                {
                    StartTracking();
                }
            }
        }

        public virtual void StartTracking()
        {
            //faceZ = LeiaDisplay.Instance.displayConfig.FocalDistance;
        }

        public virtual void InitHeadTracking()
        {
            //Implement in child classes
        }

        public virtual void StopTracking()
        {
            //implement in child classes
        }

        public Vector3 GetPredictedFacePosition()
        {
            return new Vector3(predictedFaceX, predictedFaceY, predictedFaceZ);
        }

        public Vector3 GetNonPredictedFacePosition()
        {
            return new Vector3(nonPredictedFaceX, nonPredictedFaceY, nonPredictedFaceZ);
        }

        LeiaDisplay _leiaDisplay;
        LeiaDisplay leiaDisplay
        {
            get
            {
                if (_leiaDisplay == null)
                {
                    _leiaDisplay = FindObjectOfType<LeiaDisplay>();
                }
                if (_leiaDisplay == null)
                {
                    Debug.LogError("EyeTracking:: LeiaDisplay is not present in scene and is attempting to be accessed. Check call stack");
                }
                return _leiaDisplay;
            }
        }

        public float GetEyeTrackingProcessingTime()
        {
            return EyeTrackingProcessingTime;
        }

        FaceTrackingStateEngine faceTrackingStateEngine;
        public float AnimatedBaseline
        {
            get
            {
                return faceTrackingStateEngine.eyeTrackingAnimatedBaselineScalar;
            }
        }

        public float EyeTrackingProcessingTime { get => eyeTrackingProcessingTime; protected set => eyeTrackingProcessingTime = value; }
        public bool IsProfilingEnabled { get => isProfilingEnabled; protected set => isProfilingEnabled = value; }

        private void Awake()
        {
            faceTrackingStateEngine = gameObject.AddComponent<FaceTrackingStateEngine>();
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            runningAverageFaceX = new RunningFloatAverage(60);
            runningAverageFaceY = new RunningFloatAverage(60);
            runningAverageFaceZ = new RunningFloatAverage(60);


            UpdateCameraConnectedStatus();

            if (Instance != null && Instance == this)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        public virtual void SetProfilingEnabled(bool enabed)
        {
            //Implement in child classes
        }

        public virtual void AddTestFace(float x, float y, float z) //A useful method for adding a virtual test face, which can be used for multi-face testing when you don't have other people available to help you test
        {
            //Implement in child classes
        }

        Vector3 ClampToSafeRange(Vector3 FacePositon)
        {
            //Clamp face position values within a safe & reasonable range
            FacePositon = new Vector3(
                    Mathf.Clamp(FacePositon.x, -1000, 1000),
                    Mathf.Clamp(FacePositon.y, -1000, 1000),
                    Mathf.Clamp(FacePositon.z, 50, 1000)
                    );

            return FacePositon;
        }

        public virtual void UpdateFacePosition()
        {
            //The UpdateFacePosition calls each devices GetDeviceTrackingResult method to get the list of faces

            if (CameraConnected && IsTracking)
            {
                double old_time_stamp = t;
                //DisplayConfig config = LeiaDisplay.Instance.GetDisplayConfig();
                //delay = (float)(t - old_time_stamp + config.timeDelay);
                t = Time.time * 1000;

                AbstractTrackingResult trackingResult = GetDeviceTrackingResult();

                //Clamp predicted and non-predicted face position values to be contained within a safe range
                trackingResult.Faces[0].NonPredictedPosition = ClampToSafeRange(trackingResult.Faces[0].NonPredictedPosition);
                trackingResult.Faces[0].PredictedPosition = ClampToSafeRange(trackingResult.Faces[0].PredictedPosition);

                if (trackingResult == null)
                {
                    //No faces detected
                    NumFaces = 0;
                    return;
                }

                NumFaces = trackingResult.NumFaces;

                if (NumFaces > 0)
                {
                    requestedState = TrackingState.FaceTracking;
                    chosenFaceIndexPrev = chosenFaceIndex;
                    chosenFaceIndex = -1;

                    Face ActiveFace = trackingResult.Faces[0];

                    Vector3 currentPos = new Vector3(
                            faceX,
                            faceY,
                            faceZ
                        );

                    Vector3 targetPos = new Vector3(
                        ActiveFace.PredictedPosition.x,
                        ActiveFace.PredictedPosition.y,
                        ActiveFace.PredictedPosition.z
                    );

                    if (targetPos.z > 0)
                    {
                        faceX += (targetPos.x - currentPos.x) * Mathf.Min((Time.deltaTime * 5f), 1f);
                        faceY += (targetPos.y - currentPos.y) * Mathf.Min((Time.deltaTime * 5f), 1f);
                        faceZ += (targetPos.z - currentPos.z) * Mathf.Min((Time.deltaTime * 5f), 1f);
                        predictedFaceX = faceX;
                        predictedFaceY = faceY;
                        predictedFaceZ = faceZ;
                        nonPredictedFaceX = ActiveFace.NonPredictedPosition.x;
                        nonPredictedFaceY = ActiveFace.NonPredictedPosition.y;
                        nonPredictedFaceZ = ActiveFace.NonPredictedPosition.z;
                        runningAverageFaceX.AddSample(ActiveFace.PredictedPosition.x);
                        runningAverageFaceY.AddSample(ActiveFace.PredictedPosition.y);
                        runningAverageFaceZ.AddSample(ActiveFace.PredictedPosition.z);
                    }

                    if (faceTransitionState == FaceTrackingStateEngine.FaceTransitionState.SlidingCameras
                        && Vector3.Distance(currentPos, targetPos) < 10f)
                    {
                        faceTransitionState = FaceTrackingStateEngine.FaceTransitionState.IncreasingBaseline;
                    }

                    if (faceTransitionState == FaceTrackingStateEngine.FaceTransitionState.FaceLocked)
                    {
                        Vector3 facePosPrev = new Vector3(
                            faceX,
                            faceY,
                            faceZ
                        );
                        Vector3 facePosNext = new Vector3(
                            ActiveFace.PredictedPosition.x,
                            ActiveFace.PredictedPosition.y,
                            ActiveFace.PredictedPosition.z
                        );

                        float distance = Mathf.Abs(facePosPrev.z - facePosNext.z);

                        if ((distance < 50 || facePosPrev == Vector3.zero) && ActiveFace.PredictedPosition.z > 0)
                        {
                            faceX = ActiveFace.PredictedPosition.x;
                            faceY = ActiveFace.PredictedPosition.y;
                            faceZ = ActiveFace.PredictedPosition.z;
                            predictedFaceX = ActiveFace.PredictedPosition.x + ActiveFace.Velocity.x * delay;
                            predictedFaceY = ActiveFace.PredictedPosition.y + ActiveFace.Velocity.y * delay;
                            predictedFaceZ = ActiveFace.PredictedPosition.z + ActiveFace.Velocity.z * delay;
                            nonPredictedFaceX = ActiveFace.NonPredictedPosition.x;
                            nonPredictedFaceY = ActiveFace.NonPredictedPosition.y;
                            nonPredictedFaceZ = ActiveFace.NonPredictedPosition.z;
                            runningAverageFaceX.AddSample(faceX);
                            runningAverageFaceY.AddSample(faceY);
                            runningAverageFaceZ.AddSample(faceZ);
                        }
                        else
                        {
                            if (runningAverageFaceZ.Average > 0)
                            {
                                faceX = runningAverageFaceX.Average;
                                faceY = runningAverageFaceY.Average;
                                faceZ = runningAverageFaceZ.Average;
                            }
                            faceTransitionState = FaceTrackingStateEngine.FaceTransitionState.ReducingBaseline;
                        }
                    }
                }
                else
                {
                    faceTransitionState = FaceTrackingStateEngine.FaceTransitionState.ReducingBaseline;
                    requestedState = TrackingState.NotFaceTracking;
                }
            }
        }

        public virtual AbstractTrackingResult GetDeviceTrackingResult()
        {
            //Intentionally left empty.
            //See EyeTrackingAndroid.cs and EyeTrackingWindows.cs for override implementations.
            return null;
        }

        public float GetFrameDelay()
        {
            return delay;
        }


        void OnApplicationQuit()
        {
            ApplicationQuitting = true;
            TerminateHeadTracking();
        }

        protected virtual void TerminateHeadTracking()
        {
            IsTracking = false;
            /// <remove_from_public>
#if UNITY_STANDALONE_WIN
        //HeadTrackingService.Instance.TerminateHeadTracking();
#endif
            /// </remove_from_public>
        }

    }

    public class AbstractTrackingResult
    {
        public AbstractTrackingResult()
        {
            Faces = new List<Face>();
        }

        private int chosenFaceIndex = 0;
        public int NumFaces
        {
            get
            {
                return Faces.Count;
            }
        }

        public int ChosenFaceIndex { get => chosenFaceIndex; protected set => chosenFaceIndex = value; }
        public List<Face> Faces { get => faces; set => faces = value; }

        private List<Face> faces;

        public void AddFace(Face face)
        {
            Faces.Add(face);
        }
    }

    public class Face
    {
        private int faceIndex;
        private Vector3 nonPredictedPosition;
        private Vector3 predictedPosition;
        private Vector3 velocity;
        private Vector3 angle;

        public int FaceIndex { get => faceIndex; set => faceIndex = value; }
        public Vector3 NonPredictedPosition { get => nonPredictedPosition; set => nonPredictedPosition = value; }
        public Vector3 PredictedPosition { get => predictedPosition; set => predictedPosition = value; }
        public Vector3 Velocity { get => velocity; set => velocity = value; }
        public Vector3 Angle { get => angle; set => angle = value; }

        public Face(int FaceIndex, Vector3 NonPredictedPosition, Vector3 PredictedPosition, Vector3 Velocity, Vector3 Angle)
        {
            this.FaceIndex = FaceIndex;
            this.NonPredictedPosition = NonPredictedPosition;
            this.PredictedPosition = PredictedPosition;
            this.Velocity = Velocity;
            this.Angle = Angle;
        }
    }

    public class RunningFloatAverage
    {
        private float _average;
        public float Average
        {
            get
            {
                return _average;
            }
        }
        private int _maxSamplesCount;
        public int maxSamplesCount
        {
            get
            {
                return _maxSamplesCount;
            }
            private set
            {
                _maxSamplesCount = Mathf.Max(value, 1);
            }
        }

        private readonly IndexedQueue<float> sampleValues;

        public int Count
        {
            get
            {
                return sampleValues.Count;
            }
        }

        public RunningFloatAverage(int maxSamplesCount)
        {
            this.maxSamplesCount = maxSamplesCount;
            sampleValues = new IndexedQueue<float>(maxSamplesCount);
        }

        public void AddSample(float value)
        {
            sampleValues.Enqueue(value);

            if (sampleValues.Count > maxSamplesCount)
            {
                sampleValues.Dequeue();
            }

            _average = ComputeAverage();
        }

        private float ComputeAverage()
        {
            float count = sampleValues.Count;
            float sum = 0;

            for (int i = 0; i < count; i++)
            {
                sum += sampleValues[i];
            }


            float average = sum / count;

            return average;
        }

        public void AddOffset(float offset)
        {
            float count = sampleValues.Count;

            for (int i = 0; i < count; i++)
            {
                sampleValues[i] += offset;
            }
        }

        public void Reset()
        {
            sampleValues.Reset();
        }
    }

    public class IndexedQueue<T>
    {
        private int currentPosition = 0;
        private int count;
        readonly private T[] values;

        public int Count
        {
            get
            {
                return count;
            }
        }

        public IndexedQueue(int startCount)
        {
            values = new T[startCount];
        }

        public void Enqueue(T value)
        {
            values[currentPosition] = value;
            currentPosition++;
            if (currentPosition == values.Length)
            {
                currentPosition = 0;
            }
            count = Mathf.Min(values.Length, count + 1);
        }

        public T Dequeue()
        {
            if (count > 0)
            {
                T dequeuedValue = values[currentPosition];
                currentPosition--;
                if (currentPosition == -1)
                {
                    currentPosition = values.Length - 1;
                }
                count--;
                return dequeuedValue;
            }
            return default(T);
        }

        public void Reset()
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = default(T);
            }
            currentPosition = 0;
        }
        int BoundReadPosition(int position)
        {
            if (position >= values.Length)
            {
                position -= values.Length;
            }

            return position;
        }

        public T this[int position]
        {
            get
            {
                return values[BoundReadPosition(position)];
            }
            set
            {
                values[BoundReadPosition(position)] = value;
            }
        }
    }

    public class FaceTrackingStateEngine : Singleton<FaceTrackingStateEngine>
    {
        public enum FaceTransitionState { NoFace, FaceLocked, ReducingBaseline, SlidingCameras, IncreasingBaseline };

        FaceTransitionState _faceTransitionState = FaceTransitionState.NoFace;

        public FaceTransitionState faceTransitionState
        {
            get
            {
                return _faceTransitionState;
            }
            set
            {
                _faceTransitionState = value;
            }
        }

        float _eyeTrackingAnimatedBaselineScalar; //smoothly animates to 0 when no faces detected, and to 1 when faces detected, used to scale baseline
        public float eyeTrackingAnimatedBaselineScalar //smoothly animates to 0 when no faces detected, and to 1 when faces detected, used to scale baseline
        {
            get
            {
                return _eyeTrackingAnimatedBaselineScalar;
            }
            set
            {
                _eyeTrackingAnimatedBaselineScalar = value;
            }
        }

        void Update()
        {
            switch (faceTransitionState)
            {
                case FaceTransitionState.FaceLocked:
                    HandleFaceLockedState();
                    break;
                case FaceTransitionState.NoFace:
                    NoFaceState();
                    break;
                case FaceTransitionState.ReducingBaseline:
                    HandleReducingBaselineState();
                    break;
                case FaceTransitionState.SlidingCameras:
                    HandleSlidingCamerasState();
                    break;
                case FaceTransitionState.IncreasingBaseline:
                    HandleIncreasingBaselineState();
                    break;
                default:
                    Debug.LogError("Unhandled FaceTransitionState: " + faceTransitionState);
                    break;
            }
        }

        void HandleFaceLockedState()
        {
            //Face locked, shift cameras towards the viewer
            eyeTrackingAnimatedBaselineScalar = 1;
        }
        void NoFaceState()
        {
            if (RenderTrackingDevice.Instance.NumFaces > 0)
            {
                faceTransitionState = FaceTransitionState.SlidingCameras;
            }
        }
        void HandleReducingBaselineState()
        {
            //No face detected or primary face has changed, reduce baseline to zero
            eyeTrackingAnimatedBaselineScalar += (0 - eyeTrackingAnimatedBaselineScalar) * Mathf.Min((Time.deltaTime * 5f), 1f);
            if (eyeTrackingAnimatedBaselineScalar < .1f)
            {
                faceTransitionState = FaceTransitionState.SlidingCameras;
            }
        }
        void HandleSlidingCamerasState()
        {
            //Baseline is 0, slide cameras 
            eyeTrackingAnimatedBaselineScalar = 0;
        }
        void HandleIncreasingBaselineState()
        {
            //Increase baseline to 1
            eyeTrackingAnimatedBaselineScalar += (1 - eyeTrackingAnimatedBaselineScalar) * Mathf.Min((Time.deltaTime * 5f), 1f);

            if (Mathf.Abs(eyeTrackingAnimatedBaselineScalar - 1) < .1f)
            {
                faceTransitionState = FaceTransitionState.FaceLocked;
            }
        }
    }
}