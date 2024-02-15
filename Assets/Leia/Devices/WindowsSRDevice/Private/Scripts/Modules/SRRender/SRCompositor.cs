/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

#if !UNITY_2019_1_OR_NEWER
using UnityEngine.Experimental.Rendering;
#warning "SRUnity: composition is not support before Unity 2019.1"
#endif

namespace SRUnity
{
    // Compositor class that handles the rendering composition and weaving on all rendering pipelines. Default: hooks into OnRenderImage and replaces output image. SRP: Executes a fullscreen blit on endFrameRendering.
    public class SRCompositor
    {
        public delegate void OnCompositorChangedDelegate();
        public static event OnCompositorChangedDelegate OnCompositorChanged;

        private RenderTexture frameBuffer;
        public RenderTargetIdentifier frameBufferId;

        private Camera cameraHook = null;

        public void Init()
        {
            String compositorObjectName = "SRCompositor";
            GameObject cameraObject = GameObject.Find(compositorObjectName);
            if (cameraObject == null)
            {
                cameraObject = new GameObject();
            }
            if(SRUnity.SystemHandler.Instance != null)
            {
                cameraObject.transform.parent = SRUnity.SystemHandler.Instance.transform;
            }
            cameraObject.name = compositorObjectName;
            if (cameraObject.GetComponent<CompositorCameraHook>() == null) cameraObject.AddComponent<CompositorCameraHook>();

            cameraHook = cameraObject.GetComponent<Camera>();
            if (cameraHook == null) cameraHook = cameraObject.AddComponent<Camera>();
            cameraHook.depth = 9999;
            cameraHook.clearFlags = CameraClearFlags.Nothing;
            cameraHook.nearClipPlane = 0.00001f;
            cameraHook.farClipPlane = 0.00002f;

            SRUtility.SetSrGameObjectVisibility(cameraObject);

            foreach (var kayPair in frameBuffers)
            {
                SRFrameBuffer frameBuffer = kayPair.Value;
                frameBuffer.Update();
            }

            OnCompositorChanged?.Invoke();

#if UNITY_2019_1_OR_NEWER
            RenderPipelineManager.endFrameRendering -= renderToContext;
            RenderPipelineManager.endFrameRendering += renderToContext;
#endif
        }

        public void Destroy()
        {
#if UNITY_2019_1_OR_NEWER
            RenderPipelineManager.endFrameRendering -= renderToContext;
#endif

            OnCompositorChanged?.Invoke();
        }

        public void Composite()
        {
            Composite(null);
        }

        void UpdateFrameBuffer()
        {
            Vector2Int resolution = SRUnity.SRCore.Instance.getResolution();
            Vector2Int frameBufferResolution = new Vector2Int((int) (resolution.x * SRProjectSettings.Instance.renderResolution), (int) (resolution.y * SRProjectSettings.Instance.renderResolution));

            if (frameBuffer != null)
            {
                if (frameBuffer.width != frameBufferResolution.x || frameBuffer.height != frameBufferResolution.y)
                {
                    frameBuffer = null;
                }
            }

            if (frameBuffer == null)
            {
                RenderTextureDescriptor frameBufferDesc = new RenderTextureDescriptor(frameBufferResolution.x, frameBufferResolution.y, SRProjectSettings.Instance.frameBufferFormat);
                frameBufferDesc.depthBufferBits = 24;
                frameBufferDesc.mipCount = 0;
                frameBuffer = new RenderTexture(frameBufferDesc);
                frameBuffer.name = "CompositiorFramebuffer";

                frameBufferId = new RenderTargetIdentifier(frameBuffer);
            }
        }

