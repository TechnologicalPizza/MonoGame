// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;

namespace MonoGame.Framework.Vector
{
    /// <summary>
    /// Packed vector type containing three unsigned 8-bit XYZ components.
    /// <para>
    /// Ranges from [0, 0, 0, 1] to [1, 1, 1, 1] in vector form.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Bgr24 : IPixel<Bgr24>
    {
        VectorComponentInfo IVector.ComponentInfo => new VectorComponentInfo(
            new VectorComponent(VectorComponentType.Int8, VectorComponentChannel.Blue),
            new VectorComponent(VectorComponentType.Int8, VectorComponentChannel.Green),
            new VectorComponent(VectorComponentType.Int8, VectorComponentChannel.Red));

        [CLSCompliant(false)]
        public byte B;

        [CLSCompliant(false)]
        public byte G;

        [CLSCompliant(false)]
        public byte R;

        /// <summary>
        /// Gets or sets the RGB components of this struct as <see cref="Rgb24"/>
        /// </summary>
        public Rgb24 Rgb
        {
            readonly get => new Rgb24(R, G, B);
            set
            {
                R = value.R;
                G = value.G;
                B = value.B;
            }
        }

        #region Constructors

        [CLSCompliant(false)]
        public Bgr24(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        #endregion

        public readonly Vector3 ToVector3()
        {
            return new Vector3(R, G, B) / byte.MaxValue;
        }

        #region IPackedVector

        public void FromScaledVector4(Vector4 scaledVector)
        {
            Rgb24 rgb = default; // TODO: Unsafe.SkipInit
            rgb.FromScaledVector4(scaledVector);
            FromRgb24(rgb);
        }

        public readonly Vector4 ToScaledVector4()
        {
            return new Vector4(R, G, B, byte.MaxValue) / byte.MaxValue;
        }

        #endregion

        #region IPixel

        public void FromGray8(Gray8 source)
        {
            B = G = R = source.L;
        }

        public void FromGray16(Gray16 source)
        {
            B = G = R = PackedVectorHelper.DownScale16To8Bit(source.L);
        }

        public void FromGrayAlpha16(GrayAlpha16 source)
        {
            B = G = R = source.L;
        }

        public void FromRgb24(Rgb24 source)
        {
            R = source.R;
            G = source.G;
            B = source.B;
        }

        public void FromColor(Color source)
        {
            R = source.R;
            G = source.G;
            B = source.B;
        }

        public void FromRgb48(Rgb48 source)
        {
            R = PackedVectorHelper.DownScale16To8Bit(source.R);
            G = PackedVectorHelper.DownScale16To8Bit(source.G);
            B = PackedVectorHelper.DownScale16To8Bit(source.B);
        }

        public void FromRgba64(Rgba64 source)
        {
            R = PackedVectorHelper.DownScale16To8Bit(source.R);
            G = PackedVectorHelper.DownScale16To8Bit(source.G);
            B = PackedVectorHelper.DownScale16To8Bit(source.B);
        }

        public readonly void ToColor(out Color destination)
        {
            destination.R = R;
            destination.G = G;
            destination.B = B;
            destination.A = byte.MaxValue;
        }

        #endregion

        #region Equals

        public override readonly bool Equals(object obj)
        {
            return obj is Bgr24 other && Equals(other);
        }

        public readonly bool Equals(Bgr24 other)
        {
            return this == other;
        }

        public static bool operator ==(in Bgr24 a, in Bgr24 b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B;
        }

        public static bool operator !=(in Bgr24 a, in Bgr24 b)
        {
            return !(a == b);
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Gets a <see cref="string"/> representation of the packed vector.
        /// </summary>
        public override readonly string ToString() => nameof(Bgr24) + $"(R:{R}, G:{G}, B:{B})";

        /// <summary>
        /// Gets a hash code of the packed vector.
        /// </summary>
        public override readonly int GetHashCode() => HashCode.Combine(R, G, B);

        #endregion
    }
}