// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections;
using System.Collections.Generic;

namespace MonoGame.Framework.Input
{
    /// <summary>
    /// Holds the state of keystrokes by a keyboard.
    /// </summary>
    public struct KeyboardState : IReadOnlyCollection<Keys>, IEquatable<KeyboardState>
    {
        /// <summary>
        /// Gets the max amount of keystrokes that can
        /// be tracked by a <see cref="KeyboardState"/>. 
        /// </summary>
        public const int MaxKeysPerState = 8;

        #region Key Data

        // Array of 256 bits:
        private uint keys0, keys1, keys2, keys3, keys4, keys5, keys6, keys7;

        /// <summary>
        /// Gets the amount of pressed keys.
        /// </summary>
        public readonly int Count => GetPressedKeyCount();

        private readonly uint GetKeyField(int index)
        {
            return index switch
            {
                0 => keys0,
                1 => keys1,
                2 => keys2,
                3 => keys3,
                4 => keys4,
                5 => keys5,
                6 => keys6,
                7 => keys7,
                _ => 0
            };
        }

        private readonly bool InternalGetKey(Keys key)
        {
            int index = ((int)key) >> 5;
            uint field = GetKeyField(index);
            uint mask = (uint)1 << (((int)key) & 0x1f);
            return (field & mask) != 0;
        }

        internal void InternalSetKey(Keys key)
        {
            uint mask = (uint)1 << (((int)key) & 0x1f);
            switch (((int)key) >> 5)
            {
                case 0: keys0 |= mask; break;
                case 1: keys1 |= mask; break;
                case 2: keys2 |= mask; break;
                case 3: keys3 |= mask; break;
                case 4: keys4 |= mask; break;
                case 5: keys5 |= mask; break;
                case 6: keys6 |= mask; break;
                case 7: keys7 |= mask; break;
            }
        }

        internal void InternalClearKey(Keys key)
        {
            uint mask = (uint)1 << (((int)key) & 0x1f);
            switch (((int)key) >> 5)
            {
                case 0: keys0 &= ~mask; break;
                case 1: keys1 &= ~mask; break;
                case 2: keys2 &= ~mask; break;
                case 3: keys3 &= ~mask; break;
                case 4: keys4 &= ~mask; break;
                case 5: keys5 &= ~mask; break;
                case 6: keys6 &= ~mask; break;
                case 7: keys7 &= ~mask; break;
            }
        }

        internal void InternalClearAllKeys()
        {
            keys0 = 0;
            keys1 = 0;
            keys2 = 0;
            keys3 = 0;
            keys4 = 0;
            keys5 = 0;
            keys6 = 0;
            keys7 = 0;
        }

        #endregion

        #region XNA Interface

        /// <summary>
        /// Gets the state of the Caps Lock key.
        /// </summary>
        public bool CapsLock { get; private set; }

        /// <summary>
        /// Gets the state of the Num Lock key.
        /// </summary>
        public bool NumLock { get; private set; }

