﻿using System;
using System.Collections.Concurrent;
using MonoGame.Framework;
using MonoGame.Framework.PackedVector;
using MonoGame.Imaging.Pixels;

namespace MonoGame.Imaging
{
    public partial class Image
    {
        private delegate void LoadPixelDataDelegate(
            ReadOnlySpan<byte> pixelData, Rectangle sourceRectangle, int? byteStride, Image destination);

        private static ConcurrentDictionary<(PixelTypeInfo, PixelTypeInfo), LoadPixelDataDelegate> _loadPixelSpanDelegateCache =
            new ConcurrentDictionary<(PixelTypeInfo, PixelTypeInfo), LoadPixelDataDelegate>();

        #region LoadPixelData(FromType, ToType, ReadOnlySpan<byte>)

        public static Image LoadPixelData(
            PixelTypeInfo fromPixelType,
            PixelTypeInfo toPixelType,
            ReadOnlySpan<byte> pixelData,
            Rectangle sourceRectangle,
            int? byteStride = null)
        {
            if (!_loadPixelSpanDelegateCache.TryGetValue((fromPixelType, toPixelType), out var loadDelegate))
            {

            }

            var image = Create(sourceRectangle.Size, toPixelType);
            try
            {
                loadDelegate.Invoke(pixelData, sourceRectangle, byteStride, image);
            }
            catch
            {
                image.Dispose();
                throw;
            }
            return image;
        }

        public static Image LoadPixelData(
            PixelTypeInfo fromPixelType,
            PixelTypeInfo toPixelType,
            ReadOnlySpan<byte> pixelData,
            Size size,
            int? byteStride = null)
        {
            return LoadPixelData(
                fromPixelType, toPixelType, pixelData, new Rectangle(size), byteStride);
        }

        #endregion

        #region LoadPixelData(FromType, ToType, Span<byte>)

        public static Image LoadPixelData(
            PixelTypeInfo fromPixelType,
            PixelTypeInfo toPixelType,
            Span<byte> pixelData,
            Rectangle sourceRectangle,
            int? byteStride = null)
        {
            return LoadPixelData(
                fromPixelType, toPixelType, (ReadOnlySpan<byte>)pixelData, sourceRectangle, byteStride);
        }

        public static Image LoadPixelData(
            PixelTypeInfo fromPixelType,
            PixelTypeInfo toPixelType,
            Span<byte> pixelData,
            Size size,
            int? byteStride = null)
        {
            return LoadPixelData(
                fromPixelType, toPixelType, (ReadOnlySpan<byte>)pixelData, size, byteStride);
        }

        #endregion


        #region LoadPixelData(Type, ReadOnlySpan<byte>)

        public static Image LoadPixelData(
            PixelTypeInfo pixelType,
            ReadOnlySpan<byte> pixelData,
            Rectangle sourceRectangle,
            int? byteStride = null)
        {
            return LoadPixelData(pixelType, pixelType, pixelData, sourceRectangle, byteStride);
        }

        public static Image LoadPixelData(
            PixelTypeInfo pixelType, ReadOnlySpan<byte> pixelData, Size size, int? byteStride = null)
        {
            return LoadPixelData(pixelType, pixelData, new Rectangle(size), byteStride);
        }

        #endregion

        #region LoadPixelData(Type, Span<byte>)

        public static Image LoadPixelData(
            PixelTypeInfo pixelType,
            Span<byte> pixelData,
            Rectangle sourceRectangle,
            int? byteStride = null)
        {
            return LoadPixelData(
                pixelType, (ReadOnlySpan<byte>)pixelData, sourceRectangle, byteStride);
        }

        public static Image LoadPixelData(
            PixelTypeInfo pixelType, Span<byte> pixelData, Size size, int? byteStride = null)
        {
            return LoadPixelData(
                pixelType, (ReadOnlySpan<byte>)pixelData, size, byteStride);
        }

        #endregion


        #region LoadPixelData<T>(Type, ReadOnlySpan<byte>)

        public static Image<TPixelTo> LoadPixelData<TPixelTo>(
            PixelTypeInfo fromPixelType,
            ReadOnlySpan<byte> pixelData,
            Rectangle sourceRectangle,
            int? byteStride = null)
            where TPixelTo : unmanaged, IPixel
        {
            var toType = PixelTypeInfo.Get<TPixelTo>();
            return (Image<TPixelTo>)LoadPixelData(fromPixelType, toType, pixelData, sourceRectangle, byteStride);
        }

