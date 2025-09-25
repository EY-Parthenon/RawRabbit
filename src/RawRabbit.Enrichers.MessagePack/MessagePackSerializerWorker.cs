using System;
using System.Linq;
using System.Reflection;
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
			// In MessagePack 2.x, LZ4 compression is handled via options
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
				throw new ArgumentNullException();

			// Use reflection to call the generic Serialize method
			var method = typeof(MessagePackSerializer)
				.GetMethods()
				.FirstOrDefault(m => m.Name == nameof(MessagePackSerializer.Serialize) 
					&& m.IsGenericMethodDefinition 
					&& m.GetParameters().Length == 2
					&& m.GetParameters()[1].ParameterType == typeof(MessagePackSerializerOptions));
			
			return (byte[])method
				.MakeGenericMethod(obj.GetType())
				.Invoke(null, new[] { obj, _options });
		}

		public object Deserialize(Type type, byte[] bytes)
		{
			var method = typeof(MessagePackSerializer)
				.GetMethod(nameof(MessagePackSerializer.Deserialize), new[] { typeof(byte[]), typeof(MessagePackSerializerOptions) });
			
			return method.MakeGenericMethod(type)
				.Invoke(null, new object[] { bytes, _options });
		}

		public TType Deserialize<TType>(byte[] bytes)
		{
			return MessagePackSerializer.Deserialize<TType>(bytes, _options);
		}
	}
}
