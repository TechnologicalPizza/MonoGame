﻿using System.IO;
using System.Threading;
using MonoGame.Framework.Memory;
using MonoGame.Imaging.Pixels;
using MonoGame.Imaging.Utilities;
using StbSharp;
using static StbSharp.ImageWrite;

namespace MonoGame.Imaging.Coders.Encoding
{
    public class StbImageEncoderState : ImageEncoderState
    {
        public WriteProgressCallback ProgressCallback { get; }
        private byte[]? Buffer { get; set; }

        public new IReadOnlyPixelRows? CurrentImage { get => base.CurrentImage; set => base.CurrentImage = value; }
        public new int FrameIndex { get => base.FrameIndex; set => base.FrameIndex = value; }

        public StbImageEncoderState(
            IImageEncoder encoder,
            IImagingConfig config,
            Stream stream,
            bool leaveOpen,
            CancellationToken cancellationToken) :
            base(encoder, config, stream, leaveOpen, cancellationToken)
        {
            ProgressCallback = (progress, rect) => InvokeProgress(progress, rect?.ToMGRect());
            Buffer = RecyclableMemoryManager.Default.GetBlock();
        }

        public WriteState<TPixelRowProvider> CreateWriteState<TPixelRowProvider>(TPixelRowProvider provider)
            where TPixelRowProvider : IPixelRowProvider
        {
            AssertNotDisposed();

            return new WriteState<TPixelRowProvider>(
                Stream,
                Buffer!,
                provider,
                ProgressCallback,
                CancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            RecyclableMemoryManager.Default.ReturnBlock(Buffer);
            Buffer = null;

            base.Dispose(disposing);
        }
    }
}