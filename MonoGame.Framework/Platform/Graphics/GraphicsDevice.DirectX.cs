// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DXResource = SharpDX.Direct3D11.Resource;
using DXTexture2D = SharpDX.Direct3D11.Texture2D;

#if WINDOWS_UAP
using Windows.UI.Xaml.Controls;
using Windows.Graphics.Display;
using Windows.UI.Core;
using System.Runtime.InteropServices;
#endif

namespace MonoGame.Framework.Graphics
{
    public partial class GraphicsDevice
    {
        // Core Direct3D Objects
        internal SharpDX.Direct3D11.Device _d3dDevice;
        internal DeviceContext _d3dContext;
        internal RenderTargetView _renderTargetView;
        internal DepthStencilView _depthStencilView;

        private int _vertexBufferSlotsUsed;
        private bool _blendFactorDirty;

#if WINDOWS_UAP

        // Declare Direct2D Objects
        private SharpDX.Direct2D1.Factory1 _d2dFactory;
        private SharpDX.Direct2D1.Device _d2dDevice;
        private SharpDX.Direct2D1.DeviceContext _d2dContext;

        // Declare DirectWrite & Windows Imaging Component Objects
        private SharpDX.DirectWrite.Factory _dwriteFactory;
        private SharpDX.WIC.ImagingFactory2 _wicFactory;

        // The swap chain resources.
        private SharpDX.Direct2D1.Bitmap1 _bitmapTarget;
        private SharpDX.DXGI.SwapChain1 _swapChain;

        private SwapChainPanel _swapChainPanel;
        private float _dpi; 

#elif WINDOWS

        private SwapChain _swapChain;

#endif

        // The active render targets.
        private readonly RenderTargetView[] _currentRenderTargets = new RenderTargetView[4];

        // The active depth view.
        private DepthStencilView _currentDepthStencilView;

        private readonly Dictionary<VertexDeclaration, DynamicVertexBuffer> _userVertexBuffers =
            new Dictionary<VertexDeclaration, DynamicVertexBuffer>();

        private DynamicIndexBuffer _userIndexBuffer16;
        private DynamicIndexBuffer _userIndexBuffer32;

#if WINDOWS_UAP

        internal float Dpi
        {
            get => _dpi;
            set
            {
                if (_dpi == value)
                    return;
                _dpi = value;
                _d2dContext.DotsPerInch = new Size2F(_dpi, _dpi);
                //OnDpiChanged?.Invoke(this);
            }
        }

#endif

        /// <summary>
        /// Returns a handle to internal device object. Valid only on DirectX platforms.
        /// For usage, convert this to SharpDX.Direct3D11.Device.
        /// </summary>
        public object Handle => _d3dDevice;

        private void PlatformSetup()
        {
            MaxTextureSlots = 16;
            MaxVertexTextureSlots = 16;
            
            MaxTexture2DSize = DXResource.MaximumTexture2DSize;
            MaxTexture3DSize = DXResource.MaximumTexture3DSize;
            MaxTextureCubeSize = DXResource.MaximumTextureCubeSize;

#if WINDOWS_UAP
            CreateDeviceIndependentResources();
            CreateDeviceResources();
            Dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
#endif
#if WINDOWS
            CreateDeviceResources();
#endif
            _maxVertexBufferSlots = _d3dDevice.FeatureLevel >= FeatureLevel.Level_11_0 
                ? InputAssemblerStage.VertexInputResourceSlotCount 
                : 16;
        }

        private void PlatformInitialize()
        {
#if WINDOWS
            CorrectBackBufferSize();
#endif
            CreateSizeDependentResources();
        }

#if WINDOWS_UAP
        /// <summary>
        /// Creates resources not tied the active graphics device.
        /// </summary>
        protected void CreateDeviceIndependentResources()
        {
#if DEBUG
            var debugLevel = SharpDX.Direct2D1.DebugLevel.Information;
#else
            var debugLevel = SharpDX.Direct2D1.DebugLevel.None; 
#endif
            // Dispose previous references.
            if (_d2dFactory != null)
                _d2dFactory.Dispose();
            if (_dwriteFactory != null)
                _dwriteFactory.Dispose();
            if (_wicFactory != null)
                _wicFactory.Dispose();

            // Allocate new references
            _d2dFactory = new SharpDX.Direct2D1.Factory1(SharpDX.Direct2D1.FactoryType.SingleThreaded, debugLevel);
            _dwriteFactory = new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared);
            _wicFactory = new SharpDX.WIC.ImagingFactory2();
        }

        /// <summary>
        /// Create graphics device specific resources.
        /// </summary>
        protected virtual void CreateDeviceResources()
        {
            // Dispose previous references.
            if (_d3dDevice != null)
                _d3dDevice.Dispose();
            if (_d3dContext != null)
                _d3dContext.Dispose();
            if (_d2dDevice != null)
                _d2dDevice.Dispose();
            if (_d2dContext != null)
                _d2dContext.Dispose();

            // Windows requires BGRA support out of DX.
            var creationFlags = SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport;
#if DEBUG
            var enableDebugLayers = true;
#else 
            var enableDebugLayers = false;
#endif

            if (GraphicsAdapter.UseDebugLayers)
            {
                enableDebugLayers = true;
            }

            if (enableDebugLayers)
            {
                creationFlags |= SharpDX.Direct3D11.DeviceCreationFlags.Debug;
            }

            // Pass the preferred feature levels based on the
            // target profile that may have been set by the user.
            FeatureLevel[] featureLevels;
            if (GraphicsProfile == GraphicsProfile.HiDef)
            {
                featureLevels = new[]
                    {
                        FeatureLevel.Level_11_1,
                        FeatureLevel.Level_11_0,
                        FeatureLevel.Level_10_1,
                        FeatureLevel.Level_10_0,
                        // Feature levels below 10 are not supported for the HiDef profile
                    };
            }
            else // Reach profile
            {
                featureLevels = new[]
                    {
                        // For the Reach profile, first try use the highest supported 9_X feature level
                        FeatureLevel.Level_9_3,
                        FeatureLevel.Level_9_2,
                        FeatureLevel.Level_9_1,
                        // If level 9 is not supported, then just use the highest supported level
                        FeatureLevel.Level_11_1,
                        FeatureLevel.Level_11_0,
                        FeatureLevel.Level_10_1,
                        FeatureLevel.Level_10_0,
                    };
            }

            var driverType = GraphicsAdapter.UseReferenceDevice ? DriverType.Reference : DriverType.Hardware;
        
            try 
            {
                // Create the Direct3D device.
                using (var defaultDevice = new SharpDX.Direct3D11.Device(driverType, creationFlags, featureLevels))
                    _d3dDevice = defaultDevice.QueryInterface<SharpDX.Direct3D11.Device1>();

                // Necessary to enable video playback
                var multithread = _d3dDevice.QueryInterface<SharpDX.Direct3D.DeviceMultithread>();
                multithread.SetMultithreadProtected(true);
            }
            catch(SharpDXException)
            {
                // Try again without the debug flag.  This allows debug builds to run
                // on machines that don't have the debug runtime installed.
                creationFlags &= ~SharpDX.Direct3D11.DeviceCreationFlags.Debug;
                using (var defaultDevice = new SharpDX.Direct3D11.Device(driverType, creationFlags, featureLevels))
                    _d3dDevice = defaultDevice.QueryInterface<SharpDX.Direct3D11.Device1>();
            }

            // Get Direct3D 11.1 context
            _d3dContext = _d3dDevice.ImmediateContext.QueryInterface<SharpDX.Direct3D11.DeviceContext1>();

            // Create the Direct2D device.
            using (var dxgiDevice = _d3dDevice.QueryInterface<SharpDX.DXGI.Device>())
                _d2dDevice = new SharpDX.Direct2D1.Device(_d2dFactory, dxgiDevice);

            // Create Direct2D context
            _d2dContext = new SharpDX.Direct2D1.DeviceContext(
                _d2dDevice, SharpDX.Direct2D1.DeviceContextOptions.None);
        }

