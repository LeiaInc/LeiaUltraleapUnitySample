/*
 * Copyright 2024 (c) Leia Inc.  All rights reserved.
 *
 * NOTICE:  All information contained herein is, and remains
 * the property of Leia Inc. and its suppliers, if any.  The
 * intellectual and technical concepts contained herein are
 * proprietary to Leia Inc. and its suppliers and may be covered
 * by U.S. and Foreign Patents, patents in process, and are
 * protected by trade secret or copyright law.  Dissemination of
 * this information or reproduction of this materials strictly
 * forbidden unless prior written permission is obtained from
 * Leia Inc.
 */
#if UNITY_ANDROID
using System;
using System.Runtime.InteropServices;

namespace Leia
{
    public enum EventType
    {
        LOG = 0,
        ERROR,
        COMPONENT
    }
    public enum ComponentId {
        UNDETERMINED = 0,
        CORE,
        FACE_TRACKING,
        INTERLACER,
    }
    public enum CoreEventCode {
        DID_INITIALIZE  = 0, ///< leia_core is fully initialized, payload: NONE
        INVALID_LICENSE = 1, ///< leia_core detected invalid license, payload: [optional] const char* reason - describes what exactly went wrong
    }
    public enum InterlacerEventCode {
        DEBUG_MENU_CLOSED = 0, ///< leia_interlacer's debug menu has been closed, payload: NONE
        DEBUG_MENU_UPDATE = 1, ///< leia_interlacer's debug menu has changed a value, payload: [optional] const char* reason - value's id
    }
    public enum FaceTrackingEventCode {
        /// Face tracking operation has failed and the face tracking backend enountered a fatal error.
        /// An attempt at recovery may be possible by shutting down the face tracking and trying to enable it again.
        /// Payload - const char* errorMessage.
        FATAL_ERROR = 0,
        ERROR       = 1,
        STARTED     = 2,
        STOPPED     = 3,
    }
    public enum ErrorType {
        INVALID_USE          = -1,
        INTERNAL_ERROR       = -2,
        OUT_OF_SYSTEM_MEMORY = -3,
        ASSERTION            = -4,
    };
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct EventLog
    {
        IntPtr _message;
        public string message
        {
            get
            {
                if (_message != null)
                {
                    return Marshal.PtrToStringAnsi(_message);
                }
                return null;
            }
        }
        public int level;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct EventError
    {
        IntPtr _message;
        public string message
        {
            get
            {
                if (_message != null)
                {
                    return Marshal.PtrToStringAnsi(_message);
                }
                return null;
            }
        }
        public System.Int64 code;
        public ComponentId component;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct EventComponent
    {
        public ComponentId component;
        public int code;
        IntPtr payload;
        public string stringPayload
        {
            get
            {
                if (payload != null)
                {
                    return Marshal.PtrToStringAnsi(payload);
                }
                return null;
            }
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct Event
    {
        [FieldOffset(0)]
        public EventType eventType;
        [FieldOffset(8)]
        public EventLog log;
        [FieldOffset(8)]
        public EventError error;
        [FieldOffset(8)]
        public EventComponent component;
    }
    public class EventListener : IDisposable
    {
        private Callback _callback; // prevent GC
        public EventListener(Callback callback)
        {
            _callback = callback;
            _ptr = leia_event_listener_alloc(CoreLibrary.Get());
            leia_event_listener_set_callback(_ptr, _callback, IntPtr.Zero);
            EventCenter.Register(this);
        }
        public void Dispose()
        {
            if (_ptr != IntPtr.Zero)
            {
                EventCenter.Unregister(this);
                leia_event_listener_free(_ptr);
                _ptr = IntPtr.Zero;
            }
        }
        public IntPtr GetHandle()
        {
            return _ptr;
        }
        private IntPtr _ptr;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Callback(IntPtr userData, ref Leia.Event eventPtr);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr leia_event_listener_alloc(IntPtr coreLibPtr);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_event_listener_set_callback(IntPtr listener, [MarshalAs(UnmanagedType.FunctionPtr)] Callback callback, IntPtr userData);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_event_listener_free(IntPtr listener);
    }
    class EventCenter
    {
        public static void Register(EventListener eventListener)
        {
            leia_event_center_add_listener(eventListener.GetHandle());
        }
        public static void Unregister(EventListener eventListener)
        {
            leia_event_center_remove_listener(eventListener.GetHandle());
        }
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_event_center_add_listener(IntPtr listener);
        [DllImport(Constants.SDK_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void leia_event_center_remove_listener(IntPtr listener);
    }
}
#endif