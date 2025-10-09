using Polly;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly
{
	public static class PipeContextExtensions
	{
		// .NET 9 Migration: Upgraded to ResiliencePipeline for Polly 8.x
		public static ResiliencePipeline GetPolicy(this IPipeContext context, string? policyName = null)
		{
			policyName = policyName ?? PolicyKeys.DefaultPolicy;
			var policy = context.Get<ResiliencePipeline>(policyName);
			if (policy != null)
				return policy;

			var fallback = context.Get<ResiliencePipeline>(PolicyKeys.DefaultPolicy);
			return fallback ?? ResiliencePipeline.Empty;
		}

		public static TPipeContext UsePolicy<TPipeContext>(this TPipeContext context, ResiliencePipeline policy, string? policyName = null) where TPipeContext : IPipeContext
		{
			policyName = policyName ?? PolicyKeys.DefaultPolicy;
			context.Properties.TryAdd(policyName, policy);
			return context;
		}
	}
}
