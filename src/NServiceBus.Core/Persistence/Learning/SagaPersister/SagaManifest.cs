namespace NServiceBus
{
    using System.IO;

    class SagaManifest
    {
        public string StorageDirectory { get; set; }
       
        public string GetFilePath(string sagaId)
        {
            return Path.Combine(StorageDirectory, sagaId + ".json");
        }
    }
}