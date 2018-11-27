namespace NServiceBus.Sagas
{
    using System;
    using System.Threading.Tasks;

    class SagaPersister2Adapter : ISagaPersister2
    {
        public SagaPersister2Adapter(ISagaPersister persister)
        {
            this.persister = persister;
        }

        public Task<PersistentSagaInstance> PrepareNewInstance(string sagaType, string correlationPropertyValue, SagaPersisterContext context)
        {
            throw new NotImplementedException();
        }

        public Task Save(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context)
        {
            //SagaCorrelationProperty correlationProperty;

            //if(!context.SagaMetadata.TryGetCorrelationProperty())

            return persister.Save((IContainSagaData)persistentSagaInstance.Entity, null, context.Session, context.Extensions);
        }

        public Task Update(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context)
        {
            return persister.Update((IContainSagaData)persistentSagaInstance.Entity, context.Session, context.Extensions);
        }

        public Task<PersistentSagaInstance> Get(string sagaType, string sagaId, SagaPersisterContext context)
        {
            throw new NotImplementedException();
            //var entity = await persister.Get<TSagaData>(Guid.Parse(sagaId), session, context).ConfigureAwait(false);

            //return new PersistentSagaInstance
            //{
            //    Entity = entity
            //};
        }

        public Task<PersistentSagaInstance> GetByCorrelationProperty(string sagaType, string correlationPropertyValue, SagaPersisterContext context)
        {
            throw new NotImplementedException();

            //var entity = await persister.Get<TSagaData>(propertyName, propertyValue, session, context).ConfigureAwait(false);

            //return new PersistentSagaInstance
            //{
            //    Entity = entity
            //};
        }

        public Task Complete(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context)
        {
            return persister.Complete((IContainSagaData)persistentSagaInstance.Entity, context.Session, context.Extensions);
        }

        readonly ISagaPersister persister;
    }
}