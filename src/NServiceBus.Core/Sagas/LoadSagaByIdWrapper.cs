namespace NServiceBus
{
    using System.Threading.Tasks;
    using Sagas;

    //this class in only here until we can move to a better saga persister api
    class LoadSagaByIdWrapper<T> : SagaLoader
        where T : class, IContainSagaData
    {
        public async Task<PersistentSagaInstance> Load(ISagaPersister2 persister, string sagaType, string sagaId, SagaPersisterContext context)
        {
            return await persister.Get(sagaType, sagaId, context).ConfigureAwait(false);
        }
    }
}