namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Janitor;
    using Sagas;
    using SimpleJson;

    [SkipWeaving]
    class SagaStorageFile : IDisposable
    {
        SagaStorageFile(FileStream fileStream)
        {
            this.fileStream = fileStream;
            streamWriter = new StreamWriter(fileStream, Encoding.Unicode);
            streamReader = new StreamReader(fileStream, Encoding.Unicode);
        }

        public void Dispose()
        {
            streamWriter.Close();
            streamReader.Close();

            if (isCompleted)
            {
                File.Delete(fileStream.Name);
            }

            fileStream = null;
        }

        public static Task<SagaStorageFile> Open(string sagaId, SagaManifest manifest)
        {
            var filePath = manifest.GetFilePath(sagaId);

            if (!File.Exists(filePath))
            {
                return noSagaFoundResult;
            }

            return OpenWithDelayOnConcurrency(filePath, FileMode.Open);
        }

        public static Task<SagaStorageFile> Create(string sagaId, SagaManifest manifest)
        {
            var filePath = manifest.GetFilePath(sagaId);

            return OpenWithDelayOnConcurrency(filePath, FileMode.CreateNew);
        }

        static async Task<SagaStorageFile> OpenWithDelayOnConcurrency(string filePath, FileMode fileAccess)
        {
            try
            {
                return new SagaStorageFile(new FileStream(filePath, fileAccess, FileAccess.ReadWrite, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous));
            }
            catch (IOException)
            {
                // give the other task some time to complete the saga to avoid retrying to much
                await Task.Delay(100)
                    .ConfigureAwait(false);

                throw;
            }
        }

        public Task Write(PersistentSagaInstance sagaData)
        {
            fileStream.Position = 0;

            var serializedEntity = SimpleJson.SerializeObject(sagaData.Entity, serializationStrategy);

            var json = SimpleJson.SerializeObject(new StoredInstance
            {
                Id = sagaData.Id,
                Type = sagaData.Type,
                EntityType = sagaData.Entity.GetType().AssemblyQualifiedName,
                EntityAsJson = serializedEntity,
                Timeouts = sagaData.Timeouts
            }, serializationStrategy);

            return streamWriter.WriteAsync(json);
        }

        public Task MarkAsCompleted()
        {
            isCompleted = true;
            return TaskEx.CompletedTask;
        }

        public async Task<PersistentSagaInstance> Read()
        {
            var json = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            var storedInstance = SimpleJson.DeserializeObject<StoredInstance>(json, serializationStrategy);

            return new PersistentSagaInstance(storedInstance.Id, storedInstance.Type)
            {
                Entity = SimpleJson.DeserializeObject(storedInstance.EntityAsJson, Type.GetType(storedInstance.EntityType), serializationStrategy),
                Timeouts = storedInstance.Timeouts
            };
        }

        FileStream fileStream;
        bool isCompleted;
        StreamWriter streamWriter;
        StreamReader streamReader;

        const int DefaultBufferSize = 4096;
        static Task<SagaStorageFile> noSagaFoundResult = Task.FromResult<SagaStorageFile>(null);
        static readonly EnumAwareStrategy serializationStrategy = new EnumAwareStrategy();


        public class StoredInstance
        {
            public string Id;

            public string Type;

            public string EntityType;

            public string EntityAsJson;

            public IList<PersistentSagaInstance.Timeout> Timeouts;
        }
    }
}