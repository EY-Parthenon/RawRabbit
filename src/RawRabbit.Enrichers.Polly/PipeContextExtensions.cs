using Polly;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly
{
	public static class PipeContextExtensions
	{
		// Polly 8.x: Updated from Policy to ResiliencePipeline
		public static ResiliencePipeline GetPolicy(this IPipeContext context, string policyName = null)
		{
			var fallback = context.Get<ResiliencePipeline>(PolicyKeys.DefaultPolicy);
			return context.Get(policyName, fallback);
		}

		// Polly 8.x: Updated from Policy to ResiliencePipeline
		public static TPipeContext UsePolicy<TPipeContext>(this TPipeContext context, ResiliencePipeline pipeline, string policyName = null) where TPipeContext : IPipeContext
		{
			policyName = policyName ?? PolicyKeys.DefaultPolicy;
			context.Properties.TryAdd(policyName, pipeline);
			return context;
		}
	}
}
