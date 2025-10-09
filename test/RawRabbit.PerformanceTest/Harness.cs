using System;
using BenchmarkDotNet.Running;
using Xunit;

namespace RawRabbit.PerformanceTest
{
	public class Harness
	{
		[Fact]
		public void PubSubBenchmarks()
		{
			var summary = BenchmarkRunner.Run<PubSubBenchmarks>();
			Assert.NotNull(summary);
		}

		[Fact]
		public void RpcBenchmarks()
		{
			var summary = BenchmarkRunner.Run<RpcBenchmarks>();
			Assert.NotNull(summary);
		}

		[Fact]
		public void MessageContextBenchmarks()
		{
			var summary = BenchmarkRunner.Run<MessageContextBenchmarks>();
			Assert.NotNull(summary);
		}
	}
}
