namespace NEvilES.Abstractions.Pipeline
{
    public abstract class BaseProjector
    {
        protected readonly IReadFromReadModel Reader;
        protected readonly IWriteReadModel Writer;

        protected BaseProjector(IReadFromReadModel reader, IWriteReadModel writer)
        {
            Reader = reader;
            Writer = writer;
        }
    }
}