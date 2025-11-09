using System.Collections.Generic;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class ExplicitAckMiddleware : Pipe.Middleware.ExplicitAckMiddleware
	{
		public ExplicitAckMiddleware(INamingConventions conventions, ITopologyProvider topology, IChannelFactory channelFactory, ExplicitAckOptions options = null)
				: base(conventions, topology, channelFactory, options) { }

		protected override async Task<Acknowledgement> AcknowledgeMessageAsync(IPipeContext context)
		{
			// Polly 8.x: Updated to use ResiliencePipeline instead of IAsyncPolicy
			var pipeline = context.GetPolicy(PolicyKeys.MessageAcknowledge);
			var result = await pipeline.ExecuteAsync(
				callback: async ct => await base.AcknowledgeMessageAsync(context));
			return result;
		}
	}
}
