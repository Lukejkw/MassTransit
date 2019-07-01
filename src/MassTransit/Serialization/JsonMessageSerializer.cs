namespace MassTransit.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Mime;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using JsonConverters;
    using Newtonsoft.Json;
    using Util;


    public class JsonMessageSerializer :
        IMessageSerializer
    {
        public const string ContentTypeHeaderValue = "application/vnd.masstransit+json";
        public static readonly ContentType JsonContentType = new ContentType(ContentTypeHeaderValue);

        static readonly Lazy<JsonSerializer> _deserializer;
        static readonly Lazy<Encoding> _encoding;
        static readonly Lazy<JsonSerializer> _serializer;

        public static readonly ByteArrayConverter ByteArrayConverter;
        public static readonly ListJsonConverter ListJsonConverter;
        public static readonly InterfaceProxyConverter InterfaceProxyConverter;
        public static readonly MessageDataJsonConverter MessageDataJsonConverter;
        public static readonly StringDecimalConverter StringDecimalConverter;

        public static JsonSerializerSettings DeserializerSettings;

        public static JsonSerializerSettings SerializerSettings;

        static JsonMessageSerializer()
        {
            _encoding = new Lazy<Encoding>(() => new UTF8Encoding(false, true), LazyThreadSafetyMode.PublicationOnly);

            ByteArrayConverter = new ByteArrayConverter();
            ListJsonConverter = new ListJsonConverter();
            InterfaceProxyConverter = new InterfaceProxyConverter();
            MessageDataJsonConverter = new MessageDataJsonConverter();
            StringDecimalConverter = new StringDecimalConverter();

            DeserializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ObjectCreationHandling = ObjectCreationHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new JsonContractResolver(),
                TypeNameHandling = TypeNameHandling.None,
                DateParseHandling = DateParseHandling.None,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                Converters = new List<JsonConverter>(new JsonConverter[]
                {
                    ByteArrayConverter, ListJsonConverter, InterfaceProxyConverter, StringDecimalConverter, MessageDataJsonConverter
                })
            };

            SerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ObjectCreationHandling = ObjectCreationHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new JsonContractResolver(),
                TypeNameHandling = TypeNameHandling.None,
                DateParseHandling = DateParseHandling.None,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                Converters = new List<JsonConverter>(new JsonConverter[] {ByteArrayConverter, MessageDataJsonConverter, StringDecimalConverter}),
            };

            _deserializer = new Lazy<JsonSerializer>(() => JsonSerializer.Create(DeserializerSettings));
            _serializer = new Lazy<JsonSerializer>(() => JsonSerializer.Create(SerializerSettings));
        }

        public static JsonSerializer Deserializer => _deserializer.Value;

        public static JsonSerializer Serializer => _serializer.Value;

        public void Serialize<T>(Stream stream, SendContext<T> context)
            where T : class
        {
            try
            {
                context.ContentType = JsonContentType;

                var envelope = new JsonMessageEnvelope(context, context.Message, TypeMetadataCache<T>.MessageTypeNames);

                using (var writer = new StreamWriter(stream, _encoding.Value, 1024, true))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.Formatting = Formatting.Indented;

                    _serializer.Value.Serialize(jsonWriter, envelope, typeof(MessageEnvelope));

                    jsonWriter.Flush();
                    writer.Flush();
                }
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException("Failed to serialize message", ex);
            }
        }

        public ContentType ContentType => JsonContentType;
    }
}
