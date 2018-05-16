﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Utilities
{
    internal static partial class ReflectionHelpers
    {
        /// <summary>
        /// Generics handler for Marshal.SizeOf
        /// </summary>
        internal static class SizeOf<T>
        {
            private static readonly int _sizeOf;

            static SizeOf()
            {
                var type = typeof(T);
                _sizeOf = Marshal.SizeOf(type);
            }

            public static int Get()
            {
                return _sizeOf;
            }
        }

        /// <summary>
        /// Fallback handler for Marshal.SizeOf(type)
        /// </summary>
        internal static int ManagedSizeOf(Type type)
        {
            return Marshal.SizeOf(type);
        }
    }
}
