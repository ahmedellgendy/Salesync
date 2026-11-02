# المرحلة الأولى — تدقيق سلامة البيانات — 28 أكتوبر 2026

## النطاق والنتيجة

قراءة المصدر وقاعدة **localhost / SalesyncDb** فقط، واختبارات على SQLite مؤقتة في الذاكرة. لم تُشغّل واجهة أو API لإرسال معاملات، ولم تُستخرج JWT، ولم تتغير معاملات أو سجلات أو schema. لا Migrations أو نشر أو تنفيذ إصلاحات.

- المشكلة الأولى: Flutter ينقل حقلًا مخزّنًا في البيانات الأساسية بأمانة؛ هذا الحقل ليس مجموع المديونية المحسوب من الفواتير. الخلل المؤكد في تعريف/تسمية القيمة باعتبارها «الرصيد الحالي»، وليس في تحويل رقم 1606.90 إلى صفر.
- المشكلة الثانية: يوجد اختلاف مؤكد **أثناء الجلسة المفتوحة** بين snapshot الجلسة الذي تستخدمه تفاصيل Flutter، وإجمالي إيصالات المرتجعات المحسوب مباشرة في مراجعة الإقفال. بعد الإقفال، البيانات المحفوظة للجلسة 4 والتسوية والإيصال متطابقة: **193.10**.
- لا يوجد دليل حالي على فقدان قيمة المرتجع في جلسة 4 المغلقة أو على خصم مالي غير صحيح. ظهور صفر بعد إقفالها وتحديثها فعليًا لم يُعَد إنتاجه؛ يلزم طلب GET مؤرّخ أو إعادة إنتاج معزولة لإسناده إلى cache أو إصدار بعينه.

## أدلة البيانات المحلية

| المصدر | النتيجة |
|---|---:|
| Customers.Id=2 / CurrentBalance | 0.00 |
| INV-20260922-41A6 / المتبقي | 500.00 |
| INV-20261028-16FD / الإجمالي | 2800.00 |
| الفاتورة نفسها / PaidAmount | 1000.00 |
| الفاتورة نفسها / CreditAppliedAmount | 193.10 |
| الفاتورة نفسها / المتبقي | 1606.90 |
| مجموع متبقي الفاتورتين المؤكدتين النشطتين للعميل 2 | 2106.90 |
| RET-20261028-60EE / الحالة | ReceivedAtWarehouse=4 |
| InvoiceReturnReceipts / Posting session | 4 |
| إيصال المرتجع / TotalAmount وAppliedToInvoiceAmount | 193.10 و193.10 |
| إيصال المرتجع / CustomerCreditAmount | 0.00 |
| Session 4 / TotalReturnAmount | 193.10 |
| Session 4 / NetSales | 2606.90 |
| Session 4 / Status، IsStockSettled، IsTreasurySettled | Closed=4، true، true |
| DC-20261028-0004 / TotalReturnAmount | 193.10 |
| التسوية / Status، IsCashReceived، IsStockReceived | Completed=2، true، true |

الإيصال سُجل في 16:17 UTC، ثم إقفال الجلسة في 16:25 UTC، ثم آخر تحديث للتسوية في 16:29 UTC. بالتالي هذا المستند ليس حالة استلام متأخر أُسند لجلسة أخرى. لم توجد triggers على Customers أو Invoices أو Payments أو InvoiceReturnReceipts قد تفسر تحديثًا تلقائيًا لـCurrentBalance.

## المشكلة الأولى — سلسلة CurrentBalance

جذر Flutter: `D:/My Live Projects/Salesync/salesync_mobile`.

