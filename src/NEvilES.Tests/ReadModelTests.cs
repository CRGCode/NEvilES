using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Tests.CommonDomain.Sample.ReadModel;
using Xunit;

namespace NEvilES.Tests
{
    public class ReadModelTests : IClassFixture<SharedFixtureContext>, IDisposable
    {
        private readonly IServiceScope scope;

        public ReadModelTests(SharedFixtureContext context)
        {
            scope = context.Container.CreateScope();
        }

        [Fact]
        public void ReaderForGuid_Null()
        {
            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<Guid>>();

            Assert.Throws<KeyNotFoundException>(() => reader.Get<Person>(Guid.NewGuid()));
        }

        [Fact]
        public void ReaderForString_Null()
        {
            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<string>>();

            Assert.Throws<KeyNotFoundException>(() => reader.Get<Dashboard>("1234"));
        }

        [Fact]
        public void WriterForString()
        {
            var writer = scope.ServiceProvider.GetRequiredService<IWriteReadModel<string>>();

            writer.Insert(new Dashboard{ Id = "1234" });

            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<string>>();

            var dashboard =  reader.Get<Dashboard>("1234");

            Assert.Equal("1234", dashboard.Id);
        }

        [Fact]
        public void WriterForGuid()
        {
            var writer = scope.ServiceProvider.GetRequiredService<IWriteReadModel<Guid>>();

            var id = Guid.NewGuid();
            writer.Insert(new Person { Id = id });

            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<Guid>>();

            var person = reader.Get<Person>(id);

            Assert.Equal(id, person.Id);
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}