        internal void CreateSizeDependentResources()
        {
            // Clamp MultiSampleCount
            PresentationParameters.MultiSampleCount =
                GetClampedMultisampleCount(PresentationParameters.MultiSampleCount);

            _d3dContext.OutputMerger.SetTargets(
                (SharpDX.Direct3D11.DepthStencilView)null,
                (SharpDX.Direct3D11.RenderTargetView)null);  

            _d2dContext.Target = null;
            if (_renderTargetView != null)
            {
                _renderTargetView.Dispose();
                _renderTargetView = null;
            }
            if (_depthStencilView != null)
            {
                _depthStencilView.Dispose();
                _depthStencilView = null;
            }
            if (_bitmapTarget != null)
            {
                _bitmapTarget.Dispose();
                _bitmapTarget = null;
            }

            // Clear the current render targets.
            _currentDepthStencilView = null;
            Array.Clear(_currentRenderTargets, 0, _currentRenderTargets.Length);
            Array.Clear(_currentRenderTargetBindings, 0, _currentRenderTargetBindings.Length);
            _currentRenderTargetCount = 0;

            // Make sure all pending rendering commands are flushed.
            _d3dContext.Flush();

            // We need presentation parameters to continue here.
            if (PresentationParameters == null ||
                (PresentationParameters.DeviceWindowHandle == IntPtr.Zero &&
                 PresentationParameters.SwapChainPanel == null))
            {
                if (_swapChain != null)
                {
                    _swapChain.Dispose();
                    _swapChain = null;
                }

                return;
            }

            // Did we change swap panels?
            if (PresentationParameters.SwapChainPanel != _swapChainPanel)
            {
                _swapChainPanel = null;

                if (_swapChain != null)
                {
                    _swapChain.Dispose();
                    _swapChain = null;
                }                
            }

            var format = SharpDXHelper.ToFormat(PresentationParameters.BackBufferFormat);
            var multisampleDesc = GetSupportedSampleDescription(
                format, 
                PresentationParameters.MultiSampleCount);

            // If the swap chain already exists... update it.
            if (_swapChain != null)
            {
                _swapChain.ResizeBuffers(   2,
                                            PresentationParameters.BackBufferWidth,
                                            PresentationParameters.BackBufferHeight,
                                            format, 
                                            SwapChainFlags.None);
           }

            // Otherwise, create a new swap chain.
            else
            {
                // SwapChain description
                var desc = new SharpDX.DXGI.SwapChainDescription1()
                {
                    // Automatic sizing
                    Width = PresentationParameters.BackBufferWidth,
                    Height = PresentationParameters.BackBufferHeight,
                    Format = format,
                    Stereo = false,
                    SampleDescription = multisampleDesc,
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                    BufferCount = 2,
                    SwapEffect = SharpDXHelper.ToSwapEffect(PresentationParameters.PresentationInterval),

                    // By default we scale the backbuffer to the window 
                    // rectangle to function more like a WP7 game.
                    Scaling = SharpDX.DXGI.Scaling.Stretch,
                };

                // Once the desired swap chain description is configured,
                // it must be created on the same adapter as our D3D Device

                // First, retrieve the underlying DXGI Device from the D3D Device.
                // Creates the swap chain 
                using (var dxgiDevice2 = _d3dDevice.QueryInterface<SharpDX.DXGI.Device2>())
                using (var dxgiAdapter = dxgiDevice2.Adapter)
                using (var dxgiFactory2 = dxgiAdapter.GetParent<SharpDX.DXGI.Factory2>())
                {
                    if (PresentationParameters.DeviceWindowHandle != IntPtr.Zero)
                    {
                        // Creates a SwapChain from a CoreWindow pointer.
                        var coreWindow = Marshal.GetObjectForIUnknown(
                            PresentationParameters.DeviceWindowHandle) as CoreWindow;

                        using (var comWindow = new ComObject(coreWindow))
                           _swapChain = new SwapChain1(dxgiFactory2, dxgiDevice2, comWindow, ref desc);
                    }
                    else
                    {
                        _swapChainPanel = PresentationParameters.SwapChainPanel;
                        using (var nativePanel = ComObject.As
                            <SharpDX.DXGI.ISwapChainPanelNative>(PresentationParameters.SwapChainPanel))
                        {
                            _swapChain = new SwapChain1(dxgiFactory2, dxgiDevice2, ref desc, null);
                            nativePanel.SwapChain = _swapChain;
                        }
                    }

                    // Ensure that DXGI does not queue more than one frame at a time. This both reduces 
                    // latency and ensures that the application will only render after each VSync, minimizing 
                    // power consumption.
                    dxgiDevice2.MaximumFrameLatency = 1;
                }
            }

            _swapChain.Rotation = SharpDX.DXGI.DisplayModeRotation.Identity;

            // Counter act the composition scale of the render target as 
            // we already handle this in the platform window code. 
            if (PresentationParameters.SwapChainPanel != null)
            {
                var asyncResult = PresentationParameters.SwapChainPanel.Dispatcher.RunIdleAsync((e) =>
                {   
                    var inverseScale = new RawMatrix3x2();
                    inverseScale.M11 = 1.0f / PresentationParameters.SwapChainPanel.CompositionScaleX;
                    inverseScale.M22 = 1.0f / PresentationParameters.SwapChainPanel.CompositionScaleY;
                    using (var swapChain2 = _swapChain.QueryInterface<SwapChain2>())
                        swapChain2.MatrixTransform = inverseScale;
                });
            }

            // Obtain the backbuffer for this window which will be the final 3D rendertarget.
            Point targetSize;
            using (var backBuffer = SharpDX.Direct3D11.Texture2D.FromSwapChain
                <SharpDX.Direct3D11.Texture2D>(_swapChain, 0))
            {
                // Create a view interface on the rendertarget to use on bind.
                _renderTargetView = new SharpDX.Direct3D11.RenderTargetView(_d3dDevice, backBuffer);

                // Get the rendertarget dimensions for later.
                var backBufferDesc = backBuffer.Description;
                targetSize = new Point(backBufferDesc.Width, backBufferDesc.Height);
            }

            // Create the depth buffer if we need it.
            if (PresentationParameters.DepthStencilFormat != DepthFormat.None)
            {
                var depthFormat = SharpDXHelper.ToFormat(PresentationParameters.DepthStencilFormat);

                // Allocate a 2-D surface as the depth/stencil buffer.
                using (var depthBuffer = new SharpDX.Direct3D11.Texture2D(
                    _d3dDevice, new SharpDX.Direct3D11.Texture2DDescription()
                {
                    Format = depthFormat,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = targetSize.X,
                    Height = targetSize.Y,
                    SampleDescription = multisampleDesc,
                    Usage = SharpDX.Direct3D11.ResourceUsage.Default,
                    BindFlags = SharpDX.Direct3D11.BindFlags.DepthStencil,
                }))

                // Create a DepthStencil view on this surface to use on bind.
                _depthStencilView = new SharpDX.Direct3D11.DepthStencilView(_d3dDevice, depthBuffer);
            }

            // Set the current viewport.
            Viewport = new Viewport
            { 
                X = 0, 
                Y = 0,
                Width = targetSize.X, 
                Height = targetSize.Y, 
                MinDepth = 0.0f, 
                MaxDepth = 1.0f 
            };

            // Now we set up the Direct2D render target bitmap linked to the swapchain. 
            // Whenever we render to this bitmap, it will be directly rendered to the 
            // swapchain associated with the window.
            var bitmapProperties = new SharpDX.Direct2D1.BitmapProperties1(
                new SharpDX.Direct2D1.PixelFormat(format, SharpDX.Direct2D1.AlphaMode.Premultiplied),
                _dpi, _dpi,
                SharpDX.Direct2D1.BitmapOptions.Target | SharpDX.Direct2D1.BitmapOptions.CannotDraw);
            
            // Direct2D needs the dxgi version of the backbuffer surface pointer.
            // Get a D2D surface from the DXGI back buffer to use as the D2D render target.
            using (var dxgiBackBuffer = _swapChain.GetBackBuffer<SharpDX.DXGI.Surface>(0))
                _bitmapTarget = new SharpDX.Direct2D1.Bitmap1(_d2dContext, dxgiBackBuffer, bitmapProperties);

            // So now we can set the Direct2D render target.
            _d2dContext.Target = _bitmapTarget;

            // Set D2D text anti-alias mode to Grayscale to 
            // ensure proper rendering of text on intermediate surfaces.
            _d2dContext.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;
        }

