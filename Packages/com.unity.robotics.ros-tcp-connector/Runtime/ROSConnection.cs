using System;
using UnityEngine;

namespace Unity.Robotics.ROSTCPConnector
{
    public class ROSConnection
    {
        private static ROSConnection _instance;
        
        public bool HasConnectionThread { get; private set; }
        public string RosIPAddress { get; set; }
        public int RosPort { get; set; }

        public static ROSConnection GetOrCreateInstance()
        {
            if (_instance == null)
            {
                _instance = new ROSConnection();
            }
            return _instance;
        }

        public void Connect()
        {
            HasConnectionThread = true;
            Debug.Log($"[ROSConnection] Connecting to {RosIPAddress}:{RosPort}");
        }

        public void Subscribe<T>(string topic, Action<T> callback) where T : MessageGeneration.Message
        {
            Debug.Log($"[ROSConnection] Subscribed to topic: {topic}");
        }

        public void RegisterPublisher<T>(string topic) where T : MessageGeneration.Message
        {
            Debug.Log($"[ROSConnection] Registered publisher for topic: {topic}");
        }

        public void Publish<T>(string topic, T message) where T : MessageGeneration.Message
        {
            Debug.Log($"[ROSConnection] Published to topic: {topic}");
        }
    }
}