namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    //this class in only here until we can move to a better saga persister api
    class LoadSagaByIdWrapper<T> : SagaLoader 
        where T : class, IContainSagaData
    {
        public async Task<PersistentSagaInstance> Load(ISagaPersister2 persister, string sagaId, SynchronizedStorageSession storageSession, ContextBag context)
        {
            return await persister.Get<T>(sagaId, storageSession, context).ConfigureAwait(false);
        }
    }
}