        internal void OnPresentationChanged()
        {
            CreateSizeDependentResources();
            ApplyRenderTargets(null);
        }

#endif

        private void PlatformReset()
        {
#if WINDOWS
            CorrectBackBufferSize();
#endif

#if WINDOWS_UAP
            if (PresentationParameters.DeviceWindowHandle == IntPtr.Zero &&
                PresentationParameters.SwapChainPanel == null)
                throw new ArgumentException(
                    "PresentationParameters.DeviceWindowHandle or PresentationParameters.SwapChainPanel must not be null.");
#else
            if (PresentationParameters.DeviceWindowHandle == IntPtr.Zero)
                throw new ArgumentException("PresentationParameters.DeviceWindowHandle must not be null.");
#endif
        }

#if  WINDOWS_UAP

        private void SetMultiSamplingToMaximum(
            PresentationParameters presentationParameters, out int quality)
        {
            quality = (int)SharpDX.Direct3D11.StandardMultisampleQualityLevels.StandardMultisamplePattern;
        }

#endif
#if WINDOWS

        private void CorrectBackBufferSize()
        {
            // Window size can be modified when we're going full screen, 
            // we need to take that into account so the back buffer has the right size.
            if (PresentationParameters.IsFullScreen)
            {
                int newWidth, newHeight;
                if (PresentationParameters.HardwareModeSwitch)
                    GetModeSwitchedSize(out newWidth, out newHeight);
                else
                    GetDisplayResolution(out newWidth, out newHeight);

                PresentationParameters.BackBufferWidth = newWidth;
                PresentationParameters.BackBufferHeight = newHeight;
            }
        }

        /// <summary>
        /// Create graphics device specific resources.
        /// </summary>
        protected virtual void CreateDeviceResources()
        {
            // Dispose previous references.
            if (_d3dDevice != null)
                _d3dDevice.Dispose();
            if (_d3dContext != null)
                _d3dContext.Dispose();

            // Windows requires BGRA support out of DX.
            var creationFlags = DeviceCreationFlags.BgraSupport;

            if (GraphicsAdapter.UseDebugLayers)
                creationFlags |= DeviceCreationFlags.Debug;

            // Pass the preferred feature levels based on the
            // target profile that may have been set by the user.
            FeatureLevel[] featureLevels;
            if (GraphicsProfile == GraphicsProfile.HiDef)
            {
                featureLevels = new[]
                {
                    FeatureLevel.Level_11_0,
                    FeatureLevel.Level_10_1,
                    FeatureLevel.Level_10_0,
                    // Feature levels below 10 are not supported for the HiDef profile
                };
            }
            else // Reach profile
            {
                featureLevels = new[]
                {
                    // For the Reach profile, first try use the highest supported 9_X feature level
                    FeatureLevel.Level_9_3,
                    FeatureLevel.Level_9_2,
                    FeatureLevel.Level_9_1,
                    // If level 9 is not supported, then just use the highest supported level
                    FeatureLevel.Level_11_0,
                    FeatureLevel.Level_10_1,
                    FeatureLevel.Level_10_0,
                };
            }

            var driverType = DriverType.Hardware;   //Default value
            switch (GraphicsAdapter.UseDriverType)
            {
                case GraphicsAdapter.DriverType.Reference:
                    driverType = DriverType.Reference;
                    break;

                case GraphicsAdapter.DriverType.FastSoftware:
                    driverType = DriverType.Warp;
                    break;
            }
            
            try
            {
                // Create the Direct3D device.
                using (var defaultDevice = new SharpDX.Direct3D11.Device(driverType, creationFlags, featureLevels))
                    _d3dDevice = defaultDevice.QueryInterface<SharpDX.Direct3D11.Device>();
            }
            catch (SharpDXException)
            {
                // Try again without the debug flag.  This allows debug builds to run
                // on machines that don't have the debug runtime installed.
                creationFlags &= ~DeviceCreationFlags.Debug;
                using (var defaultDevice = new SharpDX.Direct3D11.Device(driverType, creationFlags, featureLevels))
                    _d3dDevice = defaultDevice.QueryInterface<SharpDX.Direct3D11.Device>();
            }

            // Get Direct3D 11.1 context
            _d3dContext = _d3dDevice.ImmediateContext.QueryInterface<DeviceContext>();



            // Create a new instance of GraphicsDebug because we support it on Windows platforms.
            GraphicsDebug = new GraphicsDebug(this);
        }

        internal void SetHardwareFullscreen()
        {
            bool state = PresentationParameters.IsFullScreen && PresentationParameters.HardwareModeSwitch;
            _swapChain.SetFullscreenState(state, null);
        }

