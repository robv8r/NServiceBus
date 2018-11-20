namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using ObjectBuilder;
    using Persistence;
    using Sagas;

    class CustomFinderAdapter<TSagaData, TMessage> : SagaFinder where TSagaData : IContainSagaData
    {
        public override async Task<SagaInstance> Find(IBuilder builder, SagaFinderDefinition finderDefinition, SynchronizedStorageSession storageSession, ContextBag context, object message)
        {
            var customFinderType = (Type)finderDefinition.Properties["custom-finder-clr-type"];

            var finder = (IFindSagas<TSagaData>.Using<TMessage>)builder.Build(customFinderType);

            var entity = await finder
                .FindBy((TMessage)message, storageSession, context)
                .ThrowIfNull()
                .ConfigureAwait(false);

            return new SagaInstance
            {
                Entity = entity
            };
        }
    }
}