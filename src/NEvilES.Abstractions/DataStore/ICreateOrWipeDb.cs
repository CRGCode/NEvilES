namespace NEvilES.Abstractions.DataStore
{
    public interface ICreateOrWipeDb
    {
        void CreateOrWipeDb(IConnectionString connString);
    }
}