        internal void ClearHardwareFullscreen()
        {
            _swapChain.SetFullscreenState(false, null);
        }

        internal void ResizeTargets()
        {
            var format = SharpDXHelper.ToFormat(PresentationParameters.BackBufferFormat);
            var descr = new ModeDescription
            {
                Format = format,
#if WINRT
                Scaling = DisplayModeScaling.Stretched,
#else
                Scaling = DisplayModeScaling.Unspecified,
#endif
                Width = PresentationParameters.BackBufferWidth,
                Height = PresentationParameters.BackBufferHeight,
            };

            _swapChain.ResizeTarget(ref descr);
        }

        internal void GetModeSwitchedSize(out int width, out int height)
        {
            Output output = null;
            if (_swapChain == null)
            {
                // get the primary output
                using (var factory = new Factory1())
                using (var adapter = factory.GetAdapter1(0))
                    output = adapter.Outputs[0];
            }
            else
            {
                try
                {
                    output = _swapChain.ContainingOutput;
                }
                catch (SharpDXException) // ContainingOutput fails on a headless device
                {
                }
            }

            var format = SharpDXHelper.ToFormat(PresentationParameters.BackBufferFormat);
            var target = new ModeDescription
            {
                Format = format,
#if WINRT
                Scaling = DisplayModeScaling.Stretched,
#else
                Scaling = DisplayModeScaling.Unspecified,
#endif
                Width = PresentationParameters.BackBufferWidth,
                Height = PresentationParameters.BackBufferHeight,
            };

            if (output == null)
            {
                width = PresentationParameters.BackBufferWidth;
                height = PresentationParameters.BackBufferHeight;
            }
            else
            {
                output.GetClosestMatchingMode(_d3dDevice, target, out ModeDescription closest);
                width = closest.Width;
                height = closest.Height;
                output.Dispose();
            }
        }

        internal void GetDisplayResolution(out int width, out int height)
        {
            width = Adapter.CurrentDisplayMode.Width;
            height = Adapter.CurrentDisplayMode.Height;
        }

        internal void CreateSizeDependentResources()
        {
            // Clamp MultiSampleCount
            PresentationParameters.MultiSampleCount =
                GetClampedMultisampleCount(PresentationParameters.MultiSampleCount);

            _d3dContext.OutputMerger.SetTargets((DepthStencilView)null, (RenderTargetView)null);

            if (_renderTargetView != null)
            {
                _renderTargetView.Dispose();
                _renderTargetView = null;
            }
            if (_depthStencilView != null)
            {
                _depthStencilView.Dispose();
                _depthStencilView = null;
            }

            // Clear the current render targets.
            _currentDepthStencilView = null;
            Array.Clear(_currentRenderTargets, 0, _currentRenderTargets.Length);
            Array.Clear(_currentRenderTargetBindings, 0, _currentRenderTargetBindings.Length);
            RenderTargetCount = 0;

            // Make sure all pending rendering commands are flushed.
            _d3dContext.Flush();

            // We need presentation parameters to continue here.
            if (PresentationParameters == null || PresentationParameters.DeviceWindowHandle == IntPtr.Zero)
            {
                if (_swapChain != null)
                {
                    _swapChain.Dispose();
                    _swapChain = null;
                }
                return;
            }

            var format = SharpDXHelper.ToFormat(PresentationParameters.BackBufferFormat);
            var multisampleDesc = GetSupportedSampleDescription(
                format, 
                PresentationParameters.MultiSampleCount);

            // If the swap chain already exists... update it.
            if (_swapChain != null
                // check if multisampling hasn't changed
                && _swapChain.Description.SampleDescription.Count == multisampleDesc.Count
                && _swapChain.Description.SampleDescription.Quality == multisampleDesc.Quality)
            {
                _swapChain.ResizeBuffers(
                    2, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight,
                    format, SwapChainFlags.AllowModeSwitch);
            }

            // Otherwise, create a new swap chain.
            else
            {
                var wasFullScreen = false;
                // Dispose of old swap chain if exists
                if (_swapChain != null)
                {
                    wasFullScreen = _swapChain.IsFullScreen;
                    // Before releasing a swap chain, first switch to windowed mode
                    _swapChain.SetFullscreenState(false, null);
                    _swapChain.Dispose();
                }

                // SwapChain description
                var desc = new SwapChainDescription()
                {
                    ModeDescription =
                    {
                        Format = format,
#if WINDOWS_UAP
                        Scaling = DisplayModeScaling.Stretched,
#else
                        Scaling = DisplayModeScaling.Unspecified,
#endif
                        Width = PresentationParameters.BackBufferWidth,
                        Height = PresentationParameters.BackBufferHeight,
                    },

                    OutputHandle = PresentationParameters.DeviceWindowHandle,
                    SampleDescription = multisampleDesc,
                    Usage = Usage.RenderTargetOutput,
                    BufferCount = 2,
                    SwapEffect = SharpDXHelper.ToSwapEffect(PresentationParameters.PresentationInterval),
                    IsWindowed = true,
                    Flags = SwapChainFlags.AllowModeSwitch
                };

                // Once the desired swap chain description is configured, 
                // it must be created on the same adapter as our D3D Device

                // First, retrieve the underlying DXGI Device from the D3D Device.
                // Creates the swap chain 
                using (var dxgiDevice = _d3dDevice.QueryInterface<SharpDX.DXGI.Device1>())
                using (var dxgiAdapter = dxgiDevice.Adapter)
                using (var dxgiFactory = dxgiAdapter.GetParent<Factory1>())
                {
                    _swapChain = new SwapChain(dxgiFactory, dxgiDevice, desc);
                    RefreshAdapter();
                    dxgiFactory.MakeWindowAssociation(
                        PresentationParameters.DeviceWindowHandle, WindowAssociationFlags.IgnoreAll);
                    // To reduce latency, ensure that DXGI does not queue more than one frame at a time.
                    // Docs: https://msdn.microsoft.com/en-us/library/windows/desktop/ff471334(v=vs.85).aspx
                    dxgiDevice.MaximumFrameLatency = 1;
                }
                // Preserve full screen state, after swap chain is re-created 
                if (PresentationParameters.HardwareModeSwitch
                    && wasFullScreen)
                    SetHardwareFullscreen();
            }

            // Obtain the backbuffer for this window which will be the final 3D rendertarget.
            Point targetSize;
            using (var backBuffer = DXResource.FromSwapChain<DXTexture2D>(_swapChain, 0))
            {
                // Create a view interface on the rendertarget to use on bind.
                _renderTargetView = new RenderTargetView(_d3dDevice, backBuffer);

                // Get the rendertarget dimensions for later.
                var backBufferDesc = backBuffer.Description;
                targetSize = new Point(backBufferDesc.Width, backBufferDesc.Height);
            }

            // Create the depth buffer if we need it.
            if (PresentationParameters.DepthStencilFormat != DepthFormat.None)
            {
                var depthFormat = SharpDXHelper.ToFormat(PresentationParameters.DepthStencilFormat);

                // Allocate a 2-D surface as the depth/stencil buffer.
                using (var depthBuffer = new DXTexture2D(_d3dDevice, new Texture2DDescription()
                {
                    Format = depthFormat,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = targetSize.X,
                    Height = targetSize.Y,
                    SampleDescription = multisampleDesc,
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
                }))

                    // Create a DepthStencil view on this surface to use on bind.
                    _depthStencilView = new DepthStencilView(_d3dDevice, depthBuffer);
            }

            // Set the current viewport.
            Viewport = new Viewport
            {
                X = 0,
                Y = 0,
                Width = targetSize.X,
                Height = targetSize.Y,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            };
        }

