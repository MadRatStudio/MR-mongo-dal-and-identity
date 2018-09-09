using Microsoft.AspNetCore.Identity;
using MRDb.Infrastructure.Interface;
using MRDbIdentity.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace MRDbIdentity.Infrastructure.Interface
{
    public interface IRoleRepository :
        IRoleStore<Role>,
        IRepository<Role>
    {

    }
}
