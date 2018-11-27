namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Sagas;

    class UpdateAction : StorageAction
    {
        public UpdateAction(PersistentSagaInstance sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
        }

        public override Task Execute()
        {
            var sagaFile = GetSagaFile();

            return sagaFile.Write(sagaData);
        }
    }
}