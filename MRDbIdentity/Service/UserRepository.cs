using Microsoft.AspNetCore.Identity;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MRDb.Infrastructure.Exceptions;
using MRDb.Repository;
using MRDb.Tools;
using MRDbIdentity.Domain;
using MRDbIdentity.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MRDbIdentity.Service
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        protected IRoleRepository _roleRepository;

        public UserRepository(IMongoDatabase mongoDatabase, IRoleRepository roleRepository) : base(mongoDatabase)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(IRoleRepository));

            if(!BsonClassMap.IsClassMapRegistered(typeof(Claim))){
                BsonClassMap.RegisterClassMap<Claim>(cm =>
                {
                    cm.SetIgnoreExtraElements(true);
                    cm.MapMember(c => c.Issuer);
                    cm.MapMember(c => c.OriginalIssuer);
                    cm.MapMember(c => c.Properties);
                    cm.MapMember(c => c.Subject);
                    cm.MapMember(c => c.Type);
                    cm.MapMember(c => c.Value);
                    cm.MapMember(c => c.ValueType);
                    cm.MapCreator(c => new Claim(c.Type, c.Value, c.ValueType, c.Issuer, c.OriginalIssuer, c.Subject));
                });
            }
        }

        protected Expression<Func<ProjectionDefinitionBuilder<User>, ProjectionDefinition<User>>> SafeProjection => x => x.Exclude(z => z.PasswordHash).Exclude(z => z.Claims).Exclude(z => z.Logins);

        public override async Task<User> GetFirst(DbQuery<User> search)
        {
            search.Projection(SafeProjection);
            return await base.GetFirst(search);
        }

        public override async Task<ICollection<User>> Get(DbQuery<User> search)
        {
            search.Projection(SafeProjection);
            return await base.Get(search);
        }

        #region user

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            var result = await Insert(user);
            if (result != null) return IdentityResult.Success;
            return IdentityResult.Failed(new IdentityError
            {
                Code = "3",
                Description = "Can not create user"
            });
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            var result = await Remove(user.Id);

            if (result.DeletedCount == 1) return IdentityResult.Success;
            return IdentityResult.Failed(new IdentityError()
            {
                Code = "1",
                Description = "Can not delete user"
            });

        }

        public async Task<User> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            var query = DbQuery.Eq(x => x.Id, userId).Projection(SafeProjection);
            return await GetFirst(query);
        }

        public async Task<User> FindByNameAsync(string email, CancellationToken cancellationToken)
        {
            var query = DbQuery.Eq(x => x.Email, email).Projection(SafeProjection);
            return await _collection.Find(query.FilterDefinition).Project<User>(query.ProjectionDefinition).FirstOrDefaultAsync();
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            var result = await UpdateAsync(user);
            if (result.ModifiedCount == 1) return IdentityResult.Success;
            return IdentityResult.Failed(new IdentityError
            {
                Code = "2",
                Description = "Can not update user"
            });
        }

        public async Task<UpdateResult> UpdateAsync(User user)
        {
            var query = DbQuery
                .Update(z => z.Set(x => x.Avatar, user.Avatar).Set(x => x.Birthday, user.Birthday).Set(x => x.FirstName, user.FirstName).Set(x => x.LastName, user.LastName).Set(x => x.UpdatedTime, DateTime.UtcNow))
                .Eq(x => x.Id, user.Id);

            return await Update(query);
        }

        public async Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Email, user.Email.ToLower())
                .Projection(x => x.Include(z => z.Id));

            var projection = await _collection.Find(query.FilterDefinition).Project(query.ProjectionDefinition).FirstOrDefaultAsync();
            var bsonValue = projection?.GetElement("_id");
            if (bsonValue == null || !bsonValue.HasValue || bsonValue.Value.Value.IsBsonNull) return null;
            return bsonValue.Value.Value.ToString();
        }

        public async Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Projection(x => x.Include(z => z.UserName));

            var projection = await _collection.Find(query.FilterDefinition).Project(query.ProjectionDefinition).FirstOrDefaultAsync();

            var bsonValue = projection?.GetElement("UserName");
            if (bsonValue == null || !bsonValue.HasValue || bsonValue.Value.Value.IsBsonNull) return null;
            return bsonValue.Value.Value.ToString();
        }

        public async Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.Set(z => z.UserName, userName.ToLower()));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public async Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Projection(x => x.Include(z => z.UserName));

            var projection = await _collection.Find(query.FilterDefinition).Project(query.ProjectionDefinition).FirstOrDefaultAsync();
            var bsonValue = projection?.GetElement("UserName");
            if (bsonValue == null || !bsonValue.HasValue || bsonValue.Value.Value.IsBsonNull) return null;
            return bsonValue.Value.Value.ToString().ToUpper();
        }

        public async Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.Set(z => z.UserName, normalizedName));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        #endregion

        #region role

        public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.FindByNameAsync(roleName.ToUpper(), new CancellationToken());
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.AddToSet(z => z.Roles, new UserRole(role)));

            await Update(query);
        }

        public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Projection(x => x.Include(z => z.Roles));

            var projection = await _collection.Find(query.FilterDefinition).Project(query.ProjectionDefinition).FirstOrDefaultAsync();
            var value = projection.GetElement("Roles");
            if (value == null || value.Value == null) return null;

            var result = value.Value.AsBsonArray.Select(x => BsonSerializer.Deserialize<Role>(x.AsBsonDocument)).ToList();
            return result.Select(x => x.Name).ToList();
        }

        public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            var query = DbQuery.Eq(x => x.Id, user.Id).CustomSearch(x => x.ElemMatch(z => z.Roles, z => z.RoleName == roleName.ToUpper()));
            var result = await _collection.Find(query.FilterDefinition).CountDocumentsAsync() > 0;
            return result;
        }

        public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.PullFilter(z => z.Roles, z => z.RoleName == roleName.ToUpper()));

            await Update(query);
        }

        public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            var users = await Get(new DbQuery<User>()
                .CustomSearch(x => x.ElemMatch(z => z.Roles, z => z.RoleName.Contains(roleName.ToUpper()))).Projection(SafeProjection));
            return users?.ToList() ?? new List<User>();
        }

        #endregion

        #region password

        public async Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
        {
            var query = DbQuery.Eq(x => x.Id, user.Id).Update(x => x.Set(z => z.PasswordHash, passwordHash));
            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public async Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
            var query = new DbQuery<User>().Eq(x => x.Id, user.Id);
            query.Projection(x => x.Include(z => z.PasswordHash));

            var projection = await _collection.Find(query.FilterDefinition).Project(query.ProjectionDefinition).FirstOrDefaultAsync();
            var doc = projection?.GetValue("PasswordHash");
            if (doc == null || doc.IsBsonNull) return null;
            return doc.ToString();
        }

        public async Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            var hash = await GetPasswordHashAsync(user, cancellationToken);
            return !string.IsNullOrWhiteSpace(hash);
        }

        #endregion

        #region claims

        public async Task<IList<Claim>> GetClaimsAsync(User user, CancellationToken cancellationToken)
        {
            var query = new DbQuery<User>()
                .Eq(x => x.Id, user.Id)
                .Projection(x => x.Include(z => z.Claims));

            var projection = await _collection.Find(query.FilterDefinition).Project(query.ProjectionDefinition).FirstOrDefaultAsync();
            if (projection == null) return null;

            var value = projection.GetElement("Claims");
            if (value == null || value.Value == null) return null;

            var result = value.Value.AsBsonArray.Select(x => BsonSerializer.Deserialize<Claim>(x.AsBsonDocument)).ToList();
            return result;
        }

        public async Task AddClaimsAsync(User user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.AddToSetEach(z => z.Claims, claims));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public async Task ReplaceClaimAsync(User user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.Pull(z => z.Claims, claim).AddToSet(z => z.Claims, newClaim));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public async Task RemoveClaimsAsync(User user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.PullAll(z => z.Claims, claims));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public async Task<IList<User>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            var query = new DbQuery<User>()
                .CustomSearch(x => x.ElemMatch(z => z.Claims, z => z.Equals(claim)))
                .Projection(SafeProjection);

            var users = await Get(query);
            return users.ToList();
        }

        #endregion

        #region logins

        public async Task AddLoginAsync(User user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.AddToSet(z => z.Logins, login));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public async Task RemoveLoginAsync(User user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.PullFilter(z => z.Logins, z => z.LoginProvider == loginProvider && z.ProviderKey == providerKey));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(User user, CancellationToken cancellationToken)
        {
            var query = new DbQuery<User>()
                .Where(x => x.Id == user.Id)
                .Projection(x => x.Include(z => z.Logins));

            var projection = await _collection.Find(query.FilterDefinition).Project(query.ProjectionDefinition).FirstOrDefaultAsync();
            if (projection == null) return null;

            var value = projection.GetElement("Logins");
            if (value == null || value.Value == null) return null;

            var result = value.Value.AsBsonArray.Select(x => BsonSerializer.Deserialize<UserLoginInfo>(x.AsBsonDocument)).ToList();
            return result;
        }

        public async Task<User> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var query = DbQuery.CustomSearch(x => x.ElemMatch(z => z.Logins, z => z.LoginProvider == loginProvider && z.ProviderKey == providerKey)).Projection(SafeProjection);
            return await _collection.Find(query.FilterDefinition).FirstOrDefaultAsync();
        }

        #endregion

        #region email


        public async Task SetEmailAsync(User user, string email, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.Set(z => z.Email, email));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public async Task<string> GetEmailAsync(User user, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Projection(x => x.Include(z => z.Email));

            var projection = await _collection.Find(query.FilterDefinition).Project(query.ProjectionDefinition).FirstOrDefaultAsync();
            return projection?.GetElement("Email").Value?.ToString();
        }

        public async Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Projection(x => x.Include(z => z.IsEmailConfirmed));

            var projection = await _collection.Find(query.FilterDefinition).Project(query.ProjectionDefinition).FirstOrDefaultAsync();
            return projection?.GetElement("IsEmailConfirmed").Value?.ToBoolean() ?? false;
        }

        public async Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.Set(z => z.IsEmailConfirmed, confirmed));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        public async Task<User> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            var query = DbQuery.Eq(x => x.Email, normalizedEmail.ToLower()).Projection(SafeProjection);
            return await _collection.Find(query.FilterDefinition).Project<User>(query.ProjectionDefinition).FirstOrDefaultAsync();
        }

        public async Task<string> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
        {
            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Projection(x => x.Include(z => z.Email));

            var projection = await _collection.Find(query.FilterDefinition).Project(DbQuery.ProjectionDefinition).FirstOrDefaultAsync();
            return projection?.GetElement("Email").Value?.ToString().ToLower();
        }

        public async Task SetNormalizedEmailAsync(User user, string normalizedEmail, CancellationToken cancellationToken)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(normalizedEmail)) return;

            var query = DbQuery
                .Eq(x => x.Id, user.Id)
                .Update(x => x.Set(z => z.Email, normalizedEmail.ToLower()));

            await _collection.UpdateOneAsync(query.FilterDefinition, query.UpdateDefinition);
        }

        #endregion

        #region Phone

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="user"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public Task SetPhoneNumberAsync(User user, string phoneNumber, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Task<string> GetPhoneNumberAsync(User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Task<bool> GetPhoneNumberConfirmedAsync(User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>s
        /// Not implemented
        /// </summary>
        /// <param name="user"></param>
        /// <param name="confirmed"></param>
        /// <returns></returns>
        public Task SetPhoneNumberConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
