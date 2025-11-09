using System;
using MessagePack;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.MessagePack
{
	internal class MessagePackSerializerWorker : ISerializer
	{
		public string ContentType => "application/x-messagepack";
		private readonly MessagePackSerializerOptions _options;

		public MessagePackSerializerWorker(MessagePackFormat format)
		{
			// MessagePack 2.x: LZ4 compression is now handled through options
			if (format == MessagePackFormat.LZ4Compression)
				_options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
			else
				_options = MessagePackSerializerOptions.Standard;
		}

		public byte[] Serialize(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			// MessagePack 2.x: Use Typeless API for dynamic serialization
			return MessagePackSerializer.Typeless.Serialize(obj, _options);
		}

		public object Deserialize(Type type, byte[] bytes)
		{
			// MessagePack 2.x: Use Typeless API for dynamic deserialization
			return MessagePackSerializer.Typeless.Deserialize(bytes, _options);
		}

		public TType Deserialize<TType>(byte[] bytes)
		{
			return MessagePackSerializer.Deserialize<TType>(bytes, _options);
		}
	}
}
