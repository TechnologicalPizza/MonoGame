﻿using Microsoft.Xna.Framework.Audio;
using MonoGame.OpenAL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Xna.Framework.Media
{
    internal class OggStreamer : IDisposable
    {
        public readonly EffectsExtension Efx = ALController.Efx;

        const float DefaultUpdateRate = 8;
        const int DefaultBufferSize = 12000;
        const int MaxBuffers = 3;

        internal static readonly object _singletonMutex = new object();
        internal readonly object _iterationMutex = new object();
        readonly object _readMutex = new object();

        private UnmanagedPointer<float> _readBuffer;
        private UnmanagedPointer<short> _castBuffer;
        internal readonly HashSet<OggStream> _streams;
        readonly TimeSpan[] _threadTiming;
        private static OggStreamer _instance;

        private readonly Thread _thread;
        private Stopwatch _threadWatch;
        private bool _pendingFinish;
        private volatile bool _cancelled;

        public float UpdateRate { get; }
        public int BufferSize { get; }
        public ReadOnlyCollection<TimeSpan> UpdateTime { get; }

        public static OggStreamer Instance
        {
            get
            {
                lock (_singletonMutex)
                {
                    if (_instance == null)
                        throw new InvalidOperationException("No instance running.");
                    return _instance;
                }
            }
        }

        public OggStreamer(int bufferSize = DefaultBufferSize, float updateRate = DefaultUpdateRate)
        {
            lock (_singletonMutex)
            {
                if (_instance != null)
                    throw new InvalidOperationException("An instance has already been created.");
                _instance = this;
            }

            UpdateRate = updateRate;
            BufferSize = bufferSize;

            _readBuffer = new UnmanagedPointer<float>(BufferSize);
            _castBuffer = new UnmanagedPointer<short>(BufferSize);
            _streams = new HashSet<OggStream>();

            _threadWatch = new Stopwatch();
            _threadTiming = new TimeSpan[(int)(UpdateRate < 1 ? 1 : UpdateRate)];
            UpdateTime = new ReadOnlyCollection<TimeSpan>(_threadTiming);

            _thread = new Thread(SongStreamingThread)
            {
                Name = "Song Streaming Thread",
                Priority = ThreadPriority.BelowNormal
            };
            _thread.Start();
        }

        public void Dispose()
        {
            lock (_singletonMutex)
            {
                Debug.Assert(
                    _instance == this, "Something assigned a new instance without locking the singleton mutex.");

                _cancelled = true;
                lock (_iterationMutex)
                    _streams.Clear();

                _thread.Join(1000);
                _instance = null;
            }
        }

        internal bool AddStream(OggStream stream)
        {
            lock (_iterationMutex)
                return _streams.Add(stream);
        }

        internal bool RemoveStream(OggStream stream)
        {
            lock (_iterationMutex)
                return _streams.Remove(stream);
        }

        public bool TryReadBuffer(OggStream stream, out ALBuffer buffer)
        {
            lock (_readMutex)
            {
                var reader = stream.Reader;
                int readSamples = reader.ReadSamples(_readBuffer.Span);

                if (readSamples > 0)
                {
                    var channels = (AudioChannels)reader.Channels;
                    bool useFloat = ALController.Instance.SupportsFloat32;
                    ALFormat format = ALHelper.GetALFormat(channels, useFloat);

                    buffer = ALBufferPool.Rent();
                    var dataSpan = _readBuffer.Span.Slice(0, readSamples);
                    if (useFloat)
                    {
                        buffer.BufferData<float>(dataSpan, format, reader.SampleRate);
                    }
                    else
                    {
                        var castSpan = _castBuffer.Span.Slice(0, readSamples);
                        AudioLoader.ConvertSamplesToInt16(dataSpan, castSpan);
                        buffer.BufferData<short>(castSpan, format, reader.SampleRate);
                    }
                    return true;
                }
            }
            buffer = null;
            return false;
        }

        private void SongStreamingThread()
        {
            var localStreams = new List<OggStream>();
            var lastTickElapsed = TimeSpan.Zero;

            while (!_cancelled)
            {
                var updateDelay = TimeSpan.FromSeconds(1 / ((UpdateRate <= 0) ? 1 : UpdateRate));
                var toSleep = updateDelay - lastTickElapsed;
                if (toSleep.TotalMilliseconds > 0)
                    Thread.Sleep(toSleep);

                if (_cancelled)
                    break;

                _threadWatch.Restart();
                localStreams.Clear();
                lock (_iterationMutex)
                {
                    foreach (var stream in _streams)
                        localStreams.Add(stream);
                }

                foreach (OggStream stream in localStreams)
                {
                    lock (stream._prepareMutex)
                    {
                        lock (_iterationMutex)
                            if (!_streams.Contains(stream))
                                continue;

                        if (!FillStream(stream))
                            continue;
                    }

                    lock (stream._stopMutex)
                    {
                        if (stream.IsPreparing)
                            continue;

                        lock (_iterationMutex)
                            if (!_streams.Contains(stream))
                                continue;

                        var state = AL.GetSourceState(stream._alSourceID);
                        ALHelper.CheckError("Failed to get source state.");

                        if (state == ALSourceState.Stopped)
                        {
                            AL.SourcePlay(stream._alSourceID);
                            ALHelper.CheckError("Failed to play.");
                        }
                    }
                }

                _threadWatch.Stop();

                // move all elements forward one index and update the first element
                Array.Copy(_threadTiming, 0, _threadTiming, destinationIndex: 1, _threadTiming.Length - 1);
                _threadTiming[0] = _threadWatch.Elapsed;
                lastTickElapsed = _threadWatch.Elapsed;
            }
        }

        private bool FillStream(OggStream stream)
        {
            AL.GetSource(stream._alSourceID, ALGetSourcei.BuffersQueued, out int queued);
            ALHelper.CheckError("Failed to fetch queued buffers.");

            AL.GetSource(stream._alSourceID, ALGetSourcei.BuffersProcessed, out int processed);
            ALHelper.CheckError("Failed to fetch processed buffers.");

            int requested = Math.Max(MaxBuffers - stream.BufferCount, 0);
            if (processed == 0 && requested == 0)
                return false;

            if (processed > 0)
            {
                AL.SourceUnqueueBuffers(stream._alSourceID, processed);
                ALHelper.CheckError("Failed to unqueue buffers.");

                for (int i = 0; i < processed; i++)
                    stream.DequeueAndReturnBuffer();
            }

            for (int i = 0; i < requested; i++)
            {
                if (TryReadBuffer(stream, out ALBuffer buffer))
                {
                    stream.EnqueueBuffer(buffer);
                }
                else
                {
                    if (stream.IsLooped)
                    {
                        // closing and opening the stream is a bit expensive
                        //stream.Close();
                        //stream.Open();

                        // we don't support non-seekable streams anyway
                        stream.SeekToPosition(0);
                    }
                    else
                    {
                        _pendingFinish = true;
                        break;
                    }
                }
            }

            if (_pendingFinish && queued == 0)
            {
                _pendingFinish = false;
                lock (_iterationMutex)
                    _streams.Remove(stream);

                stream.OnFinished?.Invoke();
            }
            else if (!stream.IsLooped)
            {
                return false;
            }
            return true;
        }
    }
}