namespace NServiceBus.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class SagaPersister2Adapter : ISagaPersister2
    {
        public SagaPersister2Adapter(ISagaPersister persister, ISagaIdGenerator sagaIdGenerator)
        {
            this.persister = persister;
            this.sagaIdGenerator = sagaIdGenerator;
        }

        public Task<PersistentSagaInstance> PrepareNewInstance(string sagaType, object correlationPropertyValue, SagaPersisterContext context)
        {
            var sagaCorrelationProperty = SagaCorrelationProperty.None;

            if (context.SagaMetadata.TryGetCorrelationProperty(out var correlationPropertyMetadata))
            {
                sagaCorrelationProperty = new SagaCorrelationProperty(correlationPropertyMetadata.Name, correlationPropertyValue);
            }

            var sagaIdGeneratorContext = new SagaIdGeneratorContext(sagaCorrelationProperty, context.SagaMetadata, context.Extensions);

            var sagaId = sagaIdGenerator.Generate(sagaIdGeneratorContext);

            return Task.FromResult(new PersistentSagaInstance(sagaId.ToString(), sagaType));
        }

        public Task Save(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context)
        {
            var correlationProperty = SagaCorrelationProperty.None;

            if (context.SagaMetadata.TryGetCorrelationProperty(out var correlationPropertyMetadata))
            {
                var propertyInfo = context.SagaMetadata.SagaEntityType.GetProperty(correlationPropertyMetadata.Name);

                correlationProperty = new SagaCorrelationProperty(correlationPropertyMetadata.Name, propertyInfo.GetValue(persistentSagaInstance.Entity));
            }

            return persister.Save((IContainSagaData)persistentSagaInstance.Entity, correlationProperty, context.Session, context.Extensions);
        }

        public Task Update(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context)
        {
            return persister.Update((IContainSagaData)persistentSagaInstance.Entity, context.Session, context.Extensions);
        }

        public async Task<PersistentSagaInstance> Get(string sagaType, string sagaId, SagaPersisterContext context)
        {
            var method = typeof(ISagaPersister).GetMethod("Get", new[] { typeof(Guid), typeof(SynchronizedStorageSession), typeof(ContextBag) });
            var generic = method.MakeGenericMethod(context.SagaMetadata.SagaEntityType);

            var entity = await ((Task<IContainSagaData>)generic.Invoke(persister, new object[] { Guid.Parse(sagaId), context.Session, context.Extensions })).ConfigureAwait(false);

            return new PersistentSagaInstance(sagaId, sagaType)
            {
                Entity = entity
            };
        }

        public async Task<PersistentSagaInstance> GetByCorrelationProperty(string sagaType, object correlationPropertyValue, SagaPersisterContext context)
        {
            string sagaCorrelationPropertyName = null;

            if (context.SagaMetadata.TryGetCorrelationProperty(out var correlationPropertyMetadata))
            {
                sagaCorrelationPropertyName = correlationPropertyMetadata.Name;
            }

            var method = typeof(ISagaPersister).GetMethod("Get", new[] { typeof(string), typeof(object), typeof(SynchronizedStorageSession), typeof(ContextBag) });
            var generic = method.MakeGenericMethod(context.SagaMetadata.SagaEntityType);

            var entity = await ((Task<object>)generic.Invoke(persister, new[] { sagaCorrelationPropertyName, correlationPropertyValue, context.Session, context.Extensions })).ConfigureAwait(false);

            return new PersistentSagaInstance(((IContainSagaData)entity).Id.ToString(), sagaType)
            {
                Entity = entity
            };
        }

        public Task Complete(PersistentSagaInstance persistentSagaInstance, SagaPersisterContext context)
        {
            return persister.Complete((IContainSagaData)persistentSagaInstance.Entity, context.Session, context.Extensions);
        }

        readonly ISagaPersister persister;
        readonly ISagaIdGenerator sagaIdGenerator;
    }
}