// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if ANGLE
using OpenTK.Graphics;
#else
using MonoGame.OpenGL;
#endif

namespace MonoGame.Framework.Graphics
{
    public partial class GraphicsDevice
    {
#if DESKTOPGL || ANGLE
        internal IGraphicsContext Context { get; private set; }
#endif

#if !GLES
        private DrawBuffersEnum[] _drawBuffers;
#endif

        private List<ResourceHandle> _disposeThisFrame = new List<ResourceHandle>();
        private List<ResourceHandle> _disposeNextFrame = new List<ResourceHandle>();
        private object _disposeActionsLock = new object();

        private static List<IntPtr> _disposeContexts = new List<IntPtr>();
        private static object _disposeContextsLock = new object();

        private ShaderProgramCache _programCache;
        private ShaderProgram _shaderProgram = null;

        private static BufferBindingInfo[] _bufferBindingInfos;
        private static bool[] _newEnabledVertexAttributes;
        internal static HashSet<int> _enabledVertexAttributes = new HashSet<int>();
        internal static bool _attribsDirty;

        internal FramebufferHelper _framebufferHelper;

        internal int _glMajorVersion = 0;
        internal int _glMinorVersion = 0;
        internal int _glFramebuffer = 0;
        internal int MaxVertexAttributes;

        // Keeps track of last applied state to avoid redundant OpenGL calls
        internal bool _lastBlendEnable = false;
        internal BlendState _lastBlendState = new BlendState();
        internal DepthStencilState _lastDepthStencilState = new DepthStencilState();
        internal RasterizerState _lastRasterizerState = new RasterizerState();

        private DepthStencilState _clearDepthStencilState = new DepthStencilState { StencilEnable = true };
        private Vector4 _lastClearColor = Vector4.Zero;
        private float _lastClearDepth = 1f;
        private int _lastClearStencil = 0;

        /// <summary>
        /// Get a hashed value based on the currently bound shaders.
        /// </summary>
        /// <exception cref="InvalidOperationException">No shaders are bound.</exception>
        private int ShaderProgramHash
        {
            get
            {
                if (_vertexShader == null && _pixelShader == null)
                    throw new InvalidOperationException("There is no shader bound.");

                if (_vertexShader == null)
                    return _pixelShader.HashKey;
                if (_pixelShader == null)
                    return _vertexShader.HashKey;

                int hash = 17 * 23 + _vertexShader.HashKey;
                return hash * 23 + _pixelShader.HashKey;
            }
        }

        internal void SetVertexAttributeArray(bool[] attrs)
        {
            for (int i = 0; i < attrs.Length; i++)
            {
                bool contains = _enabledVertexAttributes.Contains(i);
                if (attrs[i] && !contains)
                {
                    _enabledVertexAttributes.Add(i);
                    GL.EnableVertexAttribArray(i);
                    GraphicsExtensions.CheckGLError();
                }
                else if (!attrs[i] && contains)
                {
                    _enabledVertexAttributes.Remove(i);
                    GL.DisableVertexAttribArray(i);
                    GraphicsExtensions.CheckGLError();
                }
            }
        }

        private void ApplyAttribs(Shader shader, int baseVertex)
        {
            int programHash = ShaderProgramHash;
            bool bindingsChanged = false;

            int vertexBufferCount = _vertexBuffers.Count;
            for (int slot = 0; slot < vertexBufferCount; slot++)
            {
                var vertexBufferBinding = _vertexBuffers.Get(slot);
                var vertexDeclaration = vertexBufferBinding.VertexBuffer.VertexDeclaration;
                var attrInfo = vertexDeclaration.GetAttributeInfo(shader, programHash);

                var vertexStride = vertexDeclaration.VertexStride;
                var offset = (IntPtr)(vertexDeclaration.VertexStride * (baseVertex + vertexBufferBinding.VertexOffset));

                if (!_attribsDirty)
                {
                    var info = _bufferBindingInfos[slot];
                    if (info.VertexOffset == offset &&
                        info.InstanceFrequency == vertexBufferBinding.InstanceFrequency &&
                        info.Vbo == vertexBufferBinding.VertexBuffer._vbo &&
                        ReferenceEquals(info.AttributeInfo, attrInfo))
                        continue;
                }

                bindingsChanged = true;

                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferBinding.VertexBuffer._vbo);
                GraphicsExtensions.CheckGLError();

                // If InstanceFrequency of the buffer is not zero
                // and instancing is not supported, throw an exception.
                if (vertexBufferBinding.InstanceFrequency > 0)
                    AssertSupportsInstancing();

                var elements = attrInfo.Elements;
                for (int i = 0; i < elements.Count; i++)
                {
                    var element = elements[i];

                    GL.VertexAttribPointer(element.AttributeLocation,
                        element.NumberOfElements,
                        element.VertexAttribPointerType,
                        element.Normalized,
                        vertexStride,
                        offset + element.Offset);

                    // only set the divisor if instancing is supported
                    if (GraphicsCapabilities.SupportsInstancing)
                        GL.VertexAttribDivisor(element.AttributeLocation, vertexBufferBinding.InstanceFrequency);

                    GraphicsExtensions.CheckGLError();
                }

                _bufferBindingInfos[slot].VertexOffset = offset;
                _bufferBindingInfos[slot].AttributeInfo = attrInfo;
                _bufferBindingInfos[slot].InstanceFrequency = vertexBufferBinding.InstanceFrequency;
                _bufferBindingInfos[slot].Vbo = vertexBufferBinding.VertexBuffer._vbo;
            }

