using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreManagement.Shared.Entities.Configuration;

namespace StoreManagement.Data.Configurations.Settings;

public class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable("CompanySettings");

        // 1. مسار علاقة الشركة ومنع التكرار (Unique Constraint)
        builder.HasIndex(x => x.CompanyId).IsUnique();

        builder.HasOne(x => x.Company)
               .WithOne()
               .HasForeignKey<CompanySettings>(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Cascade);

        // 2. دقة الأرقام العشرية (Decimal Precisions) لتجنب أخطاء التقريب المحاسبية
        builder.Property(x => x.MaxDiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.DefaultLateFeeAmount).HasPrecision(18, 2);
        builder.Property(x => x.ExpenseApprovalThreshold).HasPrecision(18, 2);
        
        // 3. الفلتر العام للحذف الوهمي (Global Query Filter for Soft Delete)
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        // 4. (اختياري مستقبلا) إذا احتجت لفصل الفروع يمكن وضع Index عليها:
        // builder.HasIndex(x => x.DefaultBranchId);
    }
}
