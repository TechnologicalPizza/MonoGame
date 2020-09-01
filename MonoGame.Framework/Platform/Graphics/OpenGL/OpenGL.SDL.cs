﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;

namespace MonoGame.OpenGL
{
    internal partial class GL
    {
        static partial void LoadPlatformEntryPoints()
        {
            BoundApi = RenderApi.GL;
        }

        private static T? LoadFunction<T>(string function, bool throwIfNotFound = false)
            where T : Delegate
        {
            var ret = SDL.GL.GetProcAddress(function);

            if (ret == IntPtr.Zero)
            {
                if (throwIfNotFound)
                    throw new EntryPointNotFoundException(function);

                return default;
            }

            return Marshal.GetDelegateForFunctionPointer<T>(ret);
        }

        private static IGraphicsContext PlatformCreateContext(IWindowHandle window)
        {
            return new GraphicsContext(window);
        }
    }
}
