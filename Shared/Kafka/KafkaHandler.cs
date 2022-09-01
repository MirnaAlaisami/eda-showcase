using System;
using System.Net;
using System.Threading.Tasks;
using Confluent.Kafka;
using Azure.Identity;
using Azure.Core;
using global::Avro.Specific;
using Shared.Kafka.SerializationHandler;
namespace Shared.Kafka
{
    public delegate void MessageReceived(MessageReceivedEventArgs e);
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(string topic, int partition, long offset, Timestamp timestamp, string key, object message)
        {
            Topic = topic;
            Partition = partition;
            Offset = offset;
            Timestamp = timestamp.UtcDateTime;
            Key = key;
            Message = message;
        }
        public string Topic { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
        public DateTime Timestamp { get; set; }
        public string Key { get; set; }
        public object Message { get; set; }
    }
    public class KafkaHandler
    {
        public event MessageReceived MessageReceived;
        private static KafkaHandler inst; /* Instanz des Handlers */
        private ProducerConfig _prodConfig;
        private ConsumerConfig _consConfig;
        private KafkaAvroAsyncSerializer<ISpecificRecord> _valueSerializer;
        private KafkaAvroAsyncSerializer<string> _keySerializer;
        private KafkaAvroDeserializer<ISpecificRecord> _valueDeserializer;
        private KafkaAvroDeserializer<string> _keyDeserializer;
        private bool _consumerCancelled = false;
        private bool _producerInitialized = false;
        private bool _consumerInitialized = false;
        private bool _producerCreated = false;
        private bool _consumerCreated = false;
        private bool _aadInitialized = false;
        private bool _valueSerializerInitialized = false;
        private bool _keySerializerInitialized = false;
        private bool _valueDeserializerInitialized = false;
        private bool _keyDeserializerInitialized = false;
        private DefaultAzureCredential _aadConfig;
        private TokenRequestContext _tokenRequestContext;
        public static KafkaHandler Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new KafkaHandler();
                }
                return inst;
            }
        }
        public KafkaHandler()
        {
        }
        public KafkaHandler(string BootstrapServer)
        {
            _prodConfig = new ProducerConfig()
            {
                BootstrapServers = BootstrapServer,
                ClientId = Dns.GetHostName()
            };
        }
        public void InitProducer(string BootstrapServer, SaslMechanism saslMechanism = SaslMechanism.OAuthBearer, SecurityProtocol securityProtocol = SecurityProtocol.SaslSsl)
        {
            try
            {
                _prodConfig = new ProducerConfig()
                {
                    BootstrapServers = BootstrapServer,
                    ClientId = Dns.GetHostName(),
                    SaslMechanism = saslMechanism,
                    SecurityProtocol = securityProtocol
                };
                _producerInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
        public void InitAAD(DefaultAzureCredential defaultAzureCredential, string tokenRequestContext)
        {
            try
            {
                _aadConfig = defaultAzureCredential;
                _tokenRequestContext = new TokenRequestContext(new string[] { tokenRequestContext });
                _aadInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
        public void InitConsumer(string BootstrapServer, SaslMechanism saslMechanism = SaslMechanism.OAuthBearer, SecurityProtocol securityProtocol = SecurityProtocol.SaslSsl, string Group = "$Default")
        {


            try
            {
                _consConfig = new ConsumerConfig
                {
                    BootstrapServers = BootstrapServer,
                    GroupId = Group,
                    SaslMechanism = saslMechanism,
                    SecurityProtocol = securityProtocol,
                    ClientId = Dns.GetHostName(),
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    BrokerVersionFallback = "1.0.0",        //Event Hubs for Kafka Ecosystems supports Kafka v1.0+, a fallback to an older API will fail
                    SocketTimeoutMs = 60000,                //this corresponds to the Consumer config `request.timeout.ms`
                    SessionTimeoutMs = 30000,
                };
                _consumerInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
        public void InitSchemaRegistry(string endpoint, string schemaGroup, Boolean autoRegisterSchemas = true)
        {
            if (!_aadInitialized)
            {
                Console.WriteLine("Please intialize the azure default credentials.");
                return;
            }
            try
            {
                _valueSerializer = new KafkaAvroAsyncSerializer<ISpecificRecord>(
                endpoint,
                _aadConfig,
                schemaGroup,
                autoRegisterSchemas);
                _valueSerializerInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
            try
            {
                _keySerializer = new KafkaAvroAsyncSerializer<string>(
                endpoint,
                _aadConfig,
                schemaGroup,
                autoRegisterSchemas);
                _keySerializerInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
            try
            {
                _valueDeserializer = new KafkaAvroDeserializer<ISpecificRecord>(
                endpoint,
                _aadConfig);
                _valueDeserializerInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
            try
            {
                _keyDeserializer = new KafkaAvroDeserializer<string>(
                endpoint,
                _aadConfig);
                _keyDeserializerInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
        public IProducer<string, ISpecificRecord> BuildProducerWithAADToken()
        {
            if (!_aadInitialized)
            {
                Console.WriteLine("Please intialize the azure default credentials.");
                return null;
            }
            if (!_producerInitialized)
            {
                Console.WriteLine("Please intialize the producer.");
                return null;
            }
            if (!_valueSerializerInitialized)
            {
                Console.WriteLine("Please intialize the value serializer.");
                return null;
            }
            if (!_keySerializerInitialized)
            {
                Console.WriteLine("Please intialize the key serializer.");
                return null;
            }
            IProducer<string, ISpecificRecord> producer = null;
            try
            {
                producer = new ProducerBuilder<string, ISpecificRecord>(_prodConfig)
                .SetKeySerializer(_keySerializer)
                .SetValueSerializer(_valueSerializer)
                .SetOAuthBearerTokenRefreshHandler(OAuthTokenRefreshCallback)
                .Build();
                _producerCreated = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
            return producer;
        }
        public IConsumer<string, ISpecificRecord> BuildConsumerWithAADToken()
        {
            if (!_aadInitialized)
            {
                Console.WriteLine("Please intialize the azure default credentials.");
                return null;
            }
            if (!_consumerInitialized)
            {
                Console.WriteLine("Please intialize the consumer.");
                return null;
            }
            if (!_valueDeserializerInitialized)
            {
                Console.WriteLine("Please intialize the value deserializer.");
                return null;
            }
            if (!_keyDeserializerInitialized)
            {
                Console.WriteLine("Please intialize the key deserializer.");
                return null;
            }
            IConsumer<string, ISpecificRecord> consumer = null;
            try
            {
                consumer = new ConsumerBuilder<string, ISpecificRecord>(_consConfig)
                      .SetKeyDeserializer(_keyDeserializer)
                      .SetValueDeserializer(_valueDeserializer)
                      .SetOAuthBearerTokenRefreshHandler(OAuthTokenRefreshCallback)
                      .Build();
                _consumerCreated = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
            return consumer;
        }
        private void OAuthTokenRefreshCallback(IClient client, string cfg)
        {
            try
            {
                var accessToken = _aadConfig.GetToken(_tokenRequestContext);
                var token = accessToken.Token;
                var expireOn = accessToken.ExpiresOn.ToUnixTimeMilliseconds();
                client.OAuthBearerSetToken(token, expireOn, "");
            }
            catch (Exception ex)
            {
                client.OAuthBearerSetTokenFailure(ex.ToString());
            }
        }
        public void StartConsumer(string Topic)
        {
            if (_consConfig == null) return;
            _consumerCancelled = false;
            using (var consumer = BuildConsumerWithAADToken())
            {
                if (!_consumerCreated)
                {
                    Console.WriteLine("Please create a consumer.");
                    return;
                }
                consumer.Subscribe(Topic);
                while (!_consumerCancelled)
                {
                    var consumeResult = consumer.Consume(100);
                    if (consumeResult != null)
                    {
                        if (MessageReceived != null) MessageReceived(new MessageReceivedEventArgs(consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value, consumeResult.Message.Timestamp, consumeResult.Message.Key, consumeResult.Message.Value));
                        //Console.WriteLine(string.Format("Key: {0}, Value: {1}", consumeResult.Message.Key, consumeResult.Message.Value));
                    }
                }
                consumer.Close();
            }
        }
        public async Task SendMessage(IProducer<string, ISpecificRecord> producer, string topic, Message<string, ISpecificRecord> msg)
        {
            if (!_producerCreated)
            {
                Console.WriteLine("Please create a producer.");
                return;
            }
            try
            {
                using (producer)
                {
                    // publishes the message to Event Hubs
                    var result = await producer.ProduceAsync(topic, msg);
                    Console.WriteLine($"Message {result.Value} sent to partition {result.TopicPartition} with result {result.Status}");
                    producer.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
    }
}
