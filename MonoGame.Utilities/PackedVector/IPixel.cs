using MonoGame.Framework;

namespace MonoGame.Utilities.PackedVector
{
    /// <summary>
    /// Base interface for pixels, defining the operations to be implemented by a pixel type.
    /// </summary>
    public interface IPixel : IPackedVector
    {
        /// <summary>Sets the pixel value from <see cref="Gray8"/>.</summary>
        void FromGray8<T>(Gray8 source);

        /// <summary>Sets the pixel value from <see cref="Gray16"/>.</summary>
        void FromGray16<T>(Gray16 source);

        /// <summary>Sets the pixel value from <see cref="Rgb24"/>.</summary>
        void FromRgb24(Rgb24 source);

        /// <summary>Sets the pixel value from <see cref="Color"/>.</summary>
        void FromColor(Color source);

        /// <summary>Sets the pixel instance from <see cref="Rgb48"/>.</summary>
        void FromRgb48(Rgb48 source);

        /// <summary>Sets the pixel instance from an <see cref="Rgba64"/>.</summary>
        void FromRgba64(Rgba64 source);

        /// <summary>
        /// Converts the pixel value into <see cref="Color"/> representation.
        /// </summary>
        /// <param name="destination">The destination reference for the <see cref="Color"/>.</param>
        void ToColor(ref Color destination);
    }
}
