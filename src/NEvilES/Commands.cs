// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable TypeParameterCanBeVariant

namespace NEvilES
{
    public interface IProcessCommand<T> where T : ICommand
    {
        void Handle(T command);
    }

    public interface IHandleStatelessEvent<T> where T : IEvent {}

    public interface IHandleAggregateCommand<T> : IHandleAggregateCommandMarker<T> where T : ICommand
    {
        void Handle(T command);
    }

    public interface IHandleAggregateCommand<TCommand, TDep1> : IHandleAggregateCommandMarker<TCommand> where TCommand : ICommand
    {
        void Handle(TCommand command, TDep1 dep1);
    }

    public interface IHandleAggregateCommand<TCommand, TDep1, TDep2> : IHandleAggregateCommandMarker<TCommand> where TCommand : ICommand
    {
        void Handle(TCommand command, TDep1 dep1, TDep2 dep2);
    }

    public interface IHandleAggregateCommand<TCommand, TDep1, TDep2, TDep3> : IHandleAggregateCommandMarker<TCommand> where TCommand : ICommand
    {
        void Handle(TCommand command, TDep1 dep1, TDep2 dep2, TDep3 dep3);
    }

    public interface IHandleAggregateCommand<TCommand, TDep1, TDep2, TDep3, TDep4> : IHandleAggregateCommandMarker<TCommand> where TCommand : ICommand
    {
        void Handle(TCommand command, TDep1 dep1, TDep2 dep2, TDep3 dep3, TDep4 dep4);
    }

    public interface IHandleAggregateCommandMarker<T> where T : ICommand { }
}