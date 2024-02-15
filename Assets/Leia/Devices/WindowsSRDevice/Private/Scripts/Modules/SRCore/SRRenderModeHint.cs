/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

using System.Collections.Generic;
using System;
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
using SimulatedReality;
#endif
namespace SRUnity
{
    // A class that allows various features to indicate their preferred render mode. Based on these preferences, a global stated is determined which also enables/disables the lens hint.
    public class SrRenderModeHint
    {
        public SrRenderModeHint()
        {
            currentPreference = ERenderModePreference.LHS_Indifferent;
            triggers[currentPreference]++;
        }
        ~SrRenderModeHint()
        {
            triggers[currentPreference]--;
        }

        public static void SetLensHintInstance(IntPtr instance)
        {
            lensHint = instance;

            if (lensHint != IntPtr.Zero)
            {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
                SRDisplays.lensDisableHint(lensHint);
#endif
                currentGlobalState = false;
                UpdateGlobalStateAndLensHint();
            }
        }

        // Return what the result of all active FSRRenderModeHint preferences is
        public static bool ShouldRender3D()
        {
            return currentGlobalState;
        }

        // Indicate this FSRRenderModeHint would like the application to render in 3D with the lens on
        public void Prefer3D()
        {
            if (currentPreference != ERenderModePreference.LHS_3D)
            {
                triggers[currentPreference]--;
                currentPreference = ERenderModePreference.LHS_3D;
                triggers[currentPreference]++;

                UpdateGlobalStateAndLensHint();
            }
        }

        // Indicate this FSRRenderModeHint would like the application to render in 2D with the lens off
        public void Prefer2D()
        {
            if (currentPreference != ERenderModePreference.LHS_2D)
            {
                triggers[currentPreference]--;
                currentPreference = ERenderModePreference.LHS_2D;
                triggers[currentPreference]++;

                UpdateGlobalStateAndLensHint();
            }
        }

        // Indicate this FSRRenderModeHint needs the application to be in 2D with the lens off regardless of any other preferences
        public void Force2D()
        {
            if (currentPreference != ERenderModePreference.LHS_Force2D)
            {
                triggers[currentPreference]--;
                currentPreference = ERenderModePreference.LHS_Force2D;
                triggers[currentPreference]++;

                UpdateGlobalStateAndLensHint();
            }
        }

        // Indicate this FSRRenderModeHint does not want to affect the render mode and lens hint
        public void BecomeIndifferent()
        {
            if (currentPreference != ERenderModePreference.LHS_Indifferent)
            {
                triggers[currentPreference]--;
                currentPreference = ERenderModePreference.LHS_Indifferent;
                triggers[currentPreference]++;

                UpdateGlobalStateAndLensHint();
            }
        }


        private static bool currentGlobalState;
        private static IntPtr lensHint = IntPtr.Zero;
        private static void UpdateGlobalStateAndLensHint()
        {
            bool newState = false;

            // Always render in 2D if a force trigger is active
            if (triggers[ERenderModePreference.LHS_Force2D] > 0)
            {
                newState = false;
            }
            else
            {
                // Decide for 3D if any of the following:
                // * A 3D trigger is active
                // * No 2D triggers are active
                newState = triggers[ERenderModePreference.LHS_3D] > 0 || triggers[ERenderModePreference.LHS_2D] == 0;
            }

            SRUtility.Trace("SR RenderMode: " + (newState ? "3D" : "2D") + " [3D triggers: " + triggers[ERenderModePreference.LHS_3D] + ", 2D triggers: " + triggers[ERenderModePreference.LHS_2D] + ", Force 2D triggers: " + triggers[ERenderModePreference.LHS_Force2D] + ", Indifferent triggers: " + triggers[ERenderModePreference.LHS_Indifferent] + "]");

            if (currentGlobalState != newState)
            {
                currentGlobalState = newState;

                if (lensHint != IntPtr.Zero)
                {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
                    if (currentGlobalState)
                    {
                        SRDisplays.lensEnableHint(lensHint);
                    }
                    else
                    {
                        SRDisplays.lensDisableHint(lensHint);
                    }
#endif
                }
                else
                {
                    SRUtility.Debug("SrRenderModeHint.lensHint is invalid");
                }
            }
        }

        private enum ERenderModePreference
        {
            LHS_Indifferent,
            LHS_3D,
            LHS_2D,
            LHS_Force2D
        };

        private static Dictionary<ERenderModePreference, int> triggers = new Dictionary<ERenderModePreference, int>() {
            { ERenderModePreference.LHS_Indifferent, 0 }, 
            { ERenderModePreference.LHS_3D, 0 },
            { ERenderModePreference.LHS_2D, 0 },
            { ERenderModePreference.LHS_Force2D, 0 }
        };

        ERenderModePreference currentPreference = ERenderModePreference.LHS_Indifferent;
    }
}
