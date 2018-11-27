namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Sagas;
    using Transport;

    class SagaPersistenceBehavior : IBehavior<IInvokeHandlerContext, IInvokeHandlerContext>
    {
        public SagaPersistenceBehavior(ISagaPersister2 persister, ISagaIdGenerator sagaIdGenerator, ICancelDeferredMessages timeoutCancellation, SagaMetadataCollection sagaMetadataCollection, bool persisterSupportsTimeoutStorage)
        {
            this.sagaIdGenerator = sagaIdGenerator;
            sagaPersister = persister;
            this.timeoutCancellation = timeoutCancellation;
            this.sagaMetadataCollection = sagaMetadataCollection;
            this.persisterSupportsTimeoutStorage = persisterSupportsTimeoutStorage;
        }

        public async Task Invoke(IInvokeHandlerContext context, Func<IInvokeHandlerContext, Task> next)
        {
            var isTimeoutMessage = IsTimeoutMessage(context.Headers);
            var isTimeoutHandler = context.MessageHandler.IsTimeoutHandler;

            if (isTimeoutHandler && !isTimeoutMessage)
            {
                return;
            }

            if (!isTimeoutHandler && isTimeoutMessage)
            {
                return;
            }

            RemoveSagaHeadersIfProcessingAEvent(context);

            if (!(context.MessageHandler.Instance is Saga saga))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var currentSagaMetadata = sagaMetadataCollection.Find(context.MessageHandler.Instance.GetType());
            if (context.Headers.TryGetValue(Headers.SagaType, out var targetSagaTypeString) && context.Headers.TryGetValue(Headers.SagaId, out var targetSagaId))
            {
                var targetSagaType = Type.GetType(targetSagaTypeString, false);

                if (targetSagaType == null)
                {
                    logger.Warn($"Saga headers indicated that the message was intended for {targetSagaTypeString} but that type isn't available. Will fallback to query persister for a saga instance of type {currentSagaMetadata.SagaType.FullName} and saga id {targetSagaId} instead");
                }
                else
                {
                    if (!sagaMetadataCollection.TryFind(targetSagaType, out var targetSagaMetaData))
                    {
                        logger.Warn($"Saga headers indicated that the message was intended for {targetSagaType.FullName} but no metadata was found for that saga type. Will fallback to query persister for a saga instance of type {currentSagaMetadata.SagaType.FullName} and saga id {targetSagaId} instead");
                    }
                    else
                    {
                        if (targetSagaMetaData.SagaType != currentSagaMetadata.SagaType)
                        {
                            //Message was intended for a different saga so no need to continue with this invocation
                            return;
                        }
                    }
                }
            }

            var persisterContext = new SagaPersisterContext(currentSagaMetadata, context.SynchronizedStorageSession, context.Extensions);

            var sagaInstanceState = new ActiveSagaInstance(saga, currentSagaMetadata, () => DateTime.UtcNow);

            //so that other behaviors can access the saga
            context.Extensions.Set(sagaInstanceState);

            var persistentInstance = await TryLoadSagaInstance(persisterContext, context).ConfigureAwait(false);

            if (persistentInstance == null)
            {
                if (IsMessageAllowedToStartTheSaga(context, currentSagaMetadata))
                {
                    context.Extensions.Get<SagaInvocationResult>().SagaFound();

                    var lookupValues = context.Extensions.GetOrCreate<SagaLookupValues>();

                    
                    var sagaEntityType = currentSagaMetadata.SagaEntityType;
                    object correlationPropertyValue = null;
                    PropertyInfo propertyInfo = null;

                    if (lookupValues.TryGet(sagaEntityType, out var value))
                    {
                        propertyInfo = sagaEntityType.GetProperty(value.PropertyName);

                        correlationPropertyValue = TypeDescriptor.GetConverter(propertyInfo.PropertyType)
                            .ConvertFromInvariantString(value.PropertyValue.ToString());
                    }
                   

                    persistentInstance = await sagaPersister.PrepareNewInstance(currentSagaMetadata.SagaType.FullName, correlationPropertyValue?.ToString(), persisterContext).ConfigureAwait(false);

                    persistentInstance.Entity = CreateNewSagaEntity(persistentInstance.Id, currentSagaMetadata, context);

                    if (propertyInfo != null)
                    {
                        propertyInfo.SetValue(persistentInstance.Entity, correlationPropertyValue);
                    }

                    sagaInstanceState.AttachNewPersistentInstance(persistentInstance);
                }
                else
                {
                    if (!context.Headers.ContainsKey(Headers.SagaId))
                    {
                        var finderDefinition = GetSagaFinder(currentSagaMetadata, context);
                        if (finderDefinition == null)
                        {
                            throw new Exception($"Message type {context.MessageBeingHandled.GetType().Name} is handled by saga {currentSagaMetadata.SagaType.Name}, but the saga does not contain a property mapping or custom saga finder to map the message to saga data. Consider adding a mapping in the saga's {nameof(Saga.ConfigureHowToFindSaga)} method.");
                        }
                    }

                    sagaInstanceState.MarkAsNotFound();

                    //we don't invoke not found handlers for timeouts
                    if (isTimeoutMessage)
                    {
                        context.Extensions.Get<SagaInvocationResult>().SagaFound();
                        logger.InfoFormat("No saga found for timeout message {0}, ignoring since the saga has been marked as complete before the timeout fired", context.MessageId);
                    }
                    else
                    {
                        context.Extensions.Get<SagaInvocationResult>().SagaNotFound();
                    }
                }
            }
            else
            {
                context.Extensions.Get<SagaInvocationResult>().SagaFound();
                sagaInstanceState.AttachExistingPersistentInstance(persistentInstance);
            }


            var skipHandlerInvocation = false;

            if (persisterSupportsTimeoutStorage && context.Headers.TryGetValue(TimeoutIdHeaderKey, out var timeoutId))
            {
                var timeout = persistentInstance.Timeouts.SingleOrDefault(t => t.Id == timeoutId);

                if (timeout == null || timeout.Canceled)
                {
                    skipHandlerInvocation = true;
                }
            }

            if (!skipHandlerInvocation)
            {
                await next(context).ConfigureAwait(false);
            }

            if (sagaInstanceState.NotFound)
            {
                return;
            }

            if (saga.Completed)
            {
                if (!sagaInstanceState.IsNew)
                {
                    await sagaPersister.Complete(persistentInstance, persisterContext).ConfigureAwait(false);
                }

                if (saga.Entity.Id != Guid.Empty)
                {
                    await timeoutCancellation.CancelDeferredMessages(saga.Entity.Id.ToString(), context).ConfigureAwait(false);
                }

                logger.DebugFormat("Saga: '{0}' with Id: '{1}' has completed.", sagaInstanceState.Metadata.Name, saga.Entity.Id);

                sagaInstanceState.Completed();
            }
            else
            {
                sagaInstanceState.ValidateChanges();

                foreach (var timeoutRequest in saga.RequestedTimeouts)
                {
                    var options = new SendOptions();

                    if (timeoutRequest.At.HasValue)
                    {
                        options.DoNotDeliverBefore(timeoutRequest.At.Value);
                    }

                    if (timeoutRequest.Within.HasValue)
                    {
                        options.DelayDeliveryWith(timeoutRequest.Within.Value);
                    }

                    options.RouteToThisEndpoint();

                    options.SetHeader(Headers.SagaId, saga.Entity.Id.ToString());
                    options.SetHeader(TimeoutIdHeaderKey, timeoutRequest.Id);
                    options.SetHeader(Headers.IsSagaTimeoutMessage, bool.TrueString);
                    options.SetHeader(Headers.SagaType, saga.GetType().AssemblyQualifiedName);

                    await context.Send(timeoutRequest.Message, options).ConfigureAwait(false);

                    persistentInstance.Timeouts.Add(new PersistentSagaInstance.Timeout
                    {
                        Id = timeoutRequest.Id,
                        Type = timeoutRequest.Type
                    });
                }

                if (sagaInstanceState.IsNew)
                {
                    await sagaPersister.Save(persistentInstance, persisterContext).ConfigureAwait(false);
                }
                else
                {
                    await sagaPersister.Update(persistentInstance, persisterContext).ConfigureAwait(false);
                }

                sagaInstanceState.Updated();
            }
        }

        static void RemoveSagaHeadersIfProcessingAEvent(IInvokeHandlerContext context)
        {
            // We need this for backwards compatibility because in v4.0.0 we still have this header being sent as part of the message even if MessageIntent == MessageIntentEnum.Publish
            if (context.Headers.TryGetValue(Headers.MessageIntent, out var messageIntentString))
            {
                if (Enum.TryParse(messageIntentString, true, out MessageIntentEnum messageIntent) && messageIntent == MessageIntentEnum.Publish)
                {
                    context.Headers.Remove(Headers.SagaId);
                    context.Headers.Remove(Headers.SagaType);
                }
            }
        }

        static bool IsMessageAllowedToStartTheSaga(IInvokeHandlerContext context, SagaMetadata sagaMetadata)
        {
            if (context.Headers.ContainsKey(Headers.SagaId) &&
                context.Headers.TryGetValue(Headers.SagaType, out var sagaType))
            {
                //we want to move away from the assembly fully qualified name since that will break if you move sagas
                //between assemblies. We use the FullName instead, which is enough to identify the saga.
                if (sagaType.StartsWith(sagaMetadata.Name))
                {
                    //so now we have a saga id for this saga, and if we can't find it, we shouldn't start a new one
                    return false;
                }
            }

            return context.MessageMetadata.MessageHierarchy.Any(messageType => sagaMetadata.IsMessageAllowedToStartTheSaga(messageType.FullName));
        }

        static bool IsTimeoutMessage(Dictionary<string, string> headers)
        {
            if (headers.TryGetValue(Headers.IsSagaTimeoutMessage, out _))
            {
                return true;
            }

            if (!headers.TryGetValue(Headers.NServiceBusVersion, out var version))
            {
                return false;
            }

            if (!version.StartsWith("3."))
            {
                return false;
            }

            if (headers.TryGetValue(Headers.SagaId, out var sagaId))
            {
                if (string.IsNullOrEmpty(sagaId))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (headers.TryGetValue(TimeoutManagerHeaders.Expire, out var expire))
            {
                if (string.IsNullOrEmpty(expire))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            headers[Headers.IsSagaTimeoutMessage] = bool.TrueString;
            return true;
        }

        Task<PersistentSagaInstance> TryLoadSagaInstance(SagaPersisterContext sagaPersisterContext, IInvokeHandlerContext invokeHandlerContext)
        {
            var metadata = sagaPersisterContext.SagaMetadata;

            var sagaType = metadata.SagaType.FullName;

            if (invokeHandlerContext.Headers.TryGetValue(Headers.SagaId, out var sagaId) && !string.IsNullOrEmpty(sagaId))
            {
                var sagaEntityType = metadata.SagaEntityType;

                //since we have a saga id available we can now shortcut the finders and just load the saga
                var loaderType = typeof(LoadSagaByIdWrapper<>).MakeGenericType(sagaEntityType);

                var loader = (SagaLoader)Activator.CreateInstance(loaderType);

                return loader.Load(sagaPersister, sagaType, sagaId, sagaPersisterContext);
            }

            var finderDefinition = GetSagaFinder(metadata, invokeHandlerContext);

            //check if we could find a finder
            if (finderDefinition == null)
            {
                return DefaultSagaDataCompletedTask;
            }

            var finderType = finderDefinition.Type;
            var finder = (SagaFinder)invokeHandlerContext.Builder.Build(finderType);

            return finder.Find(invokeHandlerContext.Builder, finderDefinition, sagaPersisterContext, invokeHandlerContext.MessageBeingHandled, sagaType);
        }

        SagaFinderDefinition GetSagaFinder(SagaMetadata metadata, IInvokeHandlerContext context)
        {
            foreach (var messageType in context.MessageMetadata.MessageHierarchy)
            {
                if (metadata.TryGetFinder(messageType.FullName, out var finderDefinition))
                {
                    return finderDefinition;
                }
            }
            return null;
        }

        IContainSagaData CreateNewSagaEntity(string sagaId, SagaMetadata metadata, IInvokeHandlerContext context)
        {
            var sagaEntityType = metadata.SagaEntityType;

            var sagaEntity = (IContainSagaData)Activator.CreateInstance(sagaEntityType);

            //todo: What if the persister returned a string? can we get away with converting to a deterministic GUID?
            sagaEntity.Id = Guid.Parse(sagaId);
            sagaEntity.OriginalMessageId = context.MessageId;

            if (context.Headers.TryGetValue(Headers.ReplyToAddress, out var replyToAddress))
            {
                sagaEntity.Originator = replyToAddress;
            }

            //var lookupValues = context.Extensions.GetOrCreate<SagaLookupValues>();

            //SagaCorrelationProperty correlationProperty;

            //if (lookupValues.TryGet(sagaEntityType, out var value))
            //{
            //    var propertyInfo = sagaEntityType.GetProperty(value.PropertyName);

            //    var convertedValue = TypeDescriptor.GetConverter(propertyInfo.PropertyType)
            //        .ConvertFromInvariantString(value.PropertyValue.ToString());

            //    propertyInfo.SetValue(sagaEntity, convertedValue);

            //    correlationProperty = new SagaCorrelationProperty(value.PropertyName, value.PropertyValue);
            //}
            //else
            //{
            //    correlationProperty = SagaCorrelationProperty.None;
            //}

            //var sagaIdGeneratorContext = new SagaIdGeneratorContext(correlationProperty, metadata, context.Extensions);

            //sagaEntity.Id = sagaIdGenerator.Generate(sagaIdGeneratorContext);

            return sagaEntity;
        }

        readonly SagaMetadataCollection sagaMetadataCollection;
        readonly bool persisterSupportsTimeoutStorage;
        readonly ISagaPersister2 sagaPersister;
        readonly ICancelDeferredMessages timeoutCancellation;
        readonly ISagaIdGenerator sagaIdGenerator;

        static readonly Task<PersistentSagaInstance> DefaultSagaDataCompletedTask = Task.FromResult(default(PersistentSagaInstance));
        static string TimeoutIdHeaderKey = "NServiceBus.Saga.TimeoutId";
        static readonly ILog logger = LogManager.GetLogger<SagaPersistenceBehavior>();
    }
}
