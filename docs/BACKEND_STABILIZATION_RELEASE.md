# حزمة استقرار Salesync Backend وعقود Flutter

21 أكتوبر 2026 — تم التنفيذ محليًا. لم يحدث نشر أو اتصال بقاعدة SQL Server أو تطبيق migration أو Commit/Push. لا يشمل العمل Offline-first أو Sync Queue. يظل اختبار SQL Server المعزول بوابة مطلوبة قبل الإنتاج.

## ما تغير

- نطاق وصول موحد من هوية المستخدم وعلاقات قاعدة البيانات: المندوب لنفسه، المشرف لفريقه الحالي، المخزن والخزينة لفرعهما ووحدة الأعمال عند وجودها. القوائم تُقيَّد قبل الإرجاع والعد، والتفاصيل والأوامر تتحقق من النطاق داخل الخدمات العامة والموبايل.
- شمل التقييد الفواتير والتحصيل والمرتجعات والجلسات والتسويات والزيارات والتحميل والتفريغ، وقراءات الخزائن ودفتر العهدة والمديونيات وسداد عهدة المندوب.
- الإقفال يجمع حفظ الجلسة وإنشاء مستند التسوية في معاملة واحدة. الجلسة المغلقة ذات المستند القائم ترجع الجلسة دون إنشاء آخر. يمكن لمسار الإقفال استكمال جلسة مغلقة بلا تسوية بعد إعادة فحص الشروط.
- اليوم بلا بضاعة يُقفل باختيار مخزن مصرح به، دون إنشاء طلب تفريغ صوري. التفريغ الفعلي يحدد المخزن عند وجوده؛ اختلاف المخزن أو تعدد مخازن التفريغ يمنع الإقفال.
- الزيارة المفتوحة والمخزون غير الصفري وطلبات التفريغ المعلقة/المسودات تمنع الإقفال في السيرفر. لا يمكن بدء جلسة أخرى مع جلسة سابقة مفتوحة.
- يسمح التحصيل بعد تسوية المخزون ما دامت جلسة التحصيل مفتوحة والخزينة غير مسواة. تحصيل فاتورة قديمة يُسجل على جلسة التحصيل الحالية.
- المسار القديم لاستلام النقدية يطلب CashBoxId ويستدعي تنفيذ الخزينة الكامل: إيصال وقيد ورصيد خزنة وفرق عهدة وحالة تسوية وجلسة، في معاملة واحدة. لا اختيار تلقائي للخزنة.
- إنشاء فاتورة الموبايل وتأكيدها وربطها بالزيارة في معاملة واحدة. تحديث الفاتورة العام يرفض تبديل الحالة مباشرة؛ التأكيد والإلغاء عبر عملياتهما فقط. لا تأكيد فاتورة على جلسة مغلقة أو مسواة.
- إدارة المعاملات الداخلية تشارك معاملة المستدعي؛ Commit داخلي لا يثبت جزءًا من العملية الخارجية. تبدأ الأوامر المغلفة بمعاملة Serializable قبل قراءة الأرصدة والشروط. لا إعادة تلقائية عند deadlock أو فشل مالي غامض.
- Idempotency-Key اختياري للتوافق مع العملاء القدماء. المفتاح UUID مرتبط بالمستخدم وبصمة الأمر؛ نفس المفتاح والبيانات يرجع النتيجة المحفوظة، وتغيّر البيانات يرجع Conflict. نتيجة الأمر وسجلها يُحفظان داخل المعاملة نفسها. التفويض يُعاد قبل إعادة النتيجة.
- Flutter يحفظ مفتاح الأمر قبل إرسال الفاتورة والتحصيل والإقفال، ويستعلم عن الحالة والمستندات عند الغموض. لا يعيد POST تلقائيًا ولا يحول 404 لعملية جديدة. السجل المحلي دليل سلامة فقط، بلا طابور إرسال.
- تقارير التحصيل تقرأ كل تحصيلات الجلسة، بما فيها الفواتير القديمة، بإجمالي نقدي وغير نقدي من السيرفر. الأرشيف يستخدم سجل جلسات الموبايل بصفحات حقيقية. متابعة المشرف تستخدم طلبًا واحدًا لكل صفحة لجميع أنواع الحالات، وتشمل المرتجعات.
- تاريخ العمل وحدود تقارير التاريخ يستخدمان منطقة الشركة، افتراضيًا Africa/Cairo. الإعداد: Sales:BusinessTimeZone أو متغير البيئة Sales__BusinessTimeZone. يدعم بداية اليوم وقت الانتقال الصيفي والتوقيت المكرر؛ لا إعادة تأريخ للسجلات القديمة.
- لم تتغير سياسة احتساب ائتمان المرتجع أو حدود ائتمان العملاء. لا إعادة تسوية تاريخية أو تصحيح تلقائي لأرصدة.