            _attribsDirty = false;

            if (bindingsChanged)
            {
                for (int i = 0; i < _newEnabledVertexAttributes.Length; i++)
                    _newEnabledVertexAttributes[i] = false;

                for (var slot = 0; slot < vertexBufferCount; slot++)
                {
                    var elements = _bufferBindingInfos[slot].AttributeInfo.Elements;
                    for (int i = 0, c = elements.Count; i < c; i++)
                        _newEnabledVertexAttributes[elements[i].AttributeLocation] = true;
                }
            }
            SetVertexAttributeArray(_newEnabledVertexAttributes);
        }

        private void PlatformSetup()
        {
            _programCache = new ShaderProgramCache(this);

#if DESKTOPGL || ANGLE
            var windowInfo = new WindowInfo(SdlGameWindow.Instance.Handle);

            if (Context == null || Context.IsDisposed)
                Context = GL.CreateContext(windowInfo);

            Context.MakeCurrent(windowInfo);
            Context.SwapInterval = PresentationParameters.PresentationInterval.GetSwapInterval();

            Context.MakeCurrent(windowInfo);
#endif
            GL.GetInteger(GetPName.MaxCombinedTextureImageUnits, out MaxTextureSlots);
            GraphicsExtensions.CheckGLError();

            GL.GetInteger(GetPName.MaxTextureSize, out int maxTexture2DSize);
            GraphicsExtensions.CheckGLError();
            MaxTexture2DSize = maxTexture2DSize;

            GL.GetInteger(GetPName.MaxTextureSize, out int maxTexture3DSize);
            GraphicsExtensions.CheckGLError();
            MaxTexture3DSize = maxTexture2DSize;

            GL.GetInteger(GetPName.MaxTextureSize, out int maxTextureCubeSize);
            GraphicsExtensions.CheckGLError();
            MaxTextureCubeSize = maxTextureCubeSize;

            GL.GetInteger(GetPName.MaxVertexAttribs, out MaxVertexAttributes);
            GraphicsExtensions.CheckGLError();

            _maxVertexBufferSlots = MaxVertexAttributes;
            _newEnabledVertexAttributes = new bool[MaxVertexAttributes];

            // try getting the context version
            // GL_MAJOR_VERSION and GL_MINOR_VERSION are GL 3.0+ only, so we need to rely on the GL_VERSION string
            // for non GLES this string always starts with the version number in the "major.minor" format,
            // but can be followed by multiple vendor specific characters.
            // For GLES this string is formatted as: OpenGL ES <version number> <vendor-specific information>

            try
            {
                string version = GL.GetString(StringName.Version);
                if (string.IsNullOrEmpty(version))
                    throw new NoSuitableGraphicsDeviceException("Unable to retrieve OpenGL version.");
#if GLES
                string[] versionSplit = version.Split(' ');
                if (versionSplit.Length > 2 && versionSplit[0].Equals("OpenGL") && versionSplit[1].Equals("ES"))
                {
                    _glMajorVersion = Convert.ToInt32(versionSplit[2].Substring(0, 1));
                    _glMinorVersion = Convert.ToInt32(versionSplit[2].Substring(2, 1));
                }
                else
                {
                    _glMajorVersion = 1;
                    _glMinorVersion = 1;
                }
#else
                _glMajorVersion = Convert.ToInt32(version.Substring(0, 1));
                _glMinorVersion = Convert.ToInt32(version.Substring(2, 1));
#endif
            }
            catch (FormatException)
            {
                //if it fails we default to 1.1 context
                _glMajorVersion = 1;
                _glMinorVersion = 1;
            }

#if !GLES
            // Initialize draw buffer attachment array
            GL.GetInteger(GetPName.MaxDrawBuffers, out int maxDrawBuffers);
            GraphicsExtensions.CheckGLError();

            _drawBuffers = new DrawBuffersEnum[maxDrawBuffers];
            for (int i = 0; i < maxDrawBuffers; i++)
                _drawBuffers[i] = (DrawBuffersEnum)(FramebufferAttachment.ColorAttachment0Ext + i);
#endif
        }

