using FluentValidation;
using StoreManagement.Shared.DTOs;

namespace StoreManagement.Services.Validators;

/// <summary>
/// التحقق من صحة بيانات إنشاء عميل
/// </summary>
public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        // الاسم مطلوب ولا يقل عن حرفين
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم العميل مطلوب")
            .MinimumLength(2).WithMessage("يجب أن يكون الاسم على الأقل حرفين")
            .MaximumLength(200).WithMessage("اسم العميل لا يتجاوز 200 حرف");

        // الكود اختياري لكن إن أُرسل يجب أن يكون قصيراً
        RuleFor(x => x.Code)
            .MaximumLength(30).WithMessage("كود العميل لا يتجاوز 30 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));

        // البريد الإلكتروني اختياري لكن إن أُرسل يجب أن يكون صحيحاً
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        // رصيد الافتتاح لا يكون سالباً (0 مقبول = بداية من الصفر)
        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0).WithMessage("رصيد الافتتاح لا يمكن أن يكون سالباً");

        // الحد الائتماني لا يكون سالباً
        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("الحد الائتماني لا يمكن أن يكون سالباً");

        // التحقق من تنسيق أرقام الهاتف إن أُرسلت
        RuleForEach(x => x.Phones)
            .ChildRules(phone =>
            {
                phone.RuleFor(p => p.PhoneNumber)
                    .NotEmpty().WithMessage("رقم الهاتف لا يمكن أن يكون فارغاً")
                    .Matches(@"^[0-9\+\-\s]+$").WithMessage("صيغة رقم الهاتف غير صحيحة (أرقام و + - فقط)")
                    .MinimumLength(7).WithMessage("رقم الهاتف قصير جداً")
                    .MaximumLength(20).WithMessage("رقم الهاتف طويل جداً");
            });
    }
}

/// <summary>
/// التحقق من صحة بيانات تعديل عميل
/// </summary>
public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم العميل مطلوب")
            .MinimumLength(2).WithMessage("يجب أن يكون الاسم على الأقل حرفين")
            .MaximumLength(200).WithMessage("اسم العميل لا يتجاوز 200 حرف");

        RuleFor(x => x.Code)
            .MaximumLength(30).WithMessage("كود العميل لا يتجاوز 30 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("الحد الائتماني لا يمكن أن يكون سالباً");

        RuleForEach(x => x.Phones)
            .ChildRules(phone =>
            {
                phone.RuleFor(p => p.PhoneNumber)
                    .NotEmpty().WithMessage("رقم الهاتف لا يمكن أن يكون فارغاً")
                    .Matches(@"^[0-9\+\-\s]+$").WithMessage("صيغة رقم الهاتف غير صحيحة")
                    .MinimumLength(7).WithMessage("رقم الهاتف قصير جداً")
                    .MaximumLength(20).WithMessage("رقم الهاتف طويل جداً");
            });
    }
}

/// <summary>
/// التحقق من صحة بيانات إنشاء مورد
/// </summary>
public class CreateSupplierValidator : AbstractValidator<CreateSupplierDto>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المورد مطلوب")
            .MinimumLength(2).WithMessage("يجب أن يكون الاسم على الأقل حرفين")
            .MaximumLength(200).WithMessage("اسم المورد لا يتجاوز 200 حرف");

        RuleFor(x => x.Code)
            .MaximumLength(30).WithMessage("كود المورد لا يتجاوز 30 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0).WithMessage("رصيد الافتتاح لا يمكن أن يكون سالباً");

        RuleForEach(x => x.Phones)
            .ChildRules(phone =>
            {
                phone.RuleFor(p => p.PhoneNumber)
                    .NotEmpty().WithMessage("رقم الهاتف لا يمكن أن يكون فارغاً")
                    .Matches(@"^[0-9\+\-\s]+$").WithMessage("صيغة رقم الهاتف غير صحيحة")
                    .MinimumLength(7).WithMessage("رقم الهاتف قصير جداً")
                    .MaximumLength(20).WithMessage("رقم الهاتف طويل جداً");
            });
    }
}

/// <summary>
/// التحقق من صحة بيانات تعديل مورد
/// </summary>
public class UpdateSupplierValidator : AbstractValidator<UpdateSupplierDto>
{
    public UpdateSupplierValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المورد مطلوب")
            .MinimumLength(2).WithMessage("يجب أن يكون الاسم على الأقل حرفين")
            .MaximumLength(200).WithMessage("اسم المورد لا يتجاوز 200 حرف");

        RuleFor(x => x.Code)
            .MaximumLength(30).WithMessage("كود المورد لا يتجاوز 30 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleForEach(x => x.Phones)
            .ChildRules(phone =>
            {
                phone.RuleFor(p => p.PhoneNumber)
                    .NotEmpty().WithMessage("رقم الهاتف لا يمكن أن يكون فارغاً")
                    .Matches(@"^[0-9\+\-\s]+$").WithMessage("صيغة رقم الهاتف غير صحيحة")
                    .MinimumLength(7).WithMessage("رقم الهاتف قصير جداً")
                    .MaximumLength(20).WithMessage("رقم الهاتف طويل جداً");
            });
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
