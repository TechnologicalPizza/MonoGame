﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Media
{
    public sealed class Artist : IDisposable
    {

        /// <summary>
        /// Gets the AlbumCollection for the Artist.
        /// </summary>
        public AlbumCollection Albums => throw new NotImplementedException();

        /// <summary>
        /// Gets a value indicating whether the object is disposed.
        /// </summary>
        public bool IsDisposed => true;

        /// <summary>
        /// Gets the name of the Artist.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the SongCollection for the Artist.
        /// </summary>
        public SongCollection Songs => throw new NotImplementedException();

        public Artist(string artist)
        {
            this.Name = artist;
        }

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Returns a String representation of the Artist.
        /// </summary>
        public override string ToString()
        {
            return this.Name.ToString();
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}
