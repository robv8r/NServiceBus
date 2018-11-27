namespace NServiceBus
{
    using System.Collections.Generic;

    static class SagaStorageFileExtensions
    {
        public static void RegisterSagaFile(this Dictionary<string, SagaStorageFile> sagaFiles, SagaStorageFile sagaStorageFile, string sagaId, string sagaType)
        {
            sagaFiles[$"{sagaType}{sagaId}"] = sagaStorageFile;
        }
    }
}