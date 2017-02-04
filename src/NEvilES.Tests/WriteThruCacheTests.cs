using System;
using System.Collections.Generic;
using NEvilES.DataStore;

namespace NEvilES.Tests
{
    //public class WriteThruCacheTests
    //{
    //    private readonly WriteThruCache cache;
    //    private readonly IWriteData dataWriter;

    //    private readonly User[] activeUser;
    //    private User[] lockedOut;

    //    public WriteThruCacheTests()
    //    {
    //        activeUser = new[]
    //        {
    //            new User(1, UserStatus.Active) {Email = "test1", Id = Guid.NewGuid()},
    //            new User(2, UserStatus.Active) {Email = "test2"},
    //            new User(3, UserStatus.Active) {Email = "test3"},
    //            new User(8, UserStatus.Active) {Email = "test8"},
    //        };
    //        lockedOut = new[]
    //        {
    //            new User(5, UserStatus.LockedOut) {Email = "test5"},
    //            new User(6, UserStatus.LockedOut) {Email = "test6"},
    //            new User(7, UserStatus.LockedOut) {Email = "test7"}
    //        };
    //        var writeThruCache = new WriteThruCache();
    //        dataWriter = new Mock<IWriteData>().Object;
    //        var initialData = activeUser.Concat(lockedOut).Concat(new[] {new User(4, UserStatus.AwaitingActivation) {Email = "test4"}});
    //        writeThruCache.InitCache(new UserCache(),initialData);
    //        cache = writeThruCache;
    //    }

    //    [Fact]
    //    public void Insert()
    //    {
    //        var user = new User(666, UserStatus.TemporarilyLocked) {Id = CombGuid.NewGuid()};
    //        cache.Insert(user, dataWriter);

    //        var expected = cache.Lookup<User, UserCache>(x => x.ById(user.Id)).First();

    //        Assert.True(expected == user);
    //    }

    //    [Fact]
    //    public void Update()
    //    {
    //        const string email = "New Email";
    //        var expected = cache.Lookup<User, UserCache>(x => x.ByUserId(activeUser[0].UserId)).Single();
    //        expected.Email = email;
    //        cache.Update(expected, dataWriter);

    //        var user = cache.Lookup<User, UserCache>(x => x.ByEmail(email)).Single();

    //        Assert.True(user == expected);
    //    }

    //    //[Fact]
    //    //public void Load()
    //    //{
    //    //    var id = activeUser[0].Id;
    //    //    var user = cache.Load<User>(id);

    //    //    Assert.True(user.Id == id);
    //    //}

    //    [Fact]
    //    public void Lookup()
    //    {
    //        var id = 3;
    //        var user = cache.Lookup<User, UserCache>(x => x.ByUserId(id)).Single();
    //        Assert.True(user.UserId == id);

    //        var activeUsers = cache.Lookup<User, UserCache>(x => x.ByStatus(UserStatus.Active));
    //        Assert.True(activeUsers.Count() == activeUser.Length);
    //    }
    //}

    public class UserCache : Cache<User>
    {
        protected override IEnumerable<Index> CreateIndexes(User user)
        {
            yield return ByUserId(user.UserId);
            yield return ByStatus(user.Status);
            yield return ByEmail(user.Email);
        }

        protected override IEnumerable<Index> CreatePostIndexes(User user)
        {
            yield return ById(user.Id);
        }

        public Index ById(Guid id)
        {
            return IndexFrom(new { id });
        }

        public Index ByUserId(int userId)
        {
            return IndexFrom(new { userId });
        }

        public Index ByStatus(UserStatus status)
        {
            return IndexFrom(new { status });
        }

        public Index ByEmail(string email)
        {
            return IndexFrom(new { email });
        }
    }

    public class User 
    {
        public User(int userId, UserStatus status)
        {
            HomePhone = string.Empty;
            WorkPhone = string.Empty;
            Mobile = string.Empty;
            UserId = userId;
            Status = status;
        }

        public Guid Id { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string HomePhone { get; set; }
        public string WorkPhone { get; set; }
        public string Mobile { get; set; }
        public UserStatus Status { get; set; }
        public string Username { get; set; }
    }

    public enum UserStatus
    {
        NotSet = 0,
        Active = 1,
        TemporarilyLocked = 2,
        LockedOut = 3,
        AwaitingActivation = 4,
        Deactivated = 5,
        Archived = 6
    }
}