1. [customer_details_page.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/dashboard/presentation/pages/customer_details_page.dart:390>): الصفحة تستقبل `SalesRepCustomerModel customer`، وتعرض `customer.currentBalance` بعنوان «الرصيد الحالي». لا تجلب مديونية العميل في هذه الشاشة؛ التحديث الأولي يفحص الزيارات فقط.
2. [salesrep_models.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/dashboard/data/models/salesrep_models.dart:216>): `SalesRepCustomerModel.fromJson` يقرأ `currentBalance` مباشرة. الصفر في هذه الحالة قادم من المصدر، وليس تعويضًا عن null.
3. [salesrep_repository.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/dashboard/data/repositories/salesrep_repository.dart:81>): `GetRouteCustomers` في Flutter عبر `getRouteCustomers()` يطلب `GET /api/mobile/salesrep/routes/{routeId}/customers?sessionId=4`؛ مسار الخادم البديل `GET /api/mobile/salesrep/customers?sessionId=4` يستخدم الإسقاط نفسه.
4. [SalesRepMobileController.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.API/Controllers/Mobile/SalesRepMobileController.cs:115>): `GetRouteCustomers()` يستدعي الخدمة المقابلة. `GetCustomers()` عند السطر 63 يستخدم المصدر نفسه أيضًا.
5. [SalesRepMobileService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/SalesRep/Services/SalesRepMobileService.cs:916>): `GetRouteCustomersAsync()` يتحقق من ملكية الجلسة/الخط، ثم `GetAssignedRouteCustomersAsync()` عند 795 يقرأ `Customers.CurrentBalance` عند 824، و`BuildMobileCustomers()` عند 871 ينسخه إلى DTO عند 901.
6. [SalesRepMobileCustomerDto.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/SalesRep/Dtos/Mobile/SalesRepMobileCustomerDto.cs:17>): الخاصية `decimal? CurrentBalance` دون استعلام دين إضافي.
7. [Customer.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Domain/Modules/MasterData/Entities/Customer.cs:40>): حقل `decimal CurrentBalance` مخزّن، ضمن إعدادات الائتمان. [CustomerImportService.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/DataImport/Services/CustomerImportService.cs:964>) يهيّئه بصفر عند إدراج عميل جديد. البحث في المصدر لم يجد تحديثًا لهذا الحقل في تأكيد الفاتورة أو التحصيل أو استلام المرتجع. مسار CRUD الرئيسي `CustomerService.MapCustomerAsync()` يستخدم AutoMapper للحقل نفسه أيضًا.

في المقابل، [ReceivablesService.GetCustomerReceivablesAsync()](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Treasury/Services/ReceivablesService.cs:83>) يحسب المديونية من الفواتير المؤكدة النشطة، داخل صلاحية المستخدم، باستخدام TotalAmount وPaidAmount وCreditAppliedAmount. هذا مصدر مستقل عن CurrentBalance.

### التصنيف والتعريف المقترح

- **مؤكد:** لا خطأ mapping ظاهر؛ المصدر المخزّن صفر، والـDTO وFlutter يعرضانه كما هو. لا توجد آلية مالية في المصدر تحفظ تطابقه مع المديونية. تحديث الشاشة وحده لن يحوله إلى 2106.90.
- **غير مثبت:** اعتباره رصيدًا افتتاحيًا أو رصيد ERP مستوردًا. لا يوجد عقد واضح أو timestamp يثبت هذا المعنى؛ لا يُعاد تفسيره تلقائيًا.
- يظل `CurrentBalance` حقل بيانات أساسية قديمًا، ولا يستخدم كمرجع تحصيل أو دليل أن العميل بلا مديونية.
- تُعرّف «مديونية الفواتير» كمجموع الأرصدة المستحقة للفواتير المؤكدة النشطة عبر **كل الجلسات ضمن نطاق القراءة المصرح**، مع طرح المدفوع والائتمان المطبق مرة واحدة. القيمة في هذه البيانات 2106.90، لا 1606.90 التي تخص فاتورة الاختبار وحدها.
- «رصيد حساب العميل الشامل» تعريف آخر؛ يحتاج تحديد الأرصدة الافتتاحية والخصومات/التسويات والأرصدة الدائنة غير المستخدمة ونطاق الشركة. لا يُستنتج من الفواتير وحدها ولا يُستبدل به CurrentBalance دون اتفاق.

### أقل تغييرات مقترحة — غير منفذة

1. تمييز الحقل القديم في الواجهة كرصيد بيانات أساسية أو حجبه مؤقتًا من العرض المالي.
2. إذا كان المطلوب إظهار المديونية: إضافة حقل قراءة صريح مثل `InvoiceOutstandingAmount` للـDTO الموجود، وحسابه في Backend باستعلام مجمّع يحافظ على الصلاحيات ويشمل الفواتير القديمة؛ لا جمع محلي في Flutter ولا استعلام لكل عميل.
3. قراءة الحقل الجديد في `SalesRepCustomerModel` وعرضه بعنوان «مديونية الفواتير». لا تعديل لقيمة Customers.CurrentBalance ولا كتابة أرصدة تاريخية.
4. الملفات المرشحة: `SalesRepMobileCustomerDto.cs`، `SalesRepMobileService.cs`، `salesrep_models.dart`، `customer_details_page.dart` وموضع الرصيد في `salesrep_dashboard_page.dart`. لا حاجة مبدئية لـEndpoint جديد أو Migration.

## المشكلة الثانية — سلسلة TotalReturnAmount

