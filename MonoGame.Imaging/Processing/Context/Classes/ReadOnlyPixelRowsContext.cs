﻿using System;
using MonoGame.Framework;
using MonoGame.Framework.PackedVector;
using MonoGame.Imaging.Pixels;

namespace MonoGame.Imaging.Processing
{
    public class ReadOnlyPixelRowsContext : IReadOnlyPixelRowsContext
    {
        public ImagingConfig Config { get; }
        public IReadOnlyPixelRows Pixels { get; }

        public int Length => Pixels.Length;
        public int ElementSize => Pixels.ElementSize;

        public VectorTypeInfo PixelType => Pixels.PixelType;
        public Size Size => Pixels.Size;

        public ReadOnlyPixelRowsContext(ImagingConfig imagingConfig, IReadOnlyPixelRows pixels)
        {
            Config = imagingConfig ?? throw new ArgumentNullException(nameof(imagingConfig));
            Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
        }

        public void GetPixelByteRow(int x, int y, Span<byte> destination)
        {
            Pixels.GetPixelByteRow(x, y, destination);
        }

        public virtual void Dispose()
        {
        }
    }
}