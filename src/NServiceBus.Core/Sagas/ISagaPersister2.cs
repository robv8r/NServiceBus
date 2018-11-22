namespace NServiceBus.Sagas
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    interface ISagaPersister2
    {
        Task Save(PersistentSagaInstance persistentSagaInstance, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context);

        Task Update(PersistentSagaInstance persistentSagaInstance, SynchronizedStorageSession session, ContextBag context);

        Task<PersistentSagaInstance> Get<TSagaData>(string sagaId, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData;

        Task<PersistentSagaInstance> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData;

        Task Complete(PersistentSagaInstance persistentSagaInstance, SynchronizedStorageSession session, ContextBag context);
    }
}