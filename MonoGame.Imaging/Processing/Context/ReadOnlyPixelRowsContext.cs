﻿using System;
using MonoGame.Framework;
using MonoGame.Framework.Vectors;
using MonoGame.Imaging.Pixels;

namespace MonoGame.Imaging.Processing
{
    public class ReadOnlyPixelRowsContext : IReadOnlyPixelRowsContext
    {
        public IImagingConfig Config { get; }
        public IReadOnlyPixelRows Pixels { get; }
        public bool IsDisposed { get; private set; }

        public int Length => Pixels.Length;
        public int ElementSize => Pixels.ElementSize;

        public VectorType PixelType => Pixels.PixelType;
        public Size Size => Pixels.Size;

        public ReadOnlyPixelRowsContext(IImagingConfig imagingConfig, IReadOnlyPixelRows pixels)
        {
            Config = imagingConfig ?? throw new ArgumentNullException(nameof(imagingConfig));
            Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
        }

        public void GetPixelByteRow(int x, int y, Span<byte> destination)
        {
            Pixels.GetPixelByteRow(x, y, destination);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}