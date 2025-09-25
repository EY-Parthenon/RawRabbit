using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class ExplicitAckMiddleware : Pipe.Middleware.ExplicitAckMiddleware
	{
		public ExplicitAckMiddleware(INamingConventions conventions, ITopologyProvider topology, IChannelFactory channelFactory, ExplicitAckOptions options = null)
				: base(conventions, topology, channelFactory, options) { }

		protected override async Task<Acknowledgement> AcknowledgeMessageAsync(IPipeContext context)
		{
			var policy = context.GetPolicy(PolicyKeys.MessageAcknowledge);
			var pollyContext = new Context
			{
				[RetryKey.PipeContext] = context
			};
			return await policy.ExecuteAsync(
				action: async (ctx) => await base.AcknowledgeMessageAsync(context),
				context: pollyContext);
		}
	}
}
