using System.Collections.Generic;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using System.Threading.Tasks;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class BasicPublishMiddleware : Pipe.Middleware.BasicPublishMiddleware
	{
		public BasicPublishMiddleware(IExclusiveLock exclusive, BasicPublishOptions options = null)
			: base(exclusive, options) { }

		protected override void BasicPublish(
				IModel channel,
				string exchange,
				string routingKey,
				bool mandatory,
				IBasicProperties basicProps,
				byte[] body,
				IPipeContext context)
		{
			// Polly 8.x: Updated to use ResiliencePipeline instead of IAsyncPolicy
			var pipeline = context.GetPolicy(PolicyKeys.BasicPublish);
			var pipelineTask = pipeline.ExecuteAsync(
				callback: async ct =>
				{
					base.BasicPublish(channel, exchange, routingKey, mandatory, basicProps, body, context);
					return true;
				});
			pipelineTask.ConfigureAwait(false);
			pipelineTask.GetAwaiter().GetResult();
		}
	}
}
