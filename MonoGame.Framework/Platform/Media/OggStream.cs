﻿// This code originated from:
//
//    http://theinstructionlimit.com/ogg-streaming-using-opentk-and-nvorbis
//    https://github.com/renaudbedard/nvorbis/
//
// It was released to the public domain by the author (Renaud Bedard).
// No other license is intended or required. 

using System;
using System.Collections.Generic;
using System.IO;
using NVorbis;
using MonoGame.OpenAL;
using MonoGame.Framework.Audio;

namespace MonoGame.Framework.Media
{
    internal class OggStream : IDisposable
    {
        internal readonly object _stopMutex = new object();
        internal readonly object _prepareMutex = new object();

        internal readonly int _alSourceID;
        private readonly int _alFilterID;
        private readonly Queue<ALBuffer> _queuedBuffers;
        private readonly bool _leaveInnerStreamOpen;
        private Stream _stream;

        private float _lowPassHfGain;
        private float _volume;
        private float _pitch;

        internal Song Parent { get; }
        internal VorbisReader Reader { get; private set; }
        internal bool IsReady { get; private set; }
        internal bool IsPreparing { get; private set; }

        public bool IsLooped { get; set; }
        public Action OnFinished { get; private set; }
        public int QueuedBufferCount => _queuedBuffers.Count;

        public float LowPassHFGain
        {
            get => _lowPassHfGain;
            set
            {
                if (OggStreamer.Instance.Efx.IsInitialized)
                {
                    OggStreamer.Instance.Efx.Filter(_alFilterID, EfxFilterf.LowpassGainHF, _lowPassHfGain = value);
                    ALHelper.CheckError("Failed to set Efx filter.");

                    OggStreamer.Instance.Efx.BindFilterToSource(_alSourceID, _alFilterID);
                    ALHelper.CheckError("Failed to bind Efx filter to source.");
                }
            }
        }

        public float Volume
        {
            get => _volume;
            set
            {
                AL.Source(_alSourceID, ALSourcef.Gain, _volume = value);
                ALHelper.CheckError("Failed to set volume.");
            }
        }

        public float Pitch
        {
            get => _pitch;
            set
            {
                AL.Source(_alSourceID, ALSourcef.Pitch, _pitch = value);
                ALHelper.CheckError("Failed to set pitch.");
            }
        }

        public OggStream(
            Song parent, Stream stream, bool leaveOpen, Action onFinished = null)
        {
            Parent = parent;
            OnFinished = onFinished;
            _leaveInnerStreamOpen = leaveOpen;
            _stream = stream;

            _queuedBuffers = new Queue<ALBuffer>();
            _alSourceID = ALController.Instance.ReserveSource();
            
            if (OggStreamer.Instance.Efx.IsInitialized)
            {
                _alFilterID = OggStreamer.Instance.Efx.GenFilter();
                ALHelper.CheckError("Failed to generate Efx filter.");

                OggStreamer.Instance.Efx.Filter(_alFilterID, EfxFilteri.FilterType, (int)EfxFilterType.Lowpass);
                ALHelper.CheckError("Failed to set Efx filter type.");

                OggStreamer.Instance.Efx.Filter(_alFilterID, EfxFilterf.LowpassGain, 1);
                ALHelper.CheckError("Failed to set Efx filter value.");
                LowPassHFGain = 1;
            }

            Volume = 1;
            Pitch = 1;
        }

        public void Prepare(bool immediate)
        {
            if (IsPreparing)
                return;

            var state = GetState();
            lock (_stopMutex)
            {
                switch (state)
                {
                    case ALSourceState.Playing:
                    case ALSourceState.Paused:
                        return;

                    case ALSourceState.Stopped:
                        lock (_prepareMutex)
                            Empty();
                        break;
                }

                if (!IsReady)
                {
                    lock (_prepareMutex)
                    {
                        IsPreparing = true;
                        Open(precache: immediate);
                    }
                }
                else if(immediate)
                {
                    FillAndEnqueueBuffer();
                }

                IsPreparing = false;
                OggStreamer.Instance.AddStream(this);
            }
        }

        public void Play(bool immediate)
        {
            var state = GetState();
            switch (state)
            {
                case ALSourceState.Playing:
                    return;

                case ALSourceState.Paused:
                    Resume();
                    return;

                default:
                    Prepare(immediate);

                    AL.SourcePlay(_alSourceID);
                    ALHelper.CheckError("Failed to play source.");
                    break;
            }
        }

        public void Pause()
        {
            var state = GetState();
            if (state != ALSourceState.Playing)
                return;

            OggStreamer.Instance.RemoveStream(this);
            AL.SourcePause(_alSourceID);
            ALHelper.CheckError("Failed to pause source.");
        }

