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

namespace SRUnity
{
    public abstract class ISimulatedRealityModule 
    {
        public virtual void PreInitModule() { }
        public abstract void InitModule();

        public abstract void DestroyModule();

        public abstract void UpdateModule();
    }

    public abstract class SimulatedRealityModule<ModuleType> : ISimulatedRealityModule where ModuleType : class, new()
    {
        private static ModuleType _instance;
        public static ModuleType Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ModuleType();
                }
                return _instance;
            }
        }
    }
}
