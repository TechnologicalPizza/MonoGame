// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoGame.Framework.Vector
{
    /// <summary>
    /// Packed vector type containing an 8-bit XYZ luminance an 8-bit W component.
    /// <para>
    /// Ranges from [0, 0, 0, 0] to [1, 1, 1, 1] in vector form.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GrayAlpha16 : IPackedPixel<GrayAlpha16, ushort>
    {
        VectorComponentInfo IVector.ComponentInfo => new VectorComponentInfo(
            new VectorComponent(VectorComponentType.Int8, VectorComponentChannel.Luminance),
            new VectorComponent(VectorComponentType.Int8, VectorComponentChannel.Alpha));

        public byte L;
        public byte A;

        public GrayAlpha16(byte luminance, byte alpha)
        {
            L = luminance;
            A = alpha;
        }

        #region IPackedVector

        [CLSCompliant(false)]
        public ushort PackedValue
        {
            readonly get => UnsafeR.As<GrayAlpha16, ushort>(this);
            set => Unsafe.As<GrayAlpha16, ushort>(ref this) = value;
        }

        public void FromScaledVector4(Vector4 scaledVector)
        {
            scaledVector *= byte.MaxValue;
            scaledVector += Vector4.Half;
            scaledVector.Clamp(Vector4.Zero, Vector4.MaxValueByte);

            L = (byte)(PackedVectorHelper.GetBT709Luminance(scaledVector.ToVector3()) + 0.5f);
            A = (byte)scaledVector.W;
        }

        public readonly Vector4 ToScaledVector4()
        {
            return new Vector4(L, L, L, A) / byte.MaxValue;
        }

        #endregion

        #region IPixel

        public void FromGray8(Gray8 source)
        {
            L = source.L;
            A = byte.MaxValue;
        }

        public void FromGray16(Gray16 source)
        {
            L = PackedVectorHelper.DownScale16To8Bit(source.L);
            A = byte.MaxValue;
        }

        public void FromGrayAlpha16(GrayAlpha16 source)
        {
            L = source.L;
            A = source.A;
        }

        public void FromRgb24(Rgb24 source)
        {
            L = PackedVectorHelper.Get8BitBT709Luminance(source.R, source.G, source.B);
            A = byte.MaxValue;
        }

        public void FromRgba32(Color source)
        {
            L = PackedVectorHelper.Get8BitBT709Luminance(source.R, source.G, source.B);
            A = byte.MaxValue;
        }

        public void FromRgb48(Rgb48 source)
        {
            L = PackedVectorHelper.DownScale16To8Bit(
                PackedVectorHelper.Get16BitBT709Luminance(source.R, source.G, source.B));
            A = byte.MaxValue;
        }

        public void FromRgba64(Rgba64 source)
        {
            L = PackedVectorHelper.DownScale16To8Bit(
                PackedVectorHelper.Get16BitBT709Luminance(source.R, source.G, source.B));
            A = byte.MaxValue;
        }

        public readonly Color ToColor()
        {
            return new Color(L, A);
        }

        #endregion

        public void FromArgb32(Argb32 source)
        {
            L = PackedVectorHelper.Get8BitBT709Luminance(source.R, source.G, source.B);
            A = source.A;
        }

        public void FromBgr24(Bgr24 source)
        {
            L = PackedVectorHelper.Get8BitBT709Luminance(source.R, source.G, source.B);
            A = byte.MaxValue;
        }

        public void FromBgra32(Bgra32 source)
        {
            L = PackedVectorHelper.Get8BitBT709Luminance(source.R, source.G, source.B);
            A = source.A;
        }

        #region Equals

        public readonly bool Equals(GrayAlpha16 other)
        {
            return this == other;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is GrayAlpha16 other && Equals(other);
        }

        public static bool operator ==(GrayAlpha16 a, GrayAlpha16 b)
        {
            return a.PackedValue == b.PackedValue;
        }

        public static bool operator !=(GrayAlpha16 a, GrayAlpha16 b)
        {
            return a.PackedValue != b.PackedValue;
        }

        #endregion

        #region Object Overrides

        public override readonly string ToString() => nameof(GrayAlpha16) + $"(L:{L}, A:{A})";

        public override readonly int GetHashCode() => PackedValue.GetHashCode();

        #endregion
    }
}