//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace Shared.Kafka.SerializationHandler
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Text;
    using Confluent.Kafka;
    using Azure.Core;
    using Azure.Data.SchemaRegistry;
    using Microsoft.Azure.Data.SchemaRegistry.ApacheAvro;
    /// <summary>
    /// Implementation of Confluent .NET Kafka deserializer, wrapping Azure Schema Registry C# implementation.
    ///
    /// Note that Confluent .NET Kafka removed support for IAsyncDeserializer<T>.  See: https://github.com/confluentinc/confluent-kafka-dotnet/issues/922
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KafkaAvroDeserializer<T> : IDeserializer<T>
    {
        private readonly SchemaRegistryAvroObjectSerializer serializer;
        public KafkaAvroDeserializer(string endpoint, TokenCredential credential)
        {
            this.serializer = new SchemaRegistryAvroObjectSerializer(new SchemaRegistryClient(endpoint, credential), "$default");
        }
        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (data.IsEmpty)
            {
                return default(T);
            }
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(Encoding.ASCII.GetString(data.ToArray()), typeof(T));
            }
            return (T)this.serializer.Deserialize(new MemoryStream(data.ToArray()), typeof(T), CancellationToken.None);
        }
    }
}