1. [salesrep_dashboard_page.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/dashboard/presentation/pages/salesrep_dashboard_page.dart:386>): `_SessionDayDetails.build()` يستخدم `today.session.totalReturnAmount` عند 427، بينما المبيعات والتحصيل والمتبقي تُؤخذ من حقول Today المحسوبة مباشرة.
2. `_showSessionDetails()` عند 216 يفتح تفاصيل من `_today` الموجود في الذاكرة ولا ينفذ GET جديدًا. `_loadToday()` عند 52 يجلب الحالة، والرجوع الطبيعي من مراجعة الإقفال يستدعيه عند 139؛ هذا يقلل احتمال بقاء الصفر بعد الرجوع الطبيعي لكنه لا يلغي بيانات قديمة أثناء فتح الشاشة أو تغييرات خارجية من BackOffice.
3. [salesrep_models.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/dashboard/data/models/salesrep_models.dart:55>): `SalesRepSessionModel.fromJson()` يقرأ `totalReturnAmount` مباشرة.
4. [salesrep_repository.dart](<D:/My Live Projects/Salesync/salesync_mobile/lib/features/dashboard/data/repositories/salesrep_repository.dart:23>): `getToday()` يستدعي `GET /api/mobile/salesrep/today` → `SalesRepMobileController.GetToday()` → [SalesRepMobileService.GetTodayAsync()](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/SalesRep/Services/SalesRepMobileService.cs:91>).
5. الخدمة عند 181 تستخدم `_mapper.Map<SalesRepSessionDto>(session)`؛ [MappingProfile.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Mappings/MappingProfile.cs:97>) ينقل الحقل المخزّن كما هو. لا تجمع الخدمة إيصالات المرتجعات لهذا الحقل عند كل قراءة.
6. [InvoiceReturnReceivingService](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Sales/Services/InvoiceReturnReceivingService.cs:108>) يسجل قيمة الجزء المستلم، وينسب الإيصال للجلسة النشطة عند الاستلام عند 134، ويطبق الائتمان على الفاتورة عند 144. لا يحدّث `SalesRepSession.TotalReturnAmount` في هذا الإجراء.
7. [SalesRepSessionService.CloseSessionAsyncCore()](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Sales/Services/SalesRepSessionService.cs:181>) يجمع InvoiceReturnReceipts النشطة المنسوبة للجلسة، ثم يحفظ `session.TotalReturnAmount` عند 204.
8. [SalesRepDayClosingCalculator.CalculateTotalReturnAmountAsync()](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Application/Modules/Sales/Services/SalesRepDayClosingCalculator.cs:101>) يستخدم إيصالات الاستلام نفسها. `MobileWorkflowQueryService.ReviewAsync()` يعرض نتيجته في `FinancialSummary` عبر `GET /api/mobile/salesrep/day/4/review`، و`SalesRepDayClosingService.CreateAsyncCore()` يحفظه في مستند التسوية عند 175.
9. BackOffice يقرأ `GET /api/SalesRepDayClosing/{id}` عبر [SalesRepDayClosingApiService.GetByIdAsync()](<D:/My Live Projects/Salesync/Salesync.Backoffice/Salesync.BackOffice/Services/Api/SalesRepDayClosingApiService.cs:69>)، ويعرض `TotalReturnAmount` في [DayClosing.razor](<D:/My Live Projects/Salesync/Salesync.Backoffice/Salesync.BackOffice/Components/Pages/Operations/DayClosing.razor:545>).

### التصنيف والتعريف المقترح

- **خلل قراءة/تسمية مؤكد أثناء الجلسة المفتوحة:** تعرض تفاصيل Flutter snapshot لا يتحدث إلا عند الإقفال، بجوار مؤشرات حية وتحت عنوان غير مقيد. الاختبار المعزول أثبت 0 مقابل 193.10 دون أي نقص في الإيصال.
- **سلوك متوقع:** الطلب المعلّق أو المعتمد فقط لا يدخل إجمالي المرتجعات المالية المستلمة؛ البونص وحده لا ينشئ قيمة مالية. الاستلام الجزئي يدخل بقيمة ما استُلم فقط.
- **سلوك متوقع:** الإسناد المالي حسب `InvoiceReturnReceipt.SalesRepSessionId`، لا جلسة الفاتورة أو طلب المرتجع. استلام متأخر قد يظهر في يوم لاحق؛ هذا ليس سبب الاختلاف في الجلسة 4 لأن إسنادها الفعلي متطابق.
- التعريف المناسب للعرض الحالي: «قيمة المرتجعات المستلمة والمُرحّلة على الجلسة». مصدره الإيصالات النشطة. افصل عنه «قيمة طلبات المرتجع» و«الائتمان المطبق على الفواتير» و«رصيد العميل الدائن الزائد».
- الائتمان 193.10 ليس نقدية مستلمة؛ ExpectedCashAmount يظل 1000.00. لا يُخصم المرتجع مرة ثانية من PaidAmount أو من النقدية.

