using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Configuration;

namespace RawRabbit.Enrichers.Polly.Services
{
	public class ChannelFactory : Channel.ChannelFactory
	{
		// Polly 8.x: Updated from Policy to ResiliencePipeline
		protected ResiliencePipeline CreateChannelPolicy;
		protected ResiliencePipeline ConnectPolicy;
		protected ResiliencePipeline GetConnectionPolicy;

		public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config, ConnectionPolicies policies = null)
			: base(connectionFactory, config)
		{
			// Polly 8.x: Use ResiliencePipeline.Empty instead of Policy.NoOpAsync()
			CreateChannelPolicy = policies?.CreateChannel ?? ResiliencePipeline.Empty;
			ConnectPolicy = policies?.Connect ?? ResiliencePipeline.Empty;
			GetConnectionPolicy = policies?.GetConnection ?? ResiliencePipeline.Empty;
		}

		public override Task ConnectAsync(CancellationToken token = default(CancellationToken))
		{
			// Polly 8.x: Updated ExecuteAsync pattern - context data not directly supported
			// Users should use ResilienceContext if needed in custom pipelines
			return ConnectPolicy.ExecuteAsync(
				callback: async ct => await base.ConnectAsync(ct),
				cancellationToken: token
			).AsTask();
		}

		protected override Task<IConnection> GetConnectionAsync(CancellationToken token = default(CancellationToken))
		{
			// Polly 8.x: Updated ExecuteAsync pattern - context data not directly supported
			// Users should use ResilienceContext if needed in custom pipelines
			return GetConnectionPolicy.ExecuteAsync(
				callback: async ct => await base.GetConnectionAsync(ct),
				cancellationToken: token
			).AsTask();
		}

		public override Task<IModel> CreateChannelAsync(CancellationToken token = default(CancellationToken))
		{
			// Polly 8.x: Updated ExecuteAsync pattern - context data not directly supported
			// Users should use ResilienceContext if needed in custom pipelines
			return CreateChannelPolicy.ExecuteAsync(
				callback: async ct => await base.CreateChannelAsync(ct),
				cancellationToken: token
			).AsTask();
		}
	}

	public class ConnectionPolicies
	{
		/// <summary>
		/// Used whenever 'CreateChannelAsync' is called.
		/// Polly 8.x: Expects a ResiliencePipeline (formerly IAsyncPolicy).
		/// </summary>
		public ResiliencePipeline CreateChannel { get; set; }

		/// <summary>
		/// Used whenever an existing connection is retrieved.
		/// Polly 8.x: Expects a ResiliencePipeline (formerly IAsyncPolicy).
		/// </summary>
		public ResiliencePipeline GetConnection { get; set; }

		/// <summary>
		/// Used when establishing the initial connection.
		/// Polly 8.x: Expects a ResiliencePipeline (formerly IAsyncPolicy).
		/// </summary>
		public ResiliencePipeline Connect { get; set; }
	}
}
