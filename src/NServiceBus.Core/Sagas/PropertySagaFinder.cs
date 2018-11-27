namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Sagas;

    class PropertySagaFinder<TSagaData> : SagaFinder where TSagaData : class, IContainSagaData
    {
        public PropertySagaFinder(ISagaPersister2 sagaPersister)
        {
            this.sagaPersister = sagaPersister;
        }

        public override async Task<PersistentSagaInstance> Find(IBuilder builder, SagaFinderDefinition finderDefinition, SagaPersisterContext context, object message, string sagaType)
        {
            var propertyAccessor = (Func<object, object>)finderDefinition.Properties["property-accessor"];
            var propertyValue = propertyAccessor(message);

            var sagaPropertyName = (string)finderDefinition.Properties["saga-property-name"];

            var lookupValues = context.Extensions.GetOrCreate<SagaLookupValues>();
            lookupValues.Add<TSagaData>(sagaPropertyName, propertyValue);

            if (sagaPropertyName.ToLower() == "id")
            {
                return await sagaPersister.Get(sagaType, ((Guid)propertyValue).ToString(), context).ConfigureAwait(false);
            }

            return await sagaPersister.GetByCorrelationProperty(sagaType, propertyValue.ToString(), context).ConfigureAwait(false);
        }

        ISagaPersister2 sagaPersister;
    }


    class SagaLookupValues
    {
        public void Add<TSagaData>(string propertyName, object propertyValue)
        {
            entries[typeof(TSagaData)] = new LookupValue
            {
                PropertyName = propertyName,
                PropertyValue = propertyValue
            };
        }

        public bool TryGet(Type sagaType, out LookupValue value)
        {
            return entries.TryGetValue(sagaType, out value);
        }

        Dictionary<Type, LookupValue> entries = new Dictionary<Type, LookupValue>();

        public class LookupValue
        {
            public string PropertyName { get; set; }
            public object PropertyValue { get; set; }
        }
    }
}