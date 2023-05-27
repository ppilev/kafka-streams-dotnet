﻿using Confluent.Kafka;
using Streamiz.Kafka.Net.Crosscutting;
using Streamiz.Kafka.Net.Errors;
using Streamiz.Kafka.Net.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Streamiz.Kafka.Net.Kafka.Internal;

namespace Streamiz.Kafka.Net.Mock.Pipes
{
    internal class KafkaPipeOutput : IPipeOutput
    {
        private readonly string topicName;
        private readonly TimeSpan timeout;
        private readonly ILogger logger = Logger.GetLogger(typeof(KafkaPipeOutput));

        private readonly CancellationToken token;
        private readonly Thread readThread;
        private readonly IConsumer<byte[], byte[]> consumer;
        private readonly Queue<ConsumeResult<byte[], byte[]>> queue = new();
        private readonly object _lock = new object();


        public string TopicName => topicName;

        public int Size
        {
            get
            {
                lock (_lock)
                    return queue.Count;
            }
        }

        public bool IsEmpty
        {
            get
            {
                var infos = GetInfos();
                foreach (var i in infos)
                    if (i.Offset < i.High)
                        return false;
                return true;
            }
        }

        public KafkaPipeOutput(string topicName, TimeSpan timeout, IStreamConfig configuration, IKafkaSupplier kafkaSupplier, CancellationToken token)
        {
            this.topicName = topicName;
            this.timeout = timeout;

            consumer = kafkaSupplier.GetConsumer(configuration.ToConsumerConfig($"pipe-output-{configuration.ApplicationId}-{topicName}"), null);

            this.token = token;
            readThread = new Thread(ReadThread);
            readThread.Start();
        }

        private void ReadThread()
        {
            consumer.Subscribe(topicName);
            while (!token.IsCancellationRequested)
            {
                var record = consumer.Consume(timeout);
                if (record != null)
                {
                    lock (_lock)
                        queue.Enqueue(record);
                    consumer.Commit(record);
                }
            }
        }

        public void Dispose()
        {
            if (queue.Count > 0)
                logger.LogWarning("Dispose pipe queue for topic {TopicName} whereas it's not empty (Size : {QueueCount})",
                    topicName, queue.Count);
            
            readThread.Join();
            consumer.Unsubscribe();
            consumer.Dispose();
        }

        public ConsumeResult<byte[], byte[]> Read()
        {
            int count = 0;
            while (count <= 10)
            {
                int size = 0;
                lock (_lock)
                    size = queue.Count;

                if (size > 0)
                {
                    return queue.Dequeue();
                }
                else
                {
                    Thread.Sleep((int)timeout.TotalMilliseconds / 10);
                    ++count;
                }
            }

            throw new StreamsException($"No record found in topic {topicName} after {timeout.TotalSeconds}s !");
        }

        public List<PipeOutputInfo> GetInfos()
        {
            List<PipeOutputInfo> l = new List<PipeOutputInfo>();
            var watermark = consumer.GetWatermarkOffsets();
            var offsets = consumer.Committed(TimeSpan.FromSeconds(1));
            foreach (var o in offsets)
            {
                var w = watermark.FirstOrDefault(f => f.Topic.Equals(o.Topic) && f.Partition.Equals(o.Partition));
                l.Add(new PipeOutputInfo
                {
                    Offset = o.Offset.Value,
                    Topic = o.Topic,
                    Partition = o.Partition,
                    High = w != null ? w.High.Value : o.Offset.Value,
                    Low = w != null ? w.Low.Value : o.Offset.Value
                });
            }
            return l;
        }

        public IEnumerable<ConsumeResult<byte[], byte[]>> ReadList()
        {
            List<ConsumeResult<byte[], byte[]>> records = new List<ConsumeResult<byte[], byte[]>>();
            int count = 0;
            while (count <= 10)
            {
                int size = 0;
                lock (_lock)
                    size = queue.Count;

                if (size > 0)
                {
                    for (int i = 0; i < size; ++i)
                    {
                        var r = queue.Dequeue();
                        records.Add(r);
                    }
                    return records;
                }
                else
                {
                    Thread.Sleep((int)timeout.TotalMilliseconds / 10);
                    ++count;
                }
            }
            return records;
        }
    }
}