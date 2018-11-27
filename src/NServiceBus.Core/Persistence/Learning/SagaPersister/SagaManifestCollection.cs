namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Sagas;

    class SagaManifestCollection
    {
        public SagaManifestCollection(SagaMetadataCollection sagas, string storageLocation)
        {
            foreach (var metadata in sagas)
            {
                var sagaType = metadata.SagaType.FullName;
                var sagaStorageDir = Path.Combine(storageLocation, sagaType.Replace("+", ""));

                if (!Directory.Exists(sagaStorageDir))
                {
                    Directory.CreateDirectory(sagaStorageDir);
                }

                var manifest = new SagaManifest
                {
                    StorageDirectory = sagaStorageDir
                };

                sagaManifests[sagaType] = manifest;
            }
        }

        public SagaManifest GetForSagaType(string type)
        {
            return sagaManifests[type];
        }
    

        Dictionary<string, SagaManifest> sagaManifests = new Dictionary<string, SagaManifest>();
    }
}