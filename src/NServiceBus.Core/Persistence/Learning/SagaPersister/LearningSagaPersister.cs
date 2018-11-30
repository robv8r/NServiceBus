namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Sagas;

    class LearningSagaPersister : ISagaPersister2
    {
        public Task<PersistentSagaInstance> PrepareNewInstance(string sagaType, object correlationPropertyValue, SagaPersisterContext context)
        {
            if (correlationPropertyValue == null)
            {
                throw new InvalidOperationException("The persister requires a correlation property to be defined for messages starting sagas");
            }

            return Task.FromResult(new PersistentSagaInstance(GenerateSagaId(sagaType, correlationPropertyValue), sagaType));
        }

        public Task Save(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context)
        {
            var storageSession = (LearningSynchronizedStorageSession)context.Session;
            return storageSession.Save(persistentSagaInstance);
        }

        public Task Update(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context)
        {
            var storageSession = (LearningSynchronizedStorageSession)context.Session;
            return storageSession.Update(persistentSagaInstance);
        }

        public Task<PersistentSagaInstance> Get(string sagaType, string sagaId, SagaPersisterContext context)
        {
            var storageSession = (LearningSynchronizedStorageSession)context.Session;
            return storageSession.Read(sagaId, sagaType);
        }

        public Task<PersistentSagaInstance> GetByCorrelationProperty(string sagaType, object correlationPropertyValue, SagaPersisterContext context)
        {
            return Get(sagaType, GenerateSagaId(sagaType, correlationPropertyValue), context);
        }

        public Task Complete(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context)
        {
            var storageSession = (LearningSynchronizedStorageSession)context.Session;
            return storageSession.Complete(persistentSagaInstance);
        }

        string GenerateSagaId(string sagaType, object propertyValue)
        {
            return DeterministicGuid.Create($"{sagaType}_{propertyValue}").ToString();
        }
    }
}