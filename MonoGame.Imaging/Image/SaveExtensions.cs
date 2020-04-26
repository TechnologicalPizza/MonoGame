﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MonoGame.Framework;
using MonoGame.Imaging.Attributes.Format;
using MonoGame.Imaging.Coding.Encoding;
using MonoGame.Imaging.Pixels;

namespace MonoGame.Imaging
{
    public static partial class SaveExtensions
    {
        public static void Save(
            this IEnumerable<IReadOnlyPixelRows> images,
            ImagingConfig imagingConfig,
            Stream output,
            ImageFormat format,
            EncoderOptions encoderOptions = null,
            CancellationToken cancellationToken = default,
            EncodeProgressCallback onProgress = null)
        {
            AssertValidOutput(output);

            var encoder = AssertValidArguments(imagingConfig, format);
            if (encoder == null) throw new ArgumentNullException(nameof(encoder));
            if (imagingConfig == null) throw new ArgumentNullException(nameof(imagingConfig));
            if (images == null) throw new ArgumentNullException(nameof(images));

            using (ImageEncoderState state = encoder.CreateState(imagingConfig, output))
            {
                state.EncoderOptions = encoderOptions;
                state.CancellationToken = cancellationToken;
                state.Progress += onProgress;

                foreach (var image in images)
                {
                    encoder.Encode(state, image);

                    if (!(format is IAnimatedFormatAttribute && encoder is IAnimatedFormatAttribute))
                        return;
                }
            }
        }

        public static void Save(
            this IEnumerable<IReadOnlyPixelRows> images,
            Stream output,
            ImageFormat format,
            EncoderOptions encoderOptions = null,
            CancellationToken cancellationToken = default,
            EncodeProgressCallback onProgress = null)
        {
            Save(
                images, ImagingConfig.Default, output, format,
                encoderOptions, cancellationToken, onProgress);
        }

        public static void Save(
            this IEnumerable<IReadOnlyPixelRows> images,
            ImagingConfig imagingConfig,
            string filePath,
            ImageFormat format = null,
            EncoderOptions encoderOptions = null,
            CancellationToken cancellationToken = default,
            EncodeProgressCallback onProgress = null)
        {
            if (format == null)
                format = ImageFormat.GetByPath(filePath)[0];

            using (var outputStream = OpenWrite(filePath))
                Save(
                    images, imagingConfig, outputStream, format,
                    encoderOptions, cancellationToken, onProgress);
        }

        public static void Save(
            this IEnumerable<IReadOnlyPixelRows> images,
            string filePath,
            ImageFormat format = null,
            EncoderOptions encoderOptions = null,
            CancellationToken cancellationToken = default,
            EncodeProgressCallback onProgress = null)
        {
            Save(
                images, ImagingConfig.Default, filePath, format,
                encoderOptions, cancellationToken, onProgress);
        }

        #region Save(Stream)

        public static void Save(
            this IReadOnlyPixelRows image,
            ImagingConfig imagingConfig,
            Stream output,
            ImageFormat format,
            EncoderOptions encoderOptions = null,
            CancellationToken cancellationToken = default,
            EncodeProgressCallback onProgress = null)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            AssertValidArguments(imagingConfig, format);
            AssertValidOutput(output);

            Save(
                new[] { image }, imagingConfig, output, format,
                encoderOptions, cancellationToken, onProgress);
        }

        public static void Save(
            this IReadOnlyPixelRows image,
            Stream output,
            ImageFormat format,
            EncoderOptions encoderOptions = null,
            CancellationToken cancellationToken = default,
            EncodeProgressCallback onProgress = null)
        {
            Save(
                image, ImagingConfig.Default, output, format,
                encoderOptions, cancellationToken, onProgress);
        }

        #endregion

        #region Save(FilePath)

        public static void Save(
            this IReadOnlyPixelRows image,
            ImagingConfig imagingConfig,
            string filePath,
            ImageFormat format = null,
            EncoderOptions encoderOptions = null,
            CancellationToken cancellationToken = default,
            EncodeProgressCallback onProgress = null)
        {
            if (format == null)
                format = ImageFormat.GetByPath(filePath)[0];
            AssertValidArguments(imagingConfig, format);
            AssertValidPath(filePath);

            Save(
                new[] { image }, imagingConfig, filePath, format,
                encoderOptions, cancellationToken, onProgress);
        }

        public static void Save(
            this IReadOnlyPixelRows image,
            string filePath,
            ImageFormat format = null,
            EncoderOptions encoderOptions = null,
            CancellationToken cancellationToken = default,
            EncodeProgressCallback onProgress = null)
        {
            Save(
                image, ImagingConfig.Default, filePath, format,
                encoderOptions, cancellationToken, onProgress);
        }

        #endregion

        public static FileStream OpenWrite(string filePath)
        {
            AssertValidPath(filePath);
            return new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        #region Argument Validation

        private static IImageEncoder AssertValidArguments(
            ImagingConfig imagingConfig, ImageFormat format)
        {
            if (imagingConfig == null) throw new ArgumentNullException(nameof(imagingConfig));
            if (format == null) throw new ArgumentNullException(nameof(format));

            return Image.GetEncoder(format);
        }

        private static void AssertValidOutput(Stream output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (!output.CanWrite)
                throw new ArgumentException("The stream is not writable.", nameof(output));
        }

        public static void AssertValidPath(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentEmptyException(nameof(filePath));

            Path.GetFullPath(filePath);
        }

        #endregion
    }
}