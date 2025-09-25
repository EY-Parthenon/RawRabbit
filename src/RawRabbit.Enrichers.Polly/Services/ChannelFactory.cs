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
		protected IAsyncPolicy CreateChannelPolicy;
		protected IAsyncPolicy ConnectPolicy;
		protected IAsyncPolicy GetConnectionPolicy;

		public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config, ConnectionPolicies policies = null)
			: base(connectionFactory, config)
		{
			CreateChannelPolicy = policies?.CreateChannel ?? Policy.NoOpAsync();
			ConnectPolicy = policies?.Connect ?? Policy.NoOpAsync();
			GetConnectionPolicy = policies?.GetConnection ?? Policy.NoOpAsync();
		}

		public override Task ConnectAsync(CancellationToken token = default(CancellationToken))
		{
			var pollyContext = new Context
			{
				[RetryKey.ConnectionFactory] = ConnectionFactory,
				[RetryKey.ClientConfiguration] = ClientConfig
			};
			return ConnectPolicy.ExecuteAsync(
				action: (ctx, ct) => base.ConnectAsync(ct),
				context: pollyContext,
				cancellationToken: token
			);
		}

		protected override Task<IConnection> GetConnectionAsync(CancellationToken token = default(CancellationToken))
		{
			var pollyContext = new Context
			{
				[RetryKey.ConnectionFactory] = ConnectionFactory,
				[RetryKey.ClientConfiguration] = ClientConfig
			};
			return GetConnectionPolicy.ExecuteAsync(
				action: (ctx, ct) => base.GetConnectionAsync(ct),
				context: pollyContext,
				cancellationToken: token
			);
		}

		public override Task<IModel> CreateChannelAsync(CancellationToken token = default(CancellationToken))
		{
			var pollyContext = new Context
			{
				[RetryKey.ConnectionFactory] = ConnectionFactory,
				[RetryKey.ClientConfiguration] = ClientConfig
			};
			return CreateChannelPolicy.ExecuteAsync(
				action: (ctx, ct) => base.CreateChannelAsync(ct),
				context: pollyContext,
				cancellationToken: token
			);
		}
	}

	public class ConnectionPolicies
	{
		/// <summary>
		/// Used whenever 'CreateChannelAsync' is called.
		/// Expects an async policy.
		/// </summary>
		public IAsyncPolicy CreateChannel { get; set; }

		/// <summary>
		/// Used whenever an existing connection is retrieved.
		/// </summary>
		public IAsyncPolicy GetConnection { get; set; }

		/// <summary>
		/// Used when establishing the initial connection
		/// </summary>
		public IAsyncPolicy Connect { get; set; }
	}
}
