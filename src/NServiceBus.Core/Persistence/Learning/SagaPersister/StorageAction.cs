namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Sagas;

    abstract class StorageAction
    {
        protected StorageAction(PersistentSagaInstance sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests)
        {
            this.sagaFiles = sagaFiles;
            this.sagaData = sagaData;
            this.sagaManifests = sagaManifests;
            sagaFileKey = $"{sagaData.Type}{sagaData.Id}";
        }

        public abstract Task Execute();

        protected SagaStorageFile GetSagaFile()
        {
            if (!sagaFiles.TryGetValue(sagaFileKey, out var sagaFile))
            {
                throw new Exception("The saga should be retrieved with the Get method before being updated or completed.");
            }
            return sagaFile;
        }

        protected PersistentSagaInstance sagaData;
        protected Dictionary<string, SagaStorageFile> sagaFiles;
        protected SagaManifestCollection sagaManifests;

        string sagaFileKey;
    }
}