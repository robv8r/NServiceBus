namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Sagas;

    class SaveAction : StorageAction
    {
        public SaveAction(PersistentSagaInstance sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
        }

        public override async Task Execute()
        {
            var sagaId = sagaData.Id;
            var sagaManifest = sagaManifests.GetForSagaType(sagaData.Type);

            var sagaFile = await SagaStorageFile.Create(sagaId, sagaManifest)
                .ConfigureAwait(false);

            sagaFiles.RegisterSagaFile(sagaFile, sagaId, sagaData.Type);

            await sagaFile.Write(sagaData)
                .ConfigureAwait(false);
        }
    }
}