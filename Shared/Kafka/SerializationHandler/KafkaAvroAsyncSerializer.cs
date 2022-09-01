//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace Shared.Kafka.SerializationHandler
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Data.SchemaRegistry;
    using Confluent.Kafka;
    using Microsoft.Azure.Data.SchemaRegistry.ApacheAvro;
    /// <summary>
    /// Implementation of Confluent .NET Kafka async serializer, wrapping Azure Schema Registry C# implementation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KafkaAvroAsyncSerializer<T> : IAsyncSerializer<T>
    {
        private readonly SchemaRegistryAvroObjectSerializer serializer;
        public KafkaAvroAsyncSerializer(string endpoint, TokenCredential credential, string schemaGroup, Boolean autoRegisterSchemas)
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
        public async Task<byte[]> SerializeAsync(T o, SerializationContext context)
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
                await serializer.SerializeAsync(stream, o, typeof(T), CancellationToken.None);
                return stream.ToArray();
            }
        }
    }
}
