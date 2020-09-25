﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Microsoft
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace MonoGame.Framework.Memory
{
    /// <summary>
    /// MemoryStream implementation that deals with pooling and 
    /// managing memory streams which use potentially large buffers.
    /// </summary>
    /// <remarks>
    /// This class works in tandem with the RecyclableMemoryManager to supply MemoryStream
    /// objects to callers, while avoiding these specific problems:
    /// 1. LOH allocations - since all large buffers are pooled, they will never incur a Gen2 GC
    /// 2. Memory waste - A standard memory stream doubles its size when it runs out of room. This
    ///    leads to continual memory growth as each stream approaches the maximum allowed size.
    /// 3. Memory copying - Each time a MemoryStream grows, all the bytes are copied into new buffers.
    ///    This implementation only copies the bytes when GetBuffer is called.
    /// 4. Memory fragmentation - By using homogeneous buffer sizes, it ensures that blocks of memory
    ///    can be easily reused.
    /// 
    /// The stream is implemented on top of a series of uniformly-sized blocks. As the stream's length grows,
    /// additional blocks are retrieved from the memory manager. It is these blocks that are pooled,
    /// not the stream object itself.
    /// 
    /// The biggest wrinkle in this implementation is when GetBuffer() is called. 
    /// This requires a single contiguous buffer. If only a single block is in use, 
    /// then that block is returned. If multiple blocks are in use,
    /// we retrieve a larger buffer from the memory manager. These large buffers are also pooled, 
    /// split by size--they are multiples/exponentials of a chunk size (1 MB by default).
    /// 
    /// Once a large buffer is assigned to the stream the blocks are NEVER again used for this stream. 
    /// All operations take place on the large buffer. The large buffer can be replaced by a larger buffer
    /// from the pool as needed. All blocks and large buffers are maintained in the stream until the 
    /// stream is disposed (unless AggressiveBufferReturn is enabled in the stream manager).
    /// </remarks>
    public sealed class RecyclableMemoryStream : MemoryStream
    {
        private const long MaxStreamLength = int.MaxValue;

        #region Instance Fields

        private readonly Guid _id;
        private readonly RecyclableMemoryManager _memoryManager;
        private readonly string? _tag;

        /// <summary>All of these blocks must be the same size.</summary>
        private readonly List<byte[]> _blocks = new List<byte[]>(1);

        private int _length;
        private int _position;

        /// <summary>
        /// This list is used to store buffers once they're replaced by something larger.
        /// This is for the cases where you have users of this class that may hold onto the buffers longer
        /// than they should and you want to prevent race conditions which could corrupt the data.
        /// </summary>
        private List<byte[]>? _dirtyBuffers;

        private long _disposedState;

        /// <summary>
        /// This is only set by GetBuffer() if the necessary buffer is larger than a single block size, or on
        /// construction if the caller immediately requests a single large buffer.
        /// </summary>
        /// <remarks>
        /// If this field is non-null, it contains the concatenation of the bytes found in the individual blocks.
        /// Once it is created, this (or a larger) largeBuffer will be used for the life of the stream.
        /// </remarks>
        private RecyclableBuffer? _largeBuffer;

        #endregion

        #region Internal Properties

        /// <summary>
        /// Unique identifier for this stream across it's entire lifetime
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        internal Guid Id
        {
            get
            {
                CheckDisposed();
                return _id;
            }
        }

        /// <summary>
        /// A temporary identifier for the current usage of this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        internal string? Tag
        {
            get
            {
                CheckDisposed();
                return _tag;
            }
        }

        /// <summary>
        /// Gets the memory manager being used by this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        internal RecyclableMemoryManager MemoryManager
        {
            get
            {
                CheckDisposed();
                return _memoryManager;
            }
        }

        /// <summary>
        /// Callstack of the constructor. It is only set if MemoryManager.GenerateCallStacks is true,
        /// which should only be in debugging situations.
        /// </summary>
        internal string? AllocationStack { get; }

        /// <summary>
        /// Callstack of the Dispose call. It is only set if MemoryManager.GenerateCallStacks is true,
        /// which should only be in debugging situations.
        /// </summary>
        internal string? DisposeStack { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new <see cref="RecyclableMemoryStream"/> object.
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        public RecyclableMemoryStream(RecyclableMemoryManager memoryManager)
            : this(memoryManager, 0, Guid.Empty, null, default)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="RecyclableMemoryStream"/> object.
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        public RecyclableMemoryStream(RecyclableMemoryManager memoryManager, string? tag)
            : this(memoryManager, 0, Guid.Empty, tag, default)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="RecyclableMemoryStream"/> object.
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        /// <param name="requestedSize">The initial requested size to prevent future allocations</param>
        public RecyclableMemoryStream(RecyclableMemoryManager memoryManager, string? tag, long requestedSize)
            : this(memoryManager, requestedSize, Guid.Empty, tag, default)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="RecyclableMemoryStream"/> object.
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="requestedSize">The initial requested size to prevent future allocations</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        /// <param name="initialLargeBuffer">
        /// This buffer will be owned by the stream and returned to the memory manager upon Dispose.
        /// </param>
        internal RecyclableMemoryStream(
            RecyclableMemoryManager memoryManager,
            long requestedSize,
            Guid id,
            string? tag,
            RecyclableBuffer? initialLargeBuffer)
            : base(Array.Empty<byte>())
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _id = id;
            _tag = tag;

            if (requestedSize < memoryManager.BlockSize)
                requestedSize = memoryManager.BlockSize;

            if (initialLargeBuffer == null)
                EnsureCapacity(requestedSize);
            else
                _largeBuffer = initialLargeBuffer;

            if (_memoryManager.GenerateCallStacks)
                AllocationStack = Environment.StackTrace;

            _memoryManager.ReportStreamCreated();
        }

        #endregion

        #region Dispose and Finalize

        /// <summary>
        /// Returns the memory used by this stream back to the pool.
        /// </summary>
        /// <param name="disposing">Whether we're disposing (true), or being called by the finalizer (false)</param>
        [SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly",
            Justification = "We have different disposal semantics, so SuppressFinalize is in a different spot.")]
        protected override void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposedState, 1, 0) != 0)
                return;

            if (_memoryManager.GenerateCallStacks)
                DisposeStack = Environment.StackTrace;

            if (disposing)
            {
                _memoryManager.ReportStreamDisposed();
                GC.SuppressFinalize(this);
            }
            else
            {
                _memoryManager.ReportStreamFinalized();
            }

            _memoryManager.ReportStreamLength(_length);

            _largeBuffer?.Dispose();

            if (_dirtyBuffers != null)
            {
                foreach (var buffer in _dirtyBuffers)
                    _memoryManager.ReturnBuffer(buffer, _tag);
            }

            _memoryManager.ReturnBlocks(_blocks, _tag);
            _blocks.Clear();

            base.Dispose(disposing);
        }

        /// <summary>
        /// Equivalent to <see cref="Stream.Dispose"/>.
        /// </summary>
        public override void Close()
        {
            Dispose(true);
        }

        ~RecyclableMemoryStream()
        {
            Dispose(false);
        }

        #endregion

        #region MemoryStream property overrides

        /// <summary>
        /// Gets or sets the capacity.
        /// </summary>
        /// <remarks>
        /// Capacity is always in multiples of the memory manager's block size, 
        /// unless the large buffer is in use. Capacity never decreases during a stream's lifetime. 
        /// Explicitly setting the capacity to a lower value than the current value will have no effect. 
        /// This is because the buffers are all pooled by chunks and there's little reason to 
        /// allow stream truncation.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int Capacity
        {
            get
            {
                CheckDisposed();

                if (_largeBuffer != null)
                    return _largeBuffer.BufferLength;

                long size = (long)_blocks.Count * _memoryManager.BlockSize;
                return (int)Math.Min(int.MaxValue, size);
            }
            set
            {
                CheckDisposed();
                EnsureCapacity(value);
            }
        }

        /// <summary>
        /// Gets the number of bytes written to this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override long Length
        {
            get
            {
                CheckDisposed();
                return _length;
            }
        }

        /// <summary>
        /// Gets the current position in the stream
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override long Position
        {
            get
            {
                CheckDisposed();
                return _position;
            }
            set
            {
                CheckDisposed();
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");

                if (value > MaxStreamLength)
                    throw new ArgumentOutOfRangeException(nameof(value), "value cannot be more than " + MaxStreamLength);

                _position = (int)value;
            }
        }

        /// <summary>
        /// Gets whether the stream can currently read.
        /// </summary>
        public override bool CanRead => !IsDisposed;

        /// <summary>
        /// Gets whether the stream can currently seek.
        /// </summary>
        public override bool CanSeek => !IsDisposed;

        /// <summary>
        /// Gets whether the stream can currently write.
        /// </summary>
        public override bool CanWrite => !IsDisposed;

        /// <summary>
        /// Gets whether the stream can timeout; always <see langword="false"/>.
        /// </summary>
        public override bool CanTimeout => false;

        #endregion

        #region MemoryStream method overrides

        /// <summary>
        /// Returns a single buffer containing the contents of the stream.
        /// The buffer may be longer than the stream length.
        /// </summary>
        /// <returns>A byte[] buffer</returns>
        /// <remarks>
        /// Doing a Write() after calling GetBuffer() "invalidates" the buffer. 
        /// The old buffer is held onto until Dispose is called, but the next time GetBuffer() is called,
        /// a new buffer from the pool will be required.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override byte[] GetBuffer()
        {
            CheckDisposed();

            if (_largeBuffer != null)
                return _largeBuffer.Buffer;

            if (_blocks.Count == 1)
                return _blocks[0];

            // Buffer needs to reflect the capacity, not the length, 
            // because it's possible that people will manipulate the buffer directly
            // and set the length afterward. Capacity sets the expectation
            // for the size of the buffer.
            var newBuffer = _memoryManager.GetBuffer(Capacity, _tag);

            // InternalRead will check for existence of largeBuffer, so make sure we
            // don't set it until after we've copied the data.
            InternalRead(newBuffer.AsSpan(0, _length), 0);
            _largeBuffer = newBuffer;

            if (_blocks.Count > 0 && _memoryManager.AggresiveBlockReturn)
            {
                _memoryManager.ReturnBlocks(_blocks, _tag);
                _blocks.Clear();
            }

            return _largeBuffer.Buffer;
        }

        /// <summary>
        /// Returns an <see cref="ArraySegment{byte}"/> that wraps a
        /// single buffer containing the contents of the stream.
        /// </summary>
        /// <param name="buffer">An ArraySegment containing a reference to the underlying bytes.</param>
        /// <returns>Always returns true.</returns>
        /// <remarks>
        /// GetBuffer has no failure modes 
        /// (it always returns something, even if it's an empty buffer), 
        /// therefore this method always returns a valid ArraySegment to 
        /// the same buffer returned by GetBuffer.
        /// </remarks>
        public override bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
            CheckDisposed();
            buffer = new ArraySegment<byte>(GetBuffer(), 0, (int)Length);
            // GetBuffer has no failure modes, so this should always succeed
            return true;
        }

        /// <summary>
        /// Returns a new array with a copy of the buffer's contents. 
        /// You should almost certainly be using <see cref="GetBuffer"/> combined 
        /// with the Length to access the bytes in this stream. 
        /// Calling <see cref="ToArray"/> will destroy the benefits of pooled buffers,
        /// but it is included for the sake of completeness.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
