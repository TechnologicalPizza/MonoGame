// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MonoGame.OpenGL;

namespace MonoGame.Framework.Graphics
{
    public abstract partial class Texture
    {
        internal int _glTexture = -1;
        internal TextureTarget _glTarget;
        internal TextureUnit glTextureUnit = TextureUnit.Texture0;
        internal PixelInternalFormat glInternalFormat;
        internal PixelFormat glFormat;
        internal PixelType glType;
        internal SamplerState glLastSamplerState;

        private void PlatformGraphicsDeviceResetting()
        {
            DeleteGLTexture();
            glLastSamplerState = null;
        }

        private void PlatformDispose(bool disposing) 
        { 
            if (!IsDisposed)
            {
                DeleteGLTexture();
                glLastSamplerState = null;
            }
        }

        private void DeleteGLTexture()
        {
            if (_glTexture > 0)
                GraphicsDevice.DisposeTexture(_glTexture);
            _glTexture = 0;
        }
    }
}

