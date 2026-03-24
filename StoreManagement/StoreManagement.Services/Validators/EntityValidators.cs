using FluentValidation;
using StoreManagement.Shared.DTOs;

namespace StoreManagement.Services.Validators;

/// <summary>
/// التحقق من صحة بيانات إنشاء العميل
/// </summary>
public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        // اسم العميل مطلوب ويجب ألا يقل عن 2 حروف
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم العميل مطلوب")
            .MinimumLength(2).WithMessage("يجب أن يكون الاسم على الأقل حرفين");

        // رقم الهاتف مطلوب عند وجوده
        RuleForEach(x => x.Phones)
            .NotEmpty().WithMessage("رقم الهاتف لا يمكن أن يكون فارغاً")
            .Matches(@"^[0-9\+\-\s]+$").WithMessage("صيغة رقم الهاتف غير صحيحة");
    }
}

/// <summary>
/// التحقق من صحة بيانات إنشاء منتج
/// </summary>
public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المنتج مطلوب")
            .MinimumLength(2).WithMessage("يجب أن يكون الاسم على الأقل حرفين");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("سعر البيع لا يمكن أن يكون سالباً");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر التكلفة لا يمكن أن يكون سالباً");
    }
}

/// <summary>
/// التحقق من صحة بيانات إنشاء فاتورة
/// </summary>
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

        RuleFor(x => x.Paid)
            .GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("يجب إضافة عنصر واحد على الأقل في الفاتورة");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("السعر لا يمكن أن يكون سالباً");
        });
    }
}

/// <summary>
/// التحقق من صحة بيانات تسجيل الدخول
/// </summary>
public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة")
            .MinimumLength(6).WithMessage("كلمة المرور يجب أن تكون على الأقل 6 أحرف");
    }
}

/// <summary>
/// التحقق من صحة بيانات إنشاء الموظف
/// </summary>
public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الموظف مطلوب")
            .MinimumLength(2).WithMessage("يجب أن يكون الاسم على الأقل حرفين");

        RuleFor(x => x.Salary)
            .GreaterThan(0).WithMessage("الراتب يجب أن يكون أكبر من صفر");
    }
}
