namespace NServiceBus.Sagas
{
    class SagaInstance
    {
        public IContainSagaData Entity;

        public bool Found => Entity != null;
    }
}