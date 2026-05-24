using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nimble.Modulith.Reporting.Models;

namespace Nimble.Modulith.Reporting.Data.Config;

public class FactOrderConfig : IEntityTypeConfiguration<FactOrder>
{
    public void Configure(EntityTypeBuilder<FactOrder> builder)
    {
        builder.ToTable("FactOrders");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(f => f.TotalPrice)
            .HasPrecision(18, 2);

        builder.Property(f => f.OrderTotalAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(f => new { f.OrderId, f.OrderItemId })
            .IsUnique();

        builder.HasOne<DimDate>()
            .WithMany()
            .HasForeignKey(f => f.DateKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DimCustomer>()
            .WithMany()
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DimProduct>()
            .WithMany()
            .HasForeignKey(f => f.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
