// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Author: Kenneth James Pouncey

using System;

namespace MonoGame.Framework.Graphics
{
	// http://msdn.microsoft.com/en-us/library/ff434403.aspx
	public struct RenderTargetBinding
	{
        public Texture RenderTarget { get; }
        public int ArraySlice { get; }

        internal DepthFormat DepthFormat { get; }

        public RenderTargetBinding(RenderTarget2D renderTarget)
		{
            RenderTarget = renderTarget ?? throw new ArgumentNullException(nameof(renderTarget));
            ArraySlice = (int)CubeMapFace.PositiveX;
            DepthFormat = renderTarget.DepthStencilFormat;
		}

        public RenderTargetBinding(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
        {
            RenderTarget = renderTarget ?? throw new ArgumentNullException(nameof(renderTarget));

            if (cubeMapFace < CubeMapFace.PositiveX || cubeMapFace > CubeMapFace.NegativeZ)
                throw new ArgumentOutOfRangeException(nameof(cubeMapFace));

            ArraySlice = (int)cubeMapFace;
            DepthFormat = renderTarget.DepthStencilFormat;
        }

#if DIRECTX

        public RenderTargetBinding(RenderTarget2D renderTarget, int arraySlice)
        {
            if (renderTarget == null)
                throw new ArgumentNullException(nameof(renderTarget));
            if (arraySlice < 0 || arraySlice >= renderTarget.ArraySize)
                throw new ArgumentOutOfRangeException(nameof(arraySlice));
            if (!renderTarget.GraphicsDevice.GraphicsCapabilities.SupportsTextureArrays)
                throw new InvalidOperationException("Texture arrays are not supported on this graphics device.");

            RenderTarget = renderTarget;
            _arraySlice = arraySlice;
            _depthFormat = renderTarget.DepthStencilFormat;
        }

        public RenderTargetBinding(RenderTarget3D renderTarget)
        {
            RenderTarget = renderTarget ?? throw new ArgumentNullException(nameof(renderTarget));
            _arraySlice = 0;
            _depthFormat = renderTarget.DepthStencilFormat;
        }

        public RenderTargetBinding(RenderTarget3D renderTarget, int arraySlice)
        {
            if (renderTarget == null)
                throw new ArgumentNullException(nameof(renderTarget));
            if (arraySlice < 0 || arraySlice >= renderTarget.Depth)
                throw new ArgumentOutOfRangeException(nameof(arraySlice));

            RenderTarget = renderTarget;
            _arraySlice = arraySlice;
            _depthFormat = renderTarget.DepthStencilFormat;
        }

#endif 

        public static implicit operator RenderTargetBinding(RenderTarget2D renderTarget)
        {
            return new RenderTargetBinding(renderTarget);
        }

#if DIRECTX

        public static implicit operator RenderTargetBinding(RenderTarget3D renderTarget)
        {
            return new RenderTargetBinding(renderTarget);
        }

#endif
	}
}