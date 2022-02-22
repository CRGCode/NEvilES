using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using NEvilES.Abstractions.Pipeline;

namespace GTD.ReadModel
{
    public class User : IHaveIdentity<string>
    {
        public class SignInModel
        {
            [Required]
            public string Email { get; set; }
            [Required(ErrorMessage = "Password is required!")]
            public string Password { get; set; }
        }

        public User(string id, string name, string email)
        {
            Id = id;
            Name = name;
            Email = email;
        }
        public string Id { get; }
        public string Name { get; }
        public string Email { get;  }


        public class Projector :
            IProject<Domain.User.Created>
        {
            private readonly IReadFromReadModel<string> reader;
            private readonly IWriteReadModel<string> writer;

            public Projector(IReadFromReadModel<string> reader, IWriteReadModel<string> writer)
            {
                this.reader = reader;
                this.writer = writer;
            }

            public void Project(Domain.User.Created message, IProjectorData data)
            {
                writer.Insert(new User(message.Details.Email,message.Details.Name,message.Details.Email));
            }
        }
    }
}