        public void Resume()
        {
            var state = GetState();
            if (state != ALSourceState.Paused)
                return;

            OggStreamer.Instance.AddStream(this);
            AL.SourcePlay(_alSourceID);
            ALHelper.CheckError("Failed to play source.");
        }

        void StopPlayback()
        {
            AL.SourceStop(_alSourceID);
            ALHelper.CheckError("Failed to stop source.");
        }

        public void Stop()
        {
            lock (_stopMutex)
            {
                StopPlayback();

                lock (_prepareMutex)
                {
                    OggStreamer.Instance.RemoveStream(this);
                    Empty();
                    Reader.DecodedPosition = 0;
                }

                AL.Source(_alSourceID, ALSourcei.Buffer, 0);
                ALHelper.CheckError("Failed to free source from buffers.");

                while(_queuedBuffers.Count > 0)
                    DequeueAndReturnBuffer();
            }
        }

        void Empty(int attempts = 0)
        {
            AL.GetSource(_alSourceID, ALGetSourcei.BuffersProcessed, out int processed);
            ALHelper.CheckError("Failed to fetch processed buffers.");

            // there are multiple attempts as some OpenAL implementations are faulty
            try
            {
                if (processed > 0)
                {
                    AL.SourceUnqueueBuffers(_alSourceID, processed);
                    ALHelper.CheckError("Failed to unqueue buffers (first attempt).");

                    for (int i = 0; i < processed; i++)
                        DequeueAndReturnBuffer();
                }
            }
            catch (InvalidOperationException)
            {
                if (processed > 0)
                {
                    AL.SourceUnqueueBuffers(_alSourceID, processed);
                    ALHelper.CheckError("Failed to unqueue buffers (second attempt).");
                }

                // Try turning it off again?
                AL.SourceStop(_alSourceID);
                ALHelper.CheckError("Failed to stop source.");

                if(attempts < 5)
                    Empty(attempts++);
            }
        }

        /// <summary>
        /// Seeking stops playback and empties buffers.
        /// </summary>
        /// <param name="pos"></param>
        public void SeekToPosition(TimeSpan pos)
        {
            lock (_prepareMutex)
            {
                Reader.DecodedTime = pos;
                Stop();
            }
        }
        
        public TimeSpan GetPosition()
        {
            if (Reader == null)
                return TimeSpan.Zero;
            return Reader.DecodedTime;
        }

        public ALSourceState GetState()
        {
            var state = AL.GetSourceState(_alSourceID);
            ALHelper.CheckError("Failed to get source state.");
            return state;
        }

        public TimeSpan GetLength()
        {
            return Reader.TotalTime;
        }

        internal void Open(bool precache = false)
        {
            if (Reader == null)
            {
                Reader = new VorbisReader(_stream, _leaveInnerStreamOpen);
                _stream = null;
            }
            if (precache)
                FillAndEnqueueBuffer();
            IsReady = true;
        }

        private void FillAndEnqueueBuffer()
        {
            if (OggStreamer.Instance.TryFillBuffer(this, out ALBuffer buffer))
                EnqueueBuffer(buffer);
        }

        public void EnqueueBuffer(ALBuffer buffer)
        {
            AL.SourceQueueBuffer(_alSourceID, buffer.BufferID);
            ALHelper.CheckError("Failed to queue buffer.");

            _queuedBuffers.Enqueue(buffer);
        }

        public void DequeueAndReturnBuffer()
        {
            var buffer = _queuedBuffers.Dequeue();
            ALBufferPool.Return(buffer);
        }

        public override int GetHashCode()
        {
            return _alSourceID;
        }
        
        internal void Close()
        {
            if (Reader != null)
            {
                Reader.Dispose();
                Reader = null;
            }
            IsReady = false;

            if (_stream != null && !_leaveInnerStreamOpen)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        public void Dispose()
        {
            var state = GetState();
            if (state == ALSourceState.Playing || state == ALSourceState.Paused)
                StopPlayback();

            lock (_prepareMutex)
            {
                OggStreamer.Instance.RemoveStream(this);
                if (state != ALSourceState.Initial)
                    Empty();
                Close();
            }

            AL.Source(_alSourceID, ALSourcei.Buffer, 0);
            ALHelper.CheckError("Failed to free source from buffers.");

            while (_queuedBuffers.Count > 0)
                DequeueAndReturnBuffer();

            ALController.Instance.RecycleSource(_alSourceID);
            
            if (OggStreamer.Instance.Efx.IsInitialized)
            {
                OggStreamer.Instance.Efx.DeleteFilter(_alFilterID);
                ALHelper.CheckError("Failed to delete EFX filter.");
            }
        }
    }
}