## العقود الجديدة

كل الاستجابات داخل ApiResponse المعتاد: success/message/data/errors/statusCode. تعريفات الاستجابة typed في Controllers/DTOs ليعرضها Swagger الموجود عند التشغيل في بيئة اختبار مصرح بها. لم يُشغل Host لتوليد نسخة Swagger حية لأن Program يشغل seeders عند البدء.

| Method / المسار | الدور | الفلاتر والنتيجة |
|---|---|---|
| GET api/mobile/salesrep/sessions | SalesRep | fromDate, toDate, pageNumber=1, pageSize=20؛ سجل المندوب الحالي فقط |
| GET api/mobile/salesrep/payments | SalesRep | sessionId?, fromDate?, toDate?, pageNumber, pageSize؛ paid فقط؛ totals للفترة الكاملة |
| GET api/mobile/salesrep/day/{sessionId}/review | SalesRep | warehouseId?؛ session/checks/canClose/warehouseId/warehouses/settlement/financialSummary/checkedAt |
| GET api/mobile/supervisor/attention | Supervisor | date?, kind? (load/return/closing/visit), pageNumber, pageSize؛ counts لكل الأنواع قبل تصفية النوع والصفحة |
| GET api/mobile/operations/{operationKey} | أدوار العمليات الحالية | إيصال للمستخدم المنشئ فقط: operationKey/status=Succeeded/resourceId/operation/completedAt؛ غير المرصود 404 لا يعني فشلًا مؤكدًا |

الصفحات: items/totalCount/pageNumber/pageSize/totalPages. pageSize من 1 إلى 100. إجماليات الدفع totalAmount/cashAmount/nonCashAmount مستقلة عن الصفحة. التاريخ fromDate/toDate شامل ليوم العمل؛ داخليًا يستخدم مجال UTC نصف مفتوح. زيارات attention تخص يوم العمل المختار؛ بقية المعلقات حتى نهايته وتشمل الأيام السابقة.

مثال data لتقرير التحصيل (قيم توضيحية للعقد، لا بيانات حقيقية):

```json
{
  "items": [],
  "totalCount": 0, "pageNumber": 1, "pageSize": 20, "totalPages": 0,
  "totals": { "totalAmount": 0, "cashAmount": 0, "nonCashAmount": 0 }
}
```

الإقفال القائم: PUT api/mobile/salesrep/day/{sessionId}/close، body اختياري {"warehouseId":1}. يبقى الطلب بلا body متوافقًا عند تحديد المخزن من التفريغ الفعلي. مراجعة القراءة مبدئية؛ الأمر يعيد الفحص داخل المعاملة.

الاستلام القديم: PUT api/SalesRepDayClosing/{id}/receive-cash يقبل CashBoxId مع ActualCashAmount/Notes. غياب الخزنة يُرفض قبل الكتابة. المسار الكامل يبقى PUT api/treasury/day-closing/{id}/receive-cash بعقده الحالي.

لا ضمان لاستعادة أمر قديم لم يرسل مفتاحًا. سجل OperationExecutions يمثل نتائج مكتملة فقط؛ العملية أثناء التنفيذ قد لا تظهر حتى Commit. لا تستخدم مفتاحًا جديدًا لأن الطلب الأول انتهت مهلته. الأخطاء التجارية الحالية تستخدم ApiResponse وHTTP 400/401/403/404/409؛ بعض الرسائل تحمل بادئة مثل OPEN_VISITS أو WAREHOUSE_REQUIRED أو IDEMPOTENCY_CONFLICT، وليست حقل errorCode جديدًا.

## التحقق المنفذ

- dotnet build Salesync.API.sln --no-restore: ناجح؛ تحذيرات nullable سابقة في Controllers (آخر بناء أعاد 9 تحذيرات، دون أخطاء).
- dotnet test Salesync.API.sln --no-restore: 34 اختبارًا ناجحًا، دون تخطي.
- flutter analyze: دون ملاحظات.
- flutter test: 179 اختبارًا ناجحًا.
- flutter build apk --debug: ناجح. Android/Gradle عرض تحذيرات بيئية عن إصدار SDK XML وJava native access.