        internal void RefreshAdapter()
        {
            if (_swapChain == null)
                return;

            Output output = null;
            try
            {
                output = _swapChain.ContainingOutput;
            }
            catch (SharpDXException) // ContainingOutput fails on a headless device
            {
            }

            if (output != null)
            {
                foreach (var adapter in GraphicsAdapter.Adapters)
                {
                    if (adapter.DeviceName == output.Description.DeviceName)
                    {
                        Adapter = adapter;
                        break;
                    }
                }
                output.Dispose();
            }
        }

        internal void OnPresentationChanged()
        {
            CreateSizeDependentResources();
            ApplyRenderTargets(null);
        }

#endif // WINDOWS

        /// <summary>
        /// Get highest multisample quality level for specified format and multisample count.
        /// Returns 0 if multisampling is not supported for input parameters.
        /// </summary>
        /// <param name="format">The texture format.</param>
        /// <param name="multiSampleCount">The number of samples during multisampling.</param>
        /// <returns>
        /// Higher than zero if multiSampleCount is supported. 
        /// Zero if multiSampleCount is not supported.
        /// </returns>
        private int GetMultiSamplingQuality(Format format, int multiSampleCount)
        {
            // The valid range is between zero and one less than
            // the level returned by CheckMultisampleQualityLevels
            // https://msdn.microsoft.com/en-us/library/windows/desktop/bb173072(v=vs.85).aspx
            var quality = _d3dDevice.CheckMultisampleQualityLevels(format, multiSampleCount) - 1;
            
            // NOTE: should we always return highest quality?
            return Math.Max(quality, 0); // clamp minimum to 0 
        }

        internal SampleDescription GetSupportedSampleDescription(Format format, int multiSampleCount)
        {
            var multisampleDesc = new SampleDescription(1, 0);

            if (multiSampleCount > 1)
            {
                var quality = GetMultiSamplingQuality(format, multiSampleCount);

                multisampleDesc.Count = multiSampleCount;
                multisampleDesc.Quality = quality;
            }

            return multisampleDesc;
        }

        private void PlatformClear(ClearOptions options, Vector4 color, float depth, int stencil)
        {
            // Clear options for depth/stencil buffer if not attached.
            if (_currentDepthStencilView != null)
            {
                if (_currentDepthStencilView.Description.Format != Format.D24_UNorm_S8_UInt)
                    options &= ~ClearOptions.Stencil;
            }
            else
            {
                options &= ~ClearOptions.DepthBuffer;
                options &= ~ClearOptions.Stencil;
            }

            lock (_d3dContext)
            {
                // Clear the diffuse render buffer.
                if ((options & ClearOptions.Target) == ClearOptions.Target)
                {
                    foreach (var view in _currentRenderTargets)
                    {
                        if (view != null)
                            _d3dContext.ClearRenderTargetView(
                                view, new RawColor4(color.X, color.Y, color.Z, color.W));
                    }
                }

                // Clear the depth/stencil render buffer.
                DepthStencilClearFlags flags = 0;
                if (options.HasFlags(ClearOptions.DepthBuffer))
                    flags |= DepthStencilClearFlags.Depth;

                if (options.HasFlags(ClearOptions.Stencil))
                    flags |= DepthStencilClearFlags.Stencil;

                if (flags != 0)
                    _d3dContext.ClearDepthStencilView(_currentDepthStencilView, flags, depth, (byte)stencil);
            }
        }

        private void PlatformDispose()
        {
            // make sure to release full screen or this might cause issues on exit
            if (_swapChain != null && _swapChain.IsFullScreen)
                _swapChain.SetFullscreenState(false, null);

            SharpDX.Utilities.Dispose(ref _renderTargetView);
            SharpDX.Utilities.Dispose(ref _depthStencilView);

            _userIndexBuffer16?.Dispose();
            _userIndexBuffer32?.Dispose();

            foreach (var vb in _userVertexBuffers.Values)
                vb.Dispose();

            SharpDX.Utilities.Dispose(ref _swapChain);

#if WINDOWS_UAP
            if (_bitmapTarget != null)
            {
                _bitmapTarget.Dispose();
                _depthStencilView = null;
            }

            _d2dDevice?.Dispose();
            _d2dDevice = null;

            _d2dContext?.Target = null;
            _d2dContext?.Dispose();
            _d2dContext = null;

            _d2dFactory?.Dispose();
            _d2dFactory = null;
            
            _dwriteFactory?.Dispose();
            _dwriteFactory = null;

            _wicFactory?.Dispose();
            _wicFactory = null;
#endif

            SharpDX.Utilities.Dispose(ref _d3dContext);
            SharpDX.Utilities.Dispose(ref _d3dDevice);
        }

        private void PlatformPresent()
        {
#if WINDOWS_UAP
            // The application may optionally specify "dirty" or "scroll" rects to improve efficiency
            // in certain scenarios.  In this sample, however, we do not utilize those features.
            var parameters = new SharpDX.DXGI.PresentParameters();
            
            try
            {
                // TODO: Hook in PresentationParameters here!

                // The first argument instructs DXGI to block until VSync, putting the application
                // to sleep until the next VSync. This ensures we don't waste any cycles rendering
                // frames that will never be displayed to the screen.
                lock (_d3dContext)
                    _swapChain.Present(1, PresentFlags.None, parameters);
            }
            catch (SharpDX.SharpDXException)
            {
                // TODO: How should we deal with a device lost case here?
                /*               
                // If the device was removed either by a disconnect or a driver upgrade, we 
                // must completely reinitialize the renderer.
                if (    ex.ResultCode == SharpDX.DXGI.DXGIError.DeviceRemoved ||
                        ex.ResultCode == SharpDX.DXGI.DXGIError.DeviceReset)
                    this.Initialize();
                else
                    throw;
                */
            }

#endif
#if WINDOWS

            try
            {
                var syncInterval = PresentationParameters.PresentationInterval.GetSyncInterval();

                // The first argument instructs DXGI to block n VSyncs before presenting.
                lock (_d3dContext)
                    _swapChain.Present(syncInterval, PresentFlags.None);
            }
            catch (SharpDXException)
            {
                // TODO: How should we deal with a device lost case here?
            }
#endif
        }

        private void PlatformSetViewport(Viewport value)
        {
            if (_d3dContext != null)
            {
                var rawViewport = new RawViewportF
                {
                    X = value.X,
                    Y = value.Y,
                    Width = value.Width,
                    Height = value.Height,
                    MinDepth = value.MinDepth,
                    MaxDepth = value.MaxDepth
                };
                lock (_d3dContext)
                    _d3dContext.Rasterizer.SetViewport(rawViewport);
            }
        }