### أقل تغييرات مقترحة — غير منفذة

1. في `GetTodayAsync()` حساب إجمالي إيصالات جلسة العمل المفتوحة عند القراءة، وإسقاطه على **نسخة DTO فقط** في `Session.TotalReturnAmount`، مع الاحتفاظ بملخص الجلسة المغلقة كـsnapshot. إعادة استخدام نفس شروط calculator؛ لا تحديث كيان أو جدول أثناء GET.
2. تسمية مؤشر Flutter بدقة وإتاحة تحديث الحالة قبل فتح التفاصيل أو عبر تحديث صريح، مع عدم عرض snapshot قديم كقيمة حية. لا PUT أو Close تلقائي.
3. بديل إذا كان عقد SessionDto يجب أن يظل snapshot في كل الحالات: حقل Today مستقل للإجمالي الحي ثم استخدامه في Flutter؛ تُحسم هذه النقطة قبل التنفيذ. اختبار الانحدار الأحمر الحالي يتبع الاقتراح الأول الأصغر، ويمكن نقل نفس assertion للحقل المستقل عند اعتماد البديل دون إضعافه.
4. الملفات المرشحة أساسًا: `SalesRepMobileService.cs` و`salesrep_dashboard_page.dart`. لا حاجة لتغيير خدمات الاستلام أو التحصيل أو حساب التسوية أو بيانات الجلسات السابقة.

## الاختبارات التي أُضيفت ونتائجها

الملف الوحيد المضاف إلى الاختبارات: [DataIntegrityAuditTests.cs](<D:/My Live Projects/Salesync/Salesync.API/Salesync.Tests/DataIntegrityAuditTests.cs>).

- حالتان لتمييز الرصيد الأساسي 0 أو 37 عن مديونية العميل 2106.90 ومديونية الجلسة 1606.90: ناجحتان.
- تطابق قراءة الجلسة المغلقة مع إيصالات المرتجع ومراجعة اليوم والأرشيف، مع عدم تحويل الائتمان إلى نقدية: ناجح.
- إسناد المرتجعات إلى جلسة الاستلام بدل جلسة الفاتورة/الطلب: ناجح.
- `Open_session_details_should_expose_received_return_total_without_waiting_for_closing`: **فاشل عمدًا**، Expected=193.10 وActual=0.00، لتثبيت عيب القراءة قبل تنفيذ الإصلاح. موسوم `Audit=UnfixedRegression`، غير متجاهَل وغير مخفف.

تشغيل المجموعة الجديدة: **4 ناجحة، 1 فاشلة متوقعة**. تشغيل مجموعة Backend كاملة: **108 ناجحة، 1 فاشلة متوقعة، 0 متجاهلة، الإجمالي 109**. البناء نجح؛ ظهرت تحذيرات CS8625 القديمة في Controllers. لا يُعلن أن المجموعة كاملة خضراء في مرحلة التشخيص.

الاختبارات تستخدم WorkflowFixture وSQLite in-memory، وتزرع سجلات اصطناعية فقط. لم تستدعِ ReceiveAsync أو CloseSessionAsync أو أي أمر مالي. لم تُشغّل اختبارات Flutter في هذه المرحلة ولم يُبنَ APK.

## ترتيب التنفيذ الموصى به بعد المراجعة

1. اعتماد تعريف الأسماء والنطاق: رصيد البيانات الأساسية، مديونية الفواتير، وإجمالي المرتجعات المُرحّلة للجلسة.
2. إصلاح قراءة المرتجعات المفتوحة بشكل محدود لتحويل الاختبار الأحمر إلى ناجح، ثم تعديل تسمية/تحديث تفاصيل Flutter؛ دون المساس بالمحاسبة أو snapshots التاريخية.
3. إضافة مصدر مديونية صريح للعميل عبر الـAPI الموجود وصلاحياته، ثم ربطه بالعرض؛ دون إعادة تفسير أو ترحيل CurrentBalance.
4. اختبارات عقود Flutter والتحديث بعد الرجوع، ثم قبول ميداني للقراءة على الجلسة المغلقة 4. لا تُعد إرسال المعاملات المثبتة لاختبار العرض.

هذه توصيات فقط. لم تنفذ أي إصلاحات تطبيقية أو مالية.
