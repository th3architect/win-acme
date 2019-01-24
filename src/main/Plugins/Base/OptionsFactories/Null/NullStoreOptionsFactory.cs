﻿using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullStoreFactory : IStorePluginOptionsFactory, INull
    {
        Type IHasType.InstanceType => typeof(object);
        Type IHasType.OptionsType => typeof(object);
        StorePluginOptions IStorePluginOptionsFactory.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel) => null;
        StorePluginOptions IStorePluginOptionsFactory.Default(IOptionsService optionsService) => null;
        string IHasName.Name => "None";
        string IHasName.Description => null;
        bool IHasName.Match(string name) => false;
    }
}
