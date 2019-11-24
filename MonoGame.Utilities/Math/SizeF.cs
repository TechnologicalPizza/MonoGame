﻿using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace MonoGame.Framework
{
    /// <summary>
    ///     A two dimensional size defined by two real numbers, a width and a height.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A size is a subspace of two-dimensional space, the area of which is described in terms of a two-dimensional
    ///         coordinate system, given by a reference point and two coordinate axes.
    ///     </para>
    /// </remarks>
    /// <seealso cref="IEquatable{T}" />
    [DataContract]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct SizeF : IEquatable<SizeF>
    {
        /// <summary>
        ///     Returns a <see cref="SizeF" /> with <see cref="Width" /> and <see cref="Height" /> equal to <c>0f</c>.
        /// </summary>
        public static readonly SizeF Empty = new SizeF();

        /// <summary>
        ///     The horizontal component of this <see cref="SizeF" />.
        /// </summary>
        [DataMember] public float Width;

        /// <summary>
        ///     The vertical component of this <see cref="SizeF" />.
        /// </summary>
        [DataMember] public float Height;

        /// <summary>
        ///     Gets a value that indicates whether this <see cref="SizeF" /> is empty.
        /// </summary>
        public bool IsEmpty => (Width == 0) && (Height == 0);

        private string DebuggerDisplay => $"Width = {Width}, Height = {Height}";

        /// <summary>
        ///     Initializes a new instance of the <see cref="SizeF" /> structure from the specified dimensions.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public SizeF(float width, float height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets a <see cref="Vector2"/> representation for this object.
        /// </summary>
        public readonly Vector2 ToVector2() => new Vector2(Width, Height);

        /// <summary>
        /// Gets a <see cref="Size"/> representation for this object.
        /// </summary>
        public readonly Size ToSize() => new Size((int)Width, (int)Height);

        /// <summary>
        ///     Calculates the <see cref="SizeF" /> representing the vector addition of two <see cref="SizeF" /> structures as if
        ///     they
        ///     were <see cref="Vector2" /> structures.
        /// </summary>
        /// <param name="a">The first size.</param>
        /// <param name="b">The second size.</param>
        /// <returns>
        ///     The <see cref="SizeF" /> representing the vector addition of two <see cref="SizeF" /> structures as if they
        ///     were <see cref="Vector2" /> structures.
        /// </returns>
        public static SizeF operator +(in SizeF a, in SizeF b) => new SizeF(a.Width + b.Width, a.Height + b.Height);

        /// <summary>
        ///     Calculates the <see cref="SizeF" /> representing the vector addition of two <see cref="SizeF" /> structures.
        /// </summary>
        /// <param name="a">The first size.</param>
        /// <param name="b">The second size.</param>
        /// <returns>
        ///     The <see cref="SizeF" /> representing the vector addition of two <see cref="SizeF" /> structures.
        /// </returns>
        public static SizeF Add(SizeF a, SizeF b) => a + b;

        /// <summary>
        /// Calculates the <see cref="SizeF" /> representing the vector subtraction of two <see cref="SizeF" /> structures.
        /// </summary>
        /// <param name="left">The first size.</param>
        /// <param name="right">The second size.</param>
        /// <returns>
        ///     The <see cref="SizeF" /> representing the vector subtraction of two <see cref="SizeF" /> structures.
        /// </returns>
        public static SizeF operator -(SizeF left, SizeF right) => new SizeF(
            left.Width - right.Width, left.Height - right.Height);

        public static SizeF operator /(SizeF size, float value) => new SizeF(size.Width / value, size.Height / value);

        public static SizeF operator *(SizeF size, float value) => new SizeF(size.Width * value, size.Height * value);

        /// <summary>
        ///     Calculates the <see cref="SizeF" /> representing the vector subtraction of two <see cref="SizeF" /> structures.
        /// </summary>
        /// <param name="left">The first size.</param>
        /// <param name="right">The second size.</param>
        /// <returns>
        ///     The <see cref="SizeF" /> representing the vector subtraction of two <see cref="SizeF" /> structures.
        /// </returns>
        public static SizeF Subtract(SizeF left, SizeF right) => left - right;

        /// <summary>
        ///     Performs an implicit conversion from a <see cref="PointF" /> to a <see cref="SizeF" />.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        ///     The resulting <see cref="SizeF" />.
        /// </returns>
        public static implicit operator SizeF(PointF point) => new SizeF(point.X, point.Y);

        /// <summary>
        ///     Performs an implicit conversion from a <see cref="Point" /> to a <see cref="SizeF" />.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        ///     The resulting <see cref="SizeF" />.
        /// </returns>
        public static implicit operator SizeF(Point point) => new SizeF(point.X, point.Y);

        /// <summary>
        ///     Performs an implicit conversion from a <see cref="PointF" /> to a <see cref="SizeF" />.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>
        ///     The resulting <see cref="PointF" />.
        /// </returns>
        public static implicit operator PointF(SizeF size) => new PointF(size.Width, size.Height);

        /// <summary>
        ///     Performs an implicit conversion from a <see cref="SizeF" /> to a <see cref="Vector2" />.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>
        ///     The resulting <see cref="Vector2" />.
        /// </returns>
        public static implicit operator Vector2(SizeF size) => new Vector2(size.Width, size.Height);

        /// <summary>
        ///     Performs an implicit conversion from a <see cref="Vector2" /> to a <see cref="SizeF" />.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <returns>
        ///     The resulting <see cref="SizeF" />.
        /// </returns>
        public static implicit operator SizeF(Vector2 vector) => new SizeF(vector.X, vector.Y);

        /// <summary>
        ///     Performs an implicit conversion from a <see cref="SizeF" /> to a <see cref="Size" />.
        /// </summary>
        /// <param name="size">The vector.</param>
        /// <returns>
        ///     The resulting <see cref="SizeF" />.
        /// </returns>
        public static explicit operator Size(SizeF size) => size.ToSize();

        /// <summary>
        ///     Performs an explicit conversion from a <see cref="SizeF" /> to a <see cref="Point" />.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>
        ///     The resulting <see cref="Point" />.
        /// </returns>
        public static explicit operator Point(SizeF size) => new Point((int)size.Width, (int)size.Height);

        /// <summary>
        ///     Compares two <see cref="SizeF" /> structures. The result specifies
        ///     whether the values of the <see cref="Width" /> and <see cref="Height" />
        ///     fields of the two <see cref="PointF" /> structures are equal.
        /// </summary>
        public static bool operator ==(in SizeF a, in SizeF b) => a.Width == b.Width && a.Height == b.Height;

        /// <summary>
        ///     Compares two <see cref="SizeF" /> structures. The result specifies
        ///     whether the values of the <see cref="Width" /> or <see cref="Height" />
        ///     fields of the two <see cref="SizeF" /> structures are unequal.
        /// </summary>
        public static bool operator !=(in SizeF a, in SizeF b) => !(a == b);

        /// <summary>
        ///     Indicates whether this <see cref="SizeF" /> is equal to another <see cref="SizeF" />.
        /// </summary>
        public bool Equals(SizeF other) => this == other;

        /// <summary>
        ///     Returns a value indicating whether this <see cref="SizeF" /> is equal to a specified object.
        /// </summary>
        public override bool Equals(object obj) => obj is SizeF other && Equals(other);

        /// <summary>
        ///     Returns a <see cref="string" /> that represents this <see cref="SizeF" />.
        /// </summary>
        public override string ToString() => $"Width: {Width}, Height: {Height}";

        /// <summary>
        ///     Returns a hash code of this <see cref="SizeF" />.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 7 + Width.GetHashCode();
                return hash * 31 + Height.GetHashCode();
            }
        }
    }
}