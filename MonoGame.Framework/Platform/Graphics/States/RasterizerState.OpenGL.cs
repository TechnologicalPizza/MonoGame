// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using MonoGame.OpenGL;

namespace MonoGame.Framework.Graphics
{
    public partial class RasterizerState
    {
        internal void PlatformApplyState(GraphicsDevice device, bool force = false)
        {
            // When rendering offscreen the faces change order.
            var offscreen = device.IsRenderTargetBound;

            if (force)
            {
                // Turn off dithering to make sure data returned by Texture.GetData is accurate
                GL.Disable(EnableCap.Dither);
            }

            if (CullMode == CullMode.None)
            {
                GL.Disable(EnableCap.CullFace);
                GL.CheckError();
            }
            else
            {
                GL.Enable(EnableCap.CullFace);
                GL.CheckError();
                GL.CullFace(CullFaceMode.Back);
                GL.CheckError();

                if (CullMode == CullMode.CullClockwiseFace)
                {
                    if (offscreen)
                        GL.FrontFace(FrontFaceDirection.Cw);
                    else
                        GL.FrontFace(FrontFaceDirection.Ccw);
                    GL.CheckError();
                }
                else
                {
                    if (offscreen)
                        GL.FrontFace(FrontFaceDirection.Ccw);
                    else
                        GL.FrontFace(FrontFaceDirection.Cw);
                    GL.CheckError();
                }
            }

#if WINDOWS || DESKTOPGL
            if (FillMode == FillMode.Solid)
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            else
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
#else
            if (FillMode != FillMode.Solid)
                throw new NotImplementedException();
#endif

            if (force || ScissorTestEnable != device._lastRasterizerState.ScissorTestEnable)
            {
                if (ScissorTestEnable)
                    GL.Enable(EnableCap.ScissorTest);
                else
                    GL.Disable(EnableCap.ScissorTest);
                GL.CheckError();
                device._lastRasterizerState.ScissorTestEnable = ScissorTestEnable;
            }

            if (force ||
                DepthBias != device._lastRasterizerState.DepthBias ||
                SlopeScaleDepthBias != device._lastRasterizerState.SlopeScaleDepthBias)
            {
                if (DepthBias != 0 || SlopeScaleDepthBias != 0)
                {
                    // from the docs it seems this works the same as for Direct3D
                    // https://www.khronos.org/opengles/sdk/docs/man/xhtml/glPolygonOffset.xml
                    // explanation for Direct3D is  in https://github.com/MonoGame/MonoGame/issues/4826
                    int depthMul;
                    switch (device.ActiveDepthFormat)
                    {
                        case DepthFormat.None:
                            depthMul = 0;
                            break;
                        case DepthFormat.Depth16:
                            depthMul = 1 << 16 - 1;
                            break;
                        case DepthFormat.Depth24:
                        case DepthFormat.Depth24Stencil8:
                            depthMul = 1 << 24 - 1;
                            break;
                        default:
                            throw new ArgumentException(
                                "The active depth format is not supported.", nameof(device));
                    }

                    GL.Enable(EnableCap.PolygonOffsetFill);
                    GL.PolygonOffset(SlopeScaleDepthBias, DepthBias * depthMul);
                }
                else
                    GL.Disable(EnableCap.PolygonOffsetFill);
                GL.CheckError();
                device._lastRasterizerState.DepthBias = DepthBias;
                device._lastRasterizerState.SlopeScaleDepthBias = SlopeScaleDepthBias;
            }

            if (device.Capabilities.SupportsDepthClamp &&
                (force || DepthClipEnable != device._lastRasterizerState.DepthClipEnable))
            {
                if (!DepthClipEnable)
                    GL.Enable((EnableCap)0x864F); // should be EnableCap.DepthClamp, but not available in OpenTK.Graphics.ES20.EnableCap
                else
                    GL.Disable((EnableCap)0x864F);
                GL.CheckError();
                device._lastRasterizerState.DepthClipEnable = DepthClipEnable;
            }

            // TODO: Implement MultiSampleAntiAlias
        }
    }
}
