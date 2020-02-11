﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using MonoGame.Framework;
using MonoGame.Framework.Memory;
using MonoGame.Framework.PackedVector;
using MonoGame.Imaging.Pixels;

namespace MonoGame.Imaging
{
    /// <summary>
    /// Base class for objects that store pixels.
    /// </summary>
    public abstract partial class Image : IPixelMemory
    {
        public event DatalessEvent<Image> Disposing;

        #region Properties

        /// <summary>
        /// Gets whether the object is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets the width of the image in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height of the image in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets info about the pixel type of the image.
        /// </summary>
        public VectorTypeInfo PixelType { get; }

        public abstract int ByteStride { get; }

        public bool IsPixelContiguous => Width * PixelType.ElementSize == ByteStride;

        int IElementContainer.ElementSize => PixelType.ElementSize;
        int IElementContainer.Count => Width * Height;

        #endregion

        static Image()
        {
            SetupReflection();
        }

        protected Image(VectorTypeInfo pixelType, int width, int height)
        {
            PixelType = pixelType ?? throw new ArgumentNullException(nameof(pixelType));
            ArgumentGuard.AssertGreaterThanZero(width, nameof(width));
            ArgumentGuard.AssertGreaterThanZero(height, nameof(height));
            Width = width;
            Height = height;
        }

        #region Create

        /// <summary>
        /// Creates an empty image.
        /// </summary>
        public static Image Create(VectorTypeInfo pixelType, int width, int height)
        {
            var createDelegate = GetCreateDelegate(pixelType);
            return createDelegate.Invoke(width, height);
        }

        /// <summary>
        /// Creates an empty image.
        /// </summary>
        public static Image Create(VectorTypeInfo pixelType, Size size)
        {
            return Create(pixelType, size.Width, size.Height);
        }

        #endregion

        public abstract Span<byte> GetPixelByteRowSpan(int row);

        public abstract Span<byte> GetPixelByteSpan();

        public void GetPixelByteRow(int x, int y, Span<byte> destination)
        {
            var rowSpan = GetPixelByteRowSpan(y);
            rowSpan.Slice(x).CopyTo(destination);
        }

        public void SetPixelByteRow(int x, int y, ReadOnlySpan<byte> data)
        {
            var rowSpan = GetPixelByteRowSpan(y);
            data.CopyTo(rowSpan.Slice(x));
        }

        ReadOnlySpan<byte> IReadOnlyPixelBuffer.GetPixelByteRowSpan(int row) => GetPixelByteRowSpan(row);

        ReadOnlySpan<byte> IReadOnlyPixelMemory.GetPixelByteSpan() => GetPixelByteSpan();

        #region IDisposable

        [DebuggerHidden]
        protected void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(Image));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                Disposing?.Invoke(this);
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Image()
        {
            Dispose(false);
        }

        #endregion
    }
}
