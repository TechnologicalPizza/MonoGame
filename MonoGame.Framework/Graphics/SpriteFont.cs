// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Original code from SilverSprite Project
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace MonoGame.Framework.Graphics 
{

	public sealed class SpriteFont 
    {
		internal static class Errors 
        {
			public const string TextContainsUnresolvableCharacters =
				"Text contains characters that cannot be resolved by this SpriteFont.";
			public const string UnresolvableCharacter =
				"Character cannot be resolved by this SpriteFont.";
		}

        private readonly CharacterRegion[] _regions;
        private char? _defaultCharacter;
        private int _defaultGlyphIndex = -1;

        /// <summary>
        /// Gets all the glyphs in this font.
        /// </summary>
        public Glyph[] Glyphs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteFont" /> class.
        /// </summary>
        /// <param name="texture">The font texture.</param>
        /// <param name="glyphBounds">The rectangles in the font texture containing letters.</param>
        /// <param name="cropping">The cropping rectangles, which are applied to the corresponding glyphBounds to calculate the bounds of the actual character.</param>
        /// <param name="characters">The characters.</param>
        /// <param name="lineSpacing">The line spacing (the distance from baseline to baseline) of the font.</param>
        /// <param name="spacing">The spacing (tracking) between characters in the font.</param>
        /// <param name="kerning">The letters kernings(X - left side bearing, Y - width and Z - right side bearing).</param>
        /// <param name="defaultCharacter">The character that will be substituted when a given character is not included in the font.</param>
        public SpriteFont(
            Texture2D texture, List<Rectangle> glyphBounds, List<Rectangle> cropping, List<char> characters,
            int lineSpacing, float spacing, List<Vector3> kerning, char? defaultCharacter) :
            this(texture, glyphBounds, cropping, characters, true, lineSpacing, spacing, kerning, defaultCharacter)
        {
        }

        internal SpriteFont(
            Texture2D texture, List<Rectangle> glyphBounds, List<Rectangle> cropping, List<char> characters, bool copyChars,
            int lineSpacing, float spacing, List<Vector3> kerning, char? defaultCharacter)
        {
            if (copyChars)
                Characters = new ReadOnlyCollection<char>(characters.ToArray());
            else
                Characters = new ReadOnlyCollection<char>(characters);

            Texture = texture;
            LineSpacing = lineSpacing;
            Spacing = spacing;

            Glyphs = new Glyph[characters.Count];
            var regions = new Stack<CharacterRegion>();

            for (var i = 0; i < characters.Count; i++)
            {
                Glyphs[i] = new Glyph(
                    boundsInTexture: glyphBounds[i],
                    cropping: cropping[i],
                    character: characters[i],

                    leftSideBearing: kerning[i].X,
                    width: kerning[i].Y,
                    rightSideBearing: kerning[i].Z,

                    widthIncludingBearings: kerning[i].X + kerning[i].Y + kerning[i].Z);

                if (regions.Count == 0 || characters[i] > (regions.Peek().End + 1))
                {
                    // Start a new region
                    regions.Push(new CharacterRegion(characters[i], i));
                }
                else if (characters[i] == (regions.Peek().End + 1))
                {
                    var currentRegion = regions.Pop();
                    // include character in currentRegion
                    currentRegion.End++;
                    regions.Push(currentRegion);
                }
                else // characters[i] < (regions.Peek().End+1)
                {
                    throw new InvalidOperationException("Invalid SpriteFont. Character map must be in ascending order.");
                }
            }

            _regions = regions.ToArray();
            Array.Reverse(_regions);

            DefaultCharacter = defaultCharacter;
        }

        /// <summary>
        /// Gets the texture that this SpriteFont draws from.
        /// </summary>
        /// <remarks>Can be used to implement custom rendering of a SpriteFont</remarks>
        public Texture2D Texture { get; }

        /// <summary>
        /// Returns a copy of the dictionary containing the glyphs in this SpriteFont.
        /// </summary>
        /// <returns>A new Dictionary containing all of the glyphs inthis SpriteFont</returns>
        /// <remarks>Can be used to calculate character bounds when implementing custom SpriteFont rendering.</remarks>
        public Dictionary<char, Glyph> GetGlyphs()
        {
            var glyphsDictionary = new Dictionary<char, Glyph>(Glyphs.Length);
            foreach(var glyph in Glyphs)
                glyphsDictionary.Add(glyph.Character, glyph);
            return glyphsDictionary;
        }

		/// <summary>
		/// Gets a collection of the characters in the font.
		/// </summary>
		public ReadOnlyCollection<char> Characters { get; private set; }

		/// <summary>
		/// Gets or sets the character that will be substituted when a
		/// given character is not included in the font.
		/// </summary>
		public char? DefaultCharacter
        {
            get => _defaultCharacter;
            set
            {
                // Get the default glyph index here once.
                if (value.HasValue)
                {
                    if (!TryGetGlyphIndex(value.Value, out _defaultGlyphIndex))
                        throw new ArgumentException(Errors.UnresolvableCharacter);
                }
                else
                    _defaultGlyphIndex = -1;

                _defaultCharacter = value;
            }
        }

        /// <summary>
        /// Gets or sets the line spacing (the distance from baseline
        /// to baseline) of the font.
        /// </summary>
        public int LineSpacing { get; set; }

		/// <summary>
		/// Gets or sets the spacing (tracking) between characters in
		/// the font.
		/// </summary>
		public float Spacing { get; set; }

		/// <summary>
		/// Returns the size of a string when rendered in this font.
		/// </summary>
		/// <param name="text">The text to measure.</param>
		/// <returns>The size, in pixels, of 'text' when rendered in
		/// this font.</returns>
		public Vector2 MeasureString(string text)
		{
            MeasureString(new CharacterSource(text), out Vector2 size);
            return size;
		}

		/// <summary>
		/// Returns the size of the contents of a StringBuilder when
		/// rendered in this font.
		/// </summary>
		/// <param name="text">The text to measure.</param>
		/// <returns>The size, in pixels, of 'text' when rendered in
		/// this font.</returns>
		public Vector2 MeasureString(StringBuilder text)
		{
            MeasureString(new CharacterSource(text), out Vector2 size);
            return size;
		}

		internal unsafe void MeasureString(in CharacterSource text, out Vector2 size)
		{
			if (text.Length == 0)
            {
				size = Vector2.Zero;
				return;
			}

			var width = 0f;
			var finalLineHeight = (float)LineSpacing;
            
			var offset = Vector2.Zero;
            var firstGlyphOfLine = true;

            fixed (Glyph* pGlyphs = Glyphs)
            for (var i = 0; i < text.Length; ++i)
            {
                var c = text[i];

                if (c == '\r')
                    continue;

                if (c == '\n')
                {
                    finalLineHeight = LineSpacing;

                    offset.X = 0;
                    offset.Y += LineSpacing;
                    firstGlyphOfLine = true;
                    continue;
                }

                var currentGlyphIndex = GetGlyphIndexOrDefault(c);
                Debug.Assert(currentGlyphIndex >= 0 && currentGlyphIndex < Glyphs.Length,
                    "currentGlyphIndex was outside the bounds of the array.");
                var pCurrentGlyph = pGlyphs + currentGlyphIndex;

                // The first character on a line might have a negative left side bearing.
                // In this scenario, SpriteBatch/SpriteFont normally offset the text to the right,
                //  so that text does not hang off the left side of its rectangle.
                if (firstGlyphOfLine) {
                    offset.X = Math.Max(pCurrentGlyph->LeftSideBearing, 0);
                    firstGlyphOfLine = false;
                } else {
                    offset.X += Spacing + pCurrentGlyph->LeftSideBearing;
                }

                offset.X += pCurrentGlyph->Width;

                var proposedWidth = offset.X + Math.Max(pCurrentGlyph->RightSideBearing, 0);
                if (proposedWidth > width)
                    width = proposedWidth;

                offset.X += pCurrentGlyph->RightSideBearing;

                if (pCurrentGlyph->Cropping.Height > finalLineHeight)
                    finalLineHeight = pCurrentGlyph->Cropping.Height;
            }

            size.X = width;
            size.Y = offset.Y + finalLineHeight;
		}
        
        internal unsafe bool TryGetGlyphIndex(char c, out int index)
        {
            fixed (CharacterRegion* pRegions = _regions)
            {
                // Get region Index 
                int regionIdx = -1;
                int l = 0;
                int r = _regions.Length - 1;
                while (l <= r)
                {
                    var m = (l + r) >> 1;                    
                    Debug.Assert(m >= 0 && m < _regions.Length, "Index was outside the bounds of the array.");

                    if (pRegions[m].End < c)
                        l = m + 1;
                    else if (pRegions[m].Start > c)
                        r = m - 1;
                    else
                    {
                        regionIdx = m;
                        break;
                    }
                }

                if (regionIdx == -1)
                {
                    index = -1;
                    return false;
                }

                index = pRegions[regionIdx].StartIndex + (c - pRegions[regionIdx].Start);
            }

            return true;
        }

        internal int GetGlyphIndexOrDefault(char c)
        {
            if (!TryGetGlyphIndex(c, out int glyphIdx))
            {
                if (_defaultGlyphIndex == -1)
                    throw new ArgumentException(Errors.TextContainsUnresolvableCharacters, nameof(c));

                return _defaultGlyphIndex;
            }
            else
                return glyphIdx;
        }
        
        internal readonly struct CharacterSource 
        {
			private readonly string _string;
			private readonly StringBuilder _builder;

            public readonly int Length;

            public CharacterSource(string s)
			{
				_string = s;
				_builder = null;
				Length = s.Length;
			}

			public CharacterSource(StringBuilder builder)
			{
				_builder = builder;
				_string = null;
				Length = _builder.Length;
			}

			public char this [int index] 
            {
				get 
                {
					if (_string != null)
						return _string[index];
					return _builder[index];
				}
			}
		}

        /// <summary>
        /// Struct that defines the spacing, Kerning, and bounds of a character.
        /// </summary>
        /// <remarks>Provides the data necessary to implement custom SpriteFont rendering.</remarks>
		public readonly struct Glyph 
        {
            /// <summary>
            /// The char associated with this glyph.
            /// </summary>
			public readonly char Character;

            /// <summary>
            /// Rectangle in the font texture where this letter exists.
            /// </summary>
			public readonly Rectangle BoundsInTexture;

            /// <summary>
            /// Cropping applied to the BoundsInTexture to calculate the bounds of the actual character.
            /// </summary>
			public readonly Rectangle Cropping;

            /// <summary>
            /// The amount of space between the left side ofthe character and its first pixel in the X dimention.
            /// </summary>
            public readonly float LeftSideBearing;

            /// <summary>
            /// The amount of space between the right side of the character and its last pixel in the X dimention.
            /// </summary>
            public readonly float RightSideBearing;

            /// <summary>
            /// Width of the character before kerning is applied. 
            /// </summary>
            public readonly float Width;

            /// <summary>
            /// Width of the character before kerning is applied. 
            /// </summary>
            public readonly float WidthIncludingBearings;

			public static readonly Glyph Empty = new Glyph();

            public Glyph(
                char character, in Rectangle boundsInTexture, in Rectangle cropping, 
                float leftSideBearing, float rightSideBearing, float width, float widthIncludingBearings)
            {
                Character = character;
                BoundsInTexture = boundsInTexture;
                Cropping = cropping;
                LeftSideBearing = leftSideBearing;
                RightSideBearing = rightSideBearing;
                Width = width;
                WidthIncludingBearings = widthIncludingBearings;
            }

            public override string ToString ()
			{
                return "CharacterIndex=" + Character + ", Glyph=" + BoundsInTexture + ", Cropping=" + Cropping + ", Kerning=" + LeftSideBearing + "," + Width + "," + RightSideBearing;
			}
		}

        private struct CharacterRegion
        {
            public readonly char Start;
            public char End;
            public readonly int StartIndex;

            public CharacterRegion(char start, int startIndex)
            {
                Start = start;                
                End = start;
                StartIndex = startIndex;
            }
        }
	}
}
