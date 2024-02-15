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
using UnityEngine.Rendering;

namespace LeiaUnity
{
    public class LeiaView
    {
        public static readonly CameraEvent[] LeiaMediaEventTimes = new[] { CameraEvent.BeforeGBuffer, CameraEvent.BeforeForwardOpaque, CameraEvent.AfterEverything };
        private readonly CommandBuffer[] leiaMediaCommandBuffers = new CommandBuffer[3];

        public static string ENABLED_NAME { get { return "LeiaView"; } }
        public static string DISABLED_NAME { get { return "Disabled_LeiaView"; } }
        private int _viewIndexX = -1;
        private int _viewIndexY = -1;
        private int _viewIndex = -1;
        private readonly System.Collections.Generic.Dictionary<System.Type, Behaviour> _trackedBehaviours = new System.Collections.Generic.Dictionary<System.Type, Behaviour>();
        public int ViewIndex
        {
            get
            {
                return (IsCameraNull || !Enabled) ? -1 : _viewIndex;
            }
            set
            {
                _viewIndex = value;
            }
        }
        public int ViewIndexX
        {
            get
            {
                return (IsCameraNull || !Enabled) ? -1 : _viewIndexX;
            }
            set
            {
                _viewIndexX = value;
            }
        }
        public int ViewIndexY
        {
            get
            {
                return (IsCameraNull || !Enabled) ? -1 : _viewIndexY;
            }
            set
            {
                _viewIndexY = value;
            }
        }
        private Camera _camera;
        public Camera Camera
        {
            get
            {
                return _camera;
            }
        }
        public bool IsCameraNull
        {
            get { return _camera ? false : true; }
        }
        public GameObject gameObject
        {
            get
            {
                return this.Object;
            }
        }
        public GameObject Object
        {
            get { return _camera ? _camera.gameObject : default(GameObject); }
        }
        public Vector3 Position
        {
            get { return _camera.transform.localPosition; }
            set { _camera.transform.localPosition = value; }
        }
        public Matrix4x4 Matrix
        {
            get { return _camera.projectionMatrix; }
            set { _camera.projectionMatrix = value; }
        }
        public float FarClipPlane
        {
            get { return _camera.farClipPlane; }
            set { _camera.farClipPlane = value; }
        }
        public float NearClipPlane
        {
            get { return _camera.nearClipPlane; }
            set { _camera.nearClipPlane = value; }
        }
        public Rect ViewRect
        {
            get { return _camera.rect; }
            set { _camera.rect = value; }
        }
        public RenderTexture TargetTexture
        {
            get { return !_camera ? null : _camera.targetTexture; }
            set { if (_camera) { _camera.targetTexture = value; } }
        }
        public bool Enabled
        {
            get { return !_camera ? false : _camera.enabled; }
            set { if (_camera) { _camera.enabled = value; } }
        }

        public void SetTextureParams(int width, int height, string viewName)
        {
            if (IsCameraNull)
            {
                return;
            }
            if (_camera.targetTexture == null)
            {
                TargetTexture = CreateRenderTexture(width, height, viewName);
            }
            else
            {
                if (TargetTexture.width != width ||
                    TargetTexture.height != height)
                {
                    Release();
                    TargetTexture = CreateRenderTexture(width, height, viewName);
                }
            }
        }

        private static RenderTexture CreateRenderTexture(int width, int height, string rtName)
        {
            var leiaViewSubTexture = new RenderTexture(width, height, 24)
            {
                name = rtName,
            };

            leiaViewSubTexture.Create();
            return leiaViewSubTexture;
        }

        public Behaviour AttachBehaviourToView(Behaviour original)
        {
            if (original == null) { return null; }
            System.Type type = original.GetType();

            Behaviour copy = null;

            if (_trackedBehaviours.ContainsKey(type))
            {
                copy = _trackedBehaviours[type];
            }
            if (copy == null)
            {

                copy = (Behaviour)this.gameObject.GetComponent(original.GetType());
            }
            if (copy == null)
            {

                copy = (Behaviour)gameObject.AddComponent(type);
            }
            if (copy != null)
            {
                _trackedBehaviours[type] = copy;
            }
            if (copy != null && original != null)
            {

                copy.CopyFieldsFrom(original, original.GetComponent<Camera>(), this);

                copy.enabled = false;
                copy.enabled = original.enabled;
            }

            return copy;
        }

        public LeiaView(GameObject root, UnityCameraParams cameraParams)
        {
            var rootCamera = root.GetComponent<Camera>();

            for (int i = 0; i < rootCamera.transform.childCount; i++)
            {
                var child = rootCamera.transform.GetChild(i);

                if (child.name == DISABLED_NAME)
                {
                    child.name = ENABLED_NAME;
                    child.hideFlags = HideFlags.None;
                    _camera = child.GetComponent<Camera>();
                    _camera.enabled = true;
                    _camera.allowHDR = cameraParams.AllowHDR;
                    break;
                }
            }

            if (_camera == null)
            {
                _camera = new GameObject(ENABLED_NAME).AddComponent<Camera>();
            }
            _camera.transform.parent = root.transform;
            _camera.transform.localPosition = Vector3.zero;
            _camera.transform.localRotation = Quaternion.identity;
            _camera.clearFlags = cameraParams.ClearFlags;
            _camera.cullingMask = cameraParams.CullingMask;
            _camera.depth = cameraParams.Depth;
            _camera.backgroundColor = cameraParams.BackgroundColor;
            _camera.fieldOfView = cameraParams.FieldOfView;
            _camera.depthTextureMode = DepthTextureMode.None;
            _camera.hideFlags = HideFlags.None;
            _camera.orthographic = cameraParams.Orthographic;
            _camera.orthographicSize = cameraParams.OrthographicSize;
            _camera.renderingPath = cameraParams.RenderingPath;
            ViewRect = rootCamera.rect;
            _camera.allowHDR = cameraParams.AllowHDR;

        }

        public void Release()
        {
            if (TargetTexture != null)
            {
                if (Application.isPlaying)
                {
                    TargetTexture.Release();
                    GameObject.Destroy(TargetTexture);
                }
                else
                {
                    TargetTexture.Release();
                    GameObject.DestroyImmediate(TargetTexture);
                }
                TargetTexture = null;
            }
            for (int i = 0; i < leiaMediaCommandBuffers.Length; i++)
            {
                if (leiaMediaCommandBuffers[i] != null)
                {
                    if (_camera != null)
                    {
                        _camera.RemoveCommandBuffer(LeiaMediaEventTimes[i], leiaMediaCommandBuffers[i]);
                    }
                    leiaMediaCommandBuffers[i].Dispose();
                    leiaMediaCommandBuffers[i] = null;
                }
            }
        }
    }
}