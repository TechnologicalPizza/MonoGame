﻿using System.IO;
using System.Threading;
using MonoGame.Utilities.PackedVector;

namespace MonoGame.Imaging.Encoding
{
    /// <summary>
    /// Represents a progress update for image encoding.
    /// </summary>
    /// <typeparam name="TPixel"></typeparam>
    /// <param name="frameIndex"></param>
    /// <param name="frames"></param>
    /// <param name="percentage"></param>
    public delegate void EncodeProgressCallback<TPixel>(
        int frameIndex, ReadOnlyFrameCollection<TPixel> frames, double percentage)
        where TPixel : unmanaged, IPixel;

    /// <summary>
    /// Encapsulates encoding of image frames to a stream.
    /// </summary>
    public interface IImageEncoder : IImageCoder
    {
        /// <summary>
        /// Gets the default configuration for this encoder.
        /// </summary>
        EncoderConfig DefaultConfig { get; }

        /// <summary>
        /// Encodes a collection of frames to a stream.
        /// </summary>
        /// <typeparam name="TPixel">The pixel type of the frame collection.</typeparam>
        /// <param name="frames">The collection of frames to encode.</param>
        /// <param name="stream">The stream to output to.</param>
        /// <param name="encoderConfig">The encoder configuration.</param>
        /// <param name="imagingConfig">The imaging configuration.</param>
        /// <param name="onProgress">Optional delegate for reporting encode progress.</param>
        void Encode<TPixel>(
            ReadOnlyFrameCollection<TPixel> frames,
            Stream stream,
            EncoderConfig encoderConfig,
            ImagingConfig imagingConfig,
            CancellationToken cancellationToken,
            EncodeProgressCallback<TPixel> onProgress = null)
            where TPixel : unmanaged, IPixel;
    }
}