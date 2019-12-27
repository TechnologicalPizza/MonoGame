﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MonoGame.Framework;
using MonoGame.Imaging.Attributes;
using MonoGame.Imaging.Attributes.Format;
using MonoGame.Utilities.Collections;

namespace MonoGame.Imaging
{
    [DebuggerDisplay("{ToString(),nq}")]
    public class ImageFormat : IImageFormatAttribute
    {
        private static HashSet<ImageFormat> _formats;
        private static HashSet<ImageFormat> _builtinFormats;
        private static Dictionary<string, ImageFormat> _byMimeType;
        private static Dictionary<string, ImageFormat> _byExtension;

        #region Built-in Formats

        /// <summary>
        /// Gets the "Portable Network Graphics" format.
        /// </summary>
        public static ImageFormat Png { get; }

        /// <summary> 
        /// Gets the "Joint Photographic Experts Group" (i.e JPEG) format. 
        /// </summary>
        public static ImageFormat Jpeg { get; }

        /// <summary>
        /// Gets the "Graphics Interchange Format".
        /// </summary>
        public static ImageFormat Gif { get; }

        /// <summary>
        /// Gets the "Bitmap" format.
        /// </summary>
        public static ImageFormat Bmp { get; }

        /// <summary>
        /// Gets the "Truevision Graphics Adapter" format.
        /// </summary>
        public static ImageFormat Tga { get; }

        /// <summary>
        /// Gets the "RGBE" format (also known as "Radiance HDR").
        /// </summary>
        public static ImageFormat Rgbe { get; }

        /// <summary>
        /// Gets the "PhotoShop Document" format.
        /// </summary>
        public static ImageFormat Psd { get; }

        #endregion

        // TODO: add coder priority so the user can implement
        // an alternative coder in place of an existing one

        #region Static Constructor

        static ImageFormat()
        {
            _formats = new HashSet<ImageFormat>();
            _builtinFormats = new HashSet<ImageFormat>();
            _byExtension = new Dictionary<string, ImageFormat>(StringComparer.OrdinalIgnoreCase);
            _byMimeType = new Dictionary<string, ImageFormat>(StringComparer.OrdinalIgnoreCase);

            Png = AddBuiltIn("Portable Network Graphics", "PNG", new[] { "image/png" }, new[] { ".png" });
            Jpeg = AddBuiltIn("Joint Photographic Experts Group", "JPEG", new[] { "image/jpeg" }, new[] { ".jpeg", ".jpg", ".jpe", ".jfif", ".jif" });
            Gif = AddBuiltIn("Graphics Interchange Format", "GIF", new[] { "image/gif" }, new[] { ".gif" }, new[] { typeof(IAnimatedFormatAttribute) });
            Bmp = AddBuiltIn("Bitmap", "BMP", new[] { "image/bmp", "image/x-bmp" }, new[] { ".bmp", ".bm" });
            Tga = AddBuiltIn("Truevision Graphics Adapter", "TGA", new[] { "image/x-tga", "image/x-targa" }, new[] { ".tga", ".targa" });
            Rgbe = AddBuiltIn("Radiance HDR", "RGBE", new[] { "image/vnd.radiance", "image/x-hdr" }, new[] { ".hdr", ".rgbe" });
            Psd = AddBuiltIn("PhotoShop Document", "PSD", new[] { "image/vnd.adobe.photoshop", "application/x-photoshop" }, new[] { ".psd" }, new[] { typeof(ILayeredFormatAttribute) });
        }

