// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.ImageSharp.Tests.TestUtilities;
using Xunit;

namespace SixLabors.ImageSharp.Tests.Common
{
    public partial class SimdUtilsTests
    {
        [Theory]
        [MemberData(nameof(ArraySizesDivisibleBy4))]
        public void BulkShuffleFloat4Channel(int count)
        {
            static void RunTest(string serialized)
            {
                // No need to test multiple shuffle controls as the
                // pipeline is always the same.
                int size = FeatureTestRunner.Deserialize<int>(serialized);
                byte control = default(WZYXShuffle4).Control;

                TestShuffleFloat4Channel(
                    size,
                    (s, d) => SimdUtils.Shuffle4Channel(s.Span, d.Span, control),
                    control);
            }

            FeatureTestRunner.RunWithHwIntrinsicsFeature(
                RunTest,
                count,
                HwIntrinsics.AllowAll | HwIntrinsics.DisableAVX | HwIntrinsics.DisableSSE);
        }

        [Theory]
        [MemberData(nameof(ArraySizesDivisibleBy4))]
        public void BulkShuffleByte4Channel(int count)
        {
            static void RunTest(string serialized)
            {
                int size = FeatureTestRunner.Deserialize<int>(serialized);
                foreach (var item in ArraySizesDivisibleBy4)
                {
                    // These cannot be expressed as a theory as you cannot
                    // use RemoteExecutor within generic methods nor pass
                    // IComponentShuffle to the generic utils method.
                    foreach (var count in item)
                    {
                        WXYZShuffle4 wxyz = default;
                        TestShuffleByte4Channel(
                            size,
                            (s, d) => SimdUtils.Shuffle4Channel(s.Span, d.Span, wxyz),
                            wxyz.Control);

                        WZYXShuffle4 wzyx = default;
                        TestShuffleByte4Channel(
                            size,
                            (s, d) => SimdUtils.Shuffle4Channel(s.Span, d.Span, wzyx),
                            wzyx.Control);

                        YZWXShuffle4 yzwx = default;
                        TestShuffleByte4Channel(
                            size,
                            (s, d) => SimdUtils.Shuffle4Channel(s.Span, d.Span, yzwx),
                            yzwx.Control);

                        ZYXWShuffle4 zyxw = default;
                        TestShuffleByte4Channel(
                            size,
                            (s, d) => SimdUtils.Shuffle4Channel(s.Span, d.Span, zyxw),
                            zyxw.Control);

                        var xwyz = new DefaultShuffle4(2, 1, 3, 0);
                        TestShuffleByte4Channel(
                            size,
                            (s, d) => SimdUtils.Shuffle4Channel(s.Span, d.Span, xwyz),
                            xwyz.Control);

                        var yyyy = new DefaultShuffle4(1, 1, 1, 1);
                        TestShuffleByte4Channel(
                            size,
                            (s, d) => SimdUtils.Shuffle4Channel(s.Span, d.Span, yyyy),
                            yyyy.Control);

                        var wwww = new DefaultShuffle4(3, 3, 3, 3);
                        TestShuffleByte4Channel(
                            size,
                            (s, d) => SimdUtils.Shuffle4Channel(s.Span, d.Span, wwww),
                            wwww.Control);
                    }
                }
            }

            FeatureTestRunner.RunWithHwIntrinsicsFeature(
                RunTest,
                count,
                HwIntrinsics.AllowAll | HwIntrinsics.DisableAVX2 | HwIntrinsics.DisableSSE);
        }

        private static void TestShuffleFloat4Channel(
            int count,
            Action<Memory<float>, Memory<float>> convert,
            byte control)
        {
            float[] source = new Random(count).GenerateRandomFloatArray(count, 0, 256);
            var result = new float[count];

            float[] expected = new float[count];

            SimdUtils.Shuffle.InverseMmShuffle(
                control,
                out int p3,
                out int p2,
                out int p1,
                out int p0);

            for (int i = 0; i < expected.Length; i += 4)
            {
                expected[i] = source[p0 + i];
                expected[i + 1] = source[p1 + i];
                expected[i + 2] = source[p2 + i];
                expected[i + 3] = source[p3 + i];
            }

            convert(source, result);

            Assert.Equal(expected, result, new ApproximateFloatComparer(1e-5F));
        }

        private static void TestShuffleByte4Channel(
            int count,
            Action<Memory<byte>, Memory<byte>> convert,
            byte control)
        {
            byte[] source = new byte[count];
            new Random(count).NextBytes(source);
            var result = new byte[count];

            byte[] expected = new byte[count];

            SimdUtils.Shuffle.InverseMmShuffle(
                control,
                out int p3,
                out int p2,
                out int p1,
                out int p0);

            for (int i = 0; i < expected.Length; i += 4)
            {
                expected[i] = source[p0 + i];
                expected[i + 1] = source[p1 + i];
                expected[i + 2] = source[p2 + i];
                expected[i + 3] = source[p3 + i];
            }

            convert(source, result);

            Assert.Equal(expected, result);
        }
    }
}
