using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Serialization;

namespace MonoGame.Framework
{
    // Real-Time Collision Detection, Christer Ericson, 2005. Chapter 4.2;
    // Bounding Volumes - Axis-aligned Bounding Boxes (AABBs). pg 77 

    /// <summary>
    /// An axis-aligned, four sided, two dimensional box defined by a top-left position (<see cref="X" /> and
    /// <see cref="Y" />) and a size (<see cref="Width" /> and <see cref="Height" />).
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="RectangleF" /> is categorized by having its faces oriented in such a way that its
    /// face normals are at all times parallel with the axes of the given coordinate system.
    /// </para>
    /// <para>
    /// The bounding <see cref="RectangleF" /> of a rotated <see cref="RectangleF" /> will be equivalent or larger
    /// in size than the original depending on the angle of rotation.
    /// </para>
    /// </remarks>
    /// <seealso cref="IEquatable{T}" />
    [DataContract]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct RectangleF : IEquatable<RectangleF>
    {
        /// <summary>
        /// Gets a <see cref="RectangleF" /> with all values set to zero.
        /// </summary>
        public static RectangleF Empty => default;

        /// <summary>
        /// The x-coordinate of the top-left corner position of this <see cref="RectangleF" />.
        /// </summary>
        [DataMember]
        public float X;

        /// <summary>
        /// The y-coordinate of the top-left corner position of this <see cref="RectangleF" />.
        /// </summary>
        [DataMember]
        public float Y;

        /// <summary>
        /// The width of this <see cref="RectangleF" />.
        /// </summary>
        [DataMember]
        public float Width;

        /// <summary>
        /// The height of this <see cref="RectangleF" />.
        /// </summary>
        [DataMember]
        public float Height;

        /// <summary>
        /// Gets the x-coordinate of the left edge of this <see cref="RectangleF" />.
        /// </summary>
        public readonly float Left => X;

        /// <summary>
        /// Gets the x-coordinate of the right edge of this <see cref="RectangleF" />.
        /// </summary>
        public readonly float Right => X + Width;

        /// <summary>
        /// Gets the y-coordinate of the top edge of this <see cref="RectangleF" />.
        /// </summary>
        public readonly float Top => Y;

        /// <summary>
        /// Gets the y-coordinate of the bottom edge of this <see cref="RectangleF" />.
        /// </summary>
        public readonly float Bottom => Y + Height;

        /// <summary>
        /// Gets a value indicating whether this <see cref="RectangleF" /> has a <see cref="X" />, <see cref="Y" />,
        /// <see cref="Width" />,
        /// <see cref="Height" /> all equal to <code>0f</code>.
        /// </summary>
        public readonly bool IsEmpty => Width == 0 && Height == 0 && X == 0 && Y == 0;

        internal string DebuggerDisplay => string.Concat(
            X.ToString(), "  ",
            Y.ToString(), "  ",
            Width.ToString(), "  ",
            Height.ToString());

        /// <summary>
        /// Gets the <see cref="PointF" /> representing the the top-left of this <see cref="RectangleF" />.
        /// </summary>
        public PointF Position
        {
            readonly get => new PointF(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        /// Gets the <see cref="SizeF" /> representing the extents of this <see cref="RectangleF" />.
        /// </summary>
        public SizeF Size
        {
            readonly get => new SizeF(Width, Height);
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        /// Gets the <see cref="PointF" /> representing the center of this <see cref="RectangleF" />.
        /// </summary>
        public readonly PointF Center => new PointF(X + Width * 0.5f, Y + Height * 0.5f);

        /// <summary>
        /// Gets the <see cref="PointF" /> representing the top-left of this <see cref="RectangleF" />.
        /// </summary>
        public readonly PointF TopLeft => new PointF(X, Y);

        /// <summary>
        /// Gets the <see cref="PointF" /> representing the bottom-right of this <see cref="RectangleF" />.
        /// </summary>
        public readonly PointF BottomRight => new PointF(X + Width, Y + Height);

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleF" /> structure from the specified top-left xy-coordinate
        /// <see cref="float" />s, width <see cref="float" /> and height <see cref="float" />.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleF" /> structure from the specified top-left
        /// <see cref="PointF" /> and the extents <see cref="SizeF" />.
        /// </summary>
        /// <param name="position">The top-left point.</param>
        /// <param name="size">The extents.</param>
        public RectangleF(PointF position, SizeF size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.Width;
            Height = size.Height;
        }

        public RectangleF(Vector2 position, SizeF size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        /// Computes the <see cref="RectangleF" /> from a minimum <see cref="PointF" /> and maximum
        /// <see cref="PointF" />.
        /// </summary>
        /// <param name="minimum">The minimum point.</param>
        /// <param name="maximum">The maximum point.</param>
        /// <returns>The resulting <see cref="RectangleF" />.</returns>
        public static RectangleF CreateFrom(PointF minimum, PointF maximum)
        {
            return new RectangleF(
                minimum.X, minimum.Y,
                maximum.X - minimum.X,
                maximum.Y - minimum.Y);
        }

        /// <summary>
        /// Computes the <see cref="RectangleF" /> that contains the two specified
        /// <see cref="RectangleF" /> structures.
        /// </summary>
        /// <param name="first">The first rectangle.</param>
        /// <param name="second">The second rectangle.</param>
        /// <returns>
        /// An <see cref="RectangleF" /> that contains both the <paramref name="first" /> and the
        /// <paramref name="second" />.
        /// </returns>
        public static RectangleF Union(RectangleF first, RectangleF second)
        {
            float x = Math.Min(first.X, second.X);
            float y = Math.Min(first.Y, second.Y);

            return new RectangleF(
                x, y,
                Math.Max(first.Right, second.Right) - x,
                Math.Max(first.Bottom, second.Bottom) - y);
        }

        /// <summary>
        /// Computes the <see cref="RectangleF" /> that contains both the 
        /// specified <see cref="RectangleF" /> and this <see cref="RectangleF" />.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns>
        /// An <see cref="RectangleF" /> that contains both the <paramref name="rectangle" /> and
        /// this <see cref="RectangleF" />.
        /// </returns>
        public RectangleF Union(RectangleF rectangle)
        {
            return Union(this, rectangle);
        }

        /// <summary>
        /// Computes the <see cref="RectangleF" /> that is in common between the two specified
        /// <see cref="RectangleF" /> structures.
        /// </summary>
        /// <param name="a">The first rectangle.</param>
        /// <param name="b">The second rectangle.</param>
        /// <returns>
        /// A <see cref="RectangleF" /> that is in common between both the <paramref name="rectangle" /> and
        /// this <see cref="RectangleF"/>, if they intersect; otherwise, <see cref="Empty"/>.
        /// </returns>
        public static RectangleF Intersection(RectangleF a, RectangleF b)
        {
            var firstMinimum = a.TopLeft;
            var firstMaximum = a.BottomRight;
            var secondMinimum = b.TopLeft;
            var secondMaximum = b.BottomRight;

            var min = PointF.Max(firstMinimum, secondMinimum);
            var max = PointF.Min(firstMaximum, secondMaximum);

            if ((max.X < min.X) || (max.Y < min.Y))
                return Empty;
            else
                return CreateFrom(min, max);
        }

        /// <summary>
        /// Computes the <see cref="RectangleF" /> that is in common between the specified
        /// <see cref="RectangleF" /> and this <see cref="RectangleF" />.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        public RectangleF Intersection(RectangleF rectangle)
        {
            return Intersection(this, rectangle);
        }

        /// <summary>
        /// Determines whether the two specified <see cref="RectangleF" /> structures intersect.
        /// </summary>
        /// <param name="a">The first rectangle.</param>
        /// <param name="b">The second rectangle.</param>
        /// <returns>
        /// <see langword="true"/> if the <paramref name="a" /> intersects with the <paramref name="b" />;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool Intersects(RectangleF a, RectangleF b)
        {
            return a.X < b.X + b.Width
                && a.X + a.Width > b.X
                && a.Y < b.Y + b.Height
                && a.Y + a.Height > b.Y;
        }

        /// <summary>
        /// Determines whether the specified <see cref="RectangleF" /> intersects with this
        /// <see cref="RectangleF" />.
        /// </summary>
        /// <param name="rectangle">The bounding rectangle.</param>
        /// <returns>
        /// <see langword="true"/> if the <paramref name="rectangle" /> intersects with this
        /// <see cref="RectangleF" />; otherwise, <see langword="false"/>.
        /// </returns>
        public readonly bool Intersects(RectangleF rectangle)
        {
            return Intersects(this, rectangle);
        }

        /// <summary>
        /// Determines whether the specified <see cref="RectangleF" /> contains the specified
        /// <see cref="PointF" />.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <param name="point">The point.</param>
        /// <returns>
        /// <see langword="true"/> if the <paramref name="rectangle" /> contains the <paramref name="point" />; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool Contains(RectangleF rectangle, PointF point)
        {
            return rectangle.X <= point.X
                && point.X < rectangle.X + rectangle.Width
                && rectangle.Y <= point.Y
                && point.Y < rectangle.Y + rectangle.Height;
        }

        /// <summary>
        /// Determines whether this <see cref="RectangleF" /> contains the specified
        /// <see cref="PointF" />.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        /// <see langword="true"/> if the this <see cref="RectangleF"/> contains the <paramref name="point" />; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public readonly bool Contains(PointF point)
        {
            return Contains(this, point);
        }

        /// <summary>
        /// Determines whether this <see cref="RectangleF" /> contains the specified
        /// <see cref="RectangleF" />.
        /// </summary>
        public readonly bool Contains(RectangleF value)
        {
            return X < value.X
                && value.X + value.Width <= X + Width
                && Y < value.Y
                && value.Y + value.Height <= Y + Height;
        }

        /// <summary>
        /// Computes the squared distance from this <see cref="RectangleF"/> to a <see cref="PointF"/>.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        /// The squared distance from this <see cref="RectangleF"/> to the <paramref name="point"/>.
        /// </returns>
        public readonly float SquaredDistanceTo(PointF point)
        {
            // Real-Time Collision Detection, Christer Ericson, 2005. Chapter 5.1.3.1;
            // Basic Primitive Tests - Closest-point Computations - Distance of Point to AABB.  pg 130-131
            var squaredDistance = 0f;
            var minimum = TopLeft;
            var maximum = BottomRight;

            // for each axis add up the excess distance outside the box

            // x-axis
            if (point.X < minimum.X)
            {
                var distance = minimum.X - point.X;
                squaredDistance += distance * distance;
            }
            else if (point.X > maximum.X)
            {
                var distance = maximum.X - point.X;
                squaredDistance += distance * distance;
            }

            // y-axis
            if (point.Y < minimum.Y)
            {
                var distance = minimum.Y - point.Y;
                squaredDistance += distance * distance;
            }
            else if (point.Y > maximum.Y)
            {
                var distance = maximum.Y - point.Y;
                squaredDistance += distance * distance;
            }
            return squaredDistance;
        }

        /// <summary>
        /// Computes the distance from this <see cref="RectangleF"/> to a <see cref="PointF"/>.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>The distance from this <see cref="RectangleF"/> to the <paramref name="point"/>.</returns>
        public readonly float DistanceTo(PointF point)
        {
            return MathF.Sqrt(SquaredDistanceTo(point));
        }

        public void Inflate(float horizontalAmount, float verticalAmount)
        {
            X -= horizontalAmount;
            Y -= verticalAmount;
            Width += horizontalAmount * 2;
            Height += verticalAmount * 2;
        }

        public void Inflate(Vector2 amount)
        {
            Inflate(amount.X, amount.Y);
        }

        public void Offset(float offsetX, float offsetY)
        {
            X += offsetX;
            Y += offsetY;
        }

        public void Offset(Vector2 offset)
        {
            Offset(offset.X, offset.Y);
        }

        public static RectangleF operator +(RectangleF origin, Vector2 offset)
        {
            return new RectangleF(
                origin.X + offset.X,
                origin.Y + offset.Y,
                origin.Width,
                origin.Height);
        }

        public static RectangleF operator -(RectangleF origin, Vector2 offset)
        {
            return new RectangleF(
                origin.X - offset.X,
                origin.Y - offset.Y,
                origin.Width,
                origin.Height);
        }

        public static RectangleF operator +(RectangleF a, RectangleF b)
        {
            return new RectangleF(
                a.X + b.X,
                a.Y + b.Y,
                a.Width + b.Width,
                a.Height + b.Height);
        }

        public static RectangleF operator -(RectangleF a, RectangleF b)
        {
            return new RectangleF(
                a.X - b.X,
                a.Y - b.Y,
                a.Width - b.Width,
                a.Height - b.Height);
        }

        public readonly Rectangle ToRectangle()
        {
            return new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
        }

        #region Equals

        /// <summary>
        /// Indicates whether this <see cref="RectangleF" /> is equal to another <see cref="RectangleF" />.
        /// </summary>
        public readonly bool Equals(RectangleF rectangle) => rectangle == this;

        /// <summary>
        /// Returns a value indicating whether this <see cref="RectangleF" /> is equal to a specified object.
        /// </summary>
        public override readonly bool Equals(object? obj) => obj is RectangleF rect && Equals(rect);

        /// <summary>
        /// Compares two <see cref="RectangleF" /> structures. The result specifies whether the values of the
        /// <see cref="X" />, <see cref="Y"/>, <see cref="Width"/> and <see cref="Height" /> fields of the two <see cref="RectangleF" /> structures
        /// are equal.
        /// </summary>
        public static bool operator ==(RectangleF a, RectangleF b)
        {
            return a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
        }

        /// <summary>
        /// Compares two <see cref="RectangleF" /> structures. The result specifies whether the values of the
        /// <see cref="X" />, <see cref="Y"/>, <see cref="Width"/> and <see cref="Height" /> fields of the two <see cref="RectangleF" /> structures
        /// are unequal.
        /// </summary>
        public static bool operator !=(RectangleF first, RectangleF second)
        {
            return !(first == second);
        }

        #endregion

        #region Object overrides

        /// <summary>
        /// Returns a hash code of this <see cref="RectangleF" />.
        /// </summary>
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        /// <summary>
        /// Returns a <see cref="string" /> that represents this <see cref="RectangleF" />.
        /// </summary>
        /// <returns>A <see cref="string" /> that represents this <see cref="RectangleF" />.</returns>
        public override readonly string ToString()
        {
            return $"{{X:{X}, Y:{Y}, W:{Width}, H:{Height}}}";
        }

        #endregion

        public static implicit operator RectangleF(Rectangle rectangle)
        {
            return new RectangleF(
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                rectangle.Height);
        }

        public static explicit operator Rectangle( RectangleF rectangle)
        {
            return new Rectangle(
                (int)rectangle.X,
                (int)rectangle.Y,
                (int)rectangle.Width, 
                (int)rectangle.Height);
        }
    }
}