اختبارات Backend تستخدم SQLite مؤقتة مستقلة داخل الذاكرة، مع العلاقات والفهارس الفريدة الفعلية وتكييف أنواع الأعمدة فقط لتوافق المحرك. لا تستخدم Host أو ملفات إعدادات Production. تشمل النطاقات، إعادة فحص فريق المشرف عند إعادة الإيصال، البصمة، إعادة المفتاح وتعارضه، فشل بعد Commit داخلي، فشل ربط الفاتورة بعد التأكيد، فشل إنشاء التسوية بعد حفظ الجلسة، فشل قيد الخزينة بعد حفظ الإيصال، الإقفال دون تفريغ، استعادة جلسة قديمة، التحصيل بعد تسوية البضاعة، المجاميع والصفحات، حدود Cairo الصيفية، ورفض تبديل حالة الفاتورة من Update.

اختبارات Flutter كلها Mocks للمال والشبكة. تحافظ على قفل العمليات الغامضة وتحقق النتيجة بلا إعادة إرسال، وعلى pagination وتفاصيل التحصيلات القديمة، والتنقل وRTL والأحجام الصغيرة والطباعة القائمة.

## Migration والنشر

Migration المضافة 20261021082158_AddOperationExecutionReceipts تنشئ جدول OperationExecutions وفهرس ActorId+OperationKey الفريد فقط. لم تُغير migrations قديمة؛ snapshot محدث. قيود التسوية والإيصال والمخزون الفريدة الموجودة بقيت مستخدمة.

الملف SQL المرفق للمراجعة فقط، ومولد كفرق idempotent من AddCompanyProfile إلى migration الجديدة. يتطلب أن تكون القاعدة أصلًا على migration السابقة؛ ليس سكربت تأسيس قاعدة فارغة. لم يُنفذ. MigrationDesignTimeFactory يستخدم اتصالًا محليًا غير صالح للوصول إلى قاعدة فعلية، ولا يقرأ إعدادات Production ولا يبدأ seeders.

ترتيب النشر بعد الإذن والاختبارات:

1. مراجعة السكربت وفهارس القاعدة الحالية وحالات التسوية التاريخية على نسخة اختبار معزولة.
2. تطبيق migration على قاعدة الاختبار فقط، ثم نشر API هناك. Program الحالي يشغل seeders؛ يجب أن تشير إعدادات الاختبار إلى القاعدة المعزولة.
3. اختبار التزامن بطلبين/اتصالين مستقلين لنفس المفتاح، وفاتورة/تحصيل برصيد محدود، وتحميل/تفريغ على نفس الصنف، واستلام نقدية وسداد عهدة على نفس الخزنة. تحقق من ناتج ملتزم واحد، وعدم سلبية الرصيد/تجاوز المستحق، والتراجع الكامل عند فشل الحقن وdeadlock.
4. تجربة فعلية بأدوار مندوبين/مشرفين من فريقين وفرعين، وخزينة ومخزن؛ اختبر 401/403/404 على HTTP، وSwagger وعقود PWA/BackOffice القائمة، ومسار النقدية القديم بعد إضافة CashBoxId. لا تعديل للواجهتين ضمن هذه الحزمة.
5. نشر Flutter بعد API وmigration؛ النسخة الجديدة تعتمد على العقود الجديدة. تشغيلها على API القديم سيظهر فشل تحميل واضحًا، ولا fallback يولد totals أو جاهزية مالية غير موثقة.
6. اختبار Bluetooth 80mm والمشاركة فعليًا على هاتف وطابعة، والأرشيف لكل الأنواع، واختيار مخزن يوم بلا بضاعة، واستعادة الغموض بعد إغلاق التطبيق وتبدل يوم العمل.
7. أخذ الموافقة المنفصلة قبل أي migration/نشر على قاعدة فعلية. لا يعتمد نجاح SQLite دليلًا على أقفال SQL Server أو خطط استعلامه أو أداء attention على بيانات كبيرة.

العودة لإصدار أقدم من التطبيق ممكن ضمن العقود القديمة، لكن لا تُحذف سجلات منع التكرار لمجرد الرجوع. لا Down migration أثناء طلبات قيد التنفيذ؛ أي عودة بيانات تتطلب مراجعة مستقلة.

## الملفات الخاصة بهذه الحزمة

القائمة التالية تستبعد تعديلات المستخدم السابقة على Production وCompany/Identity والـbranding وZIP، وتستبعد تغييرات تصميم Flutter السابقة. الملفات ذات التعديلات السابقة والمتداخلة (Program وSalesRepMobileService) حوفظ عليها وأضيفت التغييرات الحالية إليها. لا ملفات .bak أو Logs أو ZIP أضيفت إلى Git ولا أي staging.

### Backend

