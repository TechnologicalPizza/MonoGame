﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace MonoGame.Framework.Input
{
    public partial class Mouse
    {
        internal MouseState State;

        internal int ScrollX;
        internal int ScrollY;

        private MouseState PlatformGetState()
        {
            var winFlags = SDL.Window.GetWindowFlags(Window.WindowHandle);
            var state = SDL.Mouse.GetGlobalState(out int x, out int y);

            if ((winFlags & SDL.Window.State.MouseFocus) != 0)
            {
                // Window has mouse focus, position will be set from the motion event
                State.LeftButton = (state & SDL.Mouse.Button.Left) != 0 ? ButtonState.Pressed : ButtonState.Released;
                State.MiddleButton = (state & SDL.Mouse.Button.Middle) != 0 ? ButtonState.Pressed : ButtonState.Released;
                State.RightButton = (state & SDL.Mouse.Button.Right) != 0 ? ButtonState.Pressed : ButtonState.Released;
                State.XButton1 = (state & SDL.Mouse.Button.X1Mask) != 0 ? ButtonState.Pressed : ButtonState.Released;
                State.XButton2 = (state & SDL.Mouse.Button.X2Mask) != 0 ? ButtonState.Pressed : ButtonState.Released;

                State.HorizontalScroll = ScrollX;
                State.VerticalScroll = ScrollY;
            }
            else
            {
                // Window does not have mouse focus, we need to manually get the position
                var clientBounds = Window.Bounds;
                State.X = x - clientBounds.X;
                State.Y = y - clientBounds.Y;
            }

            return State;
        }

        private void PlatformSetPosition(int x, int y)
        {
            State.X = x;
            State.Y = y;
            SDL.Mouse.WarpInWindow(Window.WindowHandle, x, y);
        }

        private void PlatformSetCursor(MouseCursor cursor)
        {
            SDL.Mouse.SetCursor(cursor.Handle);
        }
    }
}
