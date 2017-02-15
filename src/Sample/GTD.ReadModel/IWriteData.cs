using GTD.Common;

namespace GTD.ReadModel
{
    public interface IWriteData
    {
        void Insert<T>(T item) where T : class, IHaveIdentity;
        void Update<T>(T item) where T : class, IHaveIdentity;
    }
}