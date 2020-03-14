﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoGame.Framework.PackedVector
{
    /// <summary>
    /// Packed vector type containing four 32-bit floating-point XYZW components.
    /// <para>
    /// Ranges from [0, 0, 0, 0] to [1, 1, 1, 1] in vector form.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RgbaVector : IPixel, IEquatable<RgbaVector>
    {
        VectorComponentInfo IPackedVector.ComponentInfo => new VectorComponentInfo(
            new VectorComponent(VectorComponentType.Red, sizeof(float) * 8),
            new VectorComponent(VectorComponentType.Green, sizeof(float) * 8),
            new VectorComponent(VectorComponentType.Blue, sizeof(float) * 8),
            new VectorComponent(VectorComponentType.Alpha, sizeof(float) * 8));

        public float R;
        public float G;
        public float B;
        public float A;

        #region Constructors

        /// <summary>
        /// Constructs the packed vector with vector form values.
        /// </summary>
        public RgbaVector(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Constructs the packed vector with vector form values.
        /// </summary>
        /// <param name="vector"><see cref="Vector4"/> containing the components.</param>
        public RgbaVector(in Vector4 vector) : this(vector.X, vector.Y, vector.Z, vector.W)
        {
        }

        #endregion

        #region IPackedVector

        public void FromVector4(in Vector4 vector) => FromScaledVector4(vector);

        public readonly void ToVector4(out Vector4 vector) => ToScaledVector4(out vector);

        public void FromScaledVector4(in Vector4 scaledVector)
        {
            R = scaledVector.X;
            G = scaledVector.Y;
            B = scaledVector.Z;
            A = scaledVector.W;
        }

        public readonly void ToScaledVector4(out Vector4 scaledVector)
        {
            scaledVector.X = R;
            scaledVector.Y = G;
            scaledVector.Z = B;
            scaledVector.W = A;
        }

        #endregion

        #region IPixel

        public void FromGray8(Gray8 source) => source.ToScaledVector4(out Unsafe.As<RgbaVector, Vector4>(ref this));

        public void FromGray16(Gray16 source) => source.ToScaledVector4(out Unsafe.As<RgbaVector, Vector4>(ref this));

        public void FromGrayAlpha16(GrayAlpha16 source) => source.ToScaledVector4(out Unsafe.As<RgbaVector, Vector4>(ref this));

        public void FromRgb24(Rgb24 source) => source.ToScaledVector4(out Unsafe.As<RgbaVector, Vector4>(ref this));

        public void FromRgb48(Rgb48 source) => source.ToScaledVector4(out Unsafe.As<RgbaVector, Vector4>(ref this));

        public void FromRgba64(Rgba64 source) => source.ToScaledVector4(out Unsafe.As<RgbaVector, Vector4>(ref this));

        public void FromColor(Color source) => source.ToScaledVector4(out Unsafe.As<RgbaVector, Vector4>(ref this));

        public readonly void ToColor(ref Color destination)
        {
            destination.FromScaledVector4(UnsafeUtils.As<RgbaVector, Vector4>(this));
        }

        #endregion

        #region Equals

        public static bool operator ==(in RgbaVector a, in RgbaVector b) => a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        public static bool operator !=(in RgbaVector a, in RgbaVector b) => !(a == b);

        public bool Equals(RgbaVector other) => this == other;
        public override bool Equals(object obj) => obj is RgbaVector other && Equals(other);

        #endregion

        #region Object Overrides

        /// <summary>
        /// Gets a <see cref="string"/> representation of the packed vector.
        /// </summary>
        public override string ToString() => nameof(RgbaVector) + $"(R:{R}, G:{G}, B:{B}, A:{A})";

        /// <summary>
        /// Gets a hash code of the packed vector.
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(R, G, B, A);

        #endregion
    }
}
