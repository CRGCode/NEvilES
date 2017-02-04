using System;
using System.Collections.Generic;

namespace NEvilES.Pipeline
{
    public class SecurityContext : ISecurityContext
    {
        //public Guid AccountId { get; }
        //public string Permissions { get; }

        public bool CheckSecurity()
        {
            return true;
        }
    }

    public interface ISecurityContext
    {
        //Guid AccountId { get; }
        //string Permissions { get; }

        bool CheckSecurity();
    }

    public class SecurityProcessor<TCommand> : IProcessPipelineStage<TCommand>
        where TCommand : IMessage
    {
        private readonly ISecurityContext _securityContext;
        private readonly IProcessPipelineStage<TCommand> _innerCommand;

        public SecurityProcessor(ISecurityContext securityContext, IProcessPipelineStage<TCommand> innerCommand)
        {
            _securityContext = securityContext;
            _innerCommand = innerCommand;
        }

        public CommandResult Process(TCommand command)
        {
            //var securityOfficer = new SecurityOfficer();
            //securityOfficer.Register(new UserAccountLink());

            //securityOfficer.CheckSecurity(UserId.From(), AccountId.From());

            // do some security checks....?
            if (!_securityContext.CheckSecurity())
            {
                throw new Exception("Security Issues.......");
            }
            return _innerCommand.Process(command);
        }
    }

    public class SecurityOfficer
    {
        private readonly Dictionary<object, ISecurityLink> _store = new Dictionary<object, ISecurityLink>();

        public void Register<TFrom, TTo>(ISecurityLink<TFrom, TTo> securityLink)
        {
            _store[BuildKey(typeof(TFrom), typeof(TTo))] = securityLink;
        }

        public bool CheckSecurity<TFrom, TTo>(IIdentity<TFrom> fromId, IIdentity<TTo> toId)
        {
            var key = BuildKey(typeof(TFrom), typeof(TTo));
            if (_store.ContainsKey(key))
            {
                return (_store[key] as dynamic).HasAccess((dynamic)fromId, (dynamic)toId);
            }
            return false;
        }

        private object BuildKey(Type type1, Type type2)
        {
            return new { type1, type2 };
        }
    }

    public class UserId : IIdentity<UserId>
    {
        private UserId(Guid id)
        {
            Id = id;
        }

        public static UserId From(Guid id)
        {
            return new UserId(id);
        }

        public Guid Id { get; set; }
    }

    public class AccountId : IIdentity<AccountId>
    {
        private AccountId(Guid id)
        {
            Id = id;
        }

        public static AccountId From(Guid id)
        {
            return new AccountId(id);
        }

        public Guid Id { get; set; }
    }

    public class UserAccountLink : ISecurityLink<UserId, AccountId>
    {
        public bool HasAccess(IIdentity<UserId> fromId, IIdentity<AccountId> toId)
        {
            return fromId.Id == toId.Id;
        }
    }

    public interface ISecurityLink<TFrom, TTo> : ISecurityLink
    {
        bool HasAccess(IIdentity<TFrom> fromId, IIdentity<TTo> toId);
    }

    public interface ISecurityLink
    {
    }

    public interface IIdentity<T>
    {
        Guid Id { get; set; }
    }
}