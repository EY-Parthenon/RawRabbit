using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Enrichers.Polly.Services;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	/// <summary>
	/// Options for configuring Polly resilience pipelines.
	/// Polly 8.x: Updated to use ResiliencePipeline instead of Policy.
	/// </summary>
	public class PolicyOptions
	{
		/// <summary>
		/// Action that allows users to configure custom ResiliencePipelines for different operations.
		/// Users can add pipelines to the IPipeContext using PolicyKeys constants.
		/// </summary>
		public Action<IPipeContext> PolicyAction { get; set; }

		/// <summary>
		/// Connection-level resilience pipelines for channel and connection operations.
		/// Polly 8.x: Uses ResiliencePipeline instead of Policy.
		/// </summary>
		public ConnectionPolicies ConnectionPolicies { get; set; }
	}

	/// <summary>
	/// Middleware that invokes user-provided policy configuration action.
	/// This allows users to inject custom Polly 8.x ResiliencePipelines into the pipe context.
	/// Polly 8.x: Updated to work with ResiliencePipeline instead of Policy.
	/// </summary>
	public class PolicyMiddleware : StagedMiddleware
	{
		protected Action<IPipeContext> PolicyAction;

		public PolicyMiddleware(PolicyOptions options = null)
		{
			PolicyAction = options?.PolicyAction;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			AddPolicies(context);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void AddPolicies(IPipeContext context)
		{
			PolicyAction?.Invoke(context);
		}

		public override string StageMarker => Pipe.StageMarker.Initialized;
	}
}
