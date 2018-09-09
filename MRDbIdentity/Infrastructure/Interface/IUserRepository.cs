using MRDb.Infrastructure.Interface;
using MRDbIdentity.Domain;
using Microsoft.AspNetCore.Identity;


namespace MRDbIdentity.Infrastructure.Interface
{
    public interface IUserRepository : 
        IUserStore<User>,
        IUserRoleStore<User>,
        IUserPasswordStore<User>,
        IUserClaimStore<User>,
        IUserLoginStore<User>,
        IUserEmailStore<User>,
        IUserPhoneNumberStore<User>,
        IRepository<User>
    {
    }


}
