namespace NServiceBus.Sagas
{
    using System.Collections.Generic;

    class PersistentSagaInstance
    {
        public PersistentSagaInstance(string id, string type)
        {
            Timeouts = new List<Timeout>();
            Id = id;
            Type = type;
        }

        public string Id { get; }

        public string Type { get; }

        public object Entity { get; set; }

        public IList<Timeout> Timeouts { get; set; }


        public class Timeout
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public bool Canceled { get; set; }
        }

        //metadata
    }
}