        internal KeyboardState(List<Keys> keys, bool capsLock = false, bool numLock = false) : this()
        {
            CapsLock = capsLock;
            NumLock = numLock;

            if (keys != null)
            {
                for (int i = 0; i < keys.Count; i++)
                    InternalSetKey(keys[i]);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardState"/> class.
        /// </summary>
        /// <param name="keys">List of keys to be flagged as pressed on initialization.</param>
        /// <param name="capsLock">Caps Lock state.</param>
        /// <param name="numLock">Num Lock state.</param>
        public KeyboardState(Keys[] keys, bool capsLock = false, bool numLock = false) : this()
        {
            CapsLock = capsLock;
            NumLock = numLock;

            if (keys != null)
                for (int i = 0; i < keys.Length; i++)
                    InternalSetKey(keys[i]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardState"/> class.
        /// </summary>
        /// <param name="keys">List of keys to be flagged as pressed on initialization.</param>
        public KeyboardState(params Keys[] keys) : this()
        {
            CapsLock = false;
            NumLock = false;

            if (keys != null)
            {
                for (int i = 0; i < keys.Length; i++)
                    InternalSetKey(keys[i]);
            }
        }

        /// <summary>
        /// Returns the state of a specified key.
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <returns>The state of the key.</returns>
        public readonly KeyState this[Keys key] => InternalGetKey(key) ? KeyState.Down : KeyState.Up;

        /// <summary>
        /// Gets whether given key is currently being pressed.
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <returns>true if the key is pressed; false otherwise.</returns>
        public readonly bool IsKeyDown(Keys key) => InternalGetKey(key);

        /// <summary>
        /// Gets whether given key is currently being not pressed.
        /// </summary>
        /// <param name="key">The key to query.</param>
        /// <returns>true if the key is not pressed; false otherwise.</returns>
        public readonly bool IsKeyUp(Keys key) => !InternalGetKey(key);

        #endregion

        #region GetPressedKeys()

        /// <summary>
        /// Returns the number of pressed keys in this <see cref="KeyboardState"/>.
        /// </summary>
        /// <returns>An integer representing the number of keys currently pressed in this <see cref="KeyboardState"/>.</returns>
        public readonly int GetPressedKeyCount()
        {
            uint count = CountBits(keys0) + CountBits(keys1) + CountBits(keys2) + CountBits(keys3)
                    + CountBits(keys4) + CountBits(keys5) + CountBits(keys6) + CountBits(keys7);
            return (int)count;
        }

        private static uint CountBits(uint v)
        {
            // http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel
            v -= (v >> 1) & 0x55555555;                    // reuse input as temporary
            v = (v & 0x33333333) + ((v >> 2) & 0x33333333);     // temp
            return ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24; // count
        }

        private static int AddKeysToOutput(uint keys, int offset, Span<Keys> output, int index)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((keys & (1 << i)) != 0)
                    output[index++] = (Keys)(offset + i);
            }
            return index;
        }

        /// <summary>
        /// Returns an array with keys that are currently being pressed.
        /// </summary>
        /// <returns>The keys that are currently being pressed.</returns>
        public readonly Keys[] GetPressedKeys()
        {
            var keys = new Keys[Count];
            GetPressedKeys(keys);
            return keys;
        }

        /// <summary>
        /// Fills a span with keys that are currently being pressed.
        /// </summary>
        /// <param name="keys">The destination span for the keys.</param>
        /// <returns>The amount of keys that were added to the span.</returns>
        public readonly int GetPressedKeys(Span<Keys> keys)
        {
            int index = 0;

            if (keys0 != 0 && index < keys.Length) index = AddKeysToOutput(keys0, 0 * 32, keys, index);
            if (keys1 != 0 && index < keys.Length) index = AddKeysToOutput(keys1, 1 * 32, keys, index);
            if (keys2 != 0 && index < keys.Length) index = AddKeysToOutput(keys2, 2 * 32, keys, index);
            if (keys3 != 0 && index < keys.Length) index = AddKeysToOutput(keys3, 3 * 32, keys, index);
            if (keys4 != 0 && index < keys.Length) index = AddKeysToOutput(keys4, 4 * 32, keys, index);
            if (keys5 != 0 && index < keys.Length) index = AddKeysToOutput(keys5, 5 * 32, keys, index);
            if (keys6 != 0 && index < keys.Length) index = AddKeysToOutput(keys6, 6 * 32, keys, index);
            if (keys7 != 0 && index < keys.Length) index = AddKeysToOutput(keys7, 7 * 32, keys, index);

            return index;
        }

        #endregion

        /// <summary>
        /// Returns the hash code of the <see cref="KeyboardState"/>.
        /// </summary>
        public readonly override int GetHashCode()
        {
            return (int)(keys0 ^ keys1 ^ keys2 ^ keys3 ^ keys4 ^ keys5 ^ keys6 ^ keys7);
        }

        #region Equals

        public static bool operator ==(in KeyboardState a, in KeyboardState b)
        {
            return a.keys0 == b.keys0
                && a.keys1 == b.keys1
                && a.keys2 == b.keys2
                && a.keys3 == b.keys3
                && a.keys4 == b.keys4
                && a.keys5 == b.keys5
                && a.keys6 == b.keys6
                && a.keys7 == b.keys7;
        }

        public static bool operator !=(in KeyboardState a, in KeyboardState b) => !(a == b);

        public readonly bool Equals(KeyboardState other) => this == other;
        public override bool Equals(object obj) => obj is KeyboardState other && Equals(other);

        #endregion

        /// <summary>
        /// Returns an <see cref="Enumerator"/> that 
        /// enumerates the currently pressed keys.
        /// </summary>
        public readonly Enumerator GetEnumerator() => new Enumerator(this);

        readonly IEnumerator<Keys> IEnumerable<Keys>.GetEnumerator() => GetEnumerator();
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Enumerates the pressed keys of a <see cref="KeyboardState"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<Keys>
        {
            private KeyboardState _state;
            private int _index;

            /// <summary>
            /// Gets the pressed key at the current position of the enumerator.
            /// </summary>
            public Keys Current { get; private set; }

            object IEnumerator.Current => Current;

            /// <summary>
            /// Constructs the <see cref="Enumerator"/>.
            /// </summary>
            /// <param name="state">The <see cref="KeyboardState"/> to enumerate.</param>
            public Enumerator(KeyboardState state)
            {
                _state = state;
                _index = -1;
                Current = default;
            }

            /// <summary>
            /// Advances the <see cref="Enumerator"/> to the next pressed key.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the enumerator was successfully advanced to the next element;
            /// <see langword="false"/> if the enumerator has passed the end of the collection.
            /// </returns>
            public bool MoveNext()
            {
                // TODO: finish me
                throw new NotImplementedException();
            }

            /// <summary>
            /// Resets the <see cref="Enumerator"/>.
            /// </summary>
            public void Reset()
            {
                _index = -1;
                Current = default;
            }

            void IDisposable.Dispose()
            {
            }
        }
    }
}
