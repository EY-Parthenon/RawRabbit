using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

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
			var policy = context.GetPolicy(PolicyKeys.BasicPublish);
			var policyTask = policy.ExecuteAsync(
				async ct =>
				{
					base.BasicPublish(channel, exchange, routingKey, mandatory, basicProps, body, context);
					return await Task.FromResult(true);
				},
				CancellationToken.None);
			policyTask.ConfigureAwait(false);
			policyTask.GetAwaiter().GetResult();
		}
	}
}
