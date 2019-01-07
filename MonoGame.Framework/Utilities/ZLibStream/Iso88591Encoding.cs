﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MonoGame.Utilities
{
    /// <summary>
    /// A custom encoding class that provides encoding capabilities for the
    /// 'Western European (ISO)' encoding under Silverlight.<br/>
    /// This class was generated by a tool. For more information, visit
    /// http://www.hardcodet.net/2010/03/silverlight-text-encoding-class-generator
    /// </summary>
    internal class Iso88591Encoding : Encoding
    {
        /// <summary>
        /// Gets the name registered with the
        /// Internet Assigned Numbers Authority (IANA) for the current encoding.
        /// </summary>
        /// <returns>
        /// The IANA name for the current <see cref="Encoding"/>.
        /// </returns>
        public override string WebName => "iso-8859-1";


        private char? fallbackCharacter;

        /// <summary>
        /// A character that can be set in order to make the encoding class
        /// more fault tolerant. If this property is set, the encoding class will
        /// use this property instead of throwing an exception if an unsupported
        /// byte value is being passed for decoding.
        /// </summary>
        public char? FallbackCharacter
        {
            get => fallbackCharacter;
            set
            {
                fallbackCharacter = value;

                if (value.HasValue && !charToByte.ContainsKey(value.Value))
                {
                    string msg = $"Cannot use the character [{value.Value}] (int value {(int)value.Value})" +
                        $" as fallback value - the fallback character itself is not supported by the encoding.";
                    throw new EncoderFallbackException(msg);
                }

                FallbackByte = value.HasValue ? charToByte[value.Value] : (byte?)null;
            }
        }

        /// <summary>
        /// A byte value that corresponds to the <see cref="FallbackCharacter"/>.
        /// It is used in encoding scenarios in case an unsupported character is
        /// being passed for encoding.
        /// </summary>
        public byte? FallbackByte { get; private set; }


        public Iso88591Encoding()
        {
            FallbackCharacter = '?';
        }

        /// <summary>
        /// Encodes a set of characters from the specified character array into the specified byte array.
        /// </summary>
        /// <returns>
        /// The actual number of bytes written into <paramref name="bytes"/>.
        /// </returns>
        /// <param name="chars">The character array containing the set of characters to encode. 
        /// </param><param name="charIndex">The index of the first character to encode. 
        /// </param><param name="charCount">The number of characters to encode. 
        /// </param><param name="bytes">The byte array to contain the resulting sequence of bytes.
        /// </param><param name="byteIndex">The index at which to start writing the resulting sequence of bytes. 
        /// </param>
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return FallbackByte.HasValue
                     ? GetBytesWithFallBack(chars, charIndex, charCount, bytes, byteIndex)
                     : GetBytesWithoutFallback(chars, charIndex, charCount, bytes, byteIndex);
        }


        private int GetBytesWithFallBack(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (int i = 0; i < charCount; i++)
            {
                var character = chars[i + charIndex];
                bool status = charToByte.TryGetValue(character, out byte byteValue);

                bytes[byteIndex + i] = status ? byteValue : FallbackByte.Value;
            }

            return charCount;
        }

        private int GetBytesWithoutFallback(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (int i = 0; i < charCount; i++)
            {
                var character = chars[i + charIndex];
                bool status = charToByte.TryGetValue(character, out byte byteValue);

                if (!status)
                {
                    //throw exception
                    string msg =
                      "The encoding [{0}] cannot encode the character [{1}] (int value {2}). Set the FallbackCharacter property in order to suppress this exception and encode a default character instead.";
                    msg = String.Format(msg, WebName, character, (int)character);
                    throw new EncoderFallbackException(msg);
                }

                bytes[byteIndex + i] = byteValue;
            }

            return charCount;
        }



        /// <summary>
        /// Decodes a sequence of bytes from the specified byte array into the specified character array.
        /// </summary>
        /// <returns>
        /// The actual number of characters written into <paramref name="chars"/>.
        /// </returns>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode. 
        /// </param><param name="byteIndex">The index of the first byte to decode. 
        /// </param><param name="byteCount">The number of bytes to decode. 
        /// </param><param name="chars">The character array to contain the resulting set of characters. 
        /// </param><param name="charIndex">The index at which to start writing the resulting set of characters. 
        /// </param>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return FallbackCharacter.HasValue
                     ? GetCharsWithFallback(bytes, byteIndex, byteCount, chars, charIndex)
                     : GetCharsWithoutFallback(bytes, byteIndex, byteCount, chars, charIndex);
        }


        private int GetCharsWithFallback(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (int i = 0; i < byteCount; i++)
            {
                byte lookupIndex = bytes[i + byteIndex];

                //if the byte value is not in our lookup array, fall back to default character
                char result = lookupIndex >= byteToChar.Length
                                ? FallbackCharacter.Value
                                : byteToChar[lookupIndex];

                chars[charIndex + i] = result;
            }

            return byteCount;
        }



        private int GetCharsWithoutFallback(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (int i = 0; i < byteCount; i++)
            {
                byte lookupIndex = bytes[i + byteIndex];
                if (lookupIndex >= byteToChar.Length)
                {
                    //throw exception
                    string msg = "The encoding [{0}] cannot decode byte value [{1}]. Set the FallbackCharacter property in order to suppress this exception and decode the value as a default character instead.";
                    msg = String.Format(msg, WebName, lookupIndex);
                    throw new EncoderFallbackException(msg);
                }


                chars[charIndex + i] = byteToChar[lookupIndex];
            }

            return byteCount;
        }



        /// <summary>
        /// Calculates the number of bytes produced by encoding a set of characters
        /// from the specified character array.
        /// </summary>
        /// <returns>
        /// The number of bytes produced by encoding the specified characters. This class
        /// always returns the value of <paramref name="count"/>.
        /// </returns>
        public override int GetByteCount(char[] chars, int index, int count)
        {
            return count;
        }


        /// <summary>
        /// Calculates the number of characters produced by decoding a sequence
        /// of bytes from the specified byte array.
        /// </summary>
        /// <returns>
        /// The number of characters produced by decoding the specified sequence of bytes. This class
        /// always returns the value of <paramref name="count"/>. 
        /// </returns>
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return count;
        }


        /// <summary>
        /// Calculates the maximum number of bytes produced by encoding the specified number of characters.
        /// </summary>
        /// <returns>
        /// The maximum number of bytes produced by encoding the specified number of characters. This
        /// class always returns the value of <paramref name="charCount"/>.
        /// </returns>
        /// <param name="charCount">The number of characters to encode. 
        /// </param>
        public override int GetMaxByteCount(int charCount)
        {
            return charCount;
        }

        /// <summary>
        /// Calculates the maximum number of characters produced by decoding the specified number of bytes.
        /// </summary>
        /// <returns>
        /// The maximum number of characters produced by decoding the specified number of bytes. This class
        /// always returns the value of <paramref name="byteCount"/>.
        /// </returns>
        /// <param name="byteCount">The number of bytes to decode.</param> 
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }


        /// <summary>
        /// Gets the number of characters that are supported by this encoding.
        /// This property returns a maximum value of 256, as the encoding class
        /// only supports single byte encodings (1 byte == 256 possible values).
        /// </summary>
        public static int CharacterCount => byteToChar.Length;

        #region Character Table

        /// <summary>
        /// This table contains characters in an array. The index within the
        /// array corresponds to the encoding's mapping of bytes to characters
        /// (e.g. if a byte value of 5 is used to encode the character 'x', this
        /// character will be stored at the array index 5.
        /// </summary>
        private static char[] byteToChar = new char[]
        {
          (char)0 /* byte 0 */  ,
          (char)1 /* byte 1 */  ,
          (char)2 /* byte 2 */  ,
          (char)3 /* byte 3 */  ,
          (char)4 /* byte 4 */  ,
          (char)5 /* byte 5 */  ,
          (char)6 /* byte 6 */  ,
          (char)7 /* byte 7 */  ,
          (char)8 /* byte 8 */  ,
          (char)9 /* byte 9 */  ,
          (char)10 /* byte 10 */  ,
          (char)11 /* byte 11 */  ,
          (char)12 /* byte 12 */  ,
          (char)13 /* byte 13 */  ,
          (char)14 /* byte 14 */  ,
          (char)15 /* byte 15 */  ,
          (char)16 /* byte 16 */  ,
          (char)17 /* byte 17 */  ,
          (char)18 /* byte 18 */  ,
          (char)19 /* byte 19 */  ,
          (char)20 /* byte 20 */  ,
          (char)21 /* byte 21 */  ,
          (char)22 /* byte 22 */  ,
          (char)23 /* byte 23 */  ,
          (char)24 /* byte 24 */  ,
          (char)25 /* byte 25 */  ,
          (char)26 /* byte 26 */  ,
          (char)27 /* byte 27 */  ,
          (char)28 /* byte 28 */  ,
          (char)29 /* byte 29 */  ,
          (char)30 /* byte 30 */  ,
          (char)31 /* byte 31 */  ,
          (char)32 /* byte 32 */  ,
          (char)33 /* byte 33 */  ,
          (char)34 /* byte 34 */  ,
          (char)35 /* byte 35 */  ,
          (char)36 /* byte 36 */  ,
          (char)37 /* byte 37 */  ,
          (char)38 /* byte 38 */  ,
          (char)39 /* byte 39 */  ,
          (char)40 /* byte 40 */  ,
          (char)41 /* byte 41 */  ,
          (char)42 /* byte 42 */  ,
          (char)43 /* byte 43 */  ,
          (char)44 /* byte 44 */  ,
          (char)45 /* byte 45 */  ,
          (char)46 /* byte 46 */  ,
          (char)47 /* byte 47 */  ,
          (char)48 /* byte 48 */  ,
          (char)49 /* byte 49 */  ,
          (char)50 /* byte 50 */  ,
          (char)51 /* byte 51 */  ,
          (char)52 /* byte 52 */  ,
          (char)53 /* byte 53 */  ,
          (char)54 /* byte 54 */  ,
          (char)55 /* byte 55 */  ,
          (char)56 /* byte 56 */  ,
          (char)57 /* byte 57 */  ,
          (char)58 /* byte 58 */  ,
          (char)59 /* byte 59 */  ,
          (char)60 /* byte 60 */  ,
          (char)61 /* byte 61 */  ,
          (char)62 /* byte 62 */  ,
          (char)63 /* byte 63 */  ,
          (char)64 /* byte 64 */  ,
          (char)65 /* byte 65 */  ,
          (char)66 /* byte 66 */  ,
          (char)67 /* byte 67 */  ,
          (char)68 /* byte 68 */  ,
          (char)69 /* byte 69 */  ,
          (char)70 /* byte 70 */  ,
          (char)71 /* byte 71 */  ,
          (char)72 /* byte 72 */  ,
          (char)73 /* byte 73 */  ,
          (char)74 /* byte 74 */  ,
          (char)75 /* byte 75 */  ,
          (char)76 /* byte 76 */  ,
          (char)77 /* byte 77 */  ,
          (char)78 /* byte 78 */  ,
          (char)79 /* byte 79 */  ,
          (char)80 /* byte 80 */  ,
          (char)81 /* byte 81 */  ,
          (char)82 /* byte 82 */  ,
          (char)83 /* byte 83 */  ,
          (char)84 /* byte 84 */  ,
          (char)85 /* byte 85 */  ,
          (char)86 /* byte 86 */  ,
          (char)87 /* byte 87 */  ,
          (char)88 /* byte 88 */  ,
          (char)89 /* byte 89 */  ,
          (char)90 /* byte 90 */  ,
          (char)91 /* byte 91 */  ,
          (char)92 /* byte 92 */  ,
          (char)93 /* byte 93 */  ,
          (char)94 /* byte 94 */  ,
          (char)95 /* byte 95 */  ,
          (char)96 /* byte 96 */  ,
          (char)97 /* byte 97 */  ,
          (char)98 /* byte 98 */  ,
          (char)99 /* byte 99 */  ,
          (char)100 /* byte 100 */  ,
          (char)101 /* byte 101 */  ,
          (char)102 /* byte 102 */  ,
          (char)103 /* byte 103 */  ,
          (char)104 /* byte 104 */  ,
          (char)105 /* byte 105 */  ,
          (char)106 /* byte 106 */  ,
          (char)107 /* byte 107 */  ,
          (char)108 /* byte 108 */  ,
          (char)109 /* byte 109 */  ,
          (char)110 /* byte 110 */  ,
          (char)111 /* byte 111 */  ,
          (char)112 /* byte 112 */  ,
          (char)113 /* byte 113 */  ,
          (char)114 /* byte 114 */  ,
          (char)115 /* byte 115 */  ,
          (char)116 /* byte 116 */  ,
          (char)117 /* byte 117 */  ,
          (char)118 /* byte 118 */  ,
          (char)119 /* byte 119 */  ,
          (char)120 /* byte 120 */  ,
          (char)121 /* byte 121 */  ,
          (char)122 /* byte 122 */  ,
          (char)123 /* byte 123 */  ,
          (char)124 /* byte 124 */  ,
          (char)125 /* byte 125 */  ,
          (char)126 /* byte 126 */  ,
          (char)127 /* byte 127 */  ,
          (char)128 /* byte 128 */  ,
          (char)129 /* byte 129 */  ,
          (char)130 /* byte 130 */  ,
          (char)131 /* byte 131 */  ,
          (char)132 /* byte 132 */  ,
          (char)133 /* byte 133 */  ,
          (char)134 /* byte 134 */  ,
          (char)135 /* byte 135 */  ,
          (char)136 /* byte 136 */  ,
          (char)137 /* byte 137 */  ,
          (char)138 /* byte 138 */  ,
          (char)139 /* byte 139 */  ,
          (char)140 /* byte 140 */  ,
          (char)141 /* byte 141 */  ,
          (char)142 /* byte 142 */  ,
          (char)143 /* byte 143 */  ,
          (char)144 /* byte 144 */  ,
          (char)145 /* byte 145 */  ,
          (char)146 /* byte 146 */  ,
          (char)147 /* byte 147 */  ,
          (char)148 /* byte 148 */  ,
          (char)149 /* byte 149 */  ,
          (char)150 /* byte 150 */  ,
          (char)151 /* byte 151 */  ,
          (char)152 /* byte 152 */  ,
          (char)153 /* byte 153 */  ,
          (char)154 /* byte 154 */  ,
          (char)155 /* byte 155 */  ,
          (char)156 /* byte 156 */  ,
          (char)157 /* byte 157 */  ,
          (char)158 /* byte 158 */  ,
          (char)159 /* byte 159 */  ,
          (char)160 /* byte 160 */  ,
          (char)161 /* byte 161 */  ,
          (char)162 /* byte 162 */  ,
          (char)163 /* byte 163 */  ,
          (char)164 /* byte 164 */  ,
          (char)165 /* byte 165 */  ,
          (char)166 /* byte 166 */  ,
          (char)167 /* byte 167 */  ,
          (char)168 /* byte 168 */  ,
          (char)169 /* byte 169 */  ,
          (char)170 /* byte 170 */  ,
          (char)171 /* byte 171 */  ,
          (char)172 /* byte 172 */  ,
          (char)173 /* byte 173 */  ,
          (char)174 /* byte 174 */  ,
          (char)175 /* byte 175 */  ,
          (char)176 /* byte 176 */  ,
          (char)177 /* byte 177 */  ,
          (char)178 /* byte 178 */  ,
          (char)179 /* byte 179 */  ,
          (char)180 /* byte 180 */  ,
          (char)181 /* byte 181 */  ,
          (char)182 /* byte 182 */  ,
          (char)183 /* byte 183 */  ,
          (char)184 /* byte 184 */  ,
          (char)185 /* byte 185 */  ,
          (char)186 /* byte 186 */  ,
          (char)187 /* byte 187 */  ,
          (char)188 /* byte 188 */  ,
          (char)189 /* byte 189 */  ,
          (char)190 /* byte 190 */  ,
          (char)191 /* byte 191 */  ,
          (char)192 /* byte 192 */  ,
          (char)193 /* byte 193 */  ,
          (char)194 /* byte 194 */  ,
          (char)195 /* byte 195 */  ,
          (char)196 /* byte 196 */  ,
          (char)197 /* byte 197 */  ,
          (char)198 /* byte 198 */  ,
          (char)199 /* byte 199 */  ,
          (char)200 /* byte 200 */  ,
          (char)201 /* byte 201 */  ,
          (char)202 /* byte 202 */  ,
          (char)203 /* byte 203 */  ,
          (char)204 /* byte 204 */  ,
          (char)205 /* byte 205 */  ,
          (char)206 /* byte 206 */  ,
          (char)207 /* byte 207 */  ,
          (char)208 /* byte 208 */  ,
          (char)209 /* byte 209 */  ,
          (char)210 /* byte 210 */  ,
          (char)211 /* byte 211 */  ,
          (char)212 /* byte 212 */  ,
          (char)213 /* byte 213 */  ,
          (char)214 /* byte 214 */  ,
          (char)215 /* byte 215 */  ,
          (char)216 /* byte 216 */  ,
          (char)217 /* byte 217 */  ,
          (char)218 /* byte 218 */  ,
          (char)219 /* byte 219 */  ,
          (char)220 /* byte 220 */  ,
          (char)221 /* byte 221 */  ,
          (char)222 /* byte 222 */  ,
          (char)223 /* byte 223 */  ,
          (char)224 /* byte 224 */  ,
          (char)225 /* byte 225 */  ,
          (char)226 /* byte 226 */  ,
          (char)227 /* byte 227 */  ,
          (char)228 /* byte 228 */  ,
          (char)229 /* byte 229 */  ,
          (char)230 /* byte 230 */  ,
          (char)231 /* byte 231 */  ,
          (char)232 /* byte 232 */  ,
          (char)233 /* byte 233 */  ,
          (char)234 /* byte 234 */  ,
          (char)235 /* byte 235 */  ,
          (char)236 /* byte 236 */  ,
          (char)237 /* byte 237 */  ,
          (char)238 /* byte 238 */  ,
          (char)239 /* byte 239 */  ,
          (char)240 /* byte 240 */  ,
          (char)241 /* byte 241 */  ,
          (char)242 /* byte 242 */  ,
          (char)243 /* byte 243 */  ,
          (char)244 /* byte 244 */  ,
          (char)245 /* byte 245 */  ,
          (char)246 /* byte 246 */  ,
          (char)247 /* byte 247 */  ,
          (char)248 /* byte 248 */  ,
          (char)249 /* byte 249 */  ,
          (char)250 /* byte 250 */  ,
          (char)251 /* byte 251 */  ,
          (char)252 /* byte 252 */  ,
          (char)253 /* byte 253 */  ,
          (char)254 /* byte 254 */  ,
          (char)255 /* byte 255 */  
        };

        #endregion

        #region Byte Lookup Dictionary

        /// <summary>
        /// This dictionary is used to resolve byte values for a given character.
        /// </summary>
        private static Dictionary<char, byte> charToByte = new Dictionary<char, byte>
        {
          { (char)0, 0 },
          { (char)1, 1 },
          { (char)2, 2 },
          { (char)3, 3 },
          { (char)4, 4 },
          { (char)5, 5 },
          { (char)6, 6 },
          { (char)7, 7 },
          { (char)8, 8 },
          { (char)9, 9 },
          { (char)10, 10 },
          { (char)11, 11 },
          { (char)12, 12 },
          { (char)13, 13 },
          { (char)14, 14 },
          { (char)15, 15 },
          { (char)16, 16 },
          { (char)17, 17 },
          { (char)18, 18 },
          { (char)19, 19 },
          { (char)20, 20 },
          { (char)21, 21 },
          { (char)22, 22 },
          { (char)23, 23 },
          { (char)24, 24 },
          { (char)25, 25 },
          { (char)26, 26 },
          { (char)27, 27 },
          { (char)28, 28 },
          { (char)29, 29 },
          { (char)30, 30 },
          { (char)31, 31 },
          { (char)32, 32 },
          { (char)33, 33 },
          { (char)34, 34 },
          { (char)35, 35 },
          { (char)36, 36 },
          { (char)37, 37 },
          { (char)38, 38 },
          { (char)39, 39 },
          { (char)40, 40 },
          { (char)41, 41 },
          { (char)42, 42 },
          { (char)43, 43 },
          { (char)44, 44 },
          { (char)45, 45 },
          { (char)46, 46 },
          { (char)47, 47 },
          { (char)48, 48 },
          { (char)49, 49 },
          { (char)50, 50 },
          { (char)51, 51 },
          { (char)52, 52 },
          { (char)53, 53 },
          { (char)54, 54 },
          { (char)55, 55 },
          { (char)56, 56 },
          { (char)57, 57 },
          { (char)58, 58 },
          { (char)59, 59 },
          { (char)60, 60 },
          { (char)61, 61 },
          { (char)62, 62 },
          { (char)63, 63 },
          { (char)64, 64 },
          { (char)65, 65 },
          { (char)66, 66 },
          { (char)67, 67 },
          { (char)68, 68 },
          { (char)69, 69 },
          { (char)70, 70 },
          { (char)71, 71 },
          { (char)72, 72 },
          { (char)73, 73 },
          { (char)74, 74 },
          { (char)75, 75 },
          { (char)76, 76 },
          { (char)77, 77 },
          { (char)78, 78 },
          { (char)79, 79 },
          { (char)80, 80 },
          { (char)81, 81 },
          { (char)82, 82 },
          { (char)83, 83 },
          { (char)84, 84 },
          { (char)85, 85 },
          { (char)86, 86 },
          { (char)87, 87 },
          { (char)88, 88 },
          { (char)89, 89 },
          { (char)90, 90 },
          { (char)91, 91 },
          { (char)92, 92 },
          { (char)93, 93 },
          { (char)94, 94 },
          { (char)95, 95 },
          { (char)96, 96 },
          { (char)97, 97 },
          { (char)98, 98 },
          { (char)99, 99 },
          { (char)100, 100 },
          { (char)101, 101 },
          { (char)102, 102 },
          { (char)103, 103 },
          { (char)104, 104 },
          { (char)105, 105 },
          { (char)106, 106 },
          { (char)107, 107 },
          { (char)108, 108 },
          { (char)109, 109 },
          { (char)110, 110 },
          { (char)111, 111 },
          { (char)112, 112 },
          { (char)113, 113 },
          { (char)114, 114 },
          { (char)115, 115 },
          { (char)116, 116 },
          { (char)117, 117 },
          { (char)118, 118 },
          { (char)119, 119 },
          { (char)120, 120 },
          { (char)121, 121 },
          { (char)122, 122 },
          { (char)123, 123 },
          { (char)124, 124 },
          { (char)125, 125 },
          { (char)126, 126 },
          { (char)127, 127 },
          { (char)128, 128 },
          { (char)129, 129 },
          { (char)130, 130 },
          { (char)131, 131 },
          { (char)132, 132 },
          { (char)133, 133 },
          { (char)134, 134 },
          { (char)135, 135 },
          { (char)136, 136 },
          { (char)137, 137 },
          { (char)138, 138 },
          { (char)139, 139 },
          { (char)140, 140 },
          { (char)141, 141 },
          { (char)142, 142 },
          { (char)143, 143 },
          { (char)144, 144 },
          { (char)145, 145 },
          { (char)146, 146 },
          { (char)147, 147 },
          { (char)148, 148 },
          { (char)149, 149 },
          { (char)150, 150 },
          { (char)151, 151 },
          { (char)152, 152 },
          { (char)153, 153 },
          { (char)154, 154 },
          { (char)155, 155 },
          { (char)156, 156 },
          { (char)157, 157 },
          { (char)158, 158 },
          { (char)159, 159 },
          { (char)160, 160 },
          { (char)161, 161 },
          { (char)162, 162 },
          { (char)163, 163 },
          { (char)164, 164 },
          { (char)165, 165 },
          { (char)166, 166 },
          { (char)167, 167 },
          { (char)168, 168 },
          { (char)169, 169 },
          { (char)170, 170 },
          { (char)171, 171 },
          { (char)172, 172 },
          { (char)173, 173 },
          { (char)174, 174 },
          { (char)175, 175 },
          { (char)176, 176 },
          { (char)177, 177 },
          { (char)178, 178 },
          { (char)179, 179 },
          { (char)180, 180 },
          { (char)181, 181 },
          { (char)182, 182 },
          { (char)183, 183 },
          { (char)184, 184 },
          { (char)185, 185 },
          { (char)186, 186 },
          { (char)187, 187 },
          { (char)188, 188 },
          { (char)189, 189 },
          { (char)190, 190 },
          { (char)191, 191 },
          { (char)192, 192 },
          { (char)193, 193 },
          { (char)194, 194 },
          { (char)195, 195 },
          { (char)196, 196 },
          { (char)197, 197 },
          { (char)198, 198 },
          { (char)199, 199 },
          { (char)200, 200 },
          { (char)201, 201 },
          { (char)202, 202 },
          { (char)203, 203 },
          { (char)204, 204 },
          { (char)205, 205 },
          { (char)206, 206 },
          { (char)207, 207 },
          { (char)208, 208 },
          { (char)209, 209 },
          { (char)210, 210 },
          { (char)211, 211 },
          { (char)212, 212 },
          { (char)213, 213 },
          { (char)214, 214 },
          { (char)215, 215 },
          { (char)216, 216 },
          { (char)217, 217 },
          { (char)218, 218 },
          { (char)219, 219 },
          { (char)220, 220 },
          { (char)221, 221 },
          { (char)222, 222 },
          { (char)223, 223 },
          { (char)224, 224 },
          { (char)225, 225 },
          { (char)226, 226 },
          { (char)227, 227 },
          { (char)228, 228 },
          { (char)229, 229 },
          { (char)230, 230 },
          { (char)231, 231 },
          { (char)232, 232 },
          { (char)233, 233 },
          { (char)234, 234 },
          { (char)235, 235 },
          { (char)236, 236 },
          { (char)237, 237 },
          { (char)238, 238 },
          { (char)239, 239 },
          { (char)240, 240 },
          { (char)241, 241 },
          { (char)242, 242 },
          { (char)243, 243 },
          { (char)244, 244 },
          { (char)245, 245 },
          { (char)246, 246 },
          { (char)247, 247 },
          { (char)248, 248 },
          { (char)249, 249 },
          { (char)250, 250 },
          { (char)251, 251 },
          { (char)252, 252 },
          { (char)253, 253 },
          { (char)254, 254 },
          { (char)255, 255 }
        };

        #endregion
    }
}
