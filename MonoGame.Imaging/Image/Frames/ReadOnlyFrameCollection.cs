﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MonoGame.Imaging.Pixels;
using MonoGame.Utilities.PackedVector;

namespace MonoGame.Imaging
{
    /// <summary>
    /// Represents a collection of read-only frames that have the same size and pixel type.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public class ReadOnlyFrameCollection<TPixel> : FrameCollectionBase<TPixel, ReadOnlyImageFrame<TPixel>>,
        IReadOnlyCollection<IReadOnlyPixelBuffer<TPixel>>
        where TPixel : unmanaged, IPixel
    {
        #region Constructors

        /// <summary>
        /// Constructs the collection without any initial frames, defining an inital capacity.
        /// </summary>
        public ReadOnlyFrameCollection(int capacity) : base(null, capacity)
        {
        }

        /// <summary>
        /// Constructs the collection without any initial frames.
        /// </summary>
        public ReadOnlyFrameCollection() : this(0)
        {
        }

        /// <summary>
        /// Constructs the collection and adds frames from an enumerable.
        /// </summary>
        /// <param name="frames">The enumerable of frames that will be added to the collection.</param>
        public ReadOnlyFrameCollection(IEnumerable<ReadOnlyImageFrame<TPixel>> frames) : this()
        {
            if (frames != null)
                foreach (var frame in frames)
                    Add(frame);
        }

        /// <summary>
        /// Constructs the collection and adds pixel buffers from an enumerable.
        /// </summary>
        /// <param name="buffers">The enumerable of buffers that will be added to the collection.</param>
        /// <param name="delay">The delay to use for every image in the enumerable.</param>
        public ReadOnlyFrameCollection(IEnumerable<IReadOnlyPixelBuffer<TPixel>> buffers, int delay) :
            base(null, GetInitialCapacity(buffers))
        {
            if (buffers != null)
                foreach (var image in buffers)
                    Add(new ReadOnlyImageFrame<TPixel>(image, delay));
        }

        public ReadOnlyFrameCollection(IReadOnlyPixelBuffer<TPixel> view) : this()
        {
            Add(new ReadOnlyImageFrame<TPixel>(view, 0));
        }

        #endregion

        #region Pure methods

        public bool Contains(IReadOnlyPixelBuffer<TPixel> image) => IndexOf(image) != -1;

        public void CopyTo(IReadOnlyPixelBuffer<TPixel>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("The index may not be negative.", nameof(arrayIndex));

            if (_frames.Count + arrayIndex > array.Length)
                throw new ArgumentException(
                    $"The array (starting at {nameof(arrayIndex)}) has insufficient capacity.", nameof(array));

            for (int i = 0; i < _frames.Count; i++)
                array[i + arrayIndex] = _frames[i].Pixels;
        }

        public int IndexOf(IReadOnlyPixelBuffer<TPixel> image)
        {
            for (int i = 0; i < _frames.Count; i++)
            {
                if (image == _frames[i].Pixels)
                    return i;
            }
            return -1;
        }

        #endregion

        #region Mutating methods

        public bool Remove(IReadOnlyPixelBuffer<TPixel> image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            for (int i = 0; i < _frames.Count; i++)
            {
                if (image == _frames[i].Pixels)
                {
                    RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region GetEnumerator

        IEnumerator<IReadOnlyPixelBuffer<TPixel>> IEnumerable<IReadOnlyPixelBuffer<TPixel>>.GetEnumerator() => 
            new PixelRowsEnumerator(this);

        /// <summary>
        /// Enumerates images from the frames of a <see cref="ReadOnlyFrameCollection{TPixel}"/>.
        /// </summary>
        public struct PixelRowsEnumerator : IEnumerator<IReadOnlyPixelBuffer<TPixel>>
        {
            private ReadOnlyFrameCollection<TPixel> _collection;
            private int _index;
            private int _version;

            public IReadOnlyPixelBuffer<TPixel> Current { get; private set; }
            object IEnumerator.Current => Current;

            internal PixelRowsEnumerator(ReadOnlyFrameCollection<TPixel> collection)
            {
                _collection = collection;
                _index = 0;
                _version = _collection._version;
                Current = null;
            }

            public bool MoveNext()
            {
                AssertCollectionVersion();

                if (_index < _collection.Count)
                {
                    Current = _collection._frames[_index].Pixels;
                    _index++;
                    return true;
                }

                _index = _collection.Count + 1;
                Current = null;
                return false;
            }

            public void Reset()
            {
                AssertCollectionVersion();

                _index = 0;
                Current = null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AssertCollectionVersion()
            {
                if (_version != _collection._version)
                    throw new InvalidOperationException("The underlying collection has changed.");
            }

            public void Dispose()
            {
            }
        }

        #endregion
    }
}