- [Salesync.API.sln](<D:/My Live Projects/Salesync/Salesync.API/Salesync.API.sln>)
- [Salesync.API/Controllers/Mobile/MobileOperationsController.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.API/Controllers/Mobile/MobileOperationsController.cs>)
- [Salesync.API/Controllers/Mobile/MobileWorkflowController.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.API/Controllers/Mobile/MobileWorkflowController.cs>)
- [Salesync.API/Controllers/Mobile/SalesRepMobileController.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.API/Controllers/Mobile/SalesRepMobileController.cs>)
- [Salesync.API/Controllers/Sales/SalesRepSessionController.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.API/Controllers/Sales/SalesRepSessionController.cs>)
- [Salesync.API/Filters/FinancialOperationFilter.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.API/Filters/FinancialOperationFilter.cs>)
- [Salesync.API/MigrationDesignTimeFactory.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.API/MigrationDesignTimeFactory.cs>)
- [Salesync.API/Program.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.API/Program.cs>)
- [Salesync.Application/DependencyInjection.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/DependencyInjection.cs>)
- [Salesync.Application/Interfaces/Repositories/IUnitOfWork.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Interfaces/Repositories/IUnitOfWork.cs>)
- [Salesync.Application/Interfaces/Services/IBusinessClock.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Interfaces/Services/IBusinessClock.cs>)
- [Salesync.Application/Interfaces/Services/ISalesAccessScopeService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Interfaces/Services/ISalesAccessScopeService.cs>)
- [Salesync.Application/Modules/CustomerVisit/Services/CustomerVisitService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/CustomerVisit/Services/CustomerVisitService.cs>)
- [Salesync.Application/Modules/LoadRequest/Services/LoadRequestService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/LoadRequest/Services/LoadRequestService.cs>)
- [Salesync.Application/Modules/Sales/Interfaces/ISalesRepSessionService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Sales/Interfaces/ISalesRepSessionService.cs>)
- [Salesync.Application/Modules/Sales/Services/InvoiceReturnService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Sales/Services/InvoiceReturnService.cs>)
- [Salesync.Application/Modules/Sales/Services/InvoiceService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Sales/Services/InvoiceService.cs>)
- [Salesync.Application/Modules/Sales/Services/PaymentService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Sales/Services/PaymentService.cs>)
- [Salesync.Application/Modules/Sales/Services/SalesRepDayClosingService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Sales/Services/SalesRepDayClosingService.cs>)
- [Salesync.Application/Modules/Sales/Services/SalesRepSessionService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Sales/Services/SalesRepSessionService.cs>)
- [Salesync.Application/Modules/SalesRep/Dtos/Mobile/CloseSalesRepMobileDayDto.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/SalesRep/Dtos/Mobile/CloseSalesRepMobileDayDto.cs>)
- [Salesync.Application/Modules/SalesRep/Dtos/Mobile/MobileReviewDto.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/SalesRep/Dtos/Mobile/MobileReviewDto.cs>)
- [Salesync.Application/Modules/SalesRep/Interfaces/Services/ISalesRepMobileService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/SalesRep/Interfaces/Services/ISalesRepMobileService.cs>)
- [Salesync.Application/Modules/SalesRep/Services/MobileWorkflowQueryService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/SalesRep/Services/MobileWorkflowQueryService.cs>)
- [Salesync.Application/Modules/SalesRep/Services/SalesRepMobileService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/SalesRep/Services/SalesRepMobileService.cs>)
- [Salesync.Application/Modules/Treasury/Services/ReceivablesService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Treasury/Services/ReceivablesService.cs>)
- [Salesync.Application/Modules/Treasury/Services/TreasuryService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Treasury/Services/TreasuryService.cs>)
- [Salesync.Application/Modules/UnloadRequest/Services/SalesRepUnloadRequestService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/UnloadRequest/Services/SalesRepUnloadRequestService.cs>)
- [Salesync.Application/Services/BusinessClock.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Services/BusinessClock.cs>)
- [Salesync.Application/Services/OperationFingerprint.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Services/OperationFingerprint.cs>)
- [Salesync.Application/Services/SalesAccessScopeService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Services/SalesAccessScopeService.cs>)
- [Salesync.Domain/Common/Enums/Sales/SalesRepDayClosing/ReceiveSalesRepDayClosingCashDto.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Domain/Common/Enums/Sales/SalesRepDayClosing/ReceiveSalesRepDayClosingCashDto.cs>)
- [Salesync.Domain/Modules/Sales/Entities/OperationExecution.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Domain/Modules/Sales/Entities/OperationExecution.cs>)
- [Salesync.Infrastructure/Configurations/Sales/OperationExecutionConfiguration.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Infrastructure/Configurations/Sales/OperationExecutionConfiguration.cs>)
- [Salesync.Infrastructure/DependencyInjection/DependencyInjection.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Infrastructure/DependencyInjection/DependencyInjection.cs>)
- [Salesync.Infrastructure/Migrations/20261021082158_AddOperationExecutionReceipts.Designer.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Infrastructure/Migrations/20261021082158_AddOperationExecutionReceipts.Designer.cs>)
- [Salesync.Infrastructure/Migrations/20261021082158_AddOperationExecutionReceipts.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Infrastructure/Migrations/20261021082158_AddOperationExecutionReceipts.cs>)
- [Salesync.Infrastructure/Migrations/SalesyncDbContextModelSnapshot.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Infrastructure/Migrations/SalesyncDbContextModelSnapshot.cs>)
- [Salesync.Infrastructure/Repositories/Common/UnitOfWork.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Infrastructure/Repositories/Common/UnitOfWork.cs>)
- [Salesync.Tests/FailureRecoveryTests.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Tests/FailureRecoveryTests.cs>)
- [Salesync.Tests/OperationTests.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Tests/OperationTests.cs>)
- [Salesync.Tests/QueryAndScopeTests.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Tests/QueryAndScopeTests.cs>)
- [Salesync.Tests/Salesync.Tests.csproj](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Tests/Salesync.Tests.csproj>)
- [Salesync.Tests/WorkflowFixture.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Tests/WorkflowFixture.cs>)
- [Salesync.Tests/WorkflowTests.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Tests/WorkflowTests.cs>)
- [docs/BACKEND_STABILIZATION_RELEASE.md](<D:/My Live Projects/Salesync/Salesync.API/docs/BACKEND_STABILIZATION_RELEASE.md>)
- [docs/migrations/operation-executions-review.sql](<D:/My Live Projects/Salesync/Salesync.API/docs/migrations/operation-executions-review.sql>)

