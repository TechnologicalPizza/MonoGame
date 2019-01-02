// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Microsoft.Xna.Framework.Input;

namespace Microsoft.Xna.Framework
{
    /// <summary>
    /// This class is used for the <see cref="GameWindow.TextInput"/> event as <see cref="EventArgs"/>.
    /// </summary>
    public class TextInputEventArgs : EventArgs
    {
        public char Character { get; }
        public Keys Key { get; }

        public TextInputEventArgs(char character, Keys key = Keys.None)
        {
            this.Character = character;
            this.Key = key;
        }
    }
}