        public void Composite(ScriptableRenderContext? context)
        {
            UpdateFrameBuffer();

            RenderTexture target = frameBuffer;

            float renderScale = SRProjectSettings.Instance.renderResolution;

            Vector2Int viewSize = new Vector2Int((int)(target.width / 2.0f), (int)(target.height));

            CommandBuffer cb = null;
            RenderTargetIdentifier targetId = 0;
            if (context.HasValue)
            {
                cb = new CommandBuffer();
                cb.name = "SRComposite";

                targetId = frameBufferId;
                cb.SetRenderTarget(targetId);
                cb.ClearRenderTarget(true, true, Color.clear);
            }
            else
            {
                RenderTexture.active = target;
                GL.Clear(true, true, Color.clear);
            }

            foreach (var kayPair in frameBuffers)
            {
                SRFrameBuffer frameBuffer = kayPair.Value;

                if (frameBuffer.Enabled)
                {
                    if (frameBuffer.frameBuffer == null ||
                        frameBuffer.screenRect.width * frameBuffer.screenRect.height == 0) continue;

                    Vector2Int viewMin = new Vector2Int((int) (frameBuffer.screenRect.x * viewSize.x),
                        (int) (frameBuffer.screenRect.y * viewSize.y));
                    Vector2Int viewMax = viewMin + new Vector2Int((int) (frameBuffer.screenRect.width * viewSize.x),
                        (int) (frameBuffer.screenRect.height * viewSize.y));

                    viewMax.x = Math.Min(viewMax.x, viewSize.x);
                    viewMax.y = Math.Min(viewMax.y, viewSize.y);

                    Vector2Int viewOffset = new Vector2Int(Math.Max(0, -viewMin.x), Math.Max(0, -viewMin.y));
                    viewMin += viewOffset;

                    // Skip view if not visible on screen
                    if (viewMax.x - viewMin.x < 2 || viewMax.y - viewMin.y < 2)
                    {
                        continue;
                    }

                    if (frameBuffer.viewIndex == 1)
                    {
                        viewMin.x += viewSize.x;
                        viewMax.x += viewSize.x;
                    }

                    if (context.HasValue)
                    {
                        cb.CopyTexture(frameBuffer.frameBufferId, 0, 0, viewOffset.x, viewOffset.y, viewMax.x - viewMin.x, viewMax.y - viewMin.y, targetId, 0, 0, viewMin.x, viewMin.y);
                    }
                    else
                    {
                        Graphics.CopyTexture(frameBuffer.frameBuffer, 0, 0, viewOffset.x, viewOffset.y, viewMax.x - viewMin.x, viewMax.y - viewMin.y, target, 0, 0, viewMin.x, viewMin.y);
                    }
                }
            }

            if (context.HasValue)
            {
                context.Value.ExecuteCommandBuffer(cb);
            }
        }

        public class SRFrameBuffer
        {
            public void Update()
            {
                if (Enabled)
                {
                    Vector2Int frameSize = GetViewSize();

                    if (frameBuffer != null)
                    {
                        if (frameBuffer.width != frameSize.x || frameBuffer.height != frameSize.y)
                        {
                            frameBuffer = null;
                        }
                    }

                    if (frameBuffer == null)
                    {

                        if (frameSize.x > 0 && frameSize.y > 0)
                        {
                            RenderTextureDescriptor frameBufferDesc = new RenderTextureDescriptor(frameSize.x, frameSize.y, SRProjectSettings.Instance.frameBufferFormat);
                            frameBufferDesc.depthBufferBits = 24;
                            frameBufferDesc.mipCount = 0;
                            frameBuffer = new RenderTexture(frameBufferDesc);
                        }

                        frameBufferId = new RenderTargetIdentifier(frameBuffer);
                    }
                }
            }

            public Vector2Int GetViewSize()
            {
                Vector2Int screenSize = SRUnity.SRCore.Instance.getResolution();
                float renderScale = SRProjectSettings.Instance.renderResolution;
                return new Vector2Int((int)(screenSize.x * screenRect.width / 2.0f * renderScale), (int)(screenSize.y * screenRect.height * renderScale));
            }

            public RenderTexture frameBuffer = null;
            public int viewIndex = 0;
            public Rect screenRect;
            public bool Enabled = false;

            public RenderTargetIdentifier frameBufferId;
        }

        private Dictionary<string, SRFrameBuffer> frameBuffers = new Dictionary<string, SRFrameBuffer>();

        public SRFrameBuffer GetFrameBuffer(string uniqueId)
        {
            if (!frameBuffers.ContainsKey(uniqueId))
            {
                frameBuffers.Add(uniqueId, new SRFrameBuffer());
            }

            return frameBuffers[uniqueId];
        }

        void renderToContext(ScriptableRenderContext context, Camera[] cameras)
        {
            bool isCompositorCamera = false;
            foreach (Camera camera in cameras)
            {
                if (camera == cameraHook)
                {
                    isCompositorCamera = true;
                    break;
                }
            }

            if (!isCompositorCamera) return;

#if UNITY_EDITOR
            if (Camera.current != null && Camera.current.cameraType != CameraType.Game) return;
#endif

            Composite(context);
            SRUnity.SRRender.Instance.GetWeaver().WeaveToContext(context, frameBuffer);
            context.Submit();
        }

        public void renderToTarget(RenderTexture target)
        {
            Composite();
            SRUnity.SRRender.Instance.GetWeaver().WeaveToTarget(target, frameBuffer);

            // Set target back as Active to prevent warning
            RenderTexture.active = target;
        }
    }

    [ExecuteInEditMode]
    class CompositorCameraHook : MonoBehaviour
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private void OnRenderImage(RenderTexture Source, RenderTexture Target)
        {
            SRUnity.SRRender.Instance.GetCompositor().renderToTarget(Target);
        }
#endif
    }
}
