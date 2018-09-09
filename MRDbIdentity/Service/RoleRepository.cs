using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using MRDb.Repository;
using MRDbIdentity.Domain;
using MRDbIdentity.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MRDbIdentity.Service
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(IMongoDatabase mongoDatabase) : base(mongoDatabase) { }

        public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
        {
            if (role == null) throw new ArgumentNullException(nameof(Role));
            role.Name = role.Name.ToUpper();
            if ((await Count(x => x.Name == role.Name)) > 0) return IdentityResult.Failed(new IdentityError()
            {
                Code = "0",
                Description = "Role exists"
            });
            await _collection.InsertOneAsync(role);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
        {
            if (role == null) throw new ArgumentNullException(nameof(Role));
            await _collection.DeleteOneAsync(new FilterDefinitionBuilder<Role>().Where(x => x.Name == role.Name));
            return IdentityResult.Success;
        }


        public async Task<Role> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            return await GetFirst(roleId);
        }


        public async Task<Role> FindByNameAsync(string roleName, CancellationToken cancellationToken)
        {
            return await GetFirst(x => x.Name == roleName.ToUpper());
        }

        public Task<string> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name.ToUpper());
        }

        public async Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
        {
            return (await GetFirst(x => x.Name == role.Name.ToUpper()))?.Id;
        }

        public async Task<string> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            return (await GetFirst(role.Id))?.Name;
        }

        public async Task SetNormalizedRoleNameAsync(Role role, string normalizedName, CancellationToken cancellationToken)
        {
            var r = await GetFirst(role.Id);
            r.Name = normalizedName;
            r.OnUpdate();
            await Replace(r);
        }

        public async Task SetRoleNameAsync(Role role, string roleName, CancellationToken cancellationToken)
        {
            var r = await GetFirst(role.Id);
            r.Name = roleName.ToUpper();
            r.OnUpdate();
            await Replace(r);
        }

        public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
        {
            var result = await Replace(role);
            if (result == null) return IdentityResult.Failed(new IdentityError() { Code = "1", Description = "Fail" });
            return IdentityResult.Success;
        }
    }
}
