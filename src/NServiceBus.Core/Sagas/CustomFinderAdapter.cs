namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Sagas;

    class CustomFinderAdapter<TSagaData, TMessage> : SagaFinder where TSagaData : IContainSagaData
    {
        public override async Task<PersistentSagaInstance> Find(IBuilder builder, SagaFinderDefinition finderDefinition, SagaPersisterContext context, object message, string sagaType)
        {
            var customFinderType = (Type)finderDefinition.Properties["custom-finder-clr-type"];

            var finder = (IFindSagas<TSagaData>.Using<TMessage>)builder.Build(customFinderType);

            var entity = await finder
                .FindBy((TMessage)message, context.Session, context.Extensions)
                .ThrowIfNull()
                .ConfigureAwait(false);

            return new PersistentSagaInstance(entity.Id.ToString(),sagaType)
            {
                Entity = entity
            };
        }
    }
}