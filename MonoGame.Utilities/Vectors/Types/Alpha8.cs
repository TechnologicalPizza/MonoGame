// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MonoGame.Framework.Vectors
{
    /// <summary>
    /// Packed vector type containing an 8-bit W component.
    /// <para>
    /// Ranges from [1, 1, 1, 0] to [1, 1, 1, 1] in vector form.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Alpha8 : IPixel<Alpha8>, IPackedVector<byte>
    {
        public static Alpha8 Transparent => default;
        public static Alpha8 Opaque => new Alpha8(byte.MaxValue);

        readonly VectorComponentInfo IVector.ComponentInfo => new VectorComponentInfo(
            new VectorComponent(VectorComponentType.UInt8, VectorComponentChannel.Alpha));

        public byte A;

        #region Constructors

        /// <summary>
        /// Constructs the packed vector with a raw value.
        /// </summary>
        public Alpha8(byte value)
        {
            A = value;
        }

        /// <summary>
        /// Constructs the packed vector with a vector form value.
        /// </summary>
        /// <param name="alpha">The W component.</param>
        public Alpha8(float alpha)
        {
            A = ScalingHelper.ToUInt8(alpha);
        }

        #endregion

        #region IPackedVector

        public byte PackedValue
        {
            readonly get => A;
            set => A = value;
        }

        public void FromScaledVector(Vector3 scaledVector) => A = byte.MaxValue;
        public void FromScaledVector(Vector4 scaledVector) => A = ScalingHelper.ToUInt8(scaledVector.W);

        public readonly Vector3 ToScaledVector3() => Vector3.One;
        public readonly Vector4 ToScaledVector4() => new Vector4(ToScaledVector3(), ToAlphaF());

        public readonly Vector3 ToVector3() => ToScaledVector3();
        public readonly Vector4 ToVector4() => ToScaledVector4();

        #endregion

        #region IPixel.From

        public void FromAlpha(Alpha8 source) => this = source;
        public void FromAlpha(Alpha16 source) => A = ScalingHelper.ToUInt8(source.A);
        public void FromAlpha(Alpha32 source) => A = ScalingHelper.ToUInt8(source.A);
        public void FromAlpha(AlphaF source) => A = ScalingHelper.ToUInt8(source.A);

        public void FromGray(Gray8 source) => A = byte.MaxValue;
        public void FromGray(Gray16 source) => A = byte.MaxValue;
        public void FromGray(Gray32 source) => A = byte.MaxValue;
        public void FromGray(GrayF source) => A = byte.MaxValue;
        public void FromGray(GrayAlpha16 source) => A = source.A;

        public void FromColor(Bgr24 source) => A = byte.MaxValue;
        public void FromColor(Rgb24 source) => A = byte.MaxValue;
        public void FromColor(Rgb48 source) => A = byte.MaxValue;

        public void FromColor(Abgr32 source) => A = source.A;
        public void FromColor(Argb32 source) => A = source.A;
        public void FromColor(Bgra32 source) => A = source.A;
        public void FromColor(Color source) => A = source.A;
        public void FromColor(Rgba64 source) => A = ScalingHelper.ToUInt8(source.A);

        #endregion

        #region IPixel.To

        public readonly Alpha8 ToAlpha8() => this;
        public readonly Alpha16 ToAlpha16() => ScalingHelper.ToUInt16(A);
        public readonly AlphaF ToAlphaF() => ScalingHelper.ToFloat32(A);

        public readonly Gray8 ToGray8() => Gray8.White;
        public readonly Gray16 ToGray16() => Gray16.White;
        public readonly GrayF ToGrayF() => GrayF.White;
        public readonly GrayAlpha16 ToGrayAlpha16() => GrayAlpha16.OpaqueWhite;

        public readonly Rgb24 ToRgb24() => Rgb24.White;
        public readonly Rgb48 ToRgb48() => Rgb48.White;

        public readonly Color ToRgba32() => new Color(byte.MaxValue, A);
        public readonly Rgba64 ToRgba64() => new Rgba64(ushort.MaxValue, ScalingHelper.ToUInt16(A));

        #endregion

        #region Equals

        public readonly bool Equals(byte other) => PackedValue == other;
        public readonly bool Equals(Alpha8 other) => this == other;

        public static bool operator ==(Alpha8 a, Alpha8 b) => a.A == b.A;
        public static bool operator !=(Alpha8 a, Alpha8 b) => a.A != b.A;

        #endregion

        #region Object overrides

        public override readonly bool Equals(object? obj) => obj is Alpha8 other && Equals(other);

        /// <summary>
        /// Gets a hash code of the packed vector.
        /// </summary>
        public override readonly int GetHashCode() => HashCode.Combine(A);

        /// <summary>
        /// Gets a string representation of the packed vector.
        /// </summary>
        public override readonly string ToString() => nameof(Alpha8) + $"({A})";

        #endregion

        public static implicit operator Alpha8(byte alpha) => new Alpha8(alpha);
        public static implicit operator byte(Alpha8 value) => value.A;
    }
}
