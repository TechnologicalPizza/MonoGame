﻿using System;
using System.Runtime.InteropServices;
using MonoGame.Framework;
using MonoGame.Framework.Vector;

namespace MonoGame.Imaging
{
    public partial class Image
    {
        public static void ConvertPixels<TPixelFrom, TPixelTo>(
            ReadOnlySpan<TPixelFrom> source, Span<TPixelTo> destination)
            where TPixelFrom : unmanaged, IPixel
            where TPixelTo : unmanaged, IPixel
        {
            if (destination.Length < source.Length)
                throw new ArgumentException("Destination is too short.");
            
            if (typeof(TPixelFrom) == typeof(TPixelTo))
            {
                var src = MemoryMarshal.Cast<TPixelFrom, TPixelTo>(source);
                src.CopyTo(destination);
            }
            else if (typeof(TPixelFrom) == typeof(Gray8))
            {
                var typedSource = MemoryMarshal.Cast<TPixelFrom, Gray8>(source);
                for (int x = 0; x < source.Length; x++)
                    destination[x].FromGray8(typedSource[x]);
            }
            else if (typeof(TPixelFrom) == typeof(Gray16))
            {
                var typedSource = MemoryMarshal.Cast<TPixelFrom, Gray16>(source);
                for (int x = 0; x < source.Length; x++)
                    destination[x].FromGray16(typedSource[x]);
            }
            else if (typeof(TPixelFrom) == typeof(GrayAlpha16))
            {
                var typedSource = MemoryMarshal.Cast<TPixelFrom, GrayAlpha16>(source);
                for (int x = 0; x < source.Length; x++)
                    destination[x].FromGrayAlpha16(typedSource[x]);
            }
            else if (typeof(TPixelFrom) == typeof(Rgb24))
            {
                var typedSource = MemoryMarshal.Cast<TPixelFrom, Rgb24>(source);
                for (int x = 0; x < source.Length; x++)
                    destination[x].FromRgb24(typedSource[x]);
            }
            else if (typeof(TPixelFrom) == typeof(Color))
            {
                var typedSource = MemoryMarshal.Cast<TPixelFrom, Color>(source);
                for (int x = 0; x < source.Length; x++)
                    destination[x].FromRgba32(typedSource[x]);
            }
            else if (typeof(TPixelFrom) == typeof(Rgb48))
            {
                var typedSource = MemoryMarshal.Cast<TPixelFrom, Rgb48>(source);
                for (int x = 0; x < source.Length; x++)
                    destination[x].FromRgb48(typedSource[x]);
            }
            else if (typeof(TPixelFrom) == typeof(Rgba64))
            {
                var typedSource = MemoryMarshal.Cast<TPixelFrom, Rgba64>(source);
                for (int x = 0; x < source.Length; x++)
                    destination[x].FromRgba64(typedSource[x]);
            }
            else if (typeof(TPixelTo) == typeof(Gray8))
            {
                var typedDestination = MemoryMarshal.Cast<TPixelTo, Gray8>(destination);
                for (int x = 0; x < source.Length; x++)
                    typedDestination[x] = source[x].ToGray8();
            }
            else if (typeof(TPixelTo) == typeof(Gray16))
            {
                var typedDestination = MemoryMarshal.Cast<TPixelTo, Gray16>(destination);
                for (int x = 0; x < source.Length; x++)
                    typedDestination[x] = source[x].ToGray16();
            }
            else if (typeof(TPixelTo) == typeof(GrayF))
            {
                var typedDestination = MemoryMarshal.Cast<TPixelTo, GrayF>(destination);
                for (int x = 0; x < source.Length; x++)
                    typedDestination[x] = source[x].ToGrayF();
            }
            else if (typeof(TPixelTo) == typeof(GrayAlpha16))
            {
                var typedDestination = MemoryMarshal.Cast<TPixelTo, GrayAlpha16>(destination);
                for (int x = 0; x < source.Length; x++)
                    typedDestination[x] = source[x].ToGrayAlpha16();
            }
            else if (typeof(TPixelTo) == typeof(Color))
            {
                var typedDestination = MemoryMarshal.Cast<TPixelTo, Color>(destination);
                for (int x = 0; x < source.Length; x++)
                    typedDestination[x] = source[x].ToColor();
            }
            else
            {
                for (int x = 0; x < source.Length; x++)
                    destination[x].FromScaledVector4(source[x].ToScaledVector4());
            }
        }

        public static void ConvertPixelBytes<TPixelFrom, TPixelTo>(
            ReadOnlySpan<byte> source, Span<byte> destination)
            where TPixelFrom : unmanaged, IPixel
            where TPixelTo : unmanaged, IPixel
        {
            var src = MemoryMarshal.Cast<byte, TPixelFrom>(source);
            var dst = MemoryMarshal.Cast<byte, TPixelTo>(destination);
            ConvertPixels(src, dst);
        }

        public static void ConvertPixelBytes(
            VectorTypeInfo sourceType, VectorTypeInfo destinationType,
            ReadOnlySpan<byte> source, Span<byte> destination)
        {
            var convertDelegate = GetConvertPixelsDelegate(sourceType, destinationType);
            convertDelegate.Invoke(source, destination);
        }
    }
}