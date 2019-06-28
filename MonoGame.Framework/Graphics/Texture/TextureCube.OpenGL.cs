// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;
using MonoGame.OpenGL;
using GLPixelFormat = MonoGame.OpenGL.PixelFormat;
using MonoGame.Utilities;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class TextureCube
    {
        private void PlatformConstruct(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format, bool renderTarget)
        {
            _glTarget = TextureTarget.TextureCubeMap;

            void Construct()
            {
                GL.GenTextures(1, out _glTexture);
                GraphicsExtensions.CheckGLError();
                GL.BindTexture(TextureTarget.TextureCubeMap, _glTexture);
                GraphicsExtensions.CheckGLError();

                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
                                mipMap ? (int)TextureMinFilter.LinearMipmapLinear : (int)TextureMinFilter.Linear);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
                                (int)TextureMagFilter.Linear);
                GraphicsExtensions.CheckGLError();

                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
                                (int)TextureWrapMode.ClampToEdge);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
                                (int)TextureWrapMode.ClampToEdge);
                GraphicsExtensions.CheckGLError();


                format.GetGLFormat(GraphicsDevice, out glInternalFormat, out glFormat, out glType);

                for (var i = 0; i < 6; i++)
                {
                    var target = GetGLCubeFace((CubeMapFace)i);
                    if (glFormat == GLPixelFormat.CompressedTextureFormats)
                    {
                        var imageSize = 0;
                        switch (format)
                        {
                            case SurfaceFormat.RgbPvrtc2Bpp:
                            case SurfaceFormat.RgbaPvrtc2Bpp:
                                imageSize = (Math.Max(size, 16) * Math.Max(size, 8) * 2 + 7) / 8;
                                break;

                            case SurfaceFormat.RgbPvrtc4Bpp:
                            case SurfaceFormat.RgbaPvrtc4Bpp:
                                imageSize = (Math.Max(size, 8) * Math.Max(size, 8) * 4 + 7) / 8;
                                break;

                            case SurfaceFormat.Dxt1:
                            case SurfaceFormat.Dxt1a:
                            case SurfaceFormat.Dxt1SRgb:
                            case SurfaceFormat.Dxt3:
                            case SurfaceFormat.Dxt3SRgb:
                            case SurfaceFormat.Dxt5:
                            case SurfaceFormat.Dxt5SRgb:
                            case SurfaceFormat.RgbEtc1:
                            case SurfaceFormat.RgbaAtcExplicitAlpha:
                            case SurfaceFormat.RgbaAtcInterpolatedAlpha:
                                imageSize = (size + 3) / 4 * ((size + 3) / 4) * format.GetSize();
                                break;

                            default:
                                throw new NotSupportedException();
                        }
                        GL.CompressedTexImage2D(target, 0, glInternalFormat, size, size, 0, imageSize, IntPtr.Zero);
                        GraphicsExtensions.CheckGLError();
                    }
                    else
                    {
                        GL.TexImage2D(target, 0, glInternalFormat, size, size, 0, glFormat, glType, IntPtr.Zero);
                        GraphicsExtensions.CheckGLError();
                    }
                }

                if (mipMap)
                {
#if IOS || ANDROID
                    GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
#else
                    GraphicsDevice.FramebufferHelper.Get().GenerateMipmap((int)_glTarget);
                    // This updates the mipmaps after a change in the base texture
                    GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.GenerateMipmap, (int)Bool.True);
