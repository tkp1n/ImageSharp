// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SixLabors.ImageSharp
{
    /// <summary>
    /// Defines the contract for methods that allow the shuffling of pixel components.
    /// Used for shuffling on platforms that do not support Hardware Intrinsics.
    /// </summary>
    internal interface IComponentShuffle
    {
        /// <summary>
        /// Gets the shuffle control.
        /// </summary>
        byte Control { get; }

        /// <summary>
        /// Shuffle 8-bit integers within 128-bit lanes in <paramref name="source"/>
        /// using the control and store the results in <paramref name="dest"/>.
        /// </summary>
        /// <param name="source">The source span of bytes.</param>
        /// <param name="dest">The destination span of bytes.</param>
        void RunFallbackShuffle(ReadOnlySpan<byte> source, Span<byte> dest);
    }

    internal readonly struct DefaultShuffle4 : IComponentShuffle
    {
        public DefaultShuffle4(byte p3, byte p2, byte p1, byte p0)
            : this(SimdUtils.Shuffle.MmShuffle(p3, p2, p1, p0))
        {
        }

        public DefaultShuffle4(byte control) => this.Control = control;

        public byte Control { get; }

        [MethodImpl(InliningOptions.ShortMethod)]
        public void RunFallbackShuffle(ReadOnlySpan<byte> source, Span<byte> dest)
        {
            ref byte sBase = ref MemoryMarshal.GetReference(source);
            ref byte dBase = ref MemoryMarshal.GetReference(dest);
            SimdUtils.Shuffle.InverseMmShuffle(
                this.Control,
                out int p3,
                out int p2,
                out int p1,
                out int p0);

            for (int i = 0; i < source.Length; i += 4)
            {
                Unsafe.Add(ref dBase, i) = Unsafe.Add(ref sBase, p0 + i);
                Unsafe.Add(ref dBase, i + 1) = Unsafe.Add(ref sBase, p1 + i);
                Unsafe.Add(ref dBase, i + 2) = Unsafe.Add(ref sBase, p2 + i);
                Unsafe.Add(ref dBase, i + 3) = Unsafe.Add(ref sBase, p3 + i);
            }
        }
    }

    internal readonly struct WXYZShuffle4 : IComponentShuffle
    {
        public byte Control => SimdUtils.Shuffle.MmShuffle(2, 1, 0, 3);

        [MethodImpl(InliningOptions.ShortMethod)]
        public void RunFallbackShuffle(ReadOnlySpan<byte> source, Span<byte> dest)
        {
            ReadOnlySpan<uint> s = MemoryMarshal.Cast<byte, uint>(source);
            Span<uint> d = MemoryMarshal.Cast<byte, uint>(dest);
            ref uint sBase = ref MemoryMarshal.GetReference(s);
            ref uint dBase = ref MemoryMarshal.GetReference(d);

            // The JIT can detect and optimize rotation idioms ROTL (Rotate Left)
            // and ROTR (Rotate Right) emitting efficient CPU instructions:
            // https://github.com/dotnet/coreclr/pull/1830
            for (int i = 0; i < s.Length; i++)
            {
                uint packed = Unsafe.Add(ref sBase, i);

                // packed          = [W Z Y X]
                // ROTL(8, packed) = [Z Y X W]
                Unsafe.Add(ref dBase, i) = (packed << 8) | (packed >> 24);
            }
        }
    }

    internal readonly struct WZYXShuffle4 : IComponentShuffle
    {
        public byte Control => SimdUtils.Shuffle.MmShuffle(0, 1, 2, 3);

        [MethodImpl(InliningOptions.ShortMethod)]
        public void RunFallbackShuffle(ReadOnlySpan<byte> source, Span<byte> dest)
        {
            ReadOnlySpan<uint> s = MemoryMarshal.Cast<byte, uint>(source);
            Span<uint> d = MemoryMarshal.Cast<byte, uint>(dest);
            ref uint sBase = ref MemoryMarshal.GetReference(s);
            ref uint dBase = ref MemoryMarshal.GetReference(d);

            for (int i = 0; i < s.Length; i++)
            {
                uint packed = Unsafe.Add(ref sBase, i);

                // packed              = [W Z Y X]
                // REVERSE(packedArgb) = [X Y Z W]
                Unsafe.Add(ref dBase, i) = BinaryPrimitives.ReverseEndianness(packed);
            }
        }
    }

    internal readonly struct YZWXShuffle4 : IComponentShuffle
    {
        public byte Control => SimdUtils.Shuffle.MmShuffle(0, 3, 2, 1);

        [MethodImpl(InliningOptions.ShortMethod)]
        public void RunFallbackShuffle(ReadOnlySpan<byte> source, Span<byte> dest)
        {
            ReadOnlySpan<uint> s = MemoryMarshal.Cast<byte, uint>(source);
            Span<uint> d = MemoryMarshal.Cast<byte, uint>(dest);
            ref uint sBase = ref MemoryMarshal.GetReference(s);
            ref uint dBase = ref MemoryMarshal.GetReference(d);

            for (int i = 0; i < s.Length; i++)
            {
                uint packed = Unsafe.Add(ref sBase, i);

                // packed              = [W Z Y X]
                // ROTR(8, packedArgb) = [Y Z W X]
                Unsafe.Add(ref dBase, i) = (packed >> 8) | (packed << 24);
            }
        }
    }

    internal readonly struct ZYXWShuffle4 : IComponentShuffle
    {
        public byte Control => SimdUtils.Shuffle.MmShuffle(3, 0, 1, 2);

        [MethodImpl(InliningOptions.ShortMethod)]
        public void RunFallbackShuffle(ReadOnlySpan<byte> source, Span<byte> dest)
        {
            ReadOnlySpan<uint> s = MemoryMarshal.Cast<byte, uint>(source);
            Span<uint> d = MemoryMarshal.Cast<byte, uint>(dest);
            ref uint sBase = ref MemoryMarshal.GetReference(s);
            ref uint dBase = ref MemoryMarshal.GetReference(d);

            for (int i = 0; i < s.Length; i++)
            {
                uint packed = Unsafe.Add(ref sBase, i);

                // packed              = [W Z Y X]
                // tmp1                = [W 0 Y 0]
                // tmp2                = [0 Z 0 X]
                // tmp3=ROTL(16, tmp2) = [0 X 0 Z]
                // tmp1 + tmp3         = [W X Y Z]
                uint tmp1 = packed & 0xFF00FF00;
                uint tmp2 = packed & 0x00FF00FF;
                uint tmp3 = (tmp2 << 16) | (tmp2 >> 16);

                Unsafe.Add(ref dBase, i) = tmp1 + tmp3;
            }
        }
    }
}
