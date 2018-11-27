namespace NServiceBus
{
    using System.Threading.Tasks;
    using Sagas;

    interface SagaLoader
    {
        Task<PersistentSagaInstance> Load(ISagaPersister2 persister, string sagaType, string sagaId, SagaPersisterContext context);
    }
}