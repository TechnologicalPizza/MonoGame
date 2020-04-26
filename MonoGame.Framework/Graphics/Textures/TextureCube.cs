// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace MonoGame.Framework.Graphics
{
    public partial class TextureCube : Texture
    {
        /// <summary>
        /// Gets the width and height of the cube map face in pixels.
        /// </summary>
        /// <value>The width and height of a cube map face in pixels.</value>
        public int Size { get; internal set; }

        public TextureCube(
            GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
            : this(graphicsDevice, size, mipMap, format, false)
        {
        }

        internal TextureCube(
            GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format, bool renderTarget) 
            : base(graphicsDevice)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Cube size must be greater than zero");

            Size = size;
            Format = format;
            LevelCount = mipMap ? CalculateMipLevels(size) : 1;

            PlatformConstruct(graphicsDevice, size, mipMap, format, renderTarget);
        }

        /// <summary>
        /// Gets a copy of cube texture data specifying a cubemap face.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cubeMapFace">The cube map face.</param>
        /// <param name="data">The data.</param>
        public void GetData<T>(CubeMapFace cubeMapFace, T[] data)
            where T : unmanaged
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            GetData(cubeMapFace, 0, null, data, 0, data.Length);
        }

        public void GetData<T>(CubeMapFace cubeMapFace, T[] data, int startIndex, int elementCount)
            where T : unmanaged
        {
            GetData(cubeMapFace, 0, null, data, startIndex, elementCount);
        }

        public void GetData<T>(
            CubeMapFace cubeMapFace, int level, Rectangle? rect, T[] data, int startIndex, int elementCount)
            where T : unmanaged
        {
            ValidateParams(level, rect, data, startIndex, elementCount, out Rectangle checkedRect);
            PlatformGetData(cubeMapFace, level, checkedRect, data, startIndex, elementCount);
        }

        public void SetData<T>(CubeMapFace face, T[] data)
            where T : unmanaged
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            SetData(face, 0, null, data, 0, data.Length);
        }

        public void SetData<T>(CubeMapFace face, T[] data, int startIndex, int elementCount)
            where T : unmanaged
        {
            SetData(face, 0, null, data, startIndex, elementCount);
        }

        public void SetData<T>(
            CubeMapFace face, int level, Rectangle? rect, T[] data, int startIndex, int elementCount)
            where T : unmanaged
        {
            ValidateParams(level, rect, data, startIndex, elementCount, out Rectangle checkedRect);
            PlatformSetData(face, level, checkedRect, data, startIndex, elementCount);
        }

        private unsafe void ValidateParams<T>(
            int level, Rectangle? rect, T[] data, int startIndex,
            int elementCount, out Rectangle checkedRect)
            where T : unmanaged
        {
            var textureBounds = new Rectangle(0, 0, Math.Max(Size >> level, 1), Math.Max(Size >> level, 1));
            checkedRect = rect ?? textureBounds;

            if (level < 0 || level >= LevelCount)
                throw new ArgumentException(
                    $"{nameof(level)} must be smaller than the number of levels in this texture.", nameof(level));

            if (!textureBounds.Contains(checkedRect) || checkedRect.Width <= 0 || checkedRect.Height <= 0)
                throw new ArgumentException("Rectangle must be inside the texture bounds", nameof(rect));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var fSize = Format.GetSize();
            if (sizeof(T) > fSize || fSize % sizeof(T) != 0)
                throw new ArgumentException(
                    $"Type {nameof(T)} is of an invalid size for the format of this texture.", nameof(T));

            if (startIndex < 0 || startIndex >= data.Length)
                throw new ArgumentException(
                    $"{nameof(startIndex)} must be at least zero and smaller than {nameof(data)}.{nameof(data.Length)}.",
                    nameof(startIndex));

            if (data.Length < startIndex + elementCount)
                throw new ArgumentException("The data array is too small.");

            int dataByteSize;
            if (Format.IsCompressedFormat())
            {
                // round x and y down to next multiple of four; width and height up to next multiple of four
                var roundedWidth = (checkedRect.Width + 3) & ~0x3;
                var roundedHeight = (checkedRect.Height + 3) & ~0x3;
                checkedRect = new Rectangle(checkedRect.X & ~0x3, checkedRect.Y & ~0x3,
#if OPENGL
                    // OpenGL only: The last two mip levels require the width and height to be
                    // passed as 2x2 and 1x1, but there needs to be enough data passed to occupy
                    // a 4x4 block.
                    checkedRect.Width < 4 && textureBounds.Width < 4 ? textureBounds.Width : roundedWidth,
                    checkedRect.Height < 4 && textureBounds.Height < 4 ? textureBounds.Height : roundedHeight);
#else
                    roundedWidth, roundedHeight);
#endif
                dataByteSize = roundedWidth * roundedHeight * fSize / 16;
            }
            else
            {
                dataByteSize = checkedRect.Width * checkedRect.Height * fSize;
            }
            if (elementCount * sizeof(T) != dataByteSize)
                throw new ArgumentException(
                    $"{nameof(elementCount)} is not the right size, " +
                    $"{nameof(elementCount)} * sizeof({nameof(T)}) is {elementCount * sizeof(T)}, " +
                    $"but data size is {dataByteSize}.", nameof(elementCount));
        }
    }
}
