// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using MonoGame.Utilities;

namespace Microsoft.Xna.Framework
{
    public class GameServiceContainer : IServiceProvider
    {
        Dictionary<Type, object> services;

        public GameServiceContainer()
        {
            services = new Dictionary<Type, object>();
        }

        public void AddService(Type type, object provider)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (!ReflectionHelpers.IsAssignableFrom(type, provider))
                throw new ArgumentException("The provider does not match the specified service type!");

            services.Add(type, provider);
        }

        public object GetService(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (services.TryGetValue(type, out object service))
                return service;

            return null;
        }

        public void RemoveService(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            services.Remove(type);
        }

        public void AddService<T>(T provider)
        {
            AddService(typeof(T), provider);
        }

        public T GetService<T>() where T : class
        {
            return GetService(typeof(T)) as T;
        }
    }
}