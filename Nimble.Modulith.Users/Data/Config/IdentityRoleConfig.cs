using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nimble.Modulith.Users.Data.Config;

public class IdentityRoleConfig : IEntityTypeConfiguration<IdentityRole>
{
    private const string AdminRoleId = "5f7c45ad-7f16-4c29-b1cc-83a30f0b9151";
    private const string CustomerRoleId = "f4a730e8-3f47-40ce-a15a-7a40debef6a7";
    private const string AdminConcurrencyStamp = "9ce2b8c8-98cb-4ee9-8f57-984825ef5dc0";
    private const string CustomerConcurrencyStamp = "22198b8c-0bc5-4c9b-94fb-e56d9495b4e5";

    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        // Seed Admin and Customer roles
        builder.HasData(
            new IdentityRole
            {
                Id = AdminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = AdminConcurrencyStamp
            },
            new IdentityRole
            {
                Id = CustomerRoleId,
                Name = "Customer",
                NormalizedName = "CUSTOMER",
                ConcurrencyStamp = CustomerConcurrencyStamp
            }
        );
    }
}
