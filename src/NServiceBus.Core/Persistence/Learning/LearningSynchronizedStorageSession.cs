namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Janitor;
    using Persistence;
    using Sagas;

    [SkipWeaving]
    class LearningSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public LearningSynchronizedStorageSession(SagaManifestCollection sagaManifests)
        {
            this.sagaManifests = sagaManifests;
        }

        public void Dispose()
        {
            foreach (var sagaFile in sagaFiles.Values)
            {
                sagaFile.Dispose();
            }

            sagaFiles.Clear();
        }

        public async Task CompleteAsync()
        {
            foreach (var action in deferredActions)
            {
                await action.Execute().ConfigureAwait(false);
            }
            deferredActions.Clear();
        }

        public async Task<PersistentSagaInstance> Read(string sagaId, string sagaType)
        {
            var sagaStorageFile = await Open(sagaId, sagaType)
                .ConfigureAwait(false);

            if (sagaStorageFile == null)
            {
                return null;
            }

            return await sagaStorageFile.Read()
                .ConfigureAwait(false);
        }

        public Task Update(PersistentSagaInstance sagaData)
        {
            deferredActions.Add(new UpdateAction(sagaData, sagaFiles, sagaManifests));
            return TaskEx.CompletedTask;
        }

        public Task Save(PersistentSagaInstance sagaData)
        {
            deferredActions.Add(new SaveAction(sagaData, sagaFiles, sagaManifests));
            return TaskEx.CompletedTask;
        }

        public Task Complete(PersistentSagaInstance sagaData)
        {
            deferredActions.Add(new CompleteAction(sagaData, sagaFiles, sagaManifests));
            return TaskEx.CompletedTask;
        }

        async Task<SagaStorageFile> Open(string sagaId, string sagaType)
        {
            var sagaManifest = sagaManifests.GetForSagaType(sagaType);

            var sagaStorageFile = await SagaStorageFile.Open(sagaId, sagaManifest)
                .ConfigureAwait(false);

            if (sagaStorageFile != null)
            {
                sagaFiles.RegisterSagaFile(sagaStorageFile, sagaId, sagaType);
            }

            return sagaStorageFile;
        }

        SagaManifestCollection sagaManifests;

        Dictionary<string, SagaStorageFile> sagaFiles = new Dictionary<string, SagaStorageFile>();

        List<StorageAction> deferredActions = new List<StorageAction>();
    }
}