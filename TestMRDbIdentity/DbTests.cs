using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MRDbIdentity.Domain;
using MRDbIdentity.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace TestMRDbIdentity
{
    [TestClass]
    public class DbTests
    {
        [TestMethod]
        public async Task TestUserCreate()
        {
            var newUser = GetTestUser();

            var roleRepo = new RoleRepository(GetDatabase());
            var userRepo = new UserRepository(GetDatabase(), roleRepo);

            var manager = new UserManager<User>(userRepo, null, new PasswordHasher<User>(), new List<UserValidator<User>>(), new PasswordValidator<User>[] { }, new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new LoggerFactory().CreateLogger<UserManager<User>>());

            var createRoles = await roleRepo.Insert(GetTestRoles());

            Assert.IsTrue(createRoles != null && createRoles.Any());

            var testUser = GetTestUser();
            var managerResult = await manager.CreateAsync(testUser);

            var isPassword = await manager.HasPasswordAsync(testUser);

            Assert.IsTrue(managerResult.Succeeded);

            managerResult = await manager.AddToRoleAsync(testUser, "ADMIN");

            Assert.IsTrue(managerResult.Succeeded);

            managerResult = await manager.AddToRolesAsync(testUser, new string[] { "USER" });

            Assert.IsTrue(managerResult.Succeeded);

            managerResult = await manager.AddPasswordAsync(testUser, "MyTestPass0");

            Assert.IsTrue(managerResult.Succeeded);

            managerResult = await manager.SetEmailAsync(testUser, "oleg2@gmail.com");

            Assert.IsTrue(managerResult.Succeeded);

            managerResult = await manager.RemoveFromRolesAsync(testUser, new string[] { "ADMIN", "USER" });

            Assert.IsTrue(managerResult.Succeeded);
        }

        [TestMethod]
        public async Task TestRoles()
        {
            var testUser = GetTestUser();
            var roles = GetTestRoles();

            var roleRepo = new RoleRepository(GetDatabase());
            var userRepo = new UserRepository(GetDatabase(), roleRepo);

            var manager = new UserManager<User>(userRepo, null, new PasswordHasher<User>(), new List<UserValidator<User>>(), new PasswordValidator<User>[] { }, new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new LoggerFactory().CreateLogger<UserManager<User>>());

            foreach(var role in roles)
            {
                var roleId = await roleRepo.GetRoleIdAsync(role, new System.Threading.CancellationToken());
                if (string.IsNullOrWhiteSpace(roleId))
                {
                    await roleRepo.CreateAsync(role, new System.Threading.CancellationToken());
                }
                else
                {
                    role.Id = roleId;
                }
            }

            var user = await userRepo.GetFirst(x => x.Email == "oleg.timofeev20@gmail.com");
            if(user == null)
            {
                user = GetTestUser();
                await manager.CreateAsync(user, "My awesome password");

                await manager.AddToRolesAsync(user, roles.Select(x => x.Name));
            }

            //await manager.AddClaimAsync(user, new Claim("Some type", "Value", "Value type", "issuer", "original issuer"));

            var getUser = await userRepo.GetFirst(user.Id);
            Assert.IsNotNull(getUser);
            Assert.IsNull(getUser.PasswordHash);
        }

        [TestMethod]
        public async Task TestClaims()
        {
            var user = GetTestUser();

            var roleRepo = new RoleRepository(GetDatabase());
            var userRepo = new UserRepository(GetDatabase(), roleRepo);

            var manager = new UserManager<User>(userRepo, null, new PasswordHasher<User>(), new List<UserValidator<User>>(), new PasswordValidator<User>[] { }, new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null, new LoggerFactory().CreateLogger<UserManager<User>>());

            var exists = await manager.FindByEmailAsync(user.Email);
            if(exists == null)
            {
                await manager.CreateAsync(user, "somepass");
            }
            else
            {
                user = exists;
            }

            var createClaim = await manager.AddClaimAsync(user, new Claim("123", "321", "string", "asdasd", "asda"));
            Assert.IsTrue(createClaim.Succeeded);

            var allClaims = await manager.GetClaimsAsync(user);
            Assert.IsNotNull(allClaims);
        }

        protected IMongoDatabase GetDatabase() => new MongoClient("mongodb+srv://ratadmin:ratadmin_()_@madrat-dev-cluster-0onbt.mongodb.net/auth?retryWrites=true").GetDatabase("auth");
        protected User GetTestUser() => new User
        {
            FirstName = "Oleh",
            LastName = "Tymofieiev",
            Email = "oleg.timofeev20@gmail.com",
            UserName = "oleg.timofeev20@gmail.com",
            Avatar = new UserAvatar
            {
                Src = "http://google.com"
            },
            Roles = new List<UserRole>(),
            Tels = new List<UserTel>
            {
                new UserTel{CreatedTime = DateTime.Now, Name = "My tel", Number = "+380508837161"}
            },
            Birthday = new DateTime(1995, 3, 20)
        };

        protected List<Role> GetTestRoles() => new List<Role>
        {
            new Role("Admin"),
            new Role("User"),
            new Role("Banned")
        };
    }
}
