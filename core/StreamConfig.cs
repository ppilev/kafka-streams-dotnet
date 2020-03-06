﻿using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace kafka_stream_core
{
    public interface IStreamConfig
    {
        #region Methods 

        ProducerConfig toProducerConfig();

        ConsumerConfig toConsumerConfig();
        
        ConsumerConfig toConsumerConfig(string clientid);

        ConsumerConfig toGlobalConsumerConfig(string clientId);

        AdminClientConfig toAdminConfig(string clientId);

        #endregion

        string ApplicationId { get; }

        int NumStreamThreads { get; }
    }

    public class StreamConfig : Dictionary<string, string>, IStreamConfig
    {
        private string topologyOptimizationCst = "topology.optimization";
        private string applicatonIdCst = "application.id";
        private string clientIdCst = "client.id";
        private string numStreamThreadsCst = "num.stream.threads";
        private string applicationServerCst = "application.server";
        private string cacheMaxBytesBufferingCst = "cache.max.bytes.buffering";
        private string rocksdbConfigSetterCst = "rocksdb.config.setter";
        private string stateCleanupDelayMsCst = "state.cleanup.delay.ms";
        private string pollMsCst = "poll.ms";
        private string processingGuaranteeCst = "processing.guarantee";

        public static readonly string AT_LEAST_ONCE = "at_least_once";
        public static readonly string EXACTLY_ONCE = "exactly_once";

        public string ProcessingGuaranteeConfig
        {
            get => this[processingGuaranteeCst];
            set
            {
                if (value.Equals(AT_LEAST_ONCE) || value.Equals(EXACTLY_ONCE))
                    Add(processingGuaranteeCst, value);
                else
                    throw new InvalidOperationException($"ProcessingGuaranteeConfig value must equal to {AT_LEAST_ONCE} or {EXACTLY_ONCE}");
            }
        }

        public long PollMsConfig
        {
            get => Convert.ToInt64(this[pollMsCst]);
            set => Add(pollMsCst, value.ToString());
        }

        public long StateCleanupDelayMs
        {
            get => Convert.ToInt64(this[stateCleanupDelayMsCst]);
            set => Add(stateCleanupDelayMsCst, value.ToString());
        }

        public long CacheMaxBytesBuffering
        {
            get => Convert.ToInt64(this[cacheMaxBytesBufferingCst]);
            set => Add(cacheMaxBytesBufferingCst, value.ToString());
        }
        
        public string ApplicationServer
        {
            get => this[applicationServerCst];
            set => Add(applicationServerCst, value);
        }

        public int NumStreamThreads
        {
            get => Convert.ToInt32(this[numStreamThreadsCst]);
            set => Add(numStreamThreadsCst, value.ToString());
        }

        public string ClientId
        {
            get => this[clientIdCst];
            set => Add(clientIdCst, value);
        }

        public string ApplicationId
        {
            get => this[applicatonIdCst];
            set => Add(applicatonIdCst, value);
        }

        public string Optimize
        {
            get => this[topologyOptimizationCst];
            set => Add(topologyOptimizationCst, value);
        }

        public StreamConfig()
        {
            NumStreamThreads = 1;
            Optimize = "";
        }

        public StreamConfig(IDictionary<string, string> properties)
            : this()
        {
            foreach (var k in properties)
                this.Add(k.Key, k.Value);
        }

        public ProducerConfig toProducerConfig()
        {
            return new ProducerConfig
            {
                BootstrapServers = this["bootstrap.servers"],
                SaslMechanism = SaslMechanism.ScramSha512,
                SaslUsername = this["sasl.username"],
                SaslPassword = this["sasl.password"],
                SecurityProtocol = SecurityProtocol.SaslPlaintext
            };
        }

        public ConsumerConfig toConsumerConfig()
        {
            return new ConsumerConfig
            {
                BootstrapServers = this["bootstrap.servers"],
                SaslMechanism = SaslMechanism.ScramSha512,
                SaslUsername = this["sasl.username"],
                SaslPassword = this["sasl.password"],
                SecurityProtocol = SecurityProtocol.SaslPlaintext,
                GroupId = this.ApplicationId,
                EnableAutoCommit = false
            };
        }

        public ConsumerConfig toConsumerConfig(string clientId)
        {
            var config = this.toConsumerConfig();
            config.ClientId = clientId;
            return config;
        }

        public ConsumerConfig toGlobalConsumerConfig(string clientId)
        {
            // TODO
            return new ConsumerConfig();
        }

        public AdminClientConfig toAdminConfig(string clientId)
        {
            // TODO
            return new AdminClientConfig();
        }
    }
}