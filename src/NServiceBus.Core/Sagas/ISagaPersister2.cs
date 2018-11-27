namespace NServiceBus.Sagas
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    interface ISagaPersister2
    {
        Task<PersistentSagaInstance> PrepareNewInstance(string sagaType, string correlationPropertyValue, SagaPersisterContext context);

        Task Save(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context);

        Task Update(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context);

        Task<PersistentSagaInstance> Get(string sagaType, string sagaId, SagaPersisterContext context);

        Task<PersistentSagaInstance> GetByCorrelationProperty(string sagaType, string correlationPropertyValue, SagaPersisterContext context);

        Task Complete(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context);
    }

    class SagaPersisterContext : IExtendable
    {
        public SagaPersisterContext(SagaMetadata sagaMetadata, SynchronizedStorageSession session, ContextBag extensions)
        {
            SagaMetadata = sagaMetadata;
            Session = session;
            Extensions = extensions;
        }

        public SagaMetadata SagaMetadata { get; }
        public SynchronizedStorageSession Session { get; }
        public ContextBag Extensions { get; }
    }
}