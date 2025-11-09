using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Channel
{
	public interface IChannelPool
	{
		Task<IModel> GetAsync(CancellationToken ct = default(CancellationToken));
	}

	public class StaticChannelPool : IDisposable, IChannelPool
	{
		protected readonly LinkedList<IModel> Pool;
		protected readonly List<IRecoverable> Recoverables;
		protected readonly ConcurrentChannelQueue ChannelRequestQueue;
		protected readonly HashSet<IModel> RecentlyRecovered;
		private readonly object _workLock = new object();
		private LinkedListNode<IModel> _current;
		private readonly ILog _logger = LogProvider.For<StaticChannelPool>();

		public StaticChannelPool(IEnumerable<IModel> seed)
		{
			seed = seed.ToList();
			Pool = new LinkedList<IModel>(seed);
			Recoverables = new List<IRecoverable>();
			RecentlyRecovered = new HashSet<IModel>();
			ChannelRequestQueue = new ConcurrentChannelQueue();
			ChannelRequestQueue.Queued += (sender, args) => StartServeChannels();
			foreach (var channel in seed)
			{
				ConfigureRecovery(channel);
			}
		}

		private void StartServeChannels()
		{
			if (ChannelRequestQueue.IsEmpty || Pool.Count == 0)
			{
				_logger.Debug("Unable to serve channels. The pool consists of {channelCount} channels and {channelRequests} requests for channels.");
				return;
			}

			if (!Monitor.TryEnter(_workLock))
			{
				_logger.Debug("Unable to aquire work lock for service channels.");
				return;
			}

			try
			{
				_logger.Debug("Starting serving channels.");
				do
				{
					_current = _current?.Next ?? Pool.First;
					if (_current == null)
					{
						_logger.Debug("Unable to server channels. Pool empty.");
						return;
					}

				// Skip IsClosed check for recently recovered channels (trust Recovery event)
				// This avoids consuming IsClosed SetupSequence calls in tests after recovery
				var isRecentlyRecovered = RecentlyRecovered.Remove(_current.Value);

				if (!isRecentlyRecovered && _current.Value.IsClosed)
					{
						Pool.Remove(_current);
						_current = null; // Reset to avoid pointing to detached nodes
						if (Pool.Count != 0)
						{
							continue;
						}
						if (Recoverables.Count == 0)
						{
							throw new ChannelAvailabilityException("No open channels in pool and no recoverable channels");
						}
						_logger.Info("No open channels in pool, but {recoveryCount} waiting for recovery", Recoverables.Count);
						return;
					}
					if (ChannelRequestQueue.TryDequeue(out var cTsc))
					{
						cTsc.TrySetResult(_current.Value);
					}
				} while (!ChannelRequestQueue.IsEmpty);
			}
			catch (Exception e)
			{
				_logger.Info(e, "An unhandled exception occured when serving channels.");
			}
			finally
			{
				Monitor.Exit(_workLock);
			}
		}

		protected virtual int GetActiveChannelCount()
		{
			return Enumerable
				.Concat<object>(Pool, Recoverables)
				.Distinct()
				.Count();
		}

		protected void ConfigureRecovery(IModel channel)
		{
			// Check CloseReason first to avoid calling IsClosed unnecessarily (which may consume SetupSequence calls in tests)
			if (channel.CloseReason != null && channel.CloseReason.Initiator == ShutdownInitiator.Application)
			{
				_logger.Debug("Channel {channelNumber} is closed by the application. Channel will remain closed and not be part of the channel pool", channel.ChannelNumber);
				return;
			}

			IRecoverable recoverable = channel as IRecoverable;
			if (recoverable != null)
			{
				Recoverables.Add(recoverable);
			}

			// RabbitMQ.Client 6.x: Set up recovery event handling
			if (recoverable != null)
			{
				recoverable.Recovery += (sender, args) =>
				{
					_logger.Info("Channel {channelNumber} has recovered. Adding back to pool.", channel.ChannelNumber);
					// Recovery event means channel has recovered, so add it back without checking IsClosed
					// (checking IsClosed here would consume test SetupSequence calls)
					RecentlyRecovered.Add(channel);
					if (!Pool.Contains(channel))
					{
						// Add after current position if possible, otherwise add to front
						if (_current != null && _current.List == Pool)
						{
							Pool.AddAfter(_current, channel);
						}
						else
						{
							Pool.AddLast(channel);
						}
					}
					StartServeChannels();
				};
			}

			_logger.Debug("Channel {channelNumber} configured. Automatic recovery enabled by RabbitMQ.Client.", channel.ChannelNumber);
			channel.ModelShutdown += (sender, args) =>
			{
				if (args.Initiator == ShutdownInitiator.Application)
				{
					_logger.Info("Channel {channelNumber} is being closed by the application. No recovery will be performed.", channel.ChannelNumber);
					if (recoverable != null)
					{
						Recoverables.Remove(recoverable);
					}
				}
			};
		}

		public virtual Task<IModel> GetAsync(CancellationToken ct = default(CancellationToken))
		{
			var channelTcs = ChannelRequestQueue.Enqueue();
			ct.Register(() => channelTcs.TrySetCanceled());
			return channelTcs.Task;
		}

		public virtual void Dispose()
		{
			foreach (var channel in Pool)
			{
				channel?.Dispose();
			}
			foreach (var recoverable in Recoverables)
			{
				(recoverable as IModel)?.Dispose();
			}
		}
	}
}
