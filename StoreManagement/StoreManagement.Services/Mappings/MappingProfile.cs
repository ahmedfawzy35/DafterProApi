using AutoMapper;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;

namespace StoreManagement.Services.Mappings;

/// <summary>
/// تعريف خرائط التحويل بين الكيانات والـ DTOs باستخدام AutoMapper
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ===== العميل =====
        CreateMap<CreateCustomerDto, Customer>();
        CreateMap<UpdateCustomerDto, Customer>();
        CreateMap<Customer, CustomerReadDto>()
            .ForMember(dest => dest.Phones,
                opt => opt.MapFrom(src => src.Phones.Select(p => p.PhoneNumber).ToList()));

        // ===== المورد =====
        CreateMap<CreateSupplierDto, Supplier>();
        CreateMap<UpdateSupplierDto, Supplier>();
        CreateMap<Supplier, SupplierReadDto>()
            .ForMember(dest => dest.Phones,
                opt => opt.MapFrom(src => src.Phones.Select(p => p.PhoneNumber).ToList()));

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
                opt => opt.MapFrom(src => src.Quantity * src.UnitPrice));

        // ===== الموظف =====
        CreateMap<CreateEmployeeDto, Employee>();
        CreateMap<UpdateEmployeeDto, Employee>();
        CreateMap<Employee, EmployeeReadDto>();
    }
}
