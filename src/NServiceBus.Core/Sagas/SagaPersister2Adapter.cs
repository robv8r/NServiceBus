namespace NServiceBus.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class SagaPersister2Adapter : ISagaPersister2
    {
        public SagaPersister2Adapter(ISagaPersister persister)
        {
            this.persister = persister;
        }

        public Task Save(PersistentSagaInstance persistentSagaInstance, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            return persister.Save((IContainSagaData)persistentSagaInstance.Entity, correlationProperty, session, context);
        }

        public Task Update(PersistentSagaInstance persistentSagaInstance, SynchronizedStorageSession session, ContextBag context)
        {
            return persister.Update((IContainSagaData)persistentSagaInstance.Entity, session, context);
        }

        public async Task<PersistentSagaInstance> Get<TSagaData>(string sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
        {
            var entity = await persister.Get<TSagaData>(Guid.Parse(sagaId), session, context).ConfigureAwait(false);

            return new PersistentSagaInstance
            {
                Entity = entity
            };
        }

        public async Task<PersistentSagaInstance> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
        {
            var entity = await persister.Get<TSagaData>(propertyName, propertyValue, session, context).ConfigureAwait(false);

            return new PersistentSagaInstance
            {
                Entity = entity
            };
        }

        public Task Complete(PersistentSagaInstance persistentSagaInstance, SynchronizedStorageSession session, ContextBag context)
        {
            return persister.Complete((IContainSagaData)persistentSagaInstance.Entity, session, context);
        }

        readonly ISagaPersister persister;
    }
}