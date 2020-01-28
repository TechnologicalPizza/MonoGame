// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoGame.Framework.PackedVector
{
    /// <summary>
    /// Packed vector type containing signed 16-bit XY components.
    /// <para>
    /// Ranges from [-1, -1, 0, 1] to [1, 1, 0, 1] in vector form.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NormalizedShort2 : IPackedVector<uint>, IEquatable<NormalizedShort2>
    {
        public short X;
        public short Y;

        #region Constructors

        public NormalizedShort2(short x, short y)
        {
            X = x;
            Y = y;
        }

        [CLSCompliant(false)]
        public NormalizedShort2(uint packed) : this() => PackedValue = packed;

        public NormalizedShort2(Vector2 vector) => this = Pack(vector);

        public NormalizedShort2(float x, float y) : this(new Vector2(x, y))
        {
        }

        #endregion

        private static NormalizedShort2 Pack(Vector2 vector)
        {
            vector = Vector2.Clamp(vector, -Vector2.One, Vector2.One);
            vector *= 32767f;

            return new NormalizedShort2((short)vector.X, (short)vector.Y);
        }

        /// <inheritdoc/>
        public readonly Vector2 ToVector2() => new Vector2(X, Y) / 32767f;

        #region IPackedVector

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public uint PackedValue
        {
            get => Unsafe.As<NormalizedShort2, uint>(ref this);
            set => Unsafe.As<NormalizedShort2, uint>(ref this) = value;
        }

        /// <inheritdoc/>
        public void FromVector4(Vector4 vector) => this = Pack(vector.ToVector2());

        /// <inheritdoc/>
        public readonly Vector4 ToVector4() => new Vector4(ToVector2(), 0, 1);

        #endregion

        #region Equals

        public static bool operator ==(in NormalizedShort2 a, in NormalizedShort2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(in NormalizedShort2 a, in NormalizedShort2 b) => !(a == b);

        public override bool Equals(object obj) => obj is NormalizedShort2 other && Equals(other);
        public bool Equals(NormalizedShort2 other) => this == other;

        #endregion

        #region Object Overrides

        public override string ToString() => $"NormalizedShort2({X}, {Y})";

        public override int GetHashCode() => PackedValue.GetHashCode();

        #endregion
    }
}