#pragma warning disable CS0809
        [Obsolete("This method has degraded performance vs. GetBuffer and should be avoided.")]
        public override byte[] ToArray()
        {
            CheckDisposed();

            byte[] newBuffer = new byte[_length];
            InternalRead(newBuffer, 0);

            _memoryManager.ReportStreamToArray(_tag);
            return newBuffer;
        }
#pragma warning restore CS0809

        /// <summary>
        /// Reads from the current position into the provided buffer.
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <param name="offset">Offset into buffer at which to start placing the read bytes.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is less than 0</exception>
        /// <exception cref="ArgumentException">offset subtracted from the buffer length is less than count</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer, offset, count, ref _position);

        /// <summary>
        /// Reads from the specified position into the provided buffer.
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <param name="offset">Offset into buffer at which to start placing the read bytes.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <param name="streamPosition">Position in the stream to start reading from</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is less than 0</exception>
        /// <exception cref="ArgumentException">offset subtracted from the buffer length is less than count</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public int Read(byte[] buffer, int offset, int count, ref int streamPosition)
        {
            CheckDisposed();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            int amountRead = InternalRead(buffer.AsSpan(offset, count), streamPosition);
            streamPosition += amountRead;
            return amountRead;
        }

        /// <summary>
        /// Reads from the current position into the provided buffer.
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int Read(Span<byte> buffer) => Read(buffer, ref _position);

        /// <summary>
        /// Reads from the specified position into the provided buffer.
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <param name="streamPosition">Position in the stream to start reading from</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public int Read(Span<byte> buffer, ref int streamPosition)
        {
            CheckDisposed();

            int amountRead = InternalRead(buffer, streamPosition);
            streamPosition += amountRead;
            return amountRead;
        }

        /// <summary>
        /// Writes the buffer to the stream
        /// </summary>
        /// <param name="buffer">Source buffer</param>
        /// <param name="offset">Start position</param>
        /// <param name="count">Number of bytes to write</param>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is negative</exception>
        /// <exception cref="ArgumentException">buffer.Length - offset is not less than count</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

        /// <summary>
        /// Writes the buffer to the stream.
        /// </summary>
        /// <param name="source">Source buffer</param>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void Write(ReadOnlySpan<byte> source)
        {
            CheckDisposed();

            // Check for overflow
            long end = (long)_position + source.Length;
            if (end > MaxStreamLength)
                throw new IOException("Maximum capacity exceeded.");

            EnsureCapacity(end);

            if (_largeBuffer == null)
            {
                var blockOffset = GetBlockOffset(_position);
                while (!source.IsEmpty)
                {
                    byte[] currentBlock = _blocks[blockOffset.Block];
                    int remainingInBlock = _memoryManager.BlockSize - blockOffset.Offset;
                    int amountToWriteInBlock = Math.Min(remainingInBlock, source.Length);

                    source.Slice(0, amountToWriteInBlock).CopyTo(currentBlock.AsSpan(blockOffset.Offset));
                    source = source.Slice(amountToWriteInBlock);

                    blockOffset.Block++;
                    blockOffset.Offset = 0;
                }
            }
            else
            {
                source.CopyTo(_largeBuffer.AsSpan(_position));
            }
            _position = (int)end;
            _length = Math.Max(_position, _length);
        }

        /// <summary>
        /// Returns a useful string for debugging. 
        /// This should not normally be called in actual production code.
        /// </summary>
        public override string ToString()
        {
            return $"Id = {Id}, Tag = {Tag}, Length = {Length:N0} bytes";
        }

        /// <summary>
        /// Writes a single byte to the current position in the stream.
        /// </summary>
        /// <param name="value">byte value to write</param>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void WriteByte(byte value)
        {
            CheckDisposed();
            Span<byte> tmp = stackalloc byte[1];
            tmp[0] = value;
            Write(tmp);
        }

        /// <summary>
        /// Reads a single byte from the current position in the stream.
        /// </summary>
        /// <returns>The byte at the current position, or -1 if the position is at the end of the stream.</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int ReadByte() => ReadByte(ref _position);

        /// <summary>
        /// Reads a single byte from the specified position in the stream.
        /// </summary>
        /// <param name="streamPosition">The position in the stream to read from</param>
        /// <returns>The byte at the current position, or -1 if the position is at the end of the stream.</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public int ReadByte(ref int streamPosition)
        {
            CheckDisposed();
            if (streamPosition == _length)
                return -1;

            byte value;
            if (_largeBuffer == null)
            {
                var blockOffset = GetBlockOffset(streamPosition);
                value = _blocks[blockOffset.Block][blockOffset.Offset];
            }
            else
            {
                value = _largeBuffer.Buffer[streamPosition];
            }
            streamPosition++;
            return value;
        }

        /// <summary>
        /// Sets the length of the stream.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value is negative or larger than MaxStreamLength</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void SetLength(long value)
        {
            CheckDisposed();
            if (value < 0 || value > MaxStreamLength)
                throw new ArgumentOutOfRangeException(
                    nameof(value), "value must be non-negative and at most " + MaxStreamLength);

            EnsureCapacity((int)value);

            _length = (int)value;
            if (_position > value)
                _position = (int)value;
        }

        /// <summary>
        /// Sets the position to the offset from the seek location.
        /// </summary>
        /// <param name="offset">How many bytes to move.</param>
        /// <param name="origin">Move from where.</param>
        /// <returns>The new position</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset is larger than MaxStreamLength</exception>
        /// <exception cref="ArgumentException">Invalid seek origin</exception>
        /// <exception cref="IOException">Attempt to set negative position</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            if (offset > MaxStreamLength)
                throw new ArgumentOutOfRangeException(
                    nameof(offset), "offset cannot be larger than " + MaxStreamLength);

            int newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = (int)offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = (int)offset + _position;
                    break;
                case SeekOrigin.End:
                    newPosition = (int)offset + _length;
                    break;

                default:
                    throw new ArgumentException("Invalid seek origin", nameof(origin));
            }

            if (newPosition < 0)
                throw new IOException("Seek before beginning.");

            _position = newPosition;
            return _position;
        }

        /// <summary>
        /// Synchronously writes this stream's bytes to the parameter stream.
        /// </summary>
        /// <param name="stream">Destination stream</param>
        public override void WriteTo(Stream stream)
        {
            CheckDisposed();
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (_largeBuffer == null)
            {
                int currentBlock = 0;
                int bytesRemaining = _length;

                while (bytesRemaining > 0)
                {
                    int amountToCopy = Math.Min(_blocks[currentBlock].Length, bytesRemaining);
                    stream.Write(_blocks[currentBlock], 0, amountToCopy);

                    bytesRemaining -= amountToCopy;
                    currentBlock++;
                }
            }
            else
            {
                stream.Write(_largeBuffer.Buffer, 0, _length);
            }
        }
        #endregion

        #region Helper Methods

        private bool IsDisposed => Interlocked.Read(ref _disposedState) != 0;

        private void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    GetType().FullName,
                    $"The stream with Id {_id} and Tag {_tag} is disposed.");
        }

        private int InternalRead(Span<byte> buffer, int fromPosition)
        {
            if (_length - fromPosition <= 0)
                return 0;

            if (_largeBuffer != null)
            {
                int amountToCopy = Math.Min(buffer.Length, _length - fromPosition);
                _largeBuffer.AsSpan(fromPosition, amountToCopy).CopyTo(buffer);
                return amountToCopy;
            }

            var blockOffset = GetBlockOffset(fromPosition);
            int bytesWritten = 0;
            int bytesRemaining = Math.Min(buffer.Length, _length - fromPosition);

            while (bytesRemaining > 0)
            {
                int amountToCopy = Math.Min(_blocks[blockOffset.Block].Length - blockOffset.Offset, bytesRemaining);
                var blockSrc = _blocks[blockOffset.Block].AsSpan(blockOffset.Offset, amountToCopy);
                blockSrc.CopyTo(buffer.Slice(bytesWritten));

                bytesWritten += amountToCopy;
                bytesRemaining -= amountToCopy;

                blockOffset.Block++;
                blockOffset.Offset = 0;
            }
            return bytesWritten;
        }

        public struct BlockOffset
        {
            public int Block;
            public int Offset;

            public BlockOffset(int block, int offset)
            {
                Block = block;
                Offset = offset;
            }
        }

        public BlockOffset GetBlockOffset(int offset)
        {
            int blockSize = _memoryManager.BlockSize;
            return new BlockOffset(offset / blockSize, offset % blockSize);
        }

        private void EnsureCapacity(long newCapacity)
        {
            if (newCapacity > _memoryManager.MaximumStreamCapacity &&
                _memoryManager.MaximumStreamCapacity > 0)
            {
                throw new InvalidOperationException(
                    "Requested capacity is too large (" + newCapacity +
                    "). Limit is " + _memoryManager.MaximumStreamCapacity);
            }

            if (_largeBuffer != null)
            {
                if (newCapacity > _largeBuffer.BufferLength)
                {
                    var newBuffer = _memoryManager.GetBuffer(newCapacity, _tag);
                    InternalRead(newBuffer.AsSpan(0, _length), 0);

                    ReleaseLargeBuffer();
                    _largeBuffer = newBuffer;
                }
            }
            else
            {
                long currentCapacity = Capacity;
                while (currentCapacity < newCapacity)
                {
                    var block = _memoryManager.GetBlock();
                    _blocks.Add(block);
                    currentCapacity += block.Length;
                }
            }
        }

        /// <summary>
        /// Release the large buffer (either stores it for eventual release or returns it immediately).
        /// </summary>
        private void ReleaseLargeBuffer()
        {
            if (_memoryManager.AggressiveBufferReturn)
            {
                _largeBuffer?.Dispose();
            }
            else
            {
                if (_dirtyBuffers == null)
                    // We most likely will only ever need space for one
                    _dirtyBuffers = new List<byte[]>(1);

                if (_largeBuffer != null)
                    _dirtyBuffers.Add(_largeBuffer.Buffer);
            }
            _largeBuffer = null;
        }
        #endregion
    }
}