        private static ImageFormat AddBuiltIn(
            string fullName, string name, string[] mimeTypes, string[] extensions, Type[] attributes = null)
        {
            var mimeSet = new ReadOnlySet<string>(mimeTypes, StringComparer.OrdinalIgnoreCase);
            var extensionSet = new ReadOnlySet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            var attributeSet = new ReadOnlySet<Type>(attributes ?? Array.Empty<Type>());
            var format = new ImageFormat(fullName, name, mimeTypes[0], extensions[0], mimeSet, extensionSet, attributeSet);

            _builtinFormats.Add(format);
            AddFormat(format);
            return format;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the full name of the format.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the short name of the format, often used for the extension.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the primary MIME type associated with the format.
        /// </summary>
        public string MimeType { get; }

        /// <summary>
        /// Gets the primary file extension associated with the format.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Gets MIME types associated with the format.
        /// </summary>
        public ReadOnlySet<string> MimeTypes { get; }

        /// <summary>
        /// Gets file extensions associated with the format.
        /// </summary>
        public ReadOnlySet<string> Extensions { get; }

        /// <summary>
        /// Gets the image format attributes. These derive from <see cref="IImageFormatAttribute"/>.
        /// </summary>
        public ReadOnlySet<Type> Attributes { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// </summary>
        /// <param name="fullName">The full name of the format.</param>
        /// <param name="name">The short name of the format.</param>
        /// <param name="primaryMimeType"></param>
        /// <param name="primaryExtension"></param>
        /// <param name="mimeTypes"></param>
        /// <param name="extensions"></param>
        /// <param name="attributes"></param>
        public ImageFormat(
            string fullName, string name, string primaryMimeType, string primaryExtension,
            IReadOnlySet<string> mimeTypes,
            IReadOnlySet<string> extensions,
            IReadOnlySet<Type> attributes)
        {
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MimeType = primaryMimeType ?? throw new ArgumentNullException(nameof(primaryMimeType));
            Extension = ValidateExtension(primaryExtension) ?? throw new ArgumentNullException(nameof(primaryExtension));

            if (mimeTypes == null) throw new ArgumentNullException(nameof(mimeTypes));
            if (mimeTypes.Count == 0) throw new ArgumentEmptyException(nameof(mimeTypes));
            if (!mimeTypes.Contains(primaryMimeType))
                throw new ArgumentException("The set doesn't contain the primary MIME type.", nameof(mimeTypes));

            if (extensions == null) throw new ArgumentNullException(nameof(extensions));
            if (extensions.Count == 0) throw new ArgumentEmptyException(nameof(extensions));
            if (!extensions.Contains(primaryExtension))
                throw new ArgumentException("The set doesn't contain the primary extension.", nameof(mimeTypes));

            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            foreach (var type in attributes)
                if (!typeof(IImageFormatAttribute).IsAssignableFrom(type))
                    throw new ArgumentException(
                        "The attribute set contains types that don't derive from " + typeof(IImageFormatAttribute) + ".",
                        nameof(attributes));

            MimeTypes = new ReadOnlySet<string>(mimeTypes, StringComparer.OrdinalIgnoreCase);
            Extensions = new ReadOnlySet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            Attributes = new ReadOnlySet<Type>(attributes);
        }
        
        public ImageFormat(
            string fullName, string name, string mimeType, string extension, IReadOnlySet<Type> attributes) : 
            this(
                fullName, name, mimeType, extension,
                new ReadOnlySet<string>(new[] { mimeType }),
                new ReadOnlySet<string>(new[] { extension }),
                attributes)
        {
        }

        private static string ValidateExtension(string extension)
        {
            if (extension == null)
                return null;

            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentEmptyException(nameof(extension));

            if (extension[0] == '.')
                return extension;
            return "." + extension;
        }

        #endregion

        #region Custom Format Methods

        public static void AddFormat(ImageFormat format)
        {
            if (_formats.Contains(format))
                throw new ArgumentException("The format has already been added.", nameof(format));

            _formats.Add(format);
            foreach (var mime in format.MimeTypes)
                _byMimeType.Add(mime.ToLower(), format);
            foreach (var ext in format.Extensions)
                _byExtension.Add(ext.ToLower(), format);
        }

        #endregion

        #region Format Getters

        /// <summary>
        /// Gets whether the format comes with the imaging library.
        /// </summary>
        public static bool IsBuiltIn(ImageFormat format)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            return _builtinFormats.Contains(format);
        }

        public static bool TryGetByMimeType(string mimeType, out ImageFormat format)
        {
            return _byMimeType.TryGetValue(mimeType, out format);
        }

        public static bool TryGetByExtension(string extension, out ImageFormat format)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            if (extension.Length > 0 && !extension.StartsWith("."))
                extension = "." + extension;

            return _byExtension.TryGetValue(extension, out format);
        }

        public static ImageFormat GetByExtension(string extension)
        {
            if (TryGetByExtension(extension, out var format))
                return format;

            throw new KeyNotFoundException(
                $"Image format for extension '{extension}' is not defined.");
        }

        public static bool TryGetByPath(string path, out ImageFormat format)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            string extension = Path.GetExtension(path);
            return TryGetByExtension(extension, out format);
        }

        public static ImageFormat GetByPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            string extension = Path.GetExtension(path);
            return GetByExtension(extension);
        }

        #endregion

        public override string ToString()
        {
            return $"{{Name: \"{FullName}\", Extension: \"{Extension}\", MIME: \"{MimeType}\"}}";
        }
    }
}