using MRDb.Domain;
using MRDb.Infrastructure.Interface;

namespace MRDbIdentity.Domain
{
    public class Role : 
        Entity, 
        IEntity
    {
        public string Name { get; set; }

        public Role()
        {

        }

        public Role(string name)
        {
            Name = name.ToUpper();
        }
    }
}