        public static Image<TPixelTo> LoadPixelData<TPixelTo>(
            PixelTypeInfo fromPixelType, ReadOnlySpan<byte> pixelData, Size size, int? byteStride = null)
            where TPixelTo : unmanaged, IPixel
        {
            return LoadPixelData<TPixelTo>(fromPixelType, pixelData, new Rectangle(size), byteStride);
        }

        #endregion

        #region LoadPixelData<T>(Type, Span<byte>)

        public static Image<TPixelTo> LoadPixelData<TPixelTo>(
            PixelTypeInfo fromPixelType,
            Span<byte> pixelData,
            Rectangle sourceRectangle,
            int? byteStride = null)
            where TPixelTo : unmanaged, IPixel
        {
            return LoadPixelData<TPixelTo>(
                fromPixelType, (ReadOnlySpan<byte>)pixelData, sourceRectangle, byteStride);
        }

        public static Image<TPixelTo> LoadPixelData<TPixelTo>(
            PixelTypeInfo fromPixelType, Span<byte> pixelData, Size size, int? byteStride = null)
            where TPixelTo : unmanaged, IPixel
        {
            return LoadPixelData<TPixelTo>(
                fromPixelType, (ReadOnlySpan<byte>)pixelData, size, byteStride);
        }

        #endregion


        #region LoadPixelData<T>(ReadOnlySpan<byte>)

        public static Image<TPixelTo> LoadPixelData<TPixelFrom, TPixelTo>(
            ReadOnlySpan<byte> pixelData, Rectangle sourceRectangle, int? byteStride = null)
            where TPixelFrom : unmanaged, IPixel
            where TPixelTo : unmanaged, IPixel
        {
            var fromType = PixelTypeInfo.Get<TPixelFrom>();
            return LoadPixelData<TPixelTo>(fromType, pixelData, sourceRectangle, byteStride);
        }

        public static Image<TPixelTo> LoadPixelData<TPixelFrom, TPixelTo>(
            ReadOnlySpan<byte> pixelData, Size size, int? byteStride = null)
            where TPixelFrom : unmanaged, IPixel
            where TPixelTo : unmanaged, IPixel
        {
            return LoadPixelData<TPixelFrom, TPixelTo>(pixelData, new Rectangle(size), byteStride);
        }

        public static Image<TPixel> LoadPixelData<TPixel>(
            ReadOnlySpan<byte> pixelData, Rectangle sourceRectangle, int? byteStride = null)
            where TPixel : unmanaged, IPixel
        {
            return LoadPixelData<TPixel, TPixel>(pixelData, sourceRectangle, byteStride);
        }

        public static Image<TPixel> LoadPixelData<TPixel>(
            ReadOnlySpan<byte> pixelData, Size size, int? byteStride = null)
            where TPixel : unmanaged, IPixel
        {
            return LoadPixelData<TPixel, TPixel>(pixelData, new Rectangle(size), byteStride);
        }

        #endregion

        #region LoadPixelData<T>(Span<byte>)

        public static Image<TPixelTo> LoadPixelData<TPixelFrom, TPixelTo>(
            Span<byte> pixelData, Rectangle sourceRectangle, int? byteStride = null)
            where TPixelFrom : unmanaged, IPixel
            where TPixelTo : unmanaged, IPixel
        {
            return LoadPixelData<TPixelFrom, TPixelTo>(
                (ReadOnlySpan<byte>)pixelData, sourceRectangle, byteStride);
        }

        public static Image<TPixelTo> LoadPixelData<TPixelFrom, TPixelTo>(
            Span<byte> pixelData, Size size, int? byteStride = null)
            where TPixelFrom : unmanaged, IPixel
            where TPixelTo : unmanaged, IPixel
        {
            return LoadPixelData<TPixelFrom, TPixelTo>(
                (ReadOnlySpan<byte>)pixelData, new Rectangle(size), byteStride);
        }

        public static Image<TPixel> LoadPixelData<TPixel>(
            Span<byte> pixelData, Rectangle sourceRectangle, int? byteStride = null)
            where TPixel : unmanaged, IPixel
        {
            return LoadPixelData<TPixel, TPixel>(
                (ReadOnlySpan<byte>)pixelData, sourceRectangle, byteStride);
        }

        public static Image<TPixel> LoadPixelData<TPixel>(
            Span<byte> pixelData, Size size, int? byteStride = null)
            where TPixel : unmanaged, IPixel
        {
            return LoadPixelData<TPixel, TPixel>(
                (ReadOnlySpan<byte>)pixelData, new Rectangle(size), byteStride);
        }

        #endregion
    }
}
