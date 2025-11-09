using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace RawRabbit.Common
{
	/// <summary>
	/// Simple implementation of IBasicProperties for RabbitMQ.Client 6.x compatibility
	/// where BasicProperties class is internal. This is used in factory methods where
	/// a channel is not yet available.
	/// </summary>
	public class SimpleBasicProperties : IBasicProperties
	{
		public string AppId { get; set; }
		public string ClusterId { get; set; }
		public string ContentEncoding { get; set; }
		public string ContentType { get; set; }
		public string CorrelationId { get; set; }
		public byte DeliveryMode { get; set; }
		public string Expiration { get; set; }
		public IDictionary<string, object> Headers { get; set; }
		public string MessageId { get; set; }
		public bool Persistent { get; set; }
		public byte Priority { get; set; }
		public string ReplyTo { get; set; }
		public PublicationAddress ReplyToAddress { get; set; }
		public AmqpTimestamp Timestamp { get; set; }
		public string Type { get; set; }
		public string UserId { get; set; }

		public ushort ProtocolClassId => 60;
		public string ProtocolClassName => "basic";

		public SimpleBasicProperties()
		{
			Headers = new Dictionary<string, object>();
		}

		public void ClearAppId() => AppId = null;
		public void ClearClusterId() => ClusterId = null;
		public void ClearContentEncoding() => ContentEncoding = null;
		public void ClearContentType() => ContentType = null;
		public void ClearCorrelationId() => CorrelationId = null;
		public void ClearDeliveryMode() => DeliveryMode = 0;
		public void ClearExpiration() => Expiration = null;
		public void ClearHeaders() => Headers = null;
		public void ClearMessageId() => MessageId = null;
		public void ClearPriority() => Priority = 0;
		public void ClearReplyTo() => ReplyTo = null;
		public void ClearTimestamp() => Timestamp = default;
		public void ClearType() => Type = null;
		public void ClearUserId() => UserId = null;

		public bool IsAppIdPresent() => AppId != null;
		public bool IsClusterIdPresent() => ClusterId != null;
		public bool IsContentEncodingPresent() => ContentEncoding != null;
		public bool IsContentTypePresent() => ContentType != null;
		public bool IsCorrelationIdPresent() => CorrelationId != null;
		public bool IsDeliveryModePresent() => DeliveryMode != 0;
		public bool IsExpirationPresent() => Expiration != null;
		public bool IsHeadersPresent() => Headers != null;
		public bool IsMessageIdPresent() => MessageId != null;
		public bool IsPriorityPresent() => Priority != 0;
		public bool IsReplyToPresent() => ReplyTo != null;
		public bool IsTimestampPresent() => Timestamp.UnixTime != 0;
		public bool IsTypePresent() => Type != null;
		public bool IsUserIdPresent() => UserId != null;
	}
}
