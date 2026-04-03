using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة العملاء — Thin Controller
/// كل Business Logic في ICustomerService
/// كل منطق الرصيد والكشوفات في IFinanceService (يُستدعى من CustomerService)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IFinanceService _financeService;
    private readonly IAuditLogService _auditLogService;

    public CustomersController(
        ICustomerService customerService,
        IFinanceService financeService,
        IAuditLogService auditLogService)
    {
        _customerService = customerService;
        _financeService = financeService;
        _auditLogService = auditLogService;
    }

    // ============================================================
    // CRUD الأساسي
    // ============================================================

    /// <summary>
    /// GET /api/v1/customers
    /// جلب قائمة العملاء مع فلاتر متقدمة:
    /// - search: بحث بالاسم أو الكود أو رقم الهاتف
    /// - isActive: null=الكل | true=نشط | false=معطّل
    /// - hasDebt: العملاء الذين عليهم رصيد مستحق
    /// - hasOpenInvoices: العملاء الذين لديهم فواتير مفتوحة
    /// - pageNumber, pageSize: للـ Pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerReadDto>>>> GetAll(
        [FromQuery] CustomerFilterDto filter)
    {
        var result = await _customerService.GetAllAsync(filter);
        return Ok(ApiResponse<PagedResult<CustomerReadDto>>.SuccessResult(result));
    }

    /// <summary>
    /// GET /api/v1/customers/{id}
    /// جلب عميل واحد بالـ Id مع كل هواتفه
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CustomerReadDto>>> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer is null)
            return NotFound(ApiResponse<CustomerReadDto>.Failure("العميل غير موجود"));

        return Ok(ApiResponse<CustomerReadDto>.SuccessResult(customer));
    }

    /// <summary>
    /// POST /api/v1/customers
    /// إنشاء عميل جديد
    /// Body مثال:
    /// {
    ///   "name": "محمد أحمد",
    ///   "code": "C001",
    ///   "address": "القاهرة - مصر الجديدة",
    ///   "email": "customer@example.com",
    ///   "notes": "عميل VIP",
    ///   "openingBalance": 500.00,
    ///   "creditLimit": 5000.00,
    ///   "phones": [
    ///     { "phoneNumber": "01012345678", "isPrimary": true },
    ///     { "phoneNumber": "01198765432", "isPrimary": false }
    ///   ]
    /// }
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Accountant,Sales")]
    public async Task<ActionResult<ApiResponse<CustomerReadDto>>> Create([FromBody] CreateCustomerDto dto)
    {
        var customer = await _customerService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id },
            ApiResponse<CustomerReadDto>.SuccessResult(customer, "تم إضافة العميل بنجاح"));
    }

    /// <summary>
    /// PUT /api/v1/customers/{id}
    /// تعديل بيانات عميل (الهواتف تُستبدل بالكامل)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        await _customerService.UpdateAsync(id, dto);
        return Ok(ApiResponse<object>.SuccessResult("تم تعديل بيانات العميل بنجاح"));
    }

    /// <summary>
    /// DELETE /api/v1/customers/{id}
    /// حذف آمن — يُرفض إذا كان العميل مرتبطاً بفواتير أو سندات قبض
    /// استخدم PATCH /deactivate إن أردت التعطيل بدلاً من الحذف
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResult("تم حذف العميل بنجاح"));
    }

    // ============================================================
    // تفعيل / تعطيل
    // ============================================================

    /// <summary>
    /// PATCH /api/v1/customers/{id}/activate
    /// تفعيل عميل معطّل (IsActive = true)
    /// يعمل حتى على العملاء الذين IsActive = false (لا يُطبَّق filter عليهم)
    /// </summary>
    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Activate(int id)
    {
        await _customerService.ActivateAsync(id);
        return Ok(ApiResponse<object>.SuccessResult("تم تفعيل العميل بنجاح"));
    }

    /// <summary>
    /// PATCH /api/v1/customers/{id}/deactivate
    /// تعطيل عميل (IsActive = false)
    /// العميل يبقى في قاعدة البيانات ويمكن استعادته لاحقاً
    /// لا يؤثر على البيانات التاريخية (الفواتير والسندات تبقى)
    /// </summary>
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(int id)
    {
        await _customerService.DeactivateAsync(id);
        return Ok(ApiResponse<object>.SuccessResult("تم تعطيل العميل بنجاح"));
    }

    // ============================================================
    // Profile وكشوفات الحساب
    // ============================================================

    /// <summary>
    /// GET /api/v1/customers/{id}/profile
    /// ملف العميل الشامل — يعرض:
    /// - البيانات الأساسية (الاسم، الكود، العنوان، الهواتف)
    /// - الرصيد الحالي الحقيقي (محسوب من Invoices + Receipts + OpeningBalance)
    /// - إجمالي الفواتير والمقبوضات
    /// - الفواتير المفتوحة وعددها
    /// - المقبوضات غير المخصصة
    /// - هل تجاوز الحد الائتماني؟
    /// - آخر 5 فواتير وآخر 5 سندات قبض
    /// </summary>
    [HttpGet("{id:int}/profile")]
    public async Task<ActionResult<ApiResponse<CustomerProfileDto>>> GetProfile(int id)
    {
        var profile = await _customerService.GetProfileAsync(id);
        return Ok(ApiResponse<CustomerProfileDto>.SuccessResult(profile));
    }

    /// <summary>
    /// GET /api/v1/customers/{id}/statement
    /// كشف حساب العميل التفصيلي مع Pagination وملخص الرصيد
    /// Query params:
    ///   - from: تاريخ البداية (null = آخر 90 يوماً)
    ///   - to: تاريخ النهاية (null = اليوم)
    ///   - pageNumber: رقم الصفحة (افتراضي 1)
    ///   - pageSize: عدد السطور (افتراضي 50، أقصى 500)
    /// 
    /// الاستجابة تحتوي على:
    ///   - items[]: سطور الكشف (DocumentType, Date, Debit, Credit, Balance)
    ///   - openingBalance: رصيد الافتتاح للفترة
    ///   - totalDebit / totalCredit: إجمالي الفترة
    ///   - closingBalance: الرصيد الختامي
    ///   - pagination: totalCount, totalPages, hasNextPage...
    /// </summary>
    [HttpGet("{id:int}/statement")]
    public async Task<ActionResult<ApiResponse<StatementPagedResult<CustomerStatementDto>>>> GetStatement(
        int id,
        [FromQuery] StatementQueryDto query)
    {
        var statement = await _financeService.GetCustomerStatementAsync(id, query);
        return Ok(ApiResponse<StatementPagedResult<CustomerStatementDto>>.SuccessResult(statement));
    }

    /// <summary>
    /// GET /api/v1/customers/{id}/open-invoices
    /// فواتير البيع المفتوحة (التي لم تُدفع بالكامل) للعميل
    /// مفيد لشاشة التخصيص اليدوي وشاشة الفواتير المتأخرة
    /// </summary>
    [HttpGet("{id:int}/open-invoices")]
    public async Task<ActionResult<ApiResponse<List<InvoiceReadDto>>>> GetOpenInvoices(int id)
    {
        var invoices = await _financeService.GetOpenCustomerInvoicesAsync(id);
        return Ok(ApiResponse<List<InvoiceReadDto>>.SuccessResult(invoices));
    }

    /// <summary>
    /// GET /api/v1/customers/{id}/unallocated-receipts
    /// سندات القبض غير المخصصة على فواتير (رصيد دائن متاح للتخصيص)
    /// مفيد لشاشة التخصيص اليدوي
    /// </summary>
    [HttpGet("{id:int}/unallocated-receipts")]
    public async Task<ActionResult<ApiResponse<List<ReceiptReadDto>>>> GetUnallocatedReceipts(int id)
    {
        var receipts = await _financeService.GetUnallocatedCustomerReceiptsAsync(id);
        return Ok(ApiResponse<List<ReceiptReadDto>>.SuccessResult(receipts));
    }

    /// <summary>
    /// GET /api/v1/customers/{id}/history
    /// جلب سجل التاريخ (Audit Logs) للعميل
    /// </summary>
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogReadDto>>>> GetHistory(
        int id, [FromQuery] PaginationQueryDto query)
    {
        var result = await _auditLogService.GetAllAsync(query, entityName: "Customer", entityId: id.ToString());
        return Ok(ApiResponse<PagedResult<AuditLogReadDto>>.SuccessResult(result));
    }
}
