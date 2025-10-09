using Polly;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly
{
	public static class PipeContextExtensions
	{
		// .NET 9 Migration: Use IAsyncPolicy for async operations (Polly 7.x)
		public static IAsyncPolicy GetPolicy(this IPipeContext context, string? policyName = null)
		{
			var fallback = context.Get<IAsyncPolicy>(PolicyKeys.DefaultPolicy);
			return context.Get(policyName, fallback);
		}

		public static TPipeContext UsePolicy<TPipeContext>(this TPipeContext context, IAsyncPolicy policy, string? policyName = null) where TPipeContext : IPipeContext
		{
			policyName = policyName ?? PolicyKeys.DefaultPolicy;
			context.Properties.TryAdd(policyName, policy);
			return context;
		}
	}
}
