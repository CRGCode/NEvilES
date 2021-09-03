namespace NEvilES.Abstractions.Pipeline
{
    public abstract class BaseProjector<TId>
    {
        protected readonly IReadFromReadModel<TId> Reader;
        protected readonly IWriteReadModel<TId> Writer;

        protected BaseProjector(IReadFromReadModel<TId> reader, IWriteReadModel<TId> writer)
        {
            Reader = reader;
            Writer = writer;
        }
    }
}