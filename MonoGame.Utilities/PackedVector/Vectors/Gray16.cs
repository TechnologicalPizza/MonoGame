// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;

namespace MonoGame.Framework.PackedVector
{
    /// <summary>
    /// Packed vector type containing a 16-bit XYZ luminance.
    /// <para>
    /// Ranges from [0, 0, 0, 1] to [1, 1, 1, 1] in vector form.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Gray16 : IPackedVector<ushort>, IEquatable<Gray16>, IPixel
    {
        VectorComponentInfo IPackedVector.ComponentInfo => new VectorComponentInfo(
            new VectorComponent(VectorComponentType.Luminance, sizeof(ushort) * 8));

        [CLSCompliant(false)]
        public ushort L;

        [CLSCompliant(false)]
        public Gray16(ushort luminance) => L = luminance;

        #region IPackedVector

        [CLSCompliant(false)]
        public ushort PackedValue { readonly get => L; set => L = value; }

        public void FromVector4(in Vector4 vector) => FromScaledVector4(vector);

        public readonly void ToVector4(out Vector4 vector) => ToScaledVector4(out vector);

        public void FromScaledVector4(in Vector4 vector)
        {
            var v = vector * ushort.MaxValue;
            v += Vector4.Half;
            v.Clamp(0, ushort.MaxValue);

            L = (ushort)PackedVectorHelper.GetBT709Luminance(v.X, v.Y, v.Z);
        }

        public readonly void ToScaledVector4(out Vector4 scaledVector)
        {
            scaledVector.Base.X = scaledVector.Base.Y = scaledVector.Base.Z = L / (float)ushort.MaxValue;
            scaledVector.Base.W = 1;
        }

        #endregion

        #region IPixel

        public void FromGray8(Gray8 source)
        {
            L = PackedVectorHelper.UpScale8To16Bit(source.L);
        }

        public void FromGrayAlpha16(GrayAlpha16 source)
        {
            L = PackedVectorHelper.UpScale8To16Bit(source.L);
        }

        public void FromGray16(Gray16 source)
        {
            L = source.L;
        }

        public void FromRgb24(Rgb24 source)
        {
            L = PackedVectorHelper.UpScale8To16Bit(
                PackedVectorHelper.Get8BitBT709Luminance(source.R, source.G, source.B));
        }

        public void FromColor(Color source)
        {
            L = PackedVectorHelper.UpScale8To16Bit(
                PackedVectorHelper.Get8BitBT709Luminance(source.R, source.G, source.B));
        }

        public void FromRgb48(Rgb48 source)
        {
            L = PackedVectorHelper.Get16BitBT709Luminance(source.R, source.G, source.B);
        }

        public void FromRgba64(Rgba64 source)
        {
            L = PackedVectorHelper.Get16BitBT709Luminance(source.R, source.G, source.B);
        }

        public readonly void ToColor(ref Color destination)
        {
            destination.R = destination.G = destination.B =
                PackedVectorHelper.DownScale16To8Bit(L);
            destination.A = byte.MaxValue;
        }

        #endregion

        public void FromArgb32(Argb32 source)
        {
            L = PackedVectorHelper.Get16BitBT709Luminance(
                    PackedVectorHelper.UpScale8To16Bit(source.R),
                    PackedVectorHelper.UpScale8To16Bit(source.G),
                    PackedVectorHelper.UpScale8To16Bit(source.B));
        }

        public void FromBgr24(Bgr24 source)
        {
            L = PackedVectorHelper.Get16BitBT709Luminance(
                    PackedVectorHelper.UpScale8To16Bit(source.R),
                    PackedVectorHelper.UpScale8To16Bit(source.G),
                    PackedVectorHelper.UpScale8To16Bit(source.B));
        }

        public void FromBgra32(Bgra32 source)
        {
            L = PackedVectorHelper.Get16BitBT709Luminance(
                    PackedVectorHelper.UpScale8To16Bit(source.R),
                    PackedVectorHelper.UpScale8To16Bit(source.G),
                    PackedVectorHelper.UpScale8To16Bit(source.B));
        }

        #region Equals

        public static bool operator ==(Gray16 a, Gray16 b) => a.L == b.L;
        public static bool operator !=(Gray16 a, Gray16 b) => a.L != b.L;

        public bool Equals(Gray16 other) => this == other;
        public override bool Equals(object obj) => obj is Gray16 other && Equals(other);

        #endregion

        #region Object Overrides

        public override string ToString() => nameof(Gray16) + $"({L})";

        public override int GetHashCode() => L.GetHashCode();

        #endregion
    }
}