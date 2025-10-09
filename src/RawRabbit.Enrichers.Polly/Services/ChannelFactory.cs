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
		protected ResiliencePipeline CreateChannelPolicy;
		protected ResiliencePipeline ConnectPolicy;
		protected ResiliencePipeline GetConnectionPolicy;

		public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config, ConnectionPolicies? policies = null)
			: base(connectionFactory, config)
		{
			CreateChannelPolicy = policies?.CreateChannel ?? ResiliencePipeline.Empty;
			ConnectPolicy = policies?.Connect ?? ResiliencePipeline.Empty;
			GetConnectionPolicy = policies?.GetConnection ?? ResiliencePipeline.Empty;
		}

		public override async Task ConnectAsync(CancellationToken token = default)
		{
			await ConnectPolicy.ExecuteAsync(
				async ct => await base.ConnectAsync(ct),
				token
			);
		}

		protected override async Task<IConnection> GetConnectionAsync(CancellationToken token = default)
		{
			return await GetConnectionPolicy.ExecuteAsync(
				async ct => await base.GetConnectionAsync(ct),
				token
			);
		}

		public override async Task<IModel> CreateChannelAsync(CancellationToken token = default)
		{
			return await CreateChannelPolicy.ExecuteAsync(
				async ct => await base.CreateChannelAsync(ct),
				token
			);
		}
	}

	public class ConnectionPolicies
	{
		/// <summary>
		/// Used whenever 'CreateChannelAsync' is called.
		/// Expects a ResiliencePipeline (Polly 8.x).
		/// </summary>
		public ResiliencePipeline? CreateChannel { get; set; }

		/// <summary>
		/// Used whenever an existing connection is retrieved.
		/// </summary>
		public ResiliencePipeline? GetConnection { get; set; }

		/// <summary>
		/// Used when establishing the initial connection
		/// </summary>
		public ResiliencePipeline? Connect { get; set; }
	}
}
