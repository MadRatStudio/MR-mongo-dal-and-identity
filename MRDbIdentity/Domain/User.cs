using MRDb.Domain;
using MRDb.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace MRDbIdentity.Domain
{
    public class User : Entity, IEntity
    {
        public string UserName { get; set; }

        public string Email { get; set; }
        public bool IsEmailConfirmed { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<UserTel> Tels { get; set; } = new List<UserTel>();
        public DateTime Birthday { get; set; }
        public UserAvatar Avatar { get; set; }


        // block
        public bool IsBlocked { get; set; }
        public DateTime BlockedTime { get; set; }
        public DateTime BlockUntil { get; set; }
        public string BlockReason { get; set; }

        // password

        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public List<UserRole> Roles { get; set; } = new List<UserRole>();
        public List<Claim> Claims { get; set; } = new List<Claim>();
        public List<UserLoginInfo> Logins { get; set; } = new List<UserLoginInfo>();
    }

    public class UserTel
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public DateTime CreatedTime { get; set; }
    }

    public class UserAvatar
    {
        public string Src { get; set; }
    }

    public class UserRole
    {
        public DateTime AddedTime { get; set; }

        public string RoleId { get; set; }
        public string RoleName { get; set; }

        public UserRole()
        {
            AddedTime = DateTime.UtcNow;
        }

        public UserRole(Role role) : this()
        {
            RoleId = role.Id;
            RoleName = role.Name;
        }
    }
}
