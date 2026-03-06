using System.Text;
using MQTTnet;
using MQTTnet.Client;

namespace Pendule
{
    internal class MqttLogger : IAsyncDisposable
    {
        private readonly IMqttClient _client;
        private readonly MqttClientOptions _options;
        private readonly string _topic;

        public event Action<string, string>? MessageReceived; // (topic, payload)

        public MqttLogger(string broker, int port, string topic, string? username = null, string? password = null)
        {
            _topic = topic;

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithClientId(Guid.NewGuid().ToString())
                .WithCleanSession();

            if (username != null && password != null)
                builder.WithCredentials(username, password);

            _options = builder.Build();

            _client.ApplicationMessageReceivedAsync += OnMessageReceived;
        }

        public async Task ConnectAsync()
        {
            var result = await _client.ConnectAsync(_options);

            if (result.ResultCode != MqttClientConnectResultCode.Success)
                throw new Exception($"Échec de connexion MQTT : {result.ResultCode}");

            Console.WriteLine($"[MQTT] Connecté");
            await _client.SubscribeAsync(_topic);
            Console.WriteLine($"[MQTT] Abonné au topic : {_topic}");
        }

        public bool IsConnected()
        {
            return _client.IsConnected;
        }

        public async Task PublishAsync(string payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(message);
            Console.WriteLine($"[MQTT] Envoyé : {payload}");
        }

        public void Publish(string payload, string? subtopic = null)
        {
            string topic = subtopic != null ? $"{_topic}/{subtopic}" : _topic;

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            _client.PublishAsync(message).GetAwaiter().GetResult();
        }

        public async Task DisconnectAsync()
        {
            await _client.UnsubscribeAsync(_topic);
            await _client.DisconnectAsync();
            Console.WriteLine("[MQTT] Déconnecté");
        }

        public async ValueTask DisposeAsync()
        {
            if (_client.IsConnected)
                await DisconnectAsync();

            _client.Dispose();
        }

        private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            MessageReceived?.Invoke(e.ApplicationMessage.Topic, payload);
            return Task.CompletedTask;
        }
    };
}
        

