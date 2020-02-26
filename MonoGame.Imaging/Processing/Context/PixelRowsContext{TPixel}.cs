﻿using System;
using MonoGame.Framework;
using MonoGame.Framework.PackedVector;
using MonoGame.Imaging.Pixels;

namespace MonoGame.Imaging.Processing
{
    public readonly struct PixelRowsContext<TPixel> : IPixelRows<TPixel>, IImagingConfigProvider
        where TPixel : unmanaged, IPixel
    {
        public ImagingConfig ImagingConfig { get; }
        public IPixelRows<TPixel> Pixels { get; }

        public bool IsEmpty => Pixels == null;

        public int Count => Pixels.Count;
        public int ElementSize => Pixels.ElementSize;

        public Size Size => Pixels.Size;
        public VectorTypeInfo PixelType => Pixels.PixelType;

        public PixelRowsContext(ImagingConfig imagingConfig, IPixelRows<TPixel> pixels)
        {
            ImagingConfig = imagingConfig ?? throw new ArgumentNullException(nameof(imagingConfig));
            Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
        }

        public void SetPixelByteRow(int x, int y, ReadOnlySpan<byte> data)
        {
            Pixels.SetPixelByteRow(x, y, data);
        }

        public void GetPixelByteRow(int x, int y, Span<byte> destination)
        {
            Pixels.GetPixelByteRow(x, y, destination);
        }

        public void SetPixelRow(int x, int y, ReadOnlySpan<TPixel> data)
        {
            Pixels.SetPixelRow(x, y, data);
        }

        public void GetPixelRow(int x, int y, Span<TPixel> destination)
        {
            Pixels.GetPixelRow(x, y, destination);
        }

        public void Dispose()
        {
        }
    }
}