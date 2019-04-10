using System;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class PersonalDetails
    {
        public PersonalDetails(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateOfBirth DateOfBirth { get; set; }
        public Address Address { get; set; }
        public string Name => $"{FirstName} {LastName}";
    }

    public class DateOfBirth
    {
        public DateTime Value { get; set; }
        public int Age => 10;
    }

    public class Address
    {
        public int Number { get; set; }
        public string Street { get; set; }
        public Suburb Suburb { get; set; }
        public string PostCode { get; set; }
        public State State { get; set; }
    }

    public class Suburb
    {
        public string Street { get; set; }
    }

    public enum State
    {
        Vic,
        Nsw,
        Sa,
        Wa,
        Qld,
        Nt,
        Tas,
        Act
    }
}