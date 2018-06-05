﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MonoGame.Imaging
{
    public class StackQueue<T>
    {
        private LinkedList<T> _linkedList;

        public StackQueue()
        {
            _linkedList = new LinkedList<T>();
        }

        public void Push(T obj)
        {
            this._linkedList.AddFirst(obj);
        }

        public void Enqueue(T obj)
        {
            this._linkedList.AddFirst(obj);
        }

        public T Pop()
        {
            var obj = this._linkedList.First.Value;
            this._linkedList.RemoveFirst();
            return obj;
        }

        public T Dequeue()
        {
            var obj = this._linkedList.Last.Value;
            this._linkedList.RemoveLast();
            return obj;
        }

        public T PeekStack()
        {
            return this._linkedList.First.Value;
        }

        public T PeekQueue()
        {
            return this._linkedList.Last.Value;
        }

        public int Count => _linkedList.Count;
    }

    public class FixedSizedStackQueue<T> : StackQueue<T>
    {
        private readonly int _maxCapacity;
        private readonly object _lock = new object();

        public FixedSizedStackQueue(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
        }

        public new void Enqueue(T item)
        {
            lock (_lock)
            {
                base.Enqueue(item);
                if (Count > _maxCapacity)
                    Dequeue(); // Throw away
            }
        }

        public new T Dequeue()
        {
            lock(_lock)
                return base.Dequeue();
        }
    }

    public class ImagingError
    {
        public readonly Thread WorkerThread;
        public readonly string Error;

        public ImagingError(Thread workerThread, string error)
        {
            WorkerThread = workerThread;
            Error = error;
        }

        public static ImagingError GetLatestError()
        {
            return Imaging.LastErrors.Pop();
        }

        public static ImagingError GetLastError()
        {
            return Imaging.LastErrors.Dequeue();   
        }
    }

    internal unsafe partial class Imaging
    {
        public static FixedSizedStackQueue<ImagingError> LastErrors;
        public static int _verticallyFlipOnLoad;

        static Imaging()
        {
            LastErrors = new FixedSizedStackQueue<ImagingError>(128);
        }

        private static int Error(string str)
        {
            LastErrors.Enqueue(new ImagingError(Thread.CurrentThread, str));
            return 0;
        }

        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* MAlloc(MemoryManager manager, ulong size)
        {
            return manager.MAlloc((int) size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MemCopy(MemoryManager manager, void* a, void* b, ulong size)
        {
            manager.MemCopy(a, b, (long) size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MemMove(MemoryManager manager, void* a, void* b, ulong size)
        {
            manager.MemMove(a, b, (long) size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MemCmp(MemoryManager manager, void* a, void* b, ulong size)
        {
            return manager.MemCmp(a, b, (long) size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Free(MemoryManager manager, void* a)
        {
            manager.Free(a);
        }

        private static void* ReAlloc(MemoryManager manager, void* ptr, ulong newSize)
        {
            return manager.ReAlloc(ptr, (long)newSize);
        }
        */

        private static void MemSet(void* ptr, int value, ulong size)
        {
            byte* bptr = (byte*) ptr;
            var bval = (byte) value;
            for (ulong i = 0; i < size; ++i)
            {
                *bptr++ = bval;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Rot32(uint x, int y)
        {
            return (x << y) | (x >> (32 - y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Abs(int v)
        {
            return (v + (v >> 31)) ^ (v >> 31);
        }

        public static void GifParseColortable(ReadContext s, byte* pal, int num_entries, int transp)
        {
            int i;
            for (i = 0; (i) < (num_entries); ++i)
            {
                pal[i*4 + 2] = GetByte(s);
                pal[i*4 + 1] = GetByte(s);
                pal[i*4] = GetByte(s);
                pal[i*4 + 3] = (byte) (transp == i ? 0 : 255);
            }
        }

        public const long DBL_EXP_MASK = 0x7ff0000000000000L;
        public const int DBL_MANT_BITS = 52;
        public const long DBL_SGN_MASK = -1 - 0x7fffffffffffffffL;
        public const long DBL_MANT_MASK = 0x000fffffffffffffL;
        public const long DBL_EXP_CLR_MASK = DBL_SGN_MASK | DBL_MANT_MASK;

        /// <summary>
        /// This code had been borrowed from here: https://github.com/MachineCognitis/C.math.NET
        /// </summary>
        /// <param name="number"></param>
        /// <param name="exponent"></param>
        /// <returns></returns>
        private static double FRExp(double number, int* exponent)
        {
            var bits = BitConverter.DoubleToInt64Bits(number);
            var exp = (int) ((bits & DBL_EXP_MASK) >> DBL_MANT_BITS);
            *exponent = 0;

            if (exp == 0x7ff || number == 0D)
                number += number;
            else
            {
                // Not zero and finite.
                *exponent = exp - 1022;
                if (exp == 0)
                {
                    // Subnormal, scale number so that it is in [1, 2).
                    number *= BitConverter.Int64BitsToDouble(0x4350000000000000L); // 2^54
                    bits = BitConverter.DoubleToInt64Bits(number);
                    exp = (int) ((bits & DBL_EXP_MASK) >> DBL_MANT_BITS);
                    *exponent = exp - 1022 - 54;
                }
                // Set exponent to -1 so that number is in [0.5, 1).
                number = BitConverter.Int64BitsToDouble((bits & DBL_EXP_CLR_MASK) | 0x3fe0000000000000L);
            }

            return number;
        }
    }
}
