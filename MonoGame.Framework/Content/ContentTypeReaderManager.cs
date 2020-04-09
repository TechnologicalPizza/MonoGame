// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace MonoGame.Framework.Content
{
    public sealed class ContentTypeReaderManager
    {
        private static readonly object _syncRoot;
        private static readonly Dictionary<Type, ContentTypeReader> _contentReadersCache;
        private static readonly string _assemblyName;

        private Dictionary<Type, ContentTypeReader> _contentReaders;

        static ContentTypeReaderManager()
        {
            _syncRoot = new object();
            _contentReadersCache = new Dictionary<Type, ContentTypeReader>(255);
            _assemblyName = typeof(ContentTypeReaderManager).Assembly.FullName;
        }

        public ContentTypeReader GetTypeReader(Type targetType)
        {
            if (targetType.IsArray && targetType.GetArrayRank() > 1)
                targetType = typeof(Array);

            if (_contentReaders.TryGetValue(targetType, out ContentTypeReader reader))
                return reader;

            return null;
        }

        public ContentTypeReader GetTypeReader<T>()
        {
            return GetTypeReader(typeof(T));
        }

        // Trick to prevent the linker removing the code, but not actually execute the code
        static readonly bool falseflag = false;

        internal ContentTypeReader[] LoadAssetReaders(ContentReader reader)
        {
            // Trick to prevent the linker removing the code, but not actually execute the code
            if (falseflag)
            {
                // Dummy variables required for it to work on iDevices ** DO NOT DELETE ** 
                // This forces the classes not to be optimized out when deploying to iDevices
                var hByteReader = new ByteReader();
                var hSByteReader = new SByteReader();
                var hDateTimeReader = new DateTimeReader();
                var hDecimalReader = new DecimalReader();
                var hBoundingSphereReader = new BoundingSphereReader();
                var hBoundingFrustumReader = new BoundingFrustumReader();
                var hRayReader = new RayReader();
                var hCharListReader = new ListReader<char>();
                var hRectangleListReader = new ListReader<Rectangle>();
                var hRectangleArrayReader = new ArrayReader<Rectangle>();
                var hVector3ListReader = new ListReader<Vector3>();
                var hStringListReader = new ListReader<StringReader>();
                var hIntListReader = new ListReader<int>();
                var hSpriteFontReader = new SpriteFontReader();
                var hTexture2DReader = new Texture2DReader();
                var hCharReader = new CharReader();
                var hRectangleReader = new RectangleReader();
                var hStringReader = new StringReader();
                var hVector2Reader = new Vector2Reader();
                var hVector3Reader = new Vector3Reader();
                var hVector4Reader = new Vector4Reader();
                var hCurveReader = new CurveReader();
                var hIndexBufferReader = new IndexBufferReader();
                var hBoundingBoxReader = new BoundingBoxReader();
                var hMatrixReader = new MatrixReader();
                var hBasicEffectReader = new BasicEffectReader();
                var hVertexBufferReader = new VertexBufferReader();
                var hAlphaTestEffectReader = new AlphaTestEffectReader();
                var hEnumSpriteEffectsReader = new EnumReader<Graphics.SpriteEffects>();
                var hArrayFloatReader = new ArrayReader<float>();
                var hArrayVector2Reader = new ArrayReader<Vector2>();
                var hListVector2Reader = new ListReader<Vector2>();
                var hArrayMatrixReader = new ArrayReader<Matrix>();
                var hEnumBlendReader = new EnumReader<Graphics.Blend>();
                var hNullableRectReader = new NullableReader<Rectangle>();
                var hEffectMaterialReader = new EffectMaterialReader();
                var hExternalReferenceReader = new ExternalReferenceReader();
                var hSoundEffectReader = new SoundEffectReader();
                var hSongReader = new SongReader();
                var hModelReader = new ModelReader();
                var hInt32Reader = new Int32Reader();
                var hEffectReader = new EffectReader();
                var hSingleReader = new SingleReader();

                // At the moment the Video class doesn't exist
                // on all platforms... Allow it to compile anyway.
#if (IOS && !TVOS) || MONOMAC || (WINDOWS && !OPENGL) || WINDOWS_UAP
                var hVideoReader = new VideoReader();
#endif
            }

            // The first content byte i read tells me the number of content readers in this XNB file
            var numberOfReaders = reader.Read7BitEncodedInt();
            var contentReaders = new ContentTypeReader[numberOfReaders];
            var needsInitialize = new BitArray(numberOfReaders);
            _contentReaders = new Dictionary<Type, ContentTypeReader>(numberOfReaders);

            // Lock until we're done allocating and initializing any new
            // content type readers...  this ensures we can load content
            // from multiple threads and still cache the readers.
            lock (_syncRoot)
            {
                // For each reader in the file, we read out the length of the string which contains the type of the reader,
                // then we read out the string. Finally we instantiate an instance of that reader using reflection
                for (var i = 0; i < numberOfReaders; i++)
                {
                    // This string tells us what reader we need to decode the following data
                    // string readerTypeString = reader.ReadString();
                    string originalReaderTypeString = reader.ReadString();

                    if (typeCreators.TryGetValue(originalReaderTypeString, out Func<ContentTypeReader> readerFunc))
                    {
                        contentReaders[i] = readerFunc();
                        needsInitialize[i] = true;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine(originalReaderTypeString);

                        // Need to resolve namespace differences
                        string readerTypeString = originalReaderTypeString;

                        readerTypeString = PrepareType(readerTypeString);

                        var l_readerType = Type.GetType(readerTypeString);
                        if (l_readerType != null)
                        {
                            if (!_contentReadersCache.TryGetValue(l_readerType, out ContentTypeReader typeReader))
                            {
                                try
                                {
                                    typeReader = l_readerType.GetDefaultConstructor().Invoke(null) as ContentTypeReader;
                                }
                                catch (TargetInvocationException ex)
                                {
                                    // If you are getting here, the Mono runtime is most likely not able to JIT the type.
                                    // In particular, MonoTouch needs help instantiating types that are only defined in strings in Xnb files. 
                                    throw new InvalidOperationException(
                                        "Failed to get default constructor for ContentTypeReader. " +
                                        "To work around, add a creation function to ContentTypeReaderManager.AddTypeCreator() " +
                                        "with the following failed type string: " + originalReaderTypeString, ex);
                                }

                                needsInitialize[i] = true;

                                _contentReadersCache.Add(l_readerType, typeReader);
                            }

                            contentReaders[i] = typeReader;
                        }
                        else
                            throw new ContentLoadException(
                                    "Could not find ContentTypeReader Type. Please ensure the name of the Assembly " +
                                    "that contains the Type matches the assembly in the full type name: " +
                                    originalReaderTypeString + " (" + readerTypeString + ")");
                    }

                    var targetType = contentReaders[i].TargetType;
                    if (targetType != null)
                        if (!_contentReaders.ContainsKey(targetType))
                            _contentReaders.Add(targetType, contentReaders[i]);

                    // I think the next 4 bytes refer to the "Version" of the type reader,
                    // although it always seems to be zero
                    reader.ReadInt32();
                }

                // Initialize any new readers.
                for (var i = 0; i < contentReaders.Length; i++)
                {
                    if (needsInitialize.Get(i))
                        contentReaders[i].Initialize(this);
                }

            } // lock (_locker)

            return contentReaders;
        }

        /// <summary>
        /// Removes Version, Culture and PublicKeyToken from a type string.
        /// </summary>
        /// <remarks>
        /// Supports multiple generic types 
        /// (e.g. <see cref="Dictionary{TKey, TValue}"/>) and nested generic types (e.g. List&lt;List&lt;int&gt;&gt;).
        /// </remarks> 
        /// <param name="type">
        /// A <see cref="string"/>
        /// </param>
        /// <returns>
        /// A <see cref="string"/>
        /// </returns>
        public static string PrepareType(string type)
        {
            //Needed to support nested types
            int count = type.Split(new[] { "[[" }, StringSplitOptions.None).Length - 1;

            string preparedType = type;

            for (int i = 0; i < count; i++)
            {
                preparedType = Regex.Replace(preparedType, @"\[(.+?), Version=.+?\]", "[$1]");
            }

            //Handle non generic types
            if (preparedType.Contains("PublicKeyToken"))
                preparedType = Regex.Replace(preparedType, @"(.+?), Version=.+?$", "$1");

            // TODO: For WinRT this is most likely broken!
            preparedType = preparedType.Replace(", MonoGame.Framework.Graphics", string.Format(", {0}", _assemblyName));
            preparedType = preparedType.Replace(", MonoGame.Framework.Video", string.Format(", {0}", _assemblyName));
            preparedType = preparedType.Replace(", MonoGame.Framework", string.Format(", {0}", _assemblyName));

            return preparedType;
        }

        // Static map of type names to creation functions. Required as iOS requires all types at compile time
        private static Dictionary<string, Func<ContentTypeReader>> typeCreators = new Dictionary<string, Func<ContentTypeReader>>();

        /// <summary>
        /// Adds the type creator.
        /// </summary>
        /// <param name='typeString'>Type string.</param>
        /// <param name='createFunction'>Create function.</param>
        public static void AddTypeCreator(string typeString, Func<ContentTypeReader> createFunction)
        {
            if (!typeCreators.ContainsKey(typeString))
                typeCreators.Add(typeString, createFunction);
        }

        public static void ClearTypeCreators()
        {
            typeCreators.Clear();
        }

    }
}
