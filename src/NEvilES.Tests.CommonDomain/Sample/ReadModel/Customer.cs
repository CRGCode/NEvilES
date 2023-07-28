using System;
using System.Collections.Generic;
using NEvilES.Abstractions.Pipeline;
using Newtonsoft.Json;

namespace NEvilES.Tests.CommonDomain.Sample.ReadModel
{
    public class Customer : IHaveIdentity<Guid>
    {
        public Customer()
        {
            Complaints = new List<string>();
            Notes = new List<string>();
        }

        [JsonConstructor]
        public Customer(Guid id, string name) : this()
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; }

        public string Name { get; }

        public List<string> Complaints { get; set; }

        public List<string> Notes { get; set; }

        public class Projector : BaseProjector<Customer>,
            IProject<Sample.Customer.Created>,
            IProject<Sample.Customer.Complaint>,
            IProject<Sample.Customer.NoteAdded>
        {
            public Projector(IReadFromReadModel<Guid> reader, IWriteReadModel<Guid> writer) : base(reader, writer)
            {
            }

            public void Project(Sample.Customer.Created message, IProjectorData data)
            {
                var customer = new Customer(message.CustomerId, message.Details.Name);
                Writer.Insert(customer);
            }

            public void Project(Sample.Customer.Complaint message, IProjectorData data)
            {
                var customer = Reader.Get<Customer>(message.CustomerId);
                customer.Complaints.Add(message.Reason);

                Writer.Update(customer);
            }

            public void Project(Sample.Customer.NoteAdded message, IProjectorData data)
            {
                var customer = Reader.Get<Customer>(message.CustomerId);
                customer.Notes.Add(message.Text);

                Writer.Update(customer);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;
            var other = ((Customer)obj)!;
            return Id == other.Id && Name == other.Name;
        }

        protected bool Equals(Customer other)
        {
            return Id.Equals(other.Id) && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}