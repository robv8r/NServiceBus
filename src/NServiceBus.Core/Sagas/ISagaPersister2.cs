namespace NServiceBus.Sagas
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    interface ISagaPersister2
    {
        Task Save(SagaInstance sagaInstance, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context);

        Task Update(SagaInstance sagaInstance, SynchronizedStorageSession session, ContextBag context);

        Task<SagaInstance> Get<TSagaData>(string sagaId, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData;

        Task<SagaInstance> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData;

        Task Complete(SagaInstance sagaInstance, SynchronizedStorageSession session, ContextBag context);
    }
}