### Flutter

- [lib/core/network/financial_submission_guard.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/core/network/financial_submission_guard.dart>)
- [lib/core/network/server_page.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/core/network/server_page.dart>)
- [lib/features/invoices/data/repositories/invoice_repository.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/invoices/data/repositories/invoice_repository.dart>)
- [lib/features/collections/data/repositories/collection_repository.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/collections/data/repositories/collection_repository.dart>)
- [lib/features/reports/data/repositories/document_archive_repository.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/reports/data/repositories/document_archive_repository.dart>)
- [lib/features/reports/data/repositories/end_day_review_repository.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/reports/data/repositories/end_day_review_repository.dart>)
- [lib/features/reports/data/repositories/salesrep_reports_repository.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/reports/data/repositories/salesrep_reports_repository.dart>)
- [lib/features/reports/data/session_collection_audit.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/reports/data/session_collection_audit.dart>)
- [lib/features/reports/presentation/pages/end_day_review_page.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/reports/presentation/pages/end_day_review_page.dart>)
- [lib/features/reports/presentation/pages/session_collection_audit_page.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/reports/presentation/pages/session_collection_audit_page.dart>)
- [lib/features/supervisor/data/supervisor_attention_repository.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/supervisor/data/supervisor_attention_repository.dart>)
- [lib/features/supervisor/presentation/pages/supervisor_attention_page.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/supervisor/presentation/pages/supervisor_attention_page.dart>)
- [test/document_archive_test.dart](<D:/My Live Projects/Salesync/salesync_mobile/test/document_archive_test.dart>)
- [test/document_print_widgets_test.dart](<D:/My Live Projects/Salesync/salesync_mobile/test/document_print_widgets_test.dart>)
- [test/financial_repositories_test.dart](<D:/My Live Projects/Salesync/salesync_mobile/test/financial_repositories_test.dart>)
- [test/mobile_contracts_test.dart](<D:/My Live Projects/Salesync/salesync_mobile/test/mobile_contracts_test.dart>)
- [test/operations_review_test.dart](<D:/My Live Projects/Salesync/salesync_mobile/test/operations_review_test.dart>)
- [test/operations_widgets_test.dart](<D:/My Live Projects/Salesync/salesync_mobile/test/operations_widgets_test.dart>)
- [test/salesrep_mobile_navigation_test.dart](<D:/My Live Projects/Salesync/salesync_mobile/test/salesrep_mobile_navigation_test.dart>)

APK: [app-debug.apk](<D:/My Live Projects/Salesync/salesync_mobile/build/app/outputs/flutter-apk/app-debug.apk>)
