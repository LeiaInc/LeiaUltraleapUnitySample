/*!
 * Copyright (C) 2022  Dimenco
 *
 * This software has been provided under the Dimenco EULA. (End User License Agreement)
 * You can find the agreement at https://www.dimenco.eu/eula
 *
 * This source code is considered Protected Code under the definitions of the EULA.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
#endif

namespace SRUnity
{
    [AddComponentMenu("")] // Hides handler from component list
    [ExecuteInEditMode]
    // Class that handles the lifecycle of all SR modules
    public class SystemHandler : MonoBehaviour
    {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void OnApplicationLoad()
        {
            // If 2D scene do not load
           if (FindObjectOfType<LeiaUnity.LeiaDisplay>() != null)
           {
                LoadHandler();
           }
            SRUnity.SRUtility.Trace("SystemHandler::OnApplicationLoad");
            SceneManager.activeSceneChanged += OnSceneChangedEvent;
        }
#endif
        public static SystemHandler Instance;

        public static void LoadHandler(bool forceReload = false)
        {
#if !UNITY_EDITOR && PLATFORM_STANDALONE_WIN
            SRUnity.SRUtility.Trace("SRUnity " + PluginVersion.GetFullVersion());
#endif
            SRUnity.SRUtility.Trace("SystemHandler::LoadHandler");
            if (Instance == null || forceReload)
            {
                string parentObjectName = "SRHandler";
               
                GameObject parentObject = GameObject.Find(parentObjectName);
                if (forceReload && parentObject != null)
                {
                    SRUnity.SRUtility.Debug("Force unloading " + parentObjectName);
                    UnityEngine.Object.DestroyImmediate(parentObject);
                    parentObject = null;
                }

                if (parentObject == null) 
                {
                    SRUnity.SRUtility.Debug("Creating new " + parentObjectName);
                    parentObject = new GameObject();
                    parentObject.name = parentObjectName;
                }

                SRUtility.SetSrGameObjectVisibility(parentObject);

                Instance = parentObject.GetComponent<SystemHandler>();
                if (Instance == null)
                {
                    SRUnity.SRUtility.Debug("Creating new SystemHandler");
                    Instance = parentObject.AddComponent<SystemHandler>();
                }
            }
        }

        public delegate void OnSceneChangedDelegate(Scene scene);
        public static event OnSceneChangedDelegate OnSceneChanged;

        public static void TransitionHandlerToScene(Scene scene)
        {
            GameObject[] allObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            bool hasLeiaDisplay = false;
            foreach (GameObject obj in allObjects)
            {
                // Check if it is a 3D scene and enable SRSystemHandler instance
                if (obj.GetComponent<LeiaUnity.LeiaDisplay>() != null)
                {
                    if(Application.platform == RuntimePlatform.WindowsPlayer)
                    {
                        if(!Instance)
                        {
                           LoadHandler();
                        }
                        Instance.gameObject.SetActive(true);
                        hasLeiaDisplay = true;
                        break;
                    }
                }
            }

            // If 3D scene enable SRSystemHandler instance
            if (Instance != null && !hasLeiaDisplay)
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    Instance.gameObject.SetActive(false);
                }
            }
            if (Instance != null)
            {
                SceneManager.MoveGameObjectToScene(Instance.gameObject, scene);
            }

            OnSceneChanged?.Invoke(scene);
        }

        static void OnSceneChangedEvent(Scene current, Scene next)
        {
            SRUnity.SRUtility.Trace("SystemHandler::OnSceneChangedEvent");
            TransitionHandlerToScene(next);
        }

        public void OnEnable()
        {
            SRUnity.SRUtility.Trace("SystemHandler::OnEnable");
            StartModules();
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(OnDomainUnload);
        }

        public void Update()
        {
            foreach (ISimulatedRealityModule mod in SrModules)
            {
                mod.UpdateModule();
            }
        }

        public void OnDisable()
        {
            SRUnity.SRUtility.Trace("SystemHandler::OnDisable");
            DestroyModules();
        }

        // SR modules must be unloaded explicitly to prevent GC freeze due to unmanaged delegates
        static void OnDomainUnload(object sender, EventArgs e)
        {
            SRUnity.SRUtility.Trace("SystemHandler::OnDomainUnload");
            Instance.DestroyModules();
        }

        private List<ISimulatedRealityModule> SrModules = new List<ISimulatedRealityModule>();
        private void StartModules()
        {           
            SRUnity.SRUtility.Trace("SystemHandler::StartModules");

            var moduleType = typeof(ISimulatedRealityModule);
            var moduleAssembly = moduleType.Assembly;
            var moduleSubTypes = moduleAssembly.GetTypes().Where(t => t.IsSubclassOf(moduleType));

            foreach (System.Type type in moduleSubTypes)
            {
                if (!type.IsAbstract)
                {
                    var instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    ISimulatedRealityModule module = (ISimulatedRealityModule)instanceProperty.GetValue(null, null);
                    SrModules.Add(module);
                }
            }

            foreach (ISimulatedRealityModule mod in SrModules)
            {
                mod.PreInitModule();
            }

            foreach (ISimulatedRealityModule mod in SrModules)
            {
                mod.InitModule();
            }

            SRUnity.SRUtility.Debug("Loaded modules: " + String.Join(", ", SrModules));
        }

        private void DestroyModules()
        {
            SRUnity.SRUtility.Trace("SystemHandler::DestroyModules");
            foreach (ISimulatedRealityModule mod in SrModules)
            {
                mod.DestroyModule();
            }
            SrModules.Clear();
        }

        public delegate void OnWindowFocusDelegate(bool hasFocus);
        public static event OnWindowFocusDelegate OnWindowFocus;

        private SrRenderModeHint windowFocusHint = new SrRenderModeHint();
        private void OnApplicationFocus(bool hasFocus)
        {
#if !UNITY_EDITOR
            if (hasFocus)
            {
                windowFocusHint.BecomeIndifferent();
            }
            else
            {
                windowFocusHint.Force2D();
            }
#endif
            OnWindowFocus?.Invoke(hasFocus);
        }
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    // Class that adds several event hooks for the editor
    class SystemHandlerEditorHook
    {    
        private static bool RequestFullReload = false;

        static SystemHandlerEditorHook()
        {
            RequestFullReload = false;
            EditorApplication.update += RunOnce;

            EditorSceneManager.activeSceneChanged += OnSceneChangedEvent;
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChangedEvent;
        }

        [PostProcessBuildAttribute(1)]
        public static void OnPostBuild(BuildTarget target, string pathToBuiltProject)
        {
            SRUnity.SRUtility.Debug("SystemHandlerEditorHook::OnPostBuild");
            RequestFullReload = true;
            EditorApplication.update += RunOnce;
        }

        static void OnSceneChangedEvent(Scene current, Scene next)
        {
            SRUnity.SRUtility.Debug("SystemHandlerEditorHook::OnSceneChangedEvent");
            SystemHandler.TransitionHandlerToScene(next);
        }

        static void RunOnce()
        {
            SRUnity.SRUtility.Debug("SystemHandlerEditorHook::RunOnce");
            EditorApplication.update -= RunOnce;
            SystemHandler.LoadHandler(RequestFullReload);
        }
    }
#endif
}
