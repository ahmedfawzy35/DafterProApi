using AutoMapper;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.HR;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.DTOs.Settings;

namespace StoreManagement.Services.Mappings;

/// <summary>
/// تعريف خرائط التحويل بين الكيانات والـ DTOs باستخدام AutoMapper
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ===== العميل =====
        // ملاحظة: خرائط التحويل تُستخدم بشكل محدود في هذا المشروع — الخدمات تَبني التحويل يدوياً
        // لذا الخرائط هنا هي للتوثيق فقط ولا تُستخدم من Controllers مباشرة
        CreateMap<Customer, CustomerReadDto>()
            .ForMember(dest => dest.Phones,
                opt => opt.MapFrom(src => src.Phones.Select(p => new PhoneDto
                {
                    PhoneNumber = p.PhoneNumber,
                    IsPrimary = p.IsPrimary
                }).ToList()))
            .ForMember(dest => dest.PrimaryPhone,
                opt => opt.MapFrom(src =>
                    src.Phones.FirstOrDefault(p => p.IsPrimary)!.PhoneNumber
                    ?? src.Phones.FirstOrDefault()!.PhoneNumber));

        // ===== المورد =====
        CreateMap<Supplier, SupplierReadDto>()
            .ForMember(dest => dest.Phones,
                opt => opt.MapFrom(src => src.Phones.Select(p => new PhoneDto
                {
                    PhoneNumber = p.PhoneNumber,
                    IsPrimary = p.IsPrimary
                }).ToList()))
            .ForMember(dest => dest.PrimaryPhone,
                opt => opt.MapFrom(src =>
                    src.Phones.FirstOrDefault(p => p.IsPrimary)!.PhoneNumber
                    ?? src.Phones.FirstOrDefault()!.PhoneNumber));

        // ===== المنتج =====
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();
        CreateMap<Product, ProductReadDto>()
            .ForMember(dest => dest.ThumbnailUrl,
                opt => opt.MapFrom(src => src.ProductImages
                    .FirstOrDefault(i => i.IsThumbnail)!.ImageUrl));

        // ===== الفاتورة =====
        CreateMap<Invoice, InvoiceReadDto>()
            .ForMember(dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : null))
            .ForMember(dest => dest.SupplierName,
                opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : null))
            .ForMember(dest => dest.InvoiceType,
                opt => opt.MapFrom(src => src.Type.ToString()));

        CreateMap<InvoiceItem, InvoiceItemReadDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.Subtotal,
                opt => opt.MapFrom(src => (decimal)src.Quantity * src.UnitPrice));

        // ===== الموظف =====
        CreateMap<CreateEmployeeDto, Employee>();
        CreateMap<UpdateEmployeeDto, Employee>();
        CreateMap<Employee, EmployeeReadDto>();

        // ===== الحضور =====
        CreateMap<AttendanceCreateDto, Attendance>();
        CreateMap<Attendance, AttendanceReadDto>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.Name))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // ===== الرواتب =====
        CreateMap<PayrollRun, PayrollRunReadDto>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.Name));
        
        CreateMap<PayrollRun, PayrollRunDetailsDto>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.Name));

        CreateMap<PayrollRunItem, PayrollRunItemReadDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

        // ===== القروض =====
        CreateMap<EmployeeLoan, LoanReadDto>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.Name))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.RemainingAmount, 
                opt => opt.MapFrom(src => src.Installments.Where(i => !i.IsPaid).Sum(i => i.Amount)));

        // ===== إعدادات الشركة =====
        CreateMap<CompanySettings, CompanySettingsDto>();
        CreateMap<CompanySettings, SettingsSnapshotDto>();
        CreateMap<UpdateSalesSettingsDto, CompanySettings>();
        CreateMap<UpdateInventorySettingsDto, CompanySettings>();
        CreateMap<UpdateReturnsSettingsDto, CompanySettings>();
        CreateMap<UpdateInstallmentsSettingsDto, CompanySettings>();
        CreateMap<UpdateApprovalsSettingsDto, CompanySettings>();
    }
}
