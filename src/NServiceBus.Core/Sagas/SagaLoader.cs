namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    interface SagaLoader
    {
        Task<PersistentSagaInstance> Load(ISagaPersister2 persister, string sagaId, SynchronizedStorageSession storageSession, ContextBag context);
    }
}