using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة الموردين — Thin Controller مستقل
/// يعكس تمامًا بنية CustomersController من حيث الأسلوب المعماري
/// كل Business Logic في ISupplierService
/// كل منطق الرصيد والكشوفات في IFinanceService
/// 
/// ملاحظة: هذا الـ Controller يستبدل SuppliersAndEmployeesController القديم
/// —— SuppliersAndEmployeesController.cs يمكن حذفه بعد التأكد من عدم الاستخدام ——
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly IFinanceService _financeService;

    public SuppliersController(
        ISupplierService supplierService,
        IFinanceService financeService)
    {
        _supplierService = supplierService;
        _financeService = financeService;
    }

    // ============================================================
    // CRUD الأساسي
    // ============================================================

    /// <summary>
    /// GET /api/v1/suppliers
    /// جلب قائمة الموردين مع فلاتر متقدمة:
    /// - search: بحث بالاسم أو الكود أو رقم الهاتف
    /// - isActive: null=الكل | true=نشط | false=معطّل
    /// - hasPayable: الموردون الذين عليهم مبالغ مستحقة
    /// - hasOpenInvoices: الموردون الذين لديهم فواتير مفتوحة
    /// - pageNumber, pageSize: للـ Pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplierReadDto>>>> GetAll(
        [FromQuery] SupplierFilterDto filter)
    {
        var result = await _supplierService.GetAllAsync(filter);
        return Ok(ApiResponse<PagedResult<SupplierReadDto>>.SuccessResult(result));
    }

    /// <summary>
    /// GET /api/v1/suppliers/{id}
    /// جلب مورد واحد بالـ Id مع كل هواتفه
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SupplierReadDto>>> GetById(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);
        if (supplier is null)
            return NotFound(ApiResponse<SupplierReadDto>.Failure("المورد غير موجود"));

        return Ok(ApiResponse<SupplierReadDto>.SuccessResult(supplier));
    }

    /// <summary>
    /// POST /api/v1/suppliers
    /// إنشاء مورد جديد
    /// Body مثال:
    /// {
    ///   "name": "شركة الأمل للتوريدات",
    ///   "code": "S001",
    ///   "address": "الإسكندرية - سيدي بشر",
    ///   "email": "supplier@example.com",
    ///   "notes": "مورد موثوق منذ 2020",
    ///   "openingBalance": 2000.00,
    ///   "phones": [
    ///     { "phoneNumber": "01012345678", "isPrimary": true }
    ///   ]
    /// }
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<SupplierReadDto>>> Create([FromBody] CreateSupplierDto dto)
    {
        var supplier = await _supplierService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id },
            ApiResponse<SupplierReadDto>.SuccessResult(supplier, "تم إضافة المورد بنجاح"));
    }

    /// <summary>
    /// PUT /api/v1/suppliers/{id}
    /// تعديل بيانات مورد (الهواتف تُستبدل بالكامل)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateSupplierDto dto)
    {
        await _supplierService.UpdateAsync(id, dto);
        return Ok(ApiResponse<object>.SuccessResult("تم تعديل بيانات المورد بنجاح"));
    }

    /// <summary>
    /// DELETE /api/v1/suppliers/{id}
    /// حذف آمن — يُرفض إذا كان المورد مرتبطاً بفواتير أو سندات صرف
    /// استخدم PATCH /deactivate إن أردت التعطيل
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _supplierService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResult("تم حذف المورد بنجاح"));
    }

    // ============================================================
    // تفعيل / تعطيل
    // ============================================================

    /// <summary>
    /// PATCH /api/v1/suppliers/{id}/activate
    /// تفعيل مورد معطّل (IsActive = true)
    /// </summary>
    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Activate(int id)
    {
        await _supplierService.ActivateAsync(id);
        return Ok(ApiResponse<object>.SuccessResult("تم تفعيل المورد بنجاح"));
    }

    /// <summary>
    /// PATCH /api/v1/suppliers/{id}/deactivate
    /// تعطيل مورد (IsActive = false)
    /// المورد يبقى في قاعدة البيانات مع كل بياناته التاريخية
    /// </summary>
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(int id)
    {
        await _supplierService.DeactivateAsync(id);
        return Ok(ApiResponse<object>.SuccessResult("تم تعطيل المورد بنجاح"));
    }

    // ============================================================
    // Profile وكشوفات الحساب
    // ============================================================

    /// <summary>
    /// GET /api/v1/suppliers/{id}/profile
    /// ملف المورد الشامل — يعرض:
    /// - البيانات الأساسية (الاسم، الكود، العنوان، الهواتف)
    /// - الرصيد الحالي الحقيقي (محسوب من Invoices + Payments + OpeningBalance)
    /// - إجمالي المشتريات والمدفوعات
    /// - الفواتير المفتوحة (الغير مسداة بالكامل)
    /// - المدفوعات غير المخصصة
    /// - آخر 5 فواتير وآخر 5 سندات صرف
    /// </summary>
    [HttpGet("{id:int}/profile")]
    public async Task<ActionResult<ApiResponse<SupplierProfileDto>>> GetProfile(int id)
    {
        var profile = await _supplierService.GetProfileAsync(id);
        return Ok(ApiResponse<SupplierProfileDto>.SuccessResult(profile));
    }

    /// <summary>
    /// GET /api/v1/suppliers/{id}/statement
    /// كشف حساب المورد مع Pagination وملخص الرصيد
    /// Query params:
    ///   - from / to: الفترة الزمنية
    ///   - pageNumber / pageSize: Pagination (افتراضي 50 سطر)
    /// </summary>
    [HttpGet("{id:int}/statement")]
    public async Task<ActionResult<ApiResponse<StatementPagedResult<SupplierStatementDto>>>> GetStatement(
        int id,
        [FromQuery] StatementQueryDto query)
    {
        var statement = await _financeService.GetSupplierStatementAsync(id, query);
        return Ok(ApiResponse<StatementPagedResult<SupplierStatementDto>>.SuccessResult(statement));
    }

    /// <summary>
    /// GET /api/v1/suppliers/{id}/open-invoices
    /// فواتير الشراء المفتوحة (التي لم تُسدَّد بالكامل) للمورد
    /// مفيد لشاشة التخصيص اليدوي
    /// </summary>
    [HttpGet("{id:int}/open-invoices")]
    public async Task<ActionResult<ApiResponse<List<InvoiceReadDto>>>> GetOpenInvoices(int id)
    {
        var invoices = await _financeService.GetOpenSupplierInvoicesAsync(id);
        return Ok(ApiResponse<List<InvoiceReadDto>>.SuccessResult(invoices));
    }

    /// <summary>
    /// GET /api/v1/suppliers/{id}/unallocated-payments
    /// سندات الصرف غير المخصصة على فواتير (رصيد دائن للتخصيص)
    /// مفيد لشاشة التخصيص اليدوي
    /// </summary>
    [HttpGet("{id:int}/unallocated-payments")]
    public async Task<ActionResult<ApiResponse<List<ReceiptReadDto>>>> GetUnallocatedPayments(int id)
    {
        var payments = await _financeService.GetUnallocatedSupplierPaymentsAsync(id);
        return Ok(ApiResponse<List<ReceiptReadDto>>.SuccessResult(payments));
    }
}
