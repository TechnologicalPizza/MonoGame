﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MonoGame.OpenAL;

namespace Microsoft.Xna.Framework.Audio
{
    public sealed partial class DynamicSoundEffectInstance : SoundEffectInstance
    {
        private Queue<OALSoundBuffer> _queuedBuffers;

        private void PlatformCreate()
        {
            InitializeSound();

            SourceId = controller.ReserveSource();
            HasSourceId = true;

            _queuedBuffers = new Queue<OALSoundBuffer>();
        }

        private int PlatformGetPendingBufferCount()
        {
            return _queuedBuffers.Count;
        }

        private int PlatformGetBufferedSamples()
        {
            AL.GetError();

            if (_queuedBuffers.Count == 0)
                return default;

            int total = 0;
            foreach(var buff in _queuedBuffers)
            {
                AL.GetBuffer(buff.OpenALDataBuffer, ALGetBufferi.Size, out int size);
                ALHelper.CheckError("Failed to get size of queued buffers.");
                total += size;
            }

            AL.GetSource(SourceId, ALGetSourcei.SampleOffset, out int offset);
            ALHelper.CheckError("Failed to get sample offset in source.");
            total -= offset;

            return total;
        }

        private void PlatformPlay()
        {
            AL.GetError();

            // Ensure that the source is not looped (due to source recycling)
            AL.Source(SourceId, ALSourceb.Looping, false);
            ALHelper.CheckError("Failed to set source loop state.");

            AL.SourcePlay(SourceId);
            ALHelper.CheckError("Failed to play the source.");
        }

        private void PlatformPause()
        {
            AL.GetError();
            AL.SourcePause(SourceId);
            ALHelper.CheckError("Failed to pause the source.");
        }

        private void PlatformResume()
        {
            AL.GetError();
            AL.SourcePlay(SourceId);
            ALHelper.CheckError("Failed to play the source.");
        }

        private void PlatformStop()
        {
            AL.GetError();
            AL.SourceStop(SourceId);
            ALHelper.CheckError("Failed to stop the source.");

            // Remove all queued buffers
            AL.Source(SourceId, ALSourcei.Buffer, 0);
            while (_queuedBuffers.Count > 0)
            {
                var buffer = _queuedBuffers.Dequeue();
                buffer.Dispose();
            }
        }

        private void PlatformSubmitBuffer(byte[] buffer, int offset, int count)
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject() + offset;
                PlatformSubmitBuffer(ptr, count, false);
            }
            finally
            {
                handle.Free();
            }
        }

        private void PlatformSubmitBuffer(short[] buffer, int offset, int count)
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject() + offset * sizeof(short);
                PlatformSubmitBuffer(ptr, count * sizeof(short), false);
            }
            finally
            {
                handle.Free();
            }
        }

        private void PlatformSubmitBuffer(float[] buffer, int offset, int count)
        {
            if (!OpenALSoundController.Instance.SupportsIeee)
                throw new NotSupportedException("Float data is not supported.");

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject() + offset * sizeof(float);
                PlatformSubmitBuffer(ptr, count * sizeof(float), true);
            }
            finally
            {
                handle.Free();
            }
        }

        private ALFormat GetALFormat(bool useFloat)
        {
            if (_channels == AudioChannels.Mono)
                return useFloat ? ALFormat.MonoFloat32 : ALFormat.Mono16;
            else
                return useFloat ? ALFormat.StereoFloat32 : ALFormat.Stereo16;
        }

        private void PlatformSubmitBuffer(IntPtr buffer, int bytes, bool useFloat)
        {
            // Get a buffer
            var oalBuffer = new OALSoundBuffer();

            // Bind the data
            ALFormat format = GetALFormat(useFloat);
            oalBuffer.BindDataBuffer(buffer, format, bytes, _sampleRate);

            // Queue the buffer
            AL.SourceQueueBuffer(SourceId, oalBuffer.OpenALDataBuffer);
            ALHelper.CheckError();
            _queuedBuffers.Enqueue(oalBuffer);

            // If the source has run out of buffers, restart it
            var sourceState = AL.GetSourceState(SourceId);
            if (_state == SoundState.Playing && sourceState == ALSourceState.Stopped)
            {
                AL.SourcePlay(SourceId);
                ALHelper.CheckError("Failed to resume source playback.");
            }
        }

        private void PlatformDispose(bool disposing)
        {
            // Stop the source and bind null buffer so that it can be recycled
            AL.GetError();
            if (AL.IsSource(SourceId))
            {
                AL.SourceStop(SourceId);
                AL.Source(SourceId, ALSourcei.Buffer, 0);
                ALHelper.CheckError("Failed to stop the source.");
                controller.RecycleSource(SourceId);
            }
            
            if (disposing)
            {
                while (_queuedBuffers.Count > 0)
                {
                    OALSoundBuffer buffer = _queuedBuffers.Dequeue();
                    buffer.Dispose();
                }
                DynamicSoundEffectInstanceManager.RemoveInstance(this);
            }
        }

        private void PlatformUpdateQueue()
        {
            // Get the completed buffers
            AL.GetError();
            AL.GetSource(SourceId, ALGetSourcei.BuffersProcessed, out int numBuffers);
            ALHelper.CheckError("Failed to get processed buffer count.");

            // Unqueue them
            if (numBuffers > 0)
            {
                AL.SourceUnqueueBuffers(SourceId, numBuffers);
                ALHelper.CheckError("Failed to unqueue buffers.");
                for (int i = 0; i < numBuffers; i++)
                {
                    var buffer = _queuedBuffers.Dequeue();
                    buffer.Dispose();
                }
            }

            // Raise the event for each removed buffer, if needed
            for (int i = 0; i < numBuffers; i++)
                CheckBufferCount();
        }
    }
}
