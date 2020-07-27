using System;

namespace NEvilES.Abstractions
{
    public class ICommandResponse
    {
        Guid StreamId { get; }
        string ResponseType { get; }
    }

    public enum CommandResponseStatus
    {
        /// <summary>
        /// This status will be returned as the payload of a 200 OK API response in the situation that the command failed the
        /// "Validate Command Semantics" step in the command processing pipeline.
        /// </summary>
        RejectedWithError,

        /// <summary>
        /// This status will be returned as the payload of a 200 OK API response in the situation that the command passed all validation
        /// but did not result in any events being emitted, as the command had no effect (eg Aggregate already in the requested state).
        /// </summary>
        NoActionRequired,

        /// <summary>
        /// This status will be returned as the payload of a 202 Accepted API response in the situation that the command passed all validation
        /// and will be processed asyncronously, so that the client can continue processing until results are asynchronously advised to
        /// client via another channel (eg via SignalR).
        /// </summary>
        Accepted,

        /// <summary>
        /// This status will be returned as the payload of a 200 OK API response in the situation that the command passed all validation
        /// and was processed successfully.
        /// In this scenario, there is no business domain data to be returned to the client.
        /// </summary>
        Completed,

        /// <summary>
        /// This status will be returned as the payload of a 200 OK API response in the situation that the command passed all validation
        /// and was processed successfully.
        /// In this scenario, there is no business domain data to be returned to the client.
        /// </summary>
        CompletedWithBody,

        /// <summary>
        /// This status will be returned as the payload of a 200 OK API response in the situation that the command passed all validation
        /// and was processed successfully.
        /// In this scenario, there is no business domain data to be returned to the client.
        /// </summary>
        CompletedWithId
    }

    public abstract class CommandResponse : ICommandResponse
    {
        protected CommandResponse(
            Guid streamId,
            string commandTypeName,
            CommandResponseStatus commandResponseStatus
        )
        {
            StreamId = streamId;
            CommandTypeName = commandTypeName;
            CommandResponseStatus = commandResponseStatus;
        }

        public Guid StreamId { get; }

        public string CommandTypeName { get; }

        public CommandResponseStatus CommandResponseStatus { get; }
    }

    public sealed class CommandRejectedWithError<T> : CommandResponse where T : class
    {
        public CommandRejectedWithError(
            Guid streamId,
            string commandTypeName,
            T commandError
        ) : base(streamId, commandTypeName, CommandResponseStatus.RejectedWithError)
        {
            CommandError = commandError;
            CommandErrorTypeName = typeof(T).Name;
        }

        public string CommandErrorTypeName { get; }

        public T CommandError { get; }

    }

    public sealed class CommandNoActionRequired : CommandResponse
    {
        public CommandNoActionRequired(
            Guid streamId,
            string commandTypeName
        ) : base(streamId, commandTypeName, CommandResponseStatus.NoActionRequired) { }
    }


    public sealed class CommandAccepted : CommandResponse
    {
        public CommandAccepted(
            Guid streamId,
            string commandTypeName
        ) : base(streamId, commandTypeName, CommandResponseStatus.Accepted) { }
    }

    public sealed class CommandCompleted : CommandResponse
    {
        public CommandCompleted(
            Guid streamId,
            string commandTypeName
        ) : base(streamId, commandTypeName, CommandResponseStatus.Completed) { }
    }

    public sealed class CommandCompletedWithBody<T> : CommandResponse where T : class
    {
        public CommandCompletedWithBody(
            Guid streamId,
            string commandTypeName,
            T commandReponseBody
        ) : base(streamId, commandTypeName, CommandResponseStatus.CompletedWithBody)
        {
            CommandResponseBodyTypeName = typeof(T).Name;
            CommandResponseBody = commandReponseBody;
        }

        public string CommandResponseBodyTypeName { get; }

        public T CommandResponseBody { get; }
    }

    public sealed class CommandCompletedWithId : CommandResponse
    {
        public CommandCompletedWithId(
            Guid streamId,
            string commandTypeName,
            string aggregateId
        ) : base(streamId, commandTypeName, CommandResponseStatus.CompletedWithId)
        {
            // Id = aggregateId;
        }

        // public string Id { get; }
    }
}