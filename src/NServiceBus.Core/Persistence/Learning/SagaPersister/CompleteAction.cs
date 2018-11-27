namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Sagas;

    class CompleteAction : StorageAction
    {
        public CompleteAction(PersistentSagaInstance sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
        }

        public override Task Execute()
        {
            var sagaFile = GetSagaFile();

            return sagaFile.MarkAsCompleted();
        }
    }
}