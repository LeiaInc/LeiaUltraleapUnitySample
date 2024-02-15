/*!
* Copyright (C) 2023  Leia, Inc.
*
* This software has been provided under the Leia license agreement.
* You can find the agreement at https://www.leiainc.com/legal/license-agreement
*
* This source code is considered Creator Materials under the definitions of the Leia license agreement.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeiaUnity
{
    [DefaultExecutionOrder(3)]
    public abstract class LeiaFocus : MonoBehaviour
    {
        protected LeiaDisplay leiaDisplay;

        [Tooltip("The range of allowable baseline scaling values for the Leia Camera. Can be used to prevent depth from becoming too large or too small.")]
        [SerializeField] MinMaxPair _baselineRange = new MinMaxPair(1f, 0, "MinBaseline", 10f, 100, "MaxBaseline");
        [Tooltip("The range of allowable convergence distances for the Leia Camera. Can be used to prevent the convergence plane from becoming too far away or too close to the camera.")]
        [SerializeField] MinMaxPair _convergenceRange = new MinMaxPair(1, 1.0e-5f, "MinConvergence", 1000, 1E+07f, "MaxConvergence");

        [Tooltip("After the LeiaDisplay's baseline is determined by the auto depth algorithm, and is clamped between the min and max baseline, it will be scaled by this amount. A value of 1 is the default, recommended value. This setting can be used to implement a settings slider which allows a user of the app to adjust how much depth it has.")]
        [SerializeField, Range(0, 10)]
        private float _depthScale = 1.0f;

        [Tooltip("Minimum percentage the computed convergence must change by before the target value is updated. If the convergence plane is jittery, try increasing this value.")]
        [SerializeField, Range(0, 1)] public float convergenceChangeThreshold = .01f;
        [Tooltip("Minimum percentage the computed baseline must change by before the target value is updated. If the Leia Camera bounds are jittery, try increasing this value.")]
        [SerializeField, Range(0, 1)] public float baselineChangeThreshold = .05f;

        protected float targetConvergence;
        private float targetConvergencePrev;

        private float targetBaseline;
        private float targetBaselinePrev;

        [SerializeField] private bool _setConvergence = true;
        [SerializeField] private bool _setDepthFactor = true;

        public bool SetConvergence
        {
            get
            {
                return _setConvergence;
            }
            set
            {
                _setConvergence = value;
            }
        }

        public bool SetDepthFactor
        {
            get
            {
                return _setDepthFactor;
            }
            set
            {
                _setDepthFactor = value;
            }
        }

        public float DepthScale
        {
            get
            {
                return _depthScale;
            }
            set
            {
                _depthScale = Mathf.Clamp(value, 0, float.MaxValue);
            }
        }

        public float MinBaseline
        {
            get
            {
                return _baselineRange.min;
            }
            set
            {
                _baselineRange.min = Mathf.Clamp(value, 0, float.MaxValue);
            }
        }

        public float MaxBaseline
        {
            get
            {
                return _baselineRange.max;
            }
            set
            {
                _baselineRange.max = Mathf.Clamp(value, 0, float.MaxValue);
            }
        }

        public float MinConvergence
        {
            get
            {
                return _convergenceRange.min;
            }
            set
            {
                _convergenceRange.min = Mathf.Clamp(value, 0, float.MaxValue);
            }
        }

        public float MaxConvergence
        {
            get
            {
                return _convergenceRange.max;
            }
            set
            {
                _convergenceRange.max = Mathf.Clamp(value, 0, float.MaxValue);
            }
        }

        const float idealFramerate = 60f;

        [Tooltip("Speed at which the convergence distance changes from its current value"), Range(.01f, 1f), SerializeField]
        private float _convergenceFocusSpeed = 0.5f;

        public float ConvergenceFocusSpeed
        {
            get
            {
                return _convergenceFocusSpeed;
            }
            set
            {
                _convergenceFocusSpeed = value;
            }
        }

        [Tooltip("Speed at which the baseline scaling changes from its current value"), Range(.01f, 1f), SerializeField]
        private float _baselineFocusSpeed = 0.1f;

        public float BaselineFocusSpeed
        {
            get
            {
                return _baselineFocusSpeed;
            }
            set
            {
                _baselineFocusSpeed = value;
            }
        }

        [Tooltip("Offset the LeiaDisplay convergence distance from its computed optimal value by this amount. This can be useful to bring objects more to the foreground to make them pop out more, or push them more into the background so that (for example) UI can be drawn over them."), SerializeField] private float _focusOffset = 0;
        public float FocusOffset
        {
            get
            {
                return _focusOffset;
            }
            set
            {
                _focusOffset = value;
            }
        }

        public

        RunningFloatAverage targetConvergenceHistory;
        const int targetConvergenceHistoryLength = 5;
        RunningFloatAverage targetBaselineHistory;
        const int targetBaselineHistoryLength = 15;

        protected virtual void OnEnable()
        {
            leiaDisplay = GetComponent<LeiaDisplay>();
            targetConvergenceHistory = new RunningFloatAverage(targetConvergenceHistoryLength);
            targetBaselineHistory = new RunningFloatAverage(targetBaselineHistoryLength);
            targetConvergenceHistory.AddSample(leiaDisplay.FocalDistance);
            targetBaselineHistory.AddSample(leiaDisplay.DepthFactor);
        }

        protected void SetTargetConvergence(float newTargetConvergence)
        {
            targetConvergenceHistory.AddSample(newTargetConvergence);
        }

        protected void SetTargetBaseline(float newTargetBaseline)
        {
            targetBaselineHistory.AddSample(newTargetBaseline);
        }

        void UpdateConvergenceDistance()
        {
            float target = targetConvergenceHistory.Average;

            //if new target changed by more than convergenceChangeThreshold %, then update target
            if (Mathf.Abs(target - targetConvergence) > convergenceChangeThreshold * targetConvergence
                    && Mathf.Abs(target - targetConvergencePrev) > convergenceChangeThreshold * targetConvergence)
            {
                targetConvergencePrev = targetConvergence;
                targetConvergence = target;
            }

            float newConvergence = CalculateNewConvergence();

            //Clamp convergence between min and max values
            newConvergence = Mathf.Clamp(newConvergence, this._convergenceRange.min, this._convergenceRange.max);

            //Prevent focus offset from causing the convergence plane to go behind the camera
            if (FocusOffset + targetConvergence < 1.0e-5f)
            {
                FocusOffset = -targetConvergence + 1.0e-5f;
                newConvergence = CalculateNewConvergence();
            }
            leiaDisplay.FocalDistance = newConvergence;
        }

        private float CalculateNewConvergence()
        {
            float frameSpeed = Mathf.Min(
                ConvergenceFocusSpeed * Time.deltaTime * idealFramerate,
                1f
            );

            float newConvergence = leiaDisplay.FocalDistance +
                ((targetConvergence + FocusOffset) - leiaDisplay.FocalDistance)
                * frameSpeed;

            //Prevent convergence from being set to closer than the camera's near clip plane
            newConvergence = Mathf.Max(leiaDisplay.HeadCamera.nearClipPlane, newConvergence);

            return newConvergence;
        }

        void UpdateDepthFactor()
        {
            float target = targetBaselineHistory.Average;

            //if new target changed by more than baselineChangeThreshold %, then update target
            if (Mathf.Abs(target - targetBaseline) > baselineChangeThreshold * targetBaseline
                && Mathf.Abs(target - targetBaselinePrev) > baselineChangeThreshold * targetBaseline)
            {

                targetBaselinePrev = targetBaseline;

                targetBaseline = Mathf.Clamp(
                    target,
                    _baselineRange.min,
                    _baselineRange.max
                    );
            }

            float frameSpeed = Mathf.Min(
                BaselineFocusSpeed * Time.deltaTime * idealFramerate,
                1f
            );

            leiaDisplay.DepthFactor +=
                (targetBaseline * DepthScale - leiaDisplay.DepthFactor)
                * frameSpeed;

            leiaDisplay.DepthFactor = Mathf.Clamp(
                leiaDisplay.DepthFactor,
                MinBaseline,
                MaxBaseline
            );
        }

        protected virtual void LateUpdate()
        {
            if (this.SetDepthFactor)
            {
                UpdateDepthFactor();
            }
            if (this.SetConvergence)
            {
                UpdateConvergenceDistance();
            }
        }

        public void AddOffset(float offset)
        {
            targetConvergenceHistory.AddOffset(offset);
        }
    }
}