namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Sagas;

    abstract class SagaFinder
    {
        public abstract Task<PersistentSagaInstance> Find(IBuilder builder, SagaFinderDefinition finderDefinition, SagaPersisterContext context, object message, string sagaType);
    }
}