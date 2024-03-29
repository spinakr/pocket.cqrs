using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace PocketCqrs.EventStore
{
    public class EventStore : IEventStore
    {
        public EventStore(IAppendOnlyStore appendOnlyStore, IMessaging messaging)
        {
            _appendOnlyStore = appendOnlyStore;
            _messaging = messaging;
        }

        private IAppendOnlyStore _appendOnlyStore;
        private IMessaging _messaging;

        public void AppendToStream(string streamName, ICollection<IEvent> events, int originalVersion)
        {
            var data = JsonConvert.SerializeObject(events, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            _appendOnlyStore.Append(streamName, data, originalVersion);
            foreach (var @event in events)
            {
                _messaging.Publish(@event);
            }
        }

        public EventStream LoadEventStream(string streamName)
        {
            var records = _appendOnlyStore.ReadRecords(streamName);
            var stream = new EventStream();
            foreach (var record in records)
            {
                stream.Events.AddRange(JsonConvert.DeserializeObject<ICollection<IEvent>>(record.JsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }));
                stream.Version = record.Version;
            }
            return stream;
        }

        public IList<IEvent> LoadAllEvents()
        {
            var records = _appendOnlyStore.ReadAllRecords();
            return records.SelectMany(r => JsonConvert.DeserializeObject<ICollection<IEvent>>(r.JsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto })).ToList();
        }
    }
}