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
    public enum LogLevel
    {
        Default = 0,
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Critical,
        Off,
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Vector2i
    {
        public int x, y;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Vector2d
    {
        public double x, y;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Vector3
    {
        public float x, y, z;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Timestamp
    {
        public double ms;

        public enum Space
        {
            Unknown = -1,
            System = 0,
        };
        public Int32 space;
    }
    public enum FaceDetectorBackend
    {
        Unknown = 0,
        CPU = 1 << 0,
        GPU = 1 << 1,

        Count = 2,
    }
    public enum FaceDetectorInputType
    {
        Unknown = 0,
        CPU = 1 << 0,
        GPU = 1 << 1,

        Count = 2,
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct FaceDetectorConfig
    {
        public FaceDetectorBackend backend;
        public FaceDetectorInputType inputType;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct NormalizedCameraIntrinsics {
        public float fx; /* Horizontal normalized coordinate of the principal point of the image, as an offset from the left edge */
        public float fy; /* Vertical normalized coordinate of the principal point of the image, as an offset from the top edge */
        public float ppx; /* Horizontal focal length of the image plane normalized by the width */
        public float ppy; /* Vertical focal length of the image plane normalized by the height */
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
        public float[] distortionCoeffs; /* Distortion coefficients, OpenCV-style */
    }
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct LegalOrientations
    {
        public Int32 portrait;
        public Int32 landscape;
        public Int32 reversePortrait;
        public Int32 reverseLandscape;
    }
    public enum Orientation
    {
        Unspecified      = -1,
        Landscape        = 0,
        Portrait         = 1,
        ReverseLandscape = 2,
        ReversePortrait  = 3,
        Count            = 4,
    }
}
#endif