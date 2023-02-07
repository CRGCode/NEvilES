namespace NEvilES.Tests.ObjectPath.Helpers
{
    class StringIndexerClassNoIEnumerable
    {
        private readonly string value;

        public StringIndexerClassNoIEnumerable(string value)
        {
            this.value = value;
        }

        public string this[string index] => value + index;
    }
}
