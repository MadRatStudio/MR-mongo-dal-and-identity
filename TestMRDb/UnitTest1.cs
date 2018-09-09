using MRDb.Domain;
using MRDb.Infrastructure.Interface;
using MRDb.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TestMRDb
{
    public class UnitTest1
    {


        [Fact]
        public async Task TestCreateEntity()
        {
            var repository = new BaseRepository<TestEntity>("mongodb+srv://ratadmin:ratadmin_()_@madrat-dev-cluster-0onbt.mongodb.net/auth?retryWrites=true", "auth", "test");

            var newElement = new TestEntity
            {
                Var1 = "Some",
                Var2 = 500,
                SomeValues = new List<string>
                {
                    "One",
                    "Two",
                    "Three"
                }
            };


            var result = await repository.Insert(newElement);

            Assert.NotNull(result);
        }


        [Fact]
        public async Task TestGetEntity()
        {
            var repository = new BaseRepository<TestEntity>("mongodb+srv://ratadmin:ratadmin_()_@madrat-dev-cluster-0onbt.mongodb.net/auth?retryWrites=true", "auth", "test");
            var entity = await repository.GetFirst("5b8ad71710c03c2114268479");

            Assert.NotNull(entity);
        }
    }

    public class TestEntity : Entity, IEntity
    {
        public string Var1 { get; set; }
        public int Var2 { get; set; }

        public List<string> SomeValues { get; set; }
    }
}
