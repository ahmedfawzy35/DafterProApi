using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreManagement.Shared.Entities.Sales.Installments;

namespace StoreManagement.Data.Configurations.Installments;

public class InstallmentPlanConfiguration : IEntityTypeConfiguration<InstallmentPlan>
{
    public void Configure(EntityTypeBuilder<InstallmentPlan> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,2)");
            
        builder.Property(x => x.DownPayment)
            .HasColumnType("decimal(18,2)");
            
        builder.Property(x => x.RemainingAmount)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Invoice)
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft Delete filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class InstallmentScheduleItemConfiguration : IEntityTypeConfiguration<InstallmentScheduleItem>
{
    public void Configure(EntityTypeBuilder<InstallmentScheduleItem> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.PaidAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.PenaltyAmount)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.InstallmentPlan)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.InstallmentPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft Delete filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class InstallmentPaymentAllocationConfiguration : IEntityTypeConfiguration<InstallmentPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<InstallmentPaymentAllocation> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.AmountAllocated)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.PenaltyAllocated)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.InstallmentScheduleItem)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.InstallmentScheduleItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CustomerReceipt)
            .WithMany()  // Assuming CustomerReceipt doesn't have a direct nav property for installments allocations to avoid circular
            .HasForeignKey(x => x.CustomerReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft Delete filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class InstallmentRescheduleHistoryConfiguration : IEntityTypeConfiguration<InstallmentRescheduleHistory>
{
    public void Configure(EntityTypeBuilder<InstallmentRescheduleHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OldScheduleSnapshotJson)
            .HasMaxLength(int.MaxValue);

        builder.HasOne(x => x.InstallmentPlan)
            .WithMany(x => x.RescheduleHistories)
            .HasForeignKey(x => x.InstallmentPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft Delete filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
