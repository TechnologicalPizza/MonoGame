﻿using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace MonoGame.Framework.IO
{
    /// <summary>
    ///   Computes a CRC-32. The CRC-32 algorithm is parameterized - you
    ///   can set the polynomial and enable or disable bit
    ///   reversal. This can be used for GZIP, BZip2, or ZIP.
    /// </summary>
    [SkipLocalsInit]
    public class Crc32
    {
        // private members
        private readonly uint _dwPolynomial;
        private readonly bool _reverseBits;
        private uint[] _crc32Table;
        private uint _register = 0xFFFFFFFFU;

        /// <summary>
        ///   Indicates the total number of bytes applied to the CRC.
        /// </summary>
        public long TotalBytesRead { get; private set; }

        /// <summary>
        /// Indicates the current CRC for all blocks slurped in.
        /// </summary>
        public int Crc32Result => unchecked((int)~_register);

        #region Constructors

        /// <summary>
        ///   Create an instance of the CRC32 class using the default settings: no
        ///   bit reversal, and a polynomial of 0xEDB88320.
        /// </summary>
        public Crc32() : this(false)
        {
        }

        /// <summary>
        ///   Create an instance of the CRC32 class, specifying whether to reverse
        ///   data bits or not.
        /// </summary>
        /// <param name='reverseBits'>
        ///   specify true if the instance should reverse data bits.
        /// </param>
        /// <remarks>
        ///   <para>
        /// In the CRC-32 used by BZip2, the bits are reversed. Therefore if you
        /// want a CRC32 with compatibility with BZip2, you should pass true
        /// here. In the CRC-32 used by GZIP and PKZIP, the bits are not
        /// reversed; Therefore if you want a CRC32 with compatibility with
        /// those, you should pass false.
        ///   </para>
        /// </remarks>
        public Crc32(bool reverseBits) : this(unchecked((int)0xEDB88320), reverseBits)
        {
        }


        /// <summary>
        ///   Create an instance of the CRC32 class, specifying the polynomial and
        ///   whether to reverse data bits or not.
        /// </summary>
        /// <param name='polynomial'>
        ///   The polynomial to use for the CRC, expressed in the reversed (LSB)
        ///   format: the highest ordered bit in the polynomial value is the
        ///   coefficient of the 0th power; the second-highest order bit is the
        ///   coefficient of the 1 power, and so on. Expressed this way, the
        ///   polynomial for the CRC-32C used in IEEE 802.3, is 0xEDB88320.
        /// </param>
        /// <param name='reverseBits'>
        ///   specify true if the instance should reverse data bits.
        /// </param>
        ///
        /// <remarks>
        ///   <para>
        /// In the CRC-32 used by BZip2, the bits are reversed. Therefore if you
        /// want a CRC32 with compatibility with BZip2, you should pass true
        /// here for the <c>reverseBits</c> parameter. In the CRC-32 used by
        /// GZIP and PKZIP, the bits are not reversed; Therefore if you want a
        /// CRC32 with compatibility with those, you should pass false for the
        /// <c>reverseBits</c> parameter.
        ///   </para>
        /// </remarks>
        public Crc32(int polynomial, bool reverseBits)
        {
            _reverseBits = reverseBits;
            _dwPolynomial = (uint)polynomial;
            _crc32Table = new uint[256];

            GenerateLookupTable();
        }

        #endregion

        /// <summary>
        /// Returns the CRC32 for the specified stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <returns>the CRC32 calculation</returns>
        public int GetCrc32(Stream input)
        {
            return GetCrc32AndCopy(input, null);
        }

        /// <summary>
        /// Returns the CRC32 for the specified stream, and writes the input into the
        /// output stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <param name="output">The stream into which to deflate the input</param>
        /// <returns>the CRC32 calculation</returns>
        public int GetCrc32AndCopy(Stream input, Stream? output)
        {
            if (input == null)
                throw new Exception("The input stream must not be null.");

            unchecked
            {
                TotalBytesRead = 0;
                Span<byte> buffer = stackalloc byte[4096];
                int count = input.Read(buffer);
                output?.Write(buffer.Slice(0, count));
                TotalBytesRead += count;
                while (count > 0)
                {
                    var slice = buffer.Slice(0, count);
                    SlurpBlock(slice);
                    count = input.Read(buffer);
                    output?.Write(slice);
                    TotalBytesRead += count;
                }
                return (int)~_register;
            }
        }


        /// <summary>
        ///   Get the CRC32 for the given (word,byte) combo. This is a
        ///   computation defined by PKzip for PKZIP 2.0 (weak) encryption.
        /// </summary>
        /// <param name="W">The word to start with.</param>
        /// <param name="B">The byte to combine it with.</param>
        /// <returns>The CRC-ized result.</returns>
        public int ComputeCrc32(int W, byte B)
        {
            return InternalComputeCrc32((uint)W, B);
        }

        internal int InternalComputeCrc32(uint W, byte B)
        {
            return (int)(_crc32Table[(W ^ B) & 0xFF] ^ (W >> 8));
        }

        /// <summary>
        /// Update the value for the running <see cref="Crc32"/>
        /// using the block of bytes.
        /// </summary>
        /// <param name="block">block of bytes to slurp</param>
        public void SlurpBlock(ReadOnlySpan<byte> block)
        {
            // bzip algorithm
            for (int i = 0; i < block.Length; i++)
            {
                byte b = block[i];
                if (_reverseBits)
                {
                    uint temp = (_register >> 24) ^ b;
                    _register = (_register << 8) ^ _crc32Table[temp];
                }
                else
                {
                    uint temp = (_register & 0x000000FF) ^ b;
                    _register = (_register >> 8) ^ _crc32Table[temp];
                }
            }
            TotalBytesRead += block.Length;
        }


        /// <summary>
        ///   Process one byte in the CRC.
        /// </summary>
        /// <param name = "b">The byte to include into the CRC.</param>
        public void UpdateCRC(byte b)
        {
            if (_reverseBits)
            {
                uint temp = (_register >> 24) ^ b;
                _register = (_register << 8) ^ _crc32Table[temp];
            }
            else
            {
                uint temp = (_register & 0x000000FF) ^ b;
                _register = (_register >> 8) ^ _crc32Table[temp];
            }
        }

        /// <summary>
        ///   Process a run of N identical bytes into the CRC.
        /// </summary>
        /// <remarks>
        ///   <para>
        /// This method serves as an optimization for updating the CRC when a
        /// run of identical bytes is found. Rather than passing in a buffer of
        /// length n, containing all identical bytes b, this method accepts the
        /// byte value and the length of the (virtual) buffer - the length of
        /// the run.
        ///   </para>
        /// </remarks>
        /// <param name = "b">the byte to include into the CRC.  </param>
        /// <param name = "n">the number of times that byte should be repeated. </param>
        public void UpdateCRC(byte b, int n)
        {
            while (n-- > 0)
            {
                if (_reverseBits)
                {
                    uint tmp = (_register >> 24) ^ b;
                    _register = (_register << 8) ^ _crc32Table[(tmp >= 0) ? tmp : (tmp + 256)];
                }
                else
                {
                    uint tmp = (_register & 0x000000FF) ^ b;
                    _register = (_register >> 8) ^ _crc32Table[(tmp >= 0) ? tmp : (tmp + 256)];
                }
            }
        }

        private static uint ReverseBits(uint data)
        {
            unchecked
            {
                uint ret = data;
                ret = (ret & 0x55555555) << 1 | (ret >> 1) & 0x55555555;
                ret = (ret & 0x33333333) << 2 | (ret >> 2) & 0x33333333;
                ret = (ret & 0x0F0F0F0F) << 4 | (ret >> 4) & 0x0F0F0F0F;
                ret = (ret << 24) | ((ret & 0xFF00) << 8) | ((ret >> 8) & 0xFF00) | (ret >> 24);
                return ret;
            }
        }

        private static byte ReverseBits(byte data)
        {
            unchecked
            {
                uint u = (uint)data * 0x00020202;
                uint m = 0x01044010;
                uint s = u & m;
                uint t = (u << 2) & (m << 1);
                return (byte)((0x01001001 * (s + t)) >> 24);
            }
        }



        private void GenerateLookupTable()
        {
            _crc32Table.AsSpan().Clear();
            unchecked
            {
                uint dwCrc;
                byte i = 0;
                do
                {
                    dwCrc = i;
                    for (byte j = 8; j > 0; j--)
                    {
                        if ((dwCrc & 1) == 1)
                            dwCrc = (dwCrc >> 1) ^ _dwPolynomial;
                        else
                            dwCrc >>= 1;
                    }

                    if (_reverseBits)
                        _crc32Table[ReverseBits(i)] = ReverseBits(dwCrc);
                    else
                        _crc32Table[i] = dwCrc;

                    i++;
                } while (i != 0);
            }

#if VERBOSE
            Console.WriteLine();
            Console.WriteLine("private static readonly UInt32[] crc32Table = {");
            for (int i = 0; i < crc32Table.Length; i+=4)
            {
                Console.Write("   ");
                for (int j=0; j < 4; j++)
                {
                    Console.Write(" 0x{0:X8}U,", crc32Table[i+j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("};");
            Console.WriteLine();
#endif
        }

        private static uint Gf2_matrix_times(ReadOnlySpan<uint> matrix, uint vec)
        {
            uint sum = 0;
            int i = 0;
            while (vec != 0)
            {
                if ((vec & 0x01) == 0x01)
                    sum ^= matrix[i];
                vec >>= 1;
                i++;
            }
            return sum;
        }

        private static void Gf2_matrix_square(Span<uint> square, ReadOnlySpan<uint> mat)
        {
            for (int i = 0; i < 32; i++)
                square[i] = Gf2_matrix_times(mat, mat[i]);
        }

        /// <summary>
        /// Combines the given CRC32 value with the current running total.
        /// </summary>
        /// <remarks>
        /// This is useful when using a divide-and-conquer approach to
        /// calculating a CRC. Multiple threads can each calculate a
        /// CRC32 on a segment of the data, and then combine the
        /// individual CRC32 values at the end.
        /// </remarks>
        /// <param name="crc">the crc value to be combined with this one</param>
        /// <param name="length">the length of data the CRC value was calculated on</param>
        public void Combine(int crc, int length)
        {
            Span<uint> even = stackalloc uint[32];     // even-power-of-two zeros operator
            Span<uint> odd = stackalloc uint[32];      // odd-power-of-two zeros operator

            if (length == 0)
                return;

            uint crc1 = ~_register;
            uint crc2 = (uint)crc;

            // put operator for one zero bit in odd
            odd[0] = _dwPolynomial;  // the CRC-32 polynomial
            uint row = 1;
            for (int i = 1; i < 32; i++)
            {
                odd[i] = row;
                row <<= 1;
            }

            // put operator for two zero bits in even
            Gf2_matrix_square(even, odd);

            // put operator for four zero bits in odd
            Gf2_matrix_square(odd, even);

            uint len2 = (uint)length;

            // apply len2 zeros to crc1 (first square will put the operator for one
            // zero byte, eight zero bits, in even)
            do
            {
                // apply zeros operator for this bit of len2
                Gf2_matrix_square(even, odd);

                if ((len2 & 1) == 1)
                    crc1 = Gf2_matrix_times(even, crc1);
                len2 >>= 1;

                if (len2 == 0)
                    break;

                // another iteration of the loop with odd and even swapped
                Gf2_matrix_square(odd, even);
                if ((len2 & 1) == 1)
                    crc1 = Gf2_matrix_times(odd, crc1);
                len2 >>= 1;


            } while (len2 != 0);

            crc1 ^= crc2;

            _register = ~crc1;

            //return (int) crc1;
            return;
        }

        /// <summary>
        /// Reset this <see cref="Crc32"/> instance by
        /// clearing the CRC "remainder register."
        /// </summary>
        /// <remarks>
        /// Use this when employing a single instance of this class to compute
        /// multiple, distinct CRCs on multiple, distinct data blocks.
        /// </remarks>
        public void Reset()
        {
            _register = 0xFFFFFFFFU;
        }
    }
}
