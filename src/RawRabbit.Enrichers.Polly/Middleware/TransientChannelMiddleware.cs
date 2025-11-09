using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class TransientChannelMiddleware : Pipe.Middleware.TransientChannelMiddleware
	{
		public TransientChannelMiddleware(IChannelFactory factory)
			: base(factory) { }

		protected override Task<IModel> CreateChannelAsync(IPipeContext context, CancellationToken token)
		{
			// Polly 8.x: Updated to use ResiliencePipeline instead of IAsyncPolicy
			var pipeline = context.GetPolicy(PolicyKeys.ChannelCreate);
			return pipeline.ExecuteAsync(
				callback: async ct => await base.CreateChannelAsync(context, ct),
				cancellationToken: token
			).AsTask();
		}
	}
}
