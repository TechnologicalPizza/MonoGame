﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoGame.Framework.Graphics
{
    public partial class IndexBuffer : BufferBase
    {
        internal SharpDX.Direct3D11.Buffer _buffer;

        private void PlatformConstruct()
        {
        }

        private void PlatformGraphicsDeviceResetting()
        {
            SharpDX.Utilities.Dispose(ref _buffer);
        }

        private void GenerateIfRequired()
        {
            if (_buffer != null)
                return;

            // TODO: To use true Immutable resources we would need to delay creation of 
            // the Buffer until SetData() and recreate them if set more than once.

            var accessflags = SharpDX.Direct3D11.CpuAccessFlags.None;
            var resUsage = SharpDX.Direct3D11.ResourceUsage.Default;

            if (IsDynamic)
            {
                accessflags |= SharpDX.Direct3D11.CpuAccessFlags.Write;
                resUsage = SharpDX.Direct3D11.ResourceUsage.Dynamic;
            }

            _buffer = new SharpDX.Direct3D11.Buffer(
                GraphicsDevice._d3dDevice,
                Capacity * ElementType.TypeSize(),
                resUsage,
                SharpDX.Direct3D11.BindFlags.IndexBuffer,
                accessflags,
                SharpDX.Direct3D11.ResourceOptionFlags.None,
                0); // StructureSizeInBytes
        }

        private unsafe void PlatformGetData(int byteOffset, Span<byte> destination)
        {
            if (_buffer == null)
                return;

            if (IsDynamic)
                throw new NotImplementedException();

            // Copy the texture to a staging resource
            var stagingDesc = _buffer.Description;
            stagingDesc.BindFlags = SharpDX.Direct3D11.BindFlags.None;
            stagingDesc.CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read | SharpDX.Direct3D11.CpuAccessFlags.Write;
            stagingDesc.Usage = SharpDX.Direct3D11.ResourceUsage.Staging;
            stagingDesc.OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None;

            using (var stagingBuffer = new SharpDX.Direct3D11.Buffer(GraphicsDevice._d3dDevice, stagingDesc))
            {
                var deviceContext = GraphicsDevice._d3dContext;
                lock (deviceContext)
                {
                    deviceContext.CopyResource(_buffer, stagingBuffer);

                    // Map the staging resource to CPU accessible memory
                    try
                    {
                        var box = deviceContext.MapSubresource(
                            stagingBuffer, 0, SharpDX.Direct3D11.MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                        int srcBytes = Count * ElementType.TypeSize();
                        var byteSrc = new ReadOnlySpan<byte>((void*)(box.DataPointer + byteOffset), srcBytes);
                        byteSrc.Slice(0, destination.Length).CopyTo(destination);
                    }
                    finally
                    {
                        // Make sure that we unmap the resource in case of an exception
                        deviceContext.UnmapSubresource(stagingBuffer, 0);
                    }
                }
            }
        }

        private unsafe void PlatformSetData(
            int byteOffset, ReadOnlySpan<byte> source, SetDataOptions options)
        {
            GenerateIfRequired();

            if (IsDynamic)
            {
                // We assume discard by default.
                var mode = SharpDX.Direct3D11.MapMode.WriteDiscard;
                if ((options & SetDataOptions.NoOverwrite) == SetDataOptions.NoOverwrite)
                    mode = SharpDX.Direct3D11.MapMode.WriteNoOverwrite;

                var d3dContext = GraphicsDevice._d3dContext;
                lock (d3dContext)
                {
                    var box = d3dContext.MapSubresource(_buffer, 0, mode, SharpDX.Direct3D11.MapFlags.None);

                    int dstBytes = Capacity * ElementType.TypeSize();
                    var dst = new Span<byte>((void*)(box.DataPointer + byteOffset), dstBytes);
                    source.CopyTo(dst);

                    d3dContext.UnmapSubresource(_buffer, 0);
                }
            }
            else
            {
                var region = new SharpDX.Direct3D11.ResourceRegion
                {
                    Top = 0,
                    Front = 0,
                    Back = 1,
                    Bottom = 1,
                    Left = byteOffset,
                    Right = byteOffset + source.Length
                };

                // TODO: We need to deal with threaded contexts here!
                var d3dContext = GraphicsDevice._d3dContext;
                lock (d3dContext)
                {
                    ref var mutableData = ref MemoryMarshal.GetReference(source);
                    d3dContext.UpdateSubresource(ref mutableData, _buffer, 0, source.Length, 0, region);
                }
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                SharpDX.Utilities.Dispose(ref _buffer);

            base.Dispose(disposing);
        }
    }
}
