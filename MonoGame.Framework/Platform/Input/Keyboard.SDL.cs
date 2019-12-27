﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MonoGame.Framework.Input
{
    public static partial class Keyboard
    {
        private static List<Keys> _keys;

        /// <summary>
        /// Gets all the currently pressed keys.
        /// </summary>
        public static ReadOnlyCollection<Keys> KeysDown { get; private set; }

        /// <summary>
        /// Gets the currently active key modifiers.
        /// </summary>
        public static KeyModifier Modifiers { get; internal set; }

        private static KeyboardState PlatformGetState()
        {
            return new KeyboardState(
                _keys,
                (Modifiers & KeyModifier.CapsLock) == KeyModifier.CapsLock,
                (Modifiers & KeyModifier.NumLock) == KeyModifier.NumLock);
        }

        internal static void SetKeysDownList(List<Keys> keys)
        {
            _keys = keys;
            KeysDown = _keys.AsReadOnly();
        }
    }
}
