// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MonoGame.Framework.Content;
using MonoGame.Framework.Memory;
using MonoGame.Framework.Utilities;

namespace MonoGame.Framework.Graphics
{
    /// <summary>
    /// Internal helper for accessing the bytecode for stock effects.
    /// </summary>
    internal partial class EffectResource
    {
        public static readonly EffectResource AlphaTestEffect = new EffectResource(AlphaTestEffectName);
        public static readonly EffectResource BasicEffect = new EffectResource(BasicEffectName);
        public static readonly EffectResource DualTextureEffect = new EffectResource(DualTextureEffectName);
        public static readonly EffectResource EnvironmentMapEffect = new EffectResource(EnvironmentMapEffectName);
        public static readonly EffectResource SkinnedEffect = new EffectResource(SkinnedEffectName);
        public static readonly EffectResource SpriteEffect = new EffectResource(SpriteEffectName);

        private readonly object _readMutex = new object();
        private readonly string _name;
        private volatile byte[] _bytecode;

        private EffectResource(string name)
        {
            _name = name;
        }

        public byte[] ByteCode
        {
            get
            {
                if (_bytecode == null)
                {
                    lock (_readMutex)
                    {
                        if (_bytecode != null)
                            return _bytecode;

                        var assembly = typeof(EffectResource).Assembly;
                        var stream = assembly.GetManifestResourceStream(_name);
                        if (stream == null)
                            throw new ContentLoadException($"Missing effect resource named \"{_name}\".");

                        using (var ms = RecyclableMemoryManager.Default.GetMemoryStream())
                        {
                            stream.PooledCopyTo(ms);
                            _bytecode = ms.ToArray();
                        }
                    }
                }
                return _bytecode;
            }
        }
    }
}
