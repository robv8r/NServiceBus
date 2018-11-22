namespace NServiceBus.Sagas
{
    using System.Collections.Generic;

    class PersistentSagaInstance
    {
        public PersistentSagaInstance()
        {
            Timeouts = new List<Timeout>();
        }
        public IContainSagaData Entity;
        
        public bool Found => Entity != null;

        public IList<Timeout> Timeouts { get; set; }

        public class Timeout
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public bool Canceled { get; set; }
        }
    }
}