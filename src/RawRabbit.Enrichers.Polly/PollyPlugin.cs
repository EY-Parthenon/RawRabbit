using System;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	/// <summary>
	/// Extension methods for integrating Polly 8.x resilience pipelines with RawRabbit.
	/// Polly 8.x: Updated to use ResiliencePipeline instead of Policy.
	/// </summary>
	public static class PollyPlugin
	{
		/// <summary>
		/// Enables Polly 8.x resilience pipelines for RawRabbit operations.
		/// Users can inject custom ResiliencePipelines via the action parameter.
		/// Polly 8.x: Use ResiliencePipelineBuilder instead of Policy API.
		/// </summary>
		/// <param name="builder">The client builder.</param>
		/// <param name="action">Action to configure ResiliencePipelines in the pipe context.</param>
		/// <returns>The client builder for chaining.</returns>
		public static IClientBuilder UsePolly(this IClientBuilder builder, Action<IPipeContext> action)
		{
			return UsePolly(builder, new PolicyOptions {PolicyAction = action});
		}

		/// <summary>
		/// Enables Polly 8.x resilience pipelines for RawRabbit operations.
		/// Replaces core middleware with Polly-wrapped versions.
		/// Polly 8.x: Use ResiliencePipelineBuilder instead of Policy API.
		/// </summary>
		/// <param name="builder">The client builder.</param>
		/// <param name="options">Options containing policy configuration and connection policies.</param>
		/// <returns>The client builder for chaining.</returns>
		public static IClientBuilder UsePolly(this IClientBuilder builder, PolicyOptions options)
		{
			builder.Register(
				pipe => pipe
					.Use<PolicyMiddleware>(options)
					.Replace<QueueDeclareMiddleware, Enrichers.Polly.Middleware.QueueDeclareMiddleware>(argsFunc: oldArgs => oldArgs)
					.Replace<ExchangeDeclareMiddleware, Enrichers.Polly.Middleware.ExchangeDeclareMiddleware>(argsFunc: oldArgs => oldArgs)
					.Replace<QueueBindMiddleware, Enrichers.Polly.Middleware.QueueBindMiddleware>(argsFunc: oldArgs => oldArgs)
					.Replace<ConsumerCreationMiddleware, Enrichers.Polly.Middleware.ConsumerCreationMiddleware>(argsFunc: oldArgs => oldArgs)
					.Replace<BasicPublishMiddleware, Enrichers.Polly.Middleware.BasicPublishMiddleware>(argsFunc: oldArgs => oldArgs)
					.Replace<ExplicitAckMiddleware, Enrichers.Polly.Middleware.ExplicitAckMiddleware>(argsFunc: oldArgs => oldArgs)
					.Replace<PooledChannelMiddleware, Enrichers.Polly.Middleware.PooledChannelMiddleware>(argsFunc: oldArgs => oldArgs)
					.Replace<TransientChannelMiddleware, Enrichers.Polly.Middleware.TransientChannelMiddleware>(argsFunc: oldArgs => oldArgs)
					.Replace<HandlerInvocationMiddleware, Enrichers.Polly.Middleware.HandlerInvocationMiddleware>(argsFunc: oldArgs => oldArgs),
				ioc => ioc
					.AddSingleton<IChannelFactory, RawRabbit.Enrichers.Polly.Services.ChannelFactory>()
					.AddSingleton(options.ConnectionPolicies));
			return builder;
		}
	}
}