        private void PlatformInitialize()
        {
            _viewport = new Viewport(
                0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

            // Ensure the vertex attributes are reset
            _enabledVertexAttributes.Clear();

            // Free all the cached shader programs. 
            _programCache.Clear();
            _shaderProgram = null;

            _framebufferHelper = FramebufferHelper.Create(this);

            // Force resetting states
            PlatformApplyBlend(true);
            DepthStencilState.PlatformApplyState(this, true);
            RasterizerState.PlatformApplyState(this, true);

            _bufferBindingInfos = new BufferBindingInfo[_maxVertexBufferSlots];
            for (int i = 0; i < _bufferBindingInfos.Length; i++)
                _bufferBindingInfos[i] = new BufferBindingInfo { Vbo = -1 };
        }

        private void PlatformClear(ClearOptions options, Vector4 color, float depth, int stencil)
        {
            // TODO: We need to figure out how to detect if we have a
            // depth stencil buffer or not, and clear options relating
            // to them if not attached.

            // Unlike with XNA and DirectX...  GL.Clear() obeys several
            // different render states:
            //
            //  - The color write flags.
            //  - The scissor rectangle.
            //  - The depth/stencil state.
            //
            // So overwrite these states with what is needed to perform
            // the clear correctly and restore it afterwards.
            //
            var prevScissorRect = ScissorRectangle;
            var prevDepthStencilState = DepthStencilState;
            var prevBlendState = BlendState;
            ScissorRectangle = _viewport.Bounds;
            // DepthStencilState.Default has the Stencil Test disabled; 
            // make sure stencil test is enabled before we clear since
            // some drivers won't clear with stencil test disabled
            DepthStencilState = _clearDepthStencilState;
            BlendState = BlendState.Opaque;
            ApplyState(false);

            ClearBufferMask bufferMask = 0;
            if ((options & ClearOptions.Target) == ClearOptions.Target)
            {
                if (color != _lastClearColor)
                {
                    GL.ClearColor(color.X, color.Y, color.Z, color.W);
                    GraphicsExtensions.CheckGLError();
                    _lastClearColor = color;
                }
                bufferMask |= ClearBufferMask.ColorBufferBit;
            }
            if ((options & ClearOptions.Stencil) == ClearOptions.Stencil)
            {
                if (stencil != _lastClearStencil)
                {
                    GL.ClearStencil(stencil);
                    GraphicsExtensions.CheckGLError();
                    _lastClearStencil = stencil;
                }
                bufferMask |= ClearBufferMask.StencilBufferBit;
            }

            if ((options & ClearOptions.DepthBuffer) == ClearOptions.DepthBuffer)
            {
                if (depth != _lastClearDepth)
                {
                    GL.ClearDepth(depth);
                    GraphicsExtensions.CheckGLError();
                    _lastClearDepth = depth;
                }
                bufferMask |= ClearBufferMask.DepthBufferBit;
            }

#if MONOMAC
            if (GL.CheckFramebufferStatus(FramebufferTarget.FramebufferExt) == FramebufferErrorCode.FramebufferComplete)
#endif
            {
                GL.Clear(bufferMask);
                GraphicsExtensions.CheckGLError();
            }

            // Restore the previous render state.
            ScissorRectangle = prevScissorRect;
            DepthStencilState = prevDepthStencilState;
            BlendState = prevBlendState;
        }

        private void PlatformFlush()
        {
            GL.Flush();
        }

        private void PlatformReset()
        {
        }

        private void PlatformDispose()
        {
            // Free all the cached shader programs.
            _programCache.Dispose();

#if DESKTOPGL || ANGLE
            Context?.Dispose();
            Context = null;
#endif

            _lastBlendState?.Dispose();
            _lastBlendState = null;

            _lastDepthStencilState?.Dispose();
            _lastDepthStencilState = null;

            _lastRasterizerState?.Dispose();
            _lastRasterizerState = null;

            _clearDepthStencilState?.Dispose();
            _clearDepthStencilState = null;
        }

        internal void DisposeTexture(int handle)
        {
            if (IsDisposed)
                return;

            lock (_disposeActionsLock)
            {
                _disposeNextFrame.Add(ResourceHandle.Texture(handle));
            }
        }

        internal void DisposeBuffer(int handle)
        {
            if (IsDisposed)
                return;

            lock (_disposeActionsLock)
            {
                _disposeNextFrame.Add(ResourceHandle.Buffer(handle));
            }
        }

        internal void DisposeShader(int handle)
        {
            if (IsDisposed)
                return;

            lock (_disposeActionsLock)
            {
                _disposeNextFrame.Add(ResourceHandle.Shader(handle));
            }
        }

        internal void DisposeProgram(int handle)
        {
            if (IsDisposed)
                return;

            lock (_disposeActionsLock)
            {
                _disposeNextFrame.Add(ResourceHandle.Program(handle));
            }
        }

        internal void DisposeQuery(int handle)
        {
            if (IsDisposed)
                return;

            lock (_disposeActionsLock)
            {
                _disposeNextFrame.Add(ResourceHandle.Query(handle));
            }
        }

        internal void DisposeFramebuffer(int handle)
        {
            if (IsDisposed)
                return;

            lock (_disposeActionsLock)
            {
                _disposeNextFrame.Add(ResourceHandle.Framebuffer(handle));
            }
        }

#if DESKTOPGL || ANGLE
        static internal void DisposeContext(IntPtr resource)
        {
            lock (_disposeContextsLock)
            {
                _disposeContexts.Add(resource);
            }
        }

        static internal void DisposeContexts()
        {
            lock (_disposeContextsLock)
            {
                int count = _disposeContexts.Count;
                for (int i = 0; i < count; ++i)
                    SDL.GL.DeleteContext(_disposeContexts[i]);
                _disposeContexts.Clear();
            }
        }
#endif

        private void PlatformPresent()
        {
#if DESKTOPGL || ANGLE
            Context.SwapBuffers();
#endif
            GraphicsExtensions.CheckGLError();

            // Dispose of any GL resources that were disposed in another thread
            int count = _disposeThisFrame.Count;
            for (int i = 0; i < count; ++i)
                _disposeThisFrame[i].Free();
            _disposeThisFrame.Clear();

            lock (_disposeActionsLock)
            {
                // Swap lists so resources added during this draw will be released after the next draw
                var temp = _disposeThisFrame;
                _disposeThisFrame = _disposeNextFrame;
                _disposeNextFrame = temp;
            }
        }

        private void PlatformSetViewport(in Viewport value)
        {
            if (IsRenderTargetBound)
            {
                GL.Viewport(value.X, value.Y, value.Width, value.Height);
            }
            else
            {
                var pp = PresentationParameters;
                GL.Viewport(value.X, pp.BackBufferHeight - value.Y - value.Height, value.Width, value.Height);
            }
            GraphicsExtensions.LogGLError("GraphicsDevice.Viewport_set() GL.Viewport");

            GL.DepthRange(value.MinDepth, value.MaxDepth);
            GraphicsExtensions.LogGLError("GraphicsDevice.Viewport_set() GL.DepthRange");

            // In OpenGL we have to re-apply the special "posFixup"
            // vertex shader uniform if the viewport changes.
            VertexShaderDirty = true;
        }

        private void PlatformApplyDefaultRenderTarget()
        {
            _framebufferHelper.BindFramebuffer(_glFramebuffer);

            // Reset the raster state because we flip vertices
            // when rendering offscreen and hence the cull direction.
            _rasterizerStateDirty = true;

            // Textures will need to be rebound to render correctly in the new render target.
            Textures.Dirty();
        }

        /// <summary>
        /// FBO cache, we create 1 FBO per RenderTargetBinding combination.
        /// </summary>
        private Dictionary<RenderTargetBinding[], int> _glFramebuffers =
            new Dictionary<RenderTargetBinding[], int>(RenderTargetBindingArrayComparer.Instance);

        /// <summary>
        /// FBO cache used to resolve MSAA rendertargets, we create 1 FBO per RenderTargetBinding combination
        /// </summary>
        private Dictionary<RenderTargetBinding[], int> _glResolveFramebuffers =
            new Dictionary<RenderTargetBinding[], int>(RenderTargetBindingArrayComparer.Instance);

        internal void PlatformCreateRenderTarget(
            IRenderTarget renderTarget, int width, int height, bool mipMap,
            SurfaceFormat preferredFormat,
            DepthFormat preferredDepthFormat,
            int preferredMultiSampleCount,
            RenderTargetUsage usage)
        {
            void Create()
            {
                var color = 0;
                var depth = 0;
                var stencil = 0;

                if (preferredMultiSampleCount > 0 && _framebufferHelper.SupportsBlitFramebuffer)
                {
                    _framebufferHelper.GenRenderbuffer(out color);
                    _framebufferHelper.BindRenderbuffer(color);
                    _framebufferHelper.RenderbufferStorageMultisample(
                        preferredMultiSampleCount, (int)RenderbufferStorage.Rgba8, width, height);
                }

                if (preferredDepthFormat != DepthFormat.None)
                {
                    var depthInternalFormat = RenderbufferStorage.DepthComponent16;
                    var stencilInternalFormat = (RenderbufferStorage)0;
                    switch (preferredDepthFormat)
                    {
                        case DepthFormat.Depth16:
                            depthInternalFormat = RenderbufferStorage.DepthComponent16;
                            break;
#if GLES
                    case DepthFormat.Depth24:
                        if (GraphicsCapabilities.SupportsDepth24)
                            depthInternalFormat = RenderbufferStorage.DepthComponent24Oes;
                        else if (GraphicsCapabilities.SupportsDepthNonLinear)
                            depthInternalFormat = (RenderbufferStorage)0x8E2C;
                        else
                            depthInternalFormat = RenderbufferStorage.DepthComponent16;
                        break;
                    case DepthFormat.Depth24Stencil8:
                        if (GraphicsCapabilities.SupportsPackedDepthStencil)
                            depthInternalFormat = RenderbufferStorage.Depth24Stencil8Oes;
                        else
                        {
                            if (GraphicsCapabilities.SupportsDepth24)
                                depthInternalFormat = RenderbufferStorage.DepthComponent24Oes;
                            else if (GraphicsCapabilities.SupportsDepthNonLinear)
                                depthInternalFormat = (RenderbufferStorage)0x8E2C;
                            else
                                depthInternalFormat = RenderbufferStorage.DepthComponent16;
                            stencilInternalFormat = RenderbufferStorage.StencilIndex8;
                            break;
                        }
                        break;
#else
                        case DepthFormat.Depth24:
                            depthInternalFormat = RenderbufferStorage.DepthComponent24;
                            break;
                        case DepthFormat.Depth24Stencil8:
                            depthInternalFormat = RenderbufferStorage.Depth24Stencil8;
                            break;
#endif
                    }

                    if (depthInternalFormat != 0)
                    {
                        _framebufferHelper.GenRenderbuffer(out depth);
                        _framebufferHelper.BindRenderbuffer(depth);
                        _framebufferHelper.RenderbufferStorageMultisample(
                            preferredMultiSampleCount, (int)depthInternalFormat, width, height);

                        if (preferredDepthFormat == DepthFormat.Depth24Stencil8)
                        {
                            stencil = depth;
                            if (stencilInternalFormat != 0)
                            {
                                _framebufferHelper.GenRenderbuffer(out stencil);
                                _framebufferHelper.BindRenderbuffer(stencil);
                                _framebufferHelper.RenderbufferStorageMultisample(
                                    preferredMultiSampleCount, (int)stencilInternalFormat, width, height);
                            }
                        }
                    }
                }

                if (color != 0)
                    renderTarget.GLColorBuffer = color;
                else
                    renderTarget.GLColorBuffer = renderTarget.GLTexture;
                renderTarget.GLDepthBuffer = depth;
                renderTarget.GLStencilBuffer = stencil;
            }

            if (Threading.IsOnMainThread)
                Create();
            else
                Threading.BlockOnMainThread(Create);
        }

        internal void PlatformDeleteRenderTarget(IRenderTarget renderTarget)
        {
            void Delete()
            {
                int color = renderTarget.GLColorBuffer;
                int depth = renderTarget.GLDepthBuffer;
                int stencil = renderTarget.GLStencilBuffer;
                bool colorIsRenderbuffer = color != renderTarget.GLTexture;

                if (color != 0)
                {
                    if (colorIsRenderbuffer)
                        _framebufferHelper.DeleteRenderbuffer(color);
                    if (stencil != 0 && stencil != depth)
                        _framebufferHelper.DeleteRenderbuffer(stencil);
                    if (depth != 0)
                        _framebufferHelper.DeleteRenderbuffer(depth);

                    var bindingsToDelete = new List<RenderTargetBinding[]>();
                    foreach (var bindings in _glFramebuffers.Keys)
                    {
                        foreach (var binding in bindings)
                        {
                            if (binding.RenderTarget == renderTarget)
                            {
                                bindingsToDelete.Add(bindings);
                                break;
                            }
                        }
                    }

                    foreach (var bindings in bindingsToDelete)
                    {
                        if (_glFramebuffers.TryGetValue(bindings, out int fbo))
                        {
                            _framebufferHelper.DeleteFramebuffer(fbo);
                            _glFramebuffers.Remove(bindings);
                        }
                        if (_glResolveFramebuffers.TryGetValue(bindings, out fbo))
                        {
                            _framebufferHelper.DeleteFramebuffer(fbo);
                            _glResolveFramebuffers.Remove(bindings);
                        }
                    }
                }
            }
            if (Threading.IsOnMainThread)
                Delete();
            else
                Threading.BlockOnMainThread(Delete);
        }

        private void PlatformResolveRenderTargets()
        {
            if (RenderTargetCount == 0)
                return;

            var renderTargetBinding = _currentRenderTargetBindings[0];
            var renderTarget = renderTargetBinding.RenderTarget as IRenderTarget;
            if (renderTarget.MultiSampleCount > 0 && _framebufferHelper.SupportsBlitFramebuffer)
            {
                if (!_glResolveFramebuffers.TryGetValue(_currentRenderTargetBindings, out int glResolveFramebuffer))
                {
                    _framebufferHelper.GenFramebuffer(out glResolveFramebuffer);
                    _framebufferHelper.BindFramebuffer(glResolveFramebuffer);
                    for (var i = 0; i < RenderTargetCount; ++i)
                    {
                        var rt = _currentRenderTargetBindings[i].RenderTarget as IRenderTarget;
                        var texTarget = (int)rt.GetFramebufferTarget(renderTargetBinding);
                        _framebufferHelper.FramebufferTexture2D((int)(
                            FramebufferAttachment.ColorAttachment0 + i), texTarget, rt.GLTexture);
                    }
                    _glResolveFramebuffers.Add(
                        (RenderTargetBinding[])_currentRenderTargetBindings.Clone(), glResolveFramebuffer);
                }
                else
                {
                    _framebufferHelper.BindFramebuffer(glResolveFramebuffer);
                }

                // The only fragment operations which affect the resolve are 
                // the pixel ownership test, the scissor test, and dithering.
                if (_lastRasterizerState.ScissorTestEnable)
                {
                    GL.Disable(EnableCap.ScissorTest);
                    GraphicsExtensions.CheckGLError();
                }

                var glFramebuffer = _glFramebuffers[_currentRenderTargetBindings];
                _framebufferHelper.BindReadFramebuffer(glFramebuffer);
                for (var i = 0; i < RenderTargetCount; ++i)
                {
                    renderTargetBinding = _currentRenderTargetBindings[i];
                    renderTarget = renderTargetBinding.RenderTarget as IRenderTarget;
                    _framebufferHelper.BlitFramebuffer(i, renderTarget.Width, renderTarget.Height);
                }

                if (renderTarget.RenderTargetUsage == RenderTargetUsage.DiscardContents &&
                    _framebufferHelper.SupportsInvalidateFramebuffer)
                    _framebufferHelper.InvalidateReadFramebuffer();

                if (_lastRasterizerState.ScissorTestEnable)
                {
                    GL.Enable(EnableCap.ScissorTest);
                    GraphicsExtensions.CheckGLError();
                }
            }
            for (var i = 0; i < RenderTargetCount; ++i)
            {
                renderTargetBinding = _currentRenderTargetBindings[i];
                renderTarget = renderTargetBinding.RenderTarget as IRenderTarget;
                if (renderTarget.LevelCount > 1)
                {
                    GL.BindTexture(renderTarget.GLTarget, renderTarget.GLTexture);
                    GraphicsExtensions.CheckGLError();
                    _framebufferHelper.GenerateMipmap((int)renderTarget.GLTarget);
                }
            }
        }

        private IRenderTarget PlatformApplyRenderTargets()
        {
            if (!_glFramebuffers.TryGetValue(_currentRenderTargetBindings, out int glFramebuffer))
            {
                _framebufferHelper.GenFramebuffer(out glFramebuffer);
                _framebufferHelper.BindFramebuffer(glFramebuffer);

                var renderTargetBinding = _currentRenderTargetBindings[0];
                var renderTarget = renderTargetBinding.RenderTarget as IRenderTarget;
                var depthBuffer = renderTarget.GLDepthBuffer;
                _framebufferHelper.FramebufferRenderbuffer((int)FramebufferAttachment.DepthAttachment, depthBuffer, 0);
                _framebufferHelper.FramebufferRenderbuffer((int)FramebufferAttachment.StencilAttachment, depthBuffer, 0);

                for (int i = 0; i < RenderTargetCount; ++i)
                {
                    renderTargetBinding = _currentRenderTargetBindings[i];
                    renderTarget = renderTargetBinding.RenderTarget as IRenderTarget;
                    var attachement = (int)(FramebufferAttachment.ColorAttachment0 + i);
                    if (renderTarget.GLColorBuffer != renderTarget.GLTexture)
                        _framebufferHelper.FramebufferRenderbuffer(attachement, renderTarget.GLColorBuffer, 0);
                    else
                        _framebufferHelper.FramebufferTexture2D(
                            attachement, (int)renderTarget.GetFramebufferTarget(renderTargetBinding),
                            renderTarget.GLTexture, 0, renderTarget.MultiSampleCount);
                }

#if DEBUG
                _framebufferHelper.CheckFramebufferStatus();
#endif
                _glFramebuffers.Add((RenderTargetBinding[])_currentRenderTargetBindings.Clone(), glFramebuffer);
            }
            else
            {
                _framebufferHelper.BindFramebuffer(glFramebuffer);
            }
#if !GLES
            GL.DrawBuffers(RenderTargetCount, _drawBuffers);
#endif

            // Reset the raster state because we flip vertices
            // when rendering offscreen and hence the cull direction.
            _rasterizerStateDirty = true;

            // Textures will need to be rebound to render correctly in the new render target.
            Textures.Dirty();

            return _currentRenderTargetBindings[0].RenderTarget as IRenderTarget;
        }

        private static GLPrimitiveType PrimitiveTypeGL(PrimitiveType primitiveType)
        {
            return primitiveType switch
            {
                PrimitiveType.LineList => GLPrimitiveType.Lines,
                PrimitiveType.LineStrip => GLPrimitiveType.LineStrip,
                PrimitiveType.TriangleList => GLPrimitiveType.Triangles,
                PrimitiveType.TriangleStrip => GLPrimitiveType.TriangleStrip,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        /// <summary>
        /// Activates the Current Vertex/Pixel shader pair into a program.         
        /// </summary>
        private unsafe void ActivateShaderProgram()
        {
            // Lookup the shader program.
            var shaderProgram = _programCache.GetProgram(VertexShader, PixelShader);
            if (shaderProgram.Program == -1)
                return;
            // Set the new program if it has changed.
            if (_shaderProgram != shaderProgram)
            {
                GL.UseProgram(shaderProgram.Program);
                GraphicsExtensions.CheckGLError();
                _shaderProgram = shaderProgram;
            }

            var posFixupLoc = shaderProgram.GetUniformLocation("posFixup");
            if (posFixupLoc == -1)
                return;

            // Apply vertex shader fix:
            // The following two lines are appended to the end of vertex shaders
            // to account for rendering differences between OpenGL and DirectX:
            //
            // gl_Position.y = gl_Position.y * posFixup.y;
            // gl_Position.xy += posFixup.zw * gl_Position.ww;
            //
            // (the following paraphrased from wine, wined3d/state.c and wined3d/glsl_shader.c)
            //
            // - We need to flip along the y-axis in case of offscreen rendering.
            // - D3D coordinates refer to pixel centers while GL coordinates refer
            //   to pixel corners.
            // - D3D has a top-left filling convention. We need to maintain this
            //   even after the y-flip mentioned above.
            // In order to handle the last two points, we translate by
            // (63.0 / 128.0) / VPw and (63.0 / 128.0) / VPh. This is equivalent to
            // translating slightly less than half a pixel. We want the difference to
            // be large enough that it doesn't get lost due to rounding inside the
            // driver, but small enough to prevent it from interfering with any
            // anti-aliasing.
            //
            // OpenGL coordinates specify the center of the pixel while d3d coords specify
            // the corner. The offsets are stored in z and w in posFixup. posFixup.y contains
            // 1.0 or -1.0 to turn the rendering upside down for offscreen rendering. PosFixup.x
            // contains 1.0 to allow a mad.

            Span<float> posFixup = stackalloc float[4];
            posFixup[0] = 1f;
            posFixup[1] = 1f;

            if (UseHalfPixelOffset)
            {
                posFixup[2] = 63.0f / 64.0f / Viewport.Width;
                posFixup[3] = -(63.0f / 64.0f) / Viewport.Height;
            }
            else
            {
                posFixup[2] = 0f;
                posFixup[3] = 0f;
            }

            //If we have a render target bound (rendering offscreen)
            if (IsRenderTargetBound)
            {
                //flip vertically
                posFixup[1] *= -1f;
                posFixup[3] *= -1f;
            }

            GL.Uniform4(posFixupLoc, 1, posFixup);
            GraphicsExtensions.CheckGLError();
        }

        internal void PlatformBeginApplyState()
        {
            Threading.EnsureMainThread();
        }

        private void PlatformApplyBlend(bool force = false)
        {
            _actualBlendState.PlatformApplyState(this, force);
            ApplyBlendFactor(force);
        }

        private void ApplyBlendFactor(bool force)
        {
            if (force || BlendFactor != _lastBlendState.BlendFactor)
            {
                GL.BlendColor(
                    BlendFactor.R / 255f,
                    BlendFactor.G / 255f,
                    BlendFactor.B / 255f,
                    BlendFactor.A / 255f);
                GraphicsExtensions.CheckGLError();
                _lastBlendState.BlendFactor = BlendFactor;
            }
        }

        internal void PlatformApplyState(bool applyShaders)
        {
            if (_scissorRectangleDirty)
            {
                var scissorRect = _scissorRectangle;
                if (!IsRenderTargetBound)
                    scissorRect.Y = PresentationParameters.BackBufferHeight - (scissorRect.Y + scissorRect.Height);

                GL.Scissor(scissorRect.X, scissorRect.Y, scissorRect.Width, scissorRect.Height);
                GraphicsExtensions.CheckGLError();
                _scissorRectangleDirty = false;
            }

            // If we're not applying shaders then early out now.
            if (!applyShaders)
                return;

            if (_indexBufferDirty && _indexBuffer != null)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer._vbo);
                GraphicsExtensions.CheckGLError();
            }
            _indexBufferDirty = false;

            if (_vertexShader == null)
                throw new InvalidOperationException("A vertex shader must be set.");
            if (_pixelShader == null)
                throw new InvalidOperationException("A pixel shader must be set.");

            if (VertexShaderDirty || PixelShaderDirty)
            {
                ActivateShaderProgram();

                unchecked
                {
                    if (VertexShaderDirty)
                        _graphicsMetrics._vertexShaderCount++;

                    if (PixelShaderDirty)
                        _graphicsMetrics._pixelShaderCount++;
                }

                VertexShaderDirty = false;
                PixelShaderDirty = false;
            }

            _vertexConstantBuffers.SetConstantBuffers(this, _shaderProgram);
            _pixelConstantBuffers.SetConstantBuffers(this, _shaderProgram);

            Textures.SetTextures(this);
            SamplerStates.PlatformSetSamplers(this);
        }

        private void PlatformDrawIndexedPrimitives(
            PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount)
        {
            ApplyState(true);
            ApplyAttribs(_vertexShader, baseVertex);

            bool shortIndices = _indexBuffer.IndexElementSize == IndexElementSize.SixteenBits;
            int indexElementCount = GetElementCountForType(primitiveType, primitiveCount);
            var indexElementType = shortIndices ? IndexElementType.UnsignedShort : IndexElementType.UnsignedInt;
            var indexOffsetInBytes = new IntPtr(startIndex * (shortIndices ? 2 : 4));

            GL.DrawElements(PrimitiveTypeGL(primitiveType), indexElementCount, indexElementType, indexOffsetInBytes);
            GraphicsExtensions.CheckGLError();
        }

        private void PlatformDrawPrimitives(PrimitiveType primitiveType, int vertexStart, int vertexCount)
        {
            ApplyState(true);
            ApplyAttribs(_vertexShader, 0);

            if (vertexStart < 0)
                vertexStart = 0;

            GL.DrawArrays(PrimitiveTypeGL(primitiveType), vertexStart, vertexCount);
            GraphicsExtensions.CheckGLError();
        }

        private unsafe void PlatformDrawUserPrimitives<T>(
            PrimitiveType type, ReadOnlySpan<T> vertices, VertexDeclaration declaration)
            where T : unmanaged
        {
            ApplyState(true);

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _indexBufferDirty = true;

            fixed (T* vertexPtr = vertices)
            {
                // Setup the vertex declaration to point at the VB data.
                declaration.GraphicsDevice = this;
                declaration.Apply(_vertexShader, (IntPtr)vertexPtr, ShaderProgramHash);

                //Draw
                GL.DrawArrays(PrimitiveTypeGL(type), 0, vertices.Length);
                GraphicsExtensions.CheckGLError();
            }
        }

        private unsafe void PlatformDrawUserIndexedPrimitives<TVertex, TIndex>(
            PrimitiveType type, ReadOnlySpan<TVertex> vertices,
            IndexElementSize indexElementSize, ReadOnlySpan<TIndex> indices,
            int primitiveCount, VertexDeclaration declaration)
            where TVertex : unmanaged
            where TIndex : unmanaged
        {
            int indexSize = sizeof(TIndex);
            var indexType = indexElementSize == IndexElementSize.SixteenBits ?
                IndexElementType.UnsignedShort : IndexElementType.UnsignedInt;

            ApplyState(true);

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _indexBufferDirty = true;

            fixed (TVertex* vertexPtr = vertices)
            {
                // Setup the vertex declaration to point at the data.
                declaration.GraphicsDevice = this;
                declaration.Apply(_vertexShader, (IntPtr)vertexPtr, ShaderProgramHash);

                fixed (TIndex* indexPtr = indices)
                {
                    var glPrimitive = PrimitiveTypeGL(type);
                    int count = GetElementCountForType(type, primitiveCount);

                    GL.DrawElements(glPrimitive, count, indexType, (IntPtr)indexPtr);
                    GraphicsExtensions.CheckGLError();
                }
            }
        }

        private void PlatformDrawInstancedPrimitives(
            PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount,
            int instanceCount, int baseInstance = 0)
        {
            AssertSupportsInstancing();
            ApplyState(true);

            var shortIndices = _indexBuffer.IndexElementSize == IndexElementSize.SixteenBits;
            var indexElementType = shortIndices ? IndexElementType.UnsignedShort : IndexElementType.UnsignedInt;
            var indexOffsetInBytes = new IntPtr(startIndex * (shortIndices ? 2 : 4));
            var indexElementCount = GetElementCountForType(primitiveType, primitiveCount);
            var target = PrimitiveTypeGL(primitiveType);

            ApplyAttribs(_vertexShader, baseVertex);

            if (baseInstance > 0)
            {
                if (!GraphicsCapabilities.SupportsBaseIndexInstancing)
                    throw new PlatformNotSupportedException(
                        "Instanced geometry drawing with base instance requires at least OpenGL 4.2. " +
                        "Try upgrading your graphics card drivers.");

                GL.DrawElementsInstancedBaseInstance(
                    target,
                    indexElementCount,
                    indexElementType,
                    indexOffsetInBytes,
                    instanceCount,
                    baseInstance);
            }
            else
            {
                GL.DrawElementsInstanced(
                    target,
                    indexElementCount,
                    indexElementType,
                    indexOffsetInBytes,
                    instanceCount);
            }
            GraphicsExtensions.CheckGLError();
        }

        [DebuggerHidden]
        private void AssertSupportsInstancing()
        {
            if (!GraphicsCapabilities.SupportsInstancing)
                throw new PlatformNotSupportedException(
                    "Instanced geometry drawing requires at least OpenGL 3.2 or GLES 3.2. " +
                    "Try upgrading your graphics card drivers.");
        }

        private void PlatformGetBackBufferData<T>(Span<T> destination, Rectangle rect)
            where T : unmanaged
        {
            unsafe
            {
                fixed (T* ptr = destination)
                {
                    int flippedY = PresentationParameters.BackBufferHeight - rect.Bottom;
                    GL.ReadPixels(
                        rect.X, flippedY, rect.Width, rect.Height,
                        PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
                }
            }

            // ReadPixels returns data upside down, so we must swap rows around
            int rowBytes = rect.Width * PresentationParameters.BackBufferFormat.GetSize();
            int rowSize = rowBytes / Unsafe.SizeOf<T>();
            int count = destination.Length;

            Span<byte> buffer = stackalloc byte[Math.Min(2048, rowBytes)];
            var rowBuffer = MemoryMarshal.Cast<byte, T>(buffer);

            for (int dy = 0; dy < rect.Height / 2; dy++)
            {
                int left = Math.Min(count, rowSize);
                if (left == 0)
                    break;

                int offset = 0;
                var topRow = destination.Slice(dy * rowSize, rowSize);
                var bottomRow = destination.Slice((rect.Height - dy - 1) * rowSize, rowSize);

                while (left > 0)
                {
                    int toCopy = Math.Min(left, rowBuffer.Length);

                    var bottomRowSlice = bottomRow.Slice(offset, toCopy);
                    bottomRowSlice.CopyTo(rowBuffer);

                    var topRowSlice = topRow.Slice(offset, toCopy);
                    topRowSlice.CopyTo(bottomRowSlice);

                    rowBuffer.Slice(0, toCopy).CopyTo(topRowSlice);

                    count -= toCopy;
                    offset += toCopy;
                    left -= toCopy;
                }
            }
        }

        private static Rectangle PlatformGetTitleSafeArea(int x, int y, int width, int height)
        {
            return new Rectangle(x, y, width, height);
        }

        internal void PlatformSetMultiSamplingToMaximum(
            PresentationParameters presentationParameters, out int quality)
        {
            presentationParameters.MultiSampleCount = 4;
            quality = 0;
        }

        internal void OnPresentationChanged()
        {
#if DESKTOPGL || ANGLE
            Context.MakeCurrent(new WindowInfo(SdlGameWindow.Instance.Handle));
            Context.SwapInterval = PresentationParameters.PresentationInterval.GetSwapInterval();
#endif

            ApplyRenderTargets(null);
        }

        // Holds information for caching
        private class BufferBindingInfo
        {
            public IntPtr VertexOffset;
            public int InstanceFrequency;
            public int Vbo;
            public VertexDeclaration.VertexDeclarationAttributeInfo AttributeInfo;
        }

        // FIXME: why is this even here
        //private void GetModeSwitchedSize(out int width, out int height)
        //{
        //    var mode = new Sdl.Display.Mode
        //    {
        //        Width = PresentationParameters.BackBufferWidth,
        //        Height = PresentationParameters.BackBufferHeight,
        //        Format = 0,
        //        RefreshRate = 0,
        //        DriverData = IntPtr.Zero
        //    };
        //    Sdl.Display.GetClosestDisplayMode(0, mode, out Sdl.Display.Mode closest);
        //    width = closest.Width;
        //    height = closest.Height;
        //}
        //
        //private void GetDisplayResolution(out int width, out int height)
        //{
        //    Sdl.Display.GetCurrentDisplayMode(0, out Sdl.Display.Mode mode);
        //    width = mode.Width;
        //    height = mode.Height;
        //}
    }
}