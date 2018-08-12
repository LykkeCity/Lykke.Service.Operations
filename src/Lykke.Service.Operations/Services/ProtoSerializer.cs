using System.IO;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Service.Operations.Services
{
    public class ProtoSerializer<TMessage> : IMessageDeserializer<TMessage>
    {
        public TMessage Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return ProtoBuf.Serializer.Deserialize<TMessage>(stream);
            }
        }
    }
}
