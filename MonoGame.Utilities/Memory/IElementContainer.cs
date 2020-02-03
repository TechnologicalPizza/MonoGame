﻿using System;

namespace MonoGame.Framework.Memory
{
    public interface IElementContainer : IDisposable
    {
        /// <summary>
        /// Gets the amount of elements within the container.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the size of one element.
        /// </summary>
        int ElementSize { get; }
    }
}
