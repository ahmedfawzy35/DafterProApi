using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.IntegrationTests.Helpers;
using System.IO;

namespace StoreManagement.IntegrationTests
{
    public class ScratchTest
    {
        [Xunit.Fact]
        public void PrintSchema()
        {
            var factory = new StoreManagementApiFactory();
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            var script = context.Database.GenerateCreateScript();
            File.WriteAllText("schema.sql", script);
        }
    }
}
