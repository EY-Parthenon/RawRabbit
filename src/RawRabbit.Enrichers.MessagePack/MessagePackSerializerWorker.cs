using System;
using MessagePack;
using MessagePack.Resolvers;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.MessagePack
{
	internal class MessagePackSerializerWorker : ISerializer
	{
		public string ContentType => "application/x-messagepack";
		// .NET 9 Migration: MessagePack v2.x uses MessagePackSerializerOptions instead of separate serializers
		private readonly MessagePackSerializerOptions _options;

		public MessagePackSerializerWorker(MessagePackFormat format)
		{
			// .NET 9 Migration: Configure options based on format
			if (format == MessagePackFormat.LZ4Compression)
			{
				_options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
			}
			else
			{
				_options = MessagePackSerializerOptions.Standard;
			}
		}

		public byte[] Serialize(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			// .NET 9 Migration: Use new MessagePack v2.x API
			var objectType = obj.GetType();
			return MessagePackSerializer.Serialize(objectType, obj, _options);
		}

		public object? Deserialize(Type type, byte[] bytes)
		{
			// .NET 9 Migration: Use new MessagePack v2.x API
			return MessagePackSerializer.Deserialize(type, bytes, _options);
		}

		public TType Deserialize<TType>(byte[] bytes)
		{
			// .NET 9 Migration: Use new MessagePack v2.x API with options
			return MessagePackSerializer.Deserialize<TType>(bytes, _options);
		}
	}
}
