﻿using System;
using System.IO;
using MonoGame.Imaging.Pixels;
using static StbSharp.ImageWrite;

namespace MonoGame.Imaging.Coding.Encoding
{
    public abstract partial class StbImageEncoderBase : IImageEncoder
    {
        public abstract ImageFormat Format { get; }
        public virtual EncoderOptions DefaultOptions => EncoderOptions.Default;

        CodecOptions IImageCodec.DefaultOptions => DefaultOptions;

        protected abstract bool Write(StbImageEncoderState encoderState, in WriteState writeState);

        public ImageEncoderState CreateState(
            ImagingConfig imagingConfig,
            Stream stream)
        {
            // TODO: do something about leaveOpen
            return new StbImageEncoderState(this, imagingConfig, stream, leaveOpen: true);
        }

        public bool Encode(
            ImageEncoderState encoderState,
            IReadOnlyPixelRows image)
        {
            if (encoderState == null) throw new ArgumentNullException(nameof(encoderState));
            if (image == null) throw new ArgumentNullException(nameof(image));

            var state = (StbImageEncoderState)encoderState;

            int components = 4; // TODO: change this so it's dynamic/controlled
            var provider = new PixelRowProvider(image, components, 8);

            var writeState = new WriteState(
                getPixelByteRow: provider.GetRow,
                getPixelFloatRow: provider.GetRow,
                (data) => state.Stream.Write(data),
                state.ProgressCallback,
                image.Size.Width,
                image.Size.Height,
                components,
                state.CancellationToken,
                state.ScratchBuffer);

            return Write(state, writeState);
        }
    }
}