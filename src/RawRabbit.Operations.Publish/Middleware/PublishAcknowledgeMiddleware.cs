using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Exceptions;
using RawRabbit.Logging;
using RawRabbit.Operations.Publish.Context;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class PublishAcknowledgeOptions
	{
		public Func<IPipeContext, TimeSpan> TimeOutFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, bool> EnabledFunc { get; set; }
	}

	public class PublishAcknowledgeMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IExclusiveLock _exclusive;
		private readonly ILog _logger = LogProvider.For<PublishAcknowledgeMiddleware>();
		protected Func<IPipeContext, TimeSpan> TimeOutFunc;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Func<IPipeContext, bool> EnabledFunc;

		public PublishAcknowledgeMiddleware(IExclusiveLock exclusive, PublishAcknowledgeOptions options = null)
		{
			_exclusive = exclusive;
			TimeOutFunc = options?.TimeOutFunc ?? (context => context.GetPublishAcknowledgeTimeout());
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			EnabledFunc = options?.EnabledFunc ?? (context => context.GetPublishAcknowledgeTimeout() != TimeSpan.MaxValue);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			_logger.Info("PublishAcknowledgeMiddleware.InvokeAsync START");
			var enabled = GetEnabled(context);
			_logger.Info("Publisher acknowledgement enabled: {enabled}", enabled);
			if (!enabled)
			{
				_logger.Debug("Publish Acknowledgement is disabled.");
				await Next.InvokeAsync(context, token);
				return;
			}

			var channel = GetChannel(context);
			_logger.Info("Got channel from context: ChannelNumber={channelNumber}, NextPublishSeqNo={seqNo}", channel?.ChannelNumber, channel?.NextPublishSeqNo);

			// Ensure publisher confirms are enabled on the channel
			_exclusive.Execute(channel, _ =>
			{
				if (!PublishAcknowledgeEnabled(channel))
				{
					_logger.Info("Enabling publisher confirms for channel {channelNumber}", channel.ChannelNumber);
					channel.ConfirmSelect();
				}
			}, token);

			// Invoke next middleware to publish the message
			_logger.Info("Invoking next middleware (BasicPublish)");
			await Next.InvokeAsync(context, token);

			// Use WaitForConfirmsOrDie() to synchronously wait for broker acknowledgement
			// This is more reliable than event-based approach with RabbitMQ.Client 6.x
			var timeout = GetAcknowledgeTimeOut(context);
			_logger.Info("Waiting for publisher confirmation with timeout {timeout:g}", timeout);

			try
			{
				await Task.Run(() =>
				{
					_exclusive.Execute(channel, _ =>
					{
						channel.WaitForConfirmsOrDie(timeout);
					}, token);
				}, token);
				_logger.Info("Publisher confirmation received successfully");
			}
			catch (Exception ex)
			{
				var message = $"The broker did not send a publish acknowledgement within {timeout:g}.";
				_logger.Error(ex, "Publisher confirmation failed: {message}", message);
				throw new PublishConfirmException(message, ex);
			}
		}

		protected virtual TimeSpan GetAcknowledgeTimeOut(IPipeContext context)
		{
			return TimeOutFunc(context);
		}

		protected virtual bool PublishAcknowledgeEnabled(IModel channel)
		{
			return channel.NextPublishSeqNo != 0UL;
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}

		protected virtual bool GetEnabled(IPipeContext context)
		{
			return EnabledFunc(context);
		}
	}
}

namespace RawRabbit
{
	public static class PublishAcknowledgePipeGetExtensions
	{
		public static TimeSpan GetPublishAcknowledgeTimeout(this IPipeContext context)
		{
			var fallback = context.GetClientConfiguration().PublishConfirmTimeout;
			return context.Get(Operations.Publish.PublishKey.PublishAcknowledgeTimeout, fallback);
		}
	}

	public static class PublishAcknowledgePipeUseExtensions
	{
		public static IPublishContext UsePublishAcknowledge(this IPublishContext context, TimeSpan timeout)
		{
			System.Collections.Generic.CollectionExtensions.TryAdd(context.Properties, Operations.Publish.PublishKey.PublishAcknowledgeTimeout, timeout);
			return context;
		}

		public static IPublishContext UsePublishAcknowledge(this IPublishContext context, bool use = true)
		{
			return !use
				? context.UsePublishAcknowledge(TimeSpan.MaxValue)
				: context;
		}
	}
}
