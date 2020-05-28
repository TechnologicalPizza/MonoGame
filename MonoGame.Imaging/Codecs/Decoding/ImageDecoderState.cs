﻿using System.IO;
using System.Threading;
using MonoGame.Framework;
using MonoGame.Framework.Vector;

namespace MonoGame.Imaging.Codecs.Decoding
{
    /// <summary>
    /// Represents a progress update for image decoding.
    /// </summary>
    public delegate void DecodeProgressCallback(
        ImageDecoderState decoderState,
        double percentage,
        Rectangle? rectangle);

    public abstract class ImageDecoderState : ImageCodecState
    {
        public event DecodeProgressCallback? Progress;

        public Image? CurrentImage { get; protected set; }
        public VectorTypeInfo? PreferredPixelType { get; set; }

        /// <summary>
        /// Gets the decoder that the state originates from.
        /// </summary>
        public IImageDecoder Decoder => (IImageDecoder)Codec;

        public bool HasProgressListener => Progress != null;

        public DecoderOptions? DecoderOptions
        {
            get => (DecoderOptions?)CodecOptions;
            set => CodecOptions = value;
        }

        public ImageDecoderState(
            IImageDecoder decoder,
            IImagingConfig config,
            Stream stream,
            bool leaveOpen,
            CancellationToken cancellationToken) :
            base(decoder, config, stream, leaveOpen, cancellationToken)
        {
        }

        protected void InvokeProgress(double percentage, Rectangle? rectangle)
        {
            Progress?.Invoke(this, percentage, rectangle);
        }
    }
}