#endif
                    GraphicsExtensions.CheckGLError();
                }
            }

            if (Threading.IsOnUIThread())
                Construct();
            else
                Threading.BlockOnUIThread(Construct);
        }

        private void PlatformGetData<T>(
            CubeMapFace cubeMapFace, int level, Rectangle rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

#if OPENGL && DESKTOPGL
            void Get()
            {
                int tSizeInByte = ReflectionHelpers.SizeOf<T>.Get();
                GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                IntPtr dataPointer = dataHandle.AddrOfPinnedObject();
                int dstSize = data.Length * tSizeInByte;

                try
                {
                    TextureTarget target = GetGLCubeFace(cubeMapFace);
                    GL.BindTexture(TextureTarget.TextureCubeMap, _glTexture);

                    if (glFormat == GLPixelFormat.CompressedTextureFormats)
                    {
                        // Note: for compressed format Format.GetSize() returns the size of a 4x4 block
                        var pixelToT = Format.GetSize() / tSizeInByte;
                        var tFullWidth = Math.Max(Size >> level, 1) / 4 * pixelToT;
                        IntPtr temp = Marshal.AllocHGlobal(Math.Max(Size >> level, 1) / 4 * tFullWidth * tSizeInByte);
                        GL.GetCompressedTexImage(target, level, temp);
                        GraphicsExtensions.CheckGLError();

                        var rowCount = rect.Height / 4;
                        var tRectWidth = rect.Width / 4 * Format.GetSize() / tSizeInByte;
                        for (var r = 0; r < rowCount; r++)
                        {
                            var tempStart = rect.X / 4 * pixelToT + (rect.Top / 4 + r) * tFullWidth;
                            var dataStart = startIndex + r * tRectWidth;

                            CopyMemory(temp, tempStart, dataPointer, dataStart, dstSize, tRectWidth, tSizeInByte);
                        }
                    }
                    else
                    {
                        // we need to convert from our format size to the size of T here
                        var tFullWidth = Math.Max(Size >> level, 1) * Format.GetSize() / tSizeInByte;
                        IntPtr temp = Marshal.AllocHGlobal(Math.Max(Size >> level, 1) * tFullWidth * tSizeInByte);
                        GL.GetTexImage(target, level, glFormat, glType, temp);
                        GraphicsExtensions.CheckGLError();

                        var pixelToT = Format.GetSize() / tSizeInByte;
                        var rowCount = rect.Height;
                        var tRectWidth = rect.Width * pixelToT;
                        for (var r = 0; r < rowCount; r++)
                        {
                            var tempStart = rect.X * pixelToT + (r + rect.Top) * tFullWidth;
                            var dataStart = startIndex + r * tRectWidth;

                            CopyMemory(temp, tempStart, dataPointer, dataStart, dstSize, tRectWidth, tSizeInByte);
                        }
                    }
                }
                finally
                {
                    dataHandle.Free();
                }
            }
            if (Threading.IsOnUIThread())
                Get();
            else
                Threading.BlockOnUIThread(Get);
#else
            throw new NotImplementedException();
#endif
        }

        private void PlatformSetData<T>(CubeMapFace face, int level, Rectangle rect, T[] data, int startIndex, int elementCount)
        {
            void Set()
            {
                var elementSizeInByte = ReflectionHelpers.SizeOf<T>.Get();
                var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                // Use try..finally to make sure dataHandle is freed in case of an error
                try
                {
                    var startBytes = startIndex * elementSizeInByte;
                    var dataPtr = new IntPtr(dataHandle.AddrOfPinnedObject().ToInt64() + startBytes);

                    GL.BindTexture(TextureTarget.TextureCubeMap, _glTexture);
                    GraphicsExtensions.CheckGLError();

                    var target = GetGLCubeFace(face);
                    if (glFormat == GLPixelFormat.CompressedTextureFormats)
                    {
                        GL.CompressedTexSubImage2D(target, level, rect.X, rect.Y, rect.Width, rect.Height,
                             glInternalFormat, elementCount * elementSizeInByte, dataPtr);
                        GraphicsExtensions.CheckGLError();
                    }
                    else
                    {
                        GL.TexSubImage2D(target, level, rect.X, rect.Y, rect.Width, rect.Height,
                            glFormat, glType, dataPtr);
                        GraphicsExtensions.CheckGLError();
                    }
                }
                finally
                {
                    dataHandle.Free();
                }
            }

            if (Threading.IsOnUIThread())
                Set();
            else
                Threading.BlockOnUIThread(Set);
        }

        private TextureTarget GetGLCubeFace(CubeMapFace face)
        {
            switch (face)
            {
                case CubeMapFace.PositiveX: return TextureTarget.TextureCubeMapPositiveX;
                case CubeMapFace.NegativeX: return TextureTarget.TextureCubeMapNegativeX;
                case CubeMapFace.PositiveY: return TextureTarget.TextureCubeMapPositiveY;
                case CubeMapFace.NegativeY: return TextureTarget.TextureCubeMapNegativeY;
                case CubeMapFace.PositiveZ: return TextureTarget.TextureCubeMapPositiveZ;
                case CubeMapFace.NegativeZ: return TextureTarget.TextureCubeMapNegativeZ;
                default: throw new ArgumentException();
            }
        }
    }
}
