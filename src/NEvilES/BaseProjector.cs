using System;
using NEvilES.Abstractions;
using NEvilES.Abstractions.ObjectPath;
using NEvilES.Abstractions.ObjectPath.PathElements;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;

namespace NEvilES
{
    public abstract class BaseProjector<T> : IProjectWithResult<PatchEvent> 
        where T : class, IHaveIdentity<Guid>
    {
        protected readonly IReadFromReadModel<Guid> Reader;
        protected readonly IWriteReadModel<Guid> Writer;

        protected BaseProjector(IReadFromReadModel<Guid> reader, IWriteReadModel<Guid> writer)
        {
            Reader = reader;
            Writer = writer;
        }

        public T Get(Guid id)
        {
            return Reader.Get<T>(id);
        }

        public IProjectorResult Project(PatchEvent message, IProjectorData data)
        {
            var item = Get(message.GetStreamId());

            if (item == null)
            {
                return default;
            }

            var r = new Resolver();
            var result = (Property)r.Resolve(item, message.Path);
            result.SetValue(message.Value);
            Writer.Update(item);
            return new ProjectorResult(item);
        }
    }
}