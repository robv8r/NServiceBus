namespace NServiceBus.Sagas
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class SagaPersister2Adapter : ISagaPersister2
    {
        public SagaPersister2Adapter(ISagaPersister persister)
        {
            this.persister = persister;
        }

        public Task Save(SagaInstance sagaInstance, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            return persister.Save(sagaInstance.Entity, correlationProperty, session, context);
        }

        public Task Update(SagaInstance sagaInstance, SynchronizedStorageSession session, ContextBag context)
        {
            return persister.Update(sagaInstance.Entity, session, context);
        }

        public async Task<SagaInstance> Get<TSagaData>(string sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
        {
            var entity = await persister.Get<TSagaData>(Guid.Parse(sagaId), session, context).ConfigureAwait(false);

            return new SagaInstance
            {
                Entity = entity
            };
        }

        public async Task<SagaInstance> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
        {
            var entity = await persister.Get<TSagaData>(propertyName, propertyValue, session, context).ConfigureAwait(false);

            return new SagaInstance
            {
                Entity = entity
            };
        }

        public Task Complete(SagaInstance sagaInstance, SynchronizedStorageSession session, ContextBag context)
        {
            return persister.Complete(sagaInstance.Entity, session, context);
        }

        readonly ISagaPersister persister;
    }
}