        // Only implemented for DirectX right now, so not in GraphicsDevice.cs
        public void SetRenderTarget(RenderTarget2D renderTarget, int arraySlice)
        {
            if (!GraphicsCapabilities.SupportsTextureArrays)
                throw new InvalidOperationException(
                    "Texture arrays are not supported on this graphics device.");

            if (renderTarget == null)
                SetRenderTarget(null);
            else
                SetRenderTargets(new RenderTargetBinding(renderTarget, arraySlice));
        }

        // Only implemented for DirectX right now, so not in GraphicsDevice.cs
        public void SetRenderTarget(RenderTarget3D renderTarget, int arraySlice)
        {
            if (renderTarget == null)
                SetRenderTarget(null);
            else
                SetRenderTargets(new RenderTargetBinding(renderTarget, arraySlice));
        }

        private void PlatformApplyDefaultRenderTarget()
        {
            // Set the default swap chain.
            Array.Clear(_currentRenderTargets, 0, _currentRenderTargets.Length);
            _currentRenderTargets[0] = _renderTargetView;
            _currentDepthStencilView = _depthStencilView;

            lock (_d3dContext)
                _d3dContext.OutputMerger.SetTargets(_currentDepthStencilView, _currentRenderTargets);
        }

        internal void PlatformResolveRenderTargets()
        {
            for (var i = 0; i < RenderTargetCount; i++)
            {
                var renderTargetBinding = _currentRenderTargetBindings[i];

                // Resolve MSAA render targets
                if (renderTargetBinding.RenderTarget is RenderTarget2D renderTarget &&
                    renderTarget.MultiSampleCount > 1)
                    renderTarget.ResolveSubresource();

                // Generate mipmaps.
                if (renderTargetBinding.RenderTarget.LevelCount > 1)
                {
                    lock (_d3dContext)
                        _d3dContext.GenerateMips(renderTargetBinding.RenderTarget.GetShaderResourceView());
                }
            }
        }

        private IRenderTarget PlatformApplyRenderTargets()
        {
            // Clear the current render targets.
            Array.Clear(_currentRenderTargets, 0, _currentRenderTargets.Length);
            _currentDepthStencilView = null;

            // Make sure none of the new targets are bound
            // to the device as a texture resource.
            lock (_d3dContext)
            {
                VertexTextures.ClearTargets(this, _currentRenderTargetBindings);
                Textures.ClearTargets(this, _currentRenderTargetBindings);
            }

            for (var i = 0; i < RenderTargetCount; i++)
            {
                var binding = _currentRenderTargetBindings[i];
                var target = (IRenderTarget)binding.RenderTarget;
                _currentRenderTargets[i] = target.GetRenderTargetView(binding.ArraySlice);
            }

            // Use the depth from the first target.
            var renderTarget = (IRenderTarget)_currentRenderTargetBindings[0].RenderTarget;
            _currentDepthStencilView = renderTarget.GetDepthStencilView();

            // Set the targets.
            lock (_d3dContext)
                _d3dContext.OutputMerger.SetTargets(_currentDepthStencilView, _currentRenderTargets);

            return renderTarget;
        }

#if WINDOWS_UAP
        internal void ResetRenderTargets()
        {
            if (_d3dContext != null)
            {
                lock (_d3dContext)
                {
                    var viewport = new RawViewportF
                    {
                        X = _viewport.X,
                        Y = _viewport.Y,
                        Width = _viewport.Width,
                        Height = _viewport.Height,
                        MinDepth = _viewport.MinDepth,
                        MaxDepth = _viewport.MaxDepth
                    };
                    _d3dContext.Rasterizer.SetViewport(viewport);
                    _d3dContext.OutputMerger.SetTargets(_currentDepthStencilView, _currentRenderTargets);
                }
            }

            Textures.Dirty();
            SamplerStates.Dirty();
            _depthStencilStateDirty = true;
            _blendStateDirty = true;
            _indexBufferDirty = true;
            _vertexBuffersDirty = true;
            _pixelShaderDirty = true;
            _vertexShaderDirty = true;
            _rasterizerStateDirty = true;
            _scissorRectangleDirty = true;            
        }
#endif

        private static PrimitiveTopology ToPrimitiveTopology(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.LineList:
                    return PrimitiveTopology.LineList;
                case PrimitiveType.LineStrip:
                    return PrimitiveTopology.LineStrip;
                case PrimitiveType.TriangleList:
                    return PrimitiveTopology.TriangleList;
                case PrimitiveType.TriangleStrip:
                    return PrimitiveTopology.TriangleStrip;
            }

            throw new ArgumentException();
        }

        internal void PlatformBeginApplyState()
        {
            Debug.Assert(_d3dContext != null, "The d3d context is null!");
        }

        private void PlatformApplyBlend()
        {
            if (_blendFactorDirty || _blendStateDirty)
            {
                var state = _actualBlendState.GetDxState(this);
                var factor = GetBlendFactor();
                _d3dContext.OutputMerger.SetBlendState(state, factor);

                _blendFactorDirty = false;
                _blendStateDirty = false;
            }
        }

        private RawColor4 GetBlendFactor()
        {
            return new RawColor4(
                    BlendFactor.R / 255.0f,
                    BlendFactor.G / 255.0f,
                    BlendFactor.B / 255.0f,
                    BlendFactor.A / 255.0f);
        }

        internal void PlatformApplyState(bool applyShaders)
        {
            // NOTE: This code assumes _d3dContext has been locked by the caller.

            if (_scissorRectangleDirty)
            {
                _d3dContext.Rasterizer.SetScissorRectangle(
                    _scissorRectangle.X,
                    _scissorRectangle.Y,
                    _scissorRectangle.Right,
                    _scissorRectangle.Bottom);
                _scissorRectangleDirty = false;
            }

            // If we're not applying shaders then early out now.
            if (!applyShaders)
                return;

            if (_indexBufferDirty)
            {
                if (_indexBuffer != null)
                {
                    _d3dContext.InputAssembler.SetIndexBuffer(
                        _indexBuffer._buffer,
                        _indexBuffer.IndexElementSize == IndexElementSize.SixteenBits ?
                            Format.R16_UInt : Format.R32_UInt,
                        0);
                }
                _indexBufferDirty = false;
            }

            if (_vertexBuffersDirty)
            {
                if (_vertexBuffers.Count > 0)
                {
                    for (int slot = 0; slot < _vertexBuffers.Count; slot++)
                    {
                        var vertexBufferBinding = _vertexBuffers.Get(slot);
                        var vertexBuffer = vertexBufferBinding.VertexBuffer;
                        var vertexDeclaration = vertexBuffer.VertexDeclaration;
                        int vertexStride = vertexDeclaration.VertexStride;
                        int vertexOffsetInBytes = vertexBufferBinding.VertexOffset * vertexStride;
                        _d3dContext.InputAssembler.SetVertexBuffers(
                            slot, new SharpDX.Direct3D11.VertexBufferBinding(
                                vertexBuffer._buffer, vertexStride, vertexOffsetInBytes));
                    }
                    _vertexBufferSlotsUsed = _vertexBuffers.Count;
                }
                else
                {
                    for (int slot = 0; slot < _vertexBufferSlotsUsed; slot++)
                        _d3dContext.InputAssembler.SetVertexBuffers(
                            slot, new SharpDX.Direct3D11.VertexBufferBinding());

                    _vertexBufferSlotsUsed = 0;
                }
            }

            if (_vertexShader == null)
                throw new InvalidOperationException("A vertex shader must be set!");
            if (_pixelShader == null)
                throw new InvalidOperationException("A pixel shader must be set!");

            if (VertexShaderDirty)
            {
                _d3dContext.VertexShader.Set(_vertexShader.VertexShader);

                unchecked
                {
                    _graphicsMetrics._vertexShaderCount++;
                }
            }
            if (VertexShaderDirty || _vertexBuffersDirty)
            {
                _d3dContext.InputAssembler.InputLayout = _vertexShader.InputLayouts.GetOrCreate(_vertexBuffers);
                VertexShaderDirty = _vertexBuffersDirty = false;
            }

            if (PixelShaderDirty)
            {
                _d3dContext.PixelShader.Set(_pixelShader.PixelShader);
                PixelShaderDirty = false;

                unchecked
                {
                    _graphicsMetrics._pixelShaderCount++;
                }
            }

            _vertexConstantBuffers.SetConstantBuffers(this);
            _pixelConstantBuffers.SetConstantBuffers(this);

            VertexTextures.SetTextures(this);
            VertexSamplerStates.PlatformSetSamplers(this);
            Textures.SetTextures(this);
            SamplerStates.PlatformSetSamplers(this);
        }

        private void SetUserVertexBuffer<T>(
            ReadOnlySpan<T> vertexData, VertexDeclaration declaration)
            where T : unmanaged
        {
            if (!_userVertexBuffers.TryGetValue(declaration, out DynamicVertexBuffer buffer) || 
                buffer.Capacity < vertexData.Length)
            {
                // Dispose the previous buffer if we have one.
                if (buffer != null)
                    buffer.Dispose();

                buffer = new DynamicVertexBuffer(
                    this, declaration, Math.Max(vertexData.Length, 2000), BufferUsage.WriteOnly);
                _userVertexBuffers[declaration] = buffer;
            }

            buffer.SetData(0, vertexData, declaration.VertexStride, SetDataOptions.Discard);
            SetVertexBuffer(buffer);
        }

        private unsafe void SetUserIndexBuffer<TIndex>(
            ReadOnlySpan<TIndex> indexData, IndexElementSize indexElementSize)
            where TIndex : unmanaged
        {
            DynamicIndexBuffer buffer;
            int requiredIndexCount = Math.Max(indexData.Length, 6000);
            if (indexElementSize == IndexElementSize.SixteenBits)
            {
                if (_userIndexBuffer16 == null || _userIndexBuffer16.Capacity < requiredIndexCount)
                {
                    _userIndexBuffer16?.Dispose();
                    _userIndexBuffer16 = new DynamicIndexBuffer(
                        this, indexElementSize, requiredIndexCount, BufferUsage.WriteOnly);
                }
                buffer = _userIndexBuffer16;
            }
            else
            {
                if (_userIndexBuffer32 == null || _userIndexBuffer32.Capacity < requiredIndexCount)
                {
                    _userIndexBuffer32?.Dispose();
                    _userIndexBuffer32 = new DynamicIndexBuffer(
                        this, indexElementSize, requiredIndexCount, BufferUsage.WriteOnly);
                }
                buffer = _userIndexBuffer32;                
            }

            buffer.SetData(indexData, SetDataOptions.Discard);
            Indices = buffer;
        }

        private void PlatformDrawIndexedPrimitives(
            PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount)
        {
            lock (_d3dContext)
            {
                ApplyState(true);

                _d3dContext.InputAssembler.PrimitiveTopology = ToPrimitiveTopology(primitiveType);

                var indexCount = GetElementCountForType(primitiveType, primitiveCount);
                _d3dContext.DrawIndexed(indexCount, startIndex, baseVertex);
            }
        }

        private void PlatformDrawUserPrimitives<T>(
            PrimitiveType primitiveType, ReadOnlySpan<T> vertexData, VertexDeclaration vertexDeclaration) 
            where T : unmanaged
        {
            SetUserVertexBuffer(vertexData, vertexDeclaration);

            lock (_d3dContext)
            {
                ApplyState(true);

                _d3dContext.InputAssembler.PrimitiveTopology = ToPrimitiveTopology(primitiveType);
                _d3dContext.Draw(vertexData.Length, 0);
            }
        }

        private void PlatformDrawPrimitives(PrimitiveType primitiveType, int vertexStart, int vertexCount)
        {
            lock (_d3dContext)
            {
                ApplyState(true);

                _d3dContext.InputAssembler.PrimitiveTopology = ToPrimitiveTopology(primitiveType);
                _d3dContext.Draw(vertexCount, vertexStart);
            }
        }

        private unsafe void PlatformDrawUserIndexedPrimitives<TVertex, TIndex>(
            PrimitiveType primitiveType, ReadOnlySpan<TVertex> vertexData,
            IndexElementSize indexElementSize, ReadOnlySpan<TIndex> indexData, 
            int primitiveCount, VertexDeclaration vertexDeclaration) 
            where TVertex : unmanaged
            where TIndex : unmanaged
        {
            int indexCount = GetElementCountForType(primitiveType, primitiveCount);
            SetUserVertexBuffer(vertexData, vertexDeclaration);
            SetUserIndexBuffer(indexData, indexElementSize);

            lock (_d3dContext)
            {
                ApplyState(true);

                _d3dContext.InputAssembler.PrimitiveTopology = ToPrimitiveTopology(primitiveType);
                _d3dContext.DrawIndexed(indexCount, 0, 0);
            }
        }

        private void PlatformDrawInstancedPrimitives(
            PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount, int instanceCount)
        {
            lock (_d3dContext)
            {
                ApplyState(true);

                _d3dContext.InputAssembler.PrimitiveTopology = ToPrimitiveTopology(primitiveType);
                int indexCount = GetElementCountForType(primitiveType, primitiveCount);
                _d3dContext.DrawIndexedInstanced(indexCount, instanceCount, startIndex, baseVertex, 0);
            }
        }

        private unsafe void PlatformGetBackBufferData<T>(Rectangle rect, Span<T> destination)
            where T : unmanaged
        {
            // TODO share code with Texture2D.GetData and do pooling for staging textures
            // first set up a staging texture
            const SurfaceFormat format = SurfaceFormat.Rgba32;

            //You can't Map the BackBuffer surface, so we copy to another texture
            using (var backBufferTexture = DXResource.FromSwapChain<DXTexture2D>(_swapChain, 0))
            {
                var desc = backBufferTexture.Description;
                desc.SampleDescription = new SampleDescription(1, 0);
                desc.BindFlags = BindFlags.None;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;
                desc.OptionFlags = ResourceOptionFlags.None;

                bool isRectFull =
                    rect.X == 0 &&
                    rect.Y == 0 &&
                    rect.Width == desc.Width &&
                    rect.Height == desc.Height;

                using (var stagingTex = new DXTexture2D(_d3dDevice, desc))
                {
                    lock (_d3dContext)
                    {
                        // Copy the data from the GPU to the staging texture.
                        // if MSAA is enabled we need to first copy to a resource without MSAA
                        if (backBufferTexture.Description.SampleDescription.Count > 1)
                        {
                            desc.Usage = ResourceUsage.Default;
                            desc.CpuAccessFlags = CpuAccessFlags.None;
                            using (var noMsTex = new DXTexture2D(_d3dDevice, desc))
                            {
                                _d3dContext.ResolveSubresource(backBufferTexture, 0, noMsTex, 0, desc.Format);
                                if (isRectFull)
                                {
                                    _d3dContext.CopySubresourceRegion(noMsTex, 0,
                                        new ResourceRegion(rect.Left, rect.Top, 0, rect.Right, rect.Bottom, 1), stagingTex,
                                        0);
                                }
                                else
                                    _d3dContext.CopyResource(noMsTex, stagingTex);
                            }
                        }
                        else
                        {
                            if (isRectFull)
                            {
                                _d3dContext.CopySubresourceRegion(backBufferTexture, 0,
                                    new ResourceRegion(rect.Left, rect.Top, 0, rect.Right, rect.Bottom, 1), stagingTex, 0);
                            }
                            else
                                _d3dContext.CopyResource(backBufferTexture, stagingTex);
                        }

                        // Copy the data to the array.
                        DataStream stream = null;
                        try
                        {
                            var databox = _d3dContext.MapSubresource(
                                stagingTex, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out stream);

                            int elementsInRow, rows;
                            if (isRectFull)
                            {
                                elementsInRow = rect.Width;
                                rows = rect.Height;
                            }
                            else
                            {
                                elementsInRow = stagingTex.Description.Width;
                                rows = stagingTex.Description.Height;
                            }

                            var data = new T[destination.Length];
                            var elementSize = format.GetSize();
                            var rowSize = elementSize * elementsInRow;
                            if (rowSize == databox.RowPitch)
                            {
                                stream.ReadRange(data, 0, destination.Length);
                            }
                            else
                            {
                                // Some drivers may add pitch to rows.
                                // We need to copy each row separately and skip trailing zeroes.
                                stream.Seek(0, System.IO.SeekOrigin.Begin);

                                for (var row = 0; row < rows; row++)
                                {
                                    int i;
                                    for (i = row * rowSize / sizeof(T); i < (row + 1) * rowSize / sizeof(T); i++)
                                        data[i] = stream.Read<T>();

                                    if (i >= destination.Length)
                                        break;

                                    stream.Seek(databox.RowPitch - rowSize, System.IO.SeekOrigin.Current);
                                }
                            }

                            data.CopyTo(destination);
                        }
                        finally
                        {
                            SharpDX.Utilities.Dispose(ref stream);
                        }
                    }
                }
            }

            /*

            // TODO share code with Texture2D.GetData and pool staging textures
            // first set up a staging texture
            const SurfaceFormat format = SurfaceFormat.Rgba32;

            //You can't Map the BackBuffer surface, so we copy to another texture
            using (var backBufferTexture = DXResource.FromSwapChain<DXTexture2D>(_swapChain, 0))
            {
                var desc = backBufferTexture.Description;
                desc.SampleDescription = new SampleDescription(1, 0);
                desc.BindFlags = BindFlags.None;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;
                desc.OptionFlags = ResourceOptionFlags.None;
                
                using (var stagingTex = new DXTexture2D(_d3dDevice, desc))
                {
                    var resRegion = new ResourceRegion(rect.Left, rect.Top, 0, rect.Right, rect.Bottom, 1);
                    bool isRectFull = 
                        rect.X == 0 &&
                        rect.Y == 0 &&
                        rect.Width == desc.Width &&
                        rect.Height == desc.Height;

                    lock (_d3dContext)
                    {
                        // Copy the data from the GPU to the staging texture.
                        // if MSAA is enabled we need to first copy to a resource without MSAA
                        if (backBufferTexture.Description.SampleDescription.Count > 1)
                        {
                            desc.Usage = ResourceUsage.Default;
                            desc.CpuAccessFlags = CpuAccessFlags.None;

                            using (var noMsTex = new DXTexture2D(_d3dDevice, desc))
                            {
                                _d3dContext.ResolveSubresource(backBufferTexture, 0, noMsTex, 0, desc.Format);
                                if (isRectFull)
                                    _d3dContext.CopyResource(noMsTex, stagingTex);
                                else
                                    _d3dContext.CopySubresourceRegion(noMsTex, 0, resRegion, stagingTex, 0);
                            }
                        }
                        else
                        {
                            if (isRectFull)
                                _d3dContext.CopyResource(backBufferTexture, stagingTex);
                            else
                                _d3dContext.CopySubresourceRegion(backBufferTexture, 0, resRegion, stagingTex, 0);
                        }

                        var box = _d3dContext.MapSubresource(stagingTex, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                        CopyResourceTo(format, box, rect.Width, rect.Height, destination);
                    }
                }
            }

            */
        }

        private void PlatformFlush()
        {
            _d3dContext.Flush();
        }

        internal static unsafe void CopyResourceTo<T>(
            SurfaceFormat format, DataBox box, int columns, int rows, Span<T> dst)
            where T : unmanaged
        {
            var byteSrc = new ReadOnlySpan<byte>((void*)box.DataPointer, box.RowPitch * rows);
            var byteDst = MemoryMarshal.AsBytes(dst);

            int rowBytes = format.GetSize() * columns;
            if (rowBytes == box.RowPitch)
            {
                byteSrc.CopyTo(byteDst);
            }
            else
            {
                int trailBytes = box.RowPitch - rowBytes;
                int byteOffset = 0;
                for (int row = 0; row < rows; row++)
                {
                    var byteSrcSlice = byteSrc.Slice(byteOffset);
                    var srcSlice = MemoryMarshal.Cast<byte, T>(byteSrcSlice);

                    int start = row * rowBytes / sizeof(T);
                    int end = (row + 1) * rowBytes / sizeof(T);
                    int x = 0;

                    // iterate between start and end of the row in memory
                    for (int i = start; i < end; i++, x++)
                        dst[i] = srcSlice[x];

                    if (end >= dst.Length)
                        break;

                    byteOffset += x * sizeof(T);
                    byteOffset += trailBytes;
                }
            }
        }

#if WINDOWS_UAP
        internal void Trim()
        {
            using (var dxgiDevice3 = _d3dDevice.QueryInterface<SharpDX.DXGI.Device3>())
                dxgiDevice3.Trim();
        }
#endif

        private static Rectangle PlatformGetTitleSafeArea(int x, int y, int width, int height)
        {
            return new Rectangle(x, y, width, height);
        }
    }
}
