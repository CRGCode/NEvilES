namespace NEvilES.Tests.ObjectPath.Helpers
{
    class IntIndexerClassNoIEnumerable
    {
        private readonly string value;

        public IntIndexerClassNoIEnumerable(string value)
        {
            this.value = value;
        }

        public string this[int index] => value + index;
    }
}
