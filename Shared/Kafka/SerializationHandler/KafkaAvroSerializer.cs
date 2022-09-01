//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace Shared.Kafka.SerializationHandler
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using Azure.Core;
    using Azure.Data.SchemaRegistry;
    using Avro.IO;
    using Confluent.Kafka;
    using Microsoft.Azure.Data.SchemaRegistry.ApacheAvro;
    /// <summary>
    /// Implementation of Confluent .NET Kafka async serializer, wrapping Azure Schema Registry C# implementation.
    ///
    /// Serializer should be used for GenericRecord type or generated SpecificRecord types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KafkaAvroSerializer<T> : ISerializer<T>
    {
        private readonly SchemaRegistryAvroObjectSerializer serializer;
        public KafkaAvroSerializer(string endpoint, TokenCredential credential, string schemaGroup, Boolean autoRegisterSchemas)
        {
            this.serializer = new SchemaRegistryAvroObjectSerializer(
                new SchemaRegistryClient(
                    endpoint,
                    credential),
                schemaGroup,
                new SchemaRegistryAvroObjectSerializerOptions()
                {
                    AutoRegisterSchemas = autoRegisterSchemas
                });
        }
        public byte[] Serialize(T o, SerializationContext context)
        {
            if (o == null)
            {
                return null;
            }
            if (o is string s)
            {
                return Encoding.ASCII.GetBytes(s);
            }
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, o, typeof(T), CancellationToken.None);
                return stream.ToArray();
            }
        }
    }
}
