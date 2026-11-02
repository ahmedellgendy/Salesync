# تقرير مطابقة نهائي للموافقة — db68089

27 أكتوبر 2026. **لم يُنفّذ SQL أو Migration أو Deployment، ولم يتغير أي سجل على MonsterASP.** أكد المستخدم أن البيانات هناك تجريبية؛ هذا ليس إذنًا لمسحها أو تنفيذ السكربت.

[السكربت النهائي](return-receiving-credit-manual-column-reconcile-review.sql) مخصص للحالة المؤكدة: قاعدة `db68089`، غياب الجدولين، ووجود `Invoices.CreditAppliedAmount` من نوع `decimal(18,2) NOT NULL`.

## نتيجة المطابقة مع كامل Up()

استُدعيت `UpOperations` من Migration `20261026194614_AddReturnReceivingCreditLedger` الفعلية، ووُلّد SQL بواسطة EF Core SQL Server provider مع **TargetModel الأصلي بعد تهيئته محليًا**. لم يُشغّل API Host، وظل اتصال قاعدة البيانات `Closed`.

تحتوي `Up()` على **14 عملية**: إضافة عمود واحد، إنشاء جدولين، وإنشاء 11 فهرسًا. تمت مقارنة **جميع عبارات CREATE الثلاث عشرة** بالسكربت النهائي؛ لا اختلاف في أسماء الأعمدة أو ترتيبها أو أنواعها وأطوالها ودقتها وnullable/identity، أو أسماء المفاتيح والقيود وأعمدتها وعلاقاتها، أو أسماء الفهارس وترتيب أعمدتها وUnique/filter.

| الجزء | النتيجة |
|---|---|
| `InvoiceReturnReceipts` | 20 عمودًا؛ PK واحد؛ 4 FK؛ CHECK واحد — مطابقة |
| `InvoiceReturnReceiptItems` | 16 عمودًا؛ PK واحد؛ 4 FK؛ CHECK واحد — مطابقة |
| الفهارس | 11؛ منها 4 Unique و7 Non-unique؛ لا filtered indexes — مطابقة |
| الهوية | `Id` في الجدولين يبدأ 1 ويزيد 1 — مطابقة |
| `CreditAppliedAmount` | يُستخدم العمود الموجود بدل `ADD COLUMN`؛ يتحقق السكربت من النوع والدقة وNOT NULL وعدم computed |
| `DEFAULT 0` للعمود الموجود | يُحافظ عليه إذا كان صفرًا؛ يُضاف فقط إذا غاب؛ Default مختلف/غير قابل للتحقق يوقف السكربت دون استبداله |

**الفرق المقصود عن التنفيذ الحرفي لـUp():** عدم إضافة العمود الموجود مرة ثانية. تعويض Default مفقود، إن لزم، يعادل جزء `defaultValue: 0m` من عملية الإضافة الأصلية دون تغيير قيم الصفوف. أسماء default constraints غير محددة في Migration؛ SQL Server يولدها تلقائيًا. بقية DDL مطابقة؛ تأهيل الأسماء بـ`dbo` صريح بدل المخطط الضمني، و`IDENTITY` هو نفسه `IDENTITY(1,1)`. لا يوجد Schema Change مطلوب في هذه Migration تم إسقاطه؛ تصل النسخة للحالة المطلوبة فقط إذا نجحت جميع الفحوص والتنفيذ. المطابقة لا تزعم فحص Production مباشرة أو تغطية migrations أخرى.

## المفاتيح والقيود

المفتاحان الأساسيان `PK_InvoiceReturnReceipts` و`PK_InvoiceReturnReceiptItems` على `Id`، مطابقان. كل علاقات الحذف `Restrict` في EF تتحول إلى `NO ACTION` في SQL Server، بلا حذف متتابع؛ ولم يُضف FK أو فهرس غير موجود في Up، بما في ذلك عدم اختراع علاقة لـ`SalesRepSessionId`.

| Foreign Key | الأعمدة والمرجع | الحذف |
|---|---|---|
| `FK_InvoiceReturnReceipts_Customers_CustomerId` | `CustomerId` → `Customers.Id` | `NO ACTION` |
| `FK_InvoiceReturnReceipts_InvoiceReturns_InvoiceReturnId` | `InvoiceReturnId` → `InvoiceReturns.Id` | `NO ACTION` |
| `FK_InvoiceReturnReceipts_Invoices_InvoiceId` | `InvoiceId` → `Invoices.Id` | `NO ACTION` |
| `FK_InvoiceReturnReceipts_SalesReps_SalesRepId` | `SalesRepId` → `SalesReps.Id` | `NO ACTION` |
| `FK_InvoiceReturnReceiptItems_InvoiceReturnItems_InvoiceReturnItemId` | `InvoiceReturnItemId` → `InvoiceReturnItems.Id` | `NO ACTION` |
| `FK_InvoiceReturnReceiptItems_InvoiceReturnReceipts_ReceiptId` | `ReceiptId` → `InvoiceReturnReceipts.Id` | `NO ACTION` |
| `FK_InvoiceReturnReceiptItems_Products_ProductId` | `ProductId` → `Products.Id` | `NO ACTION` |
| `FK_InvoiceReturnReceiptItems_Warehouses_WarehouseId` | `WarehouseId` → `Warehouses.Id` | `NO ACTION` |

- `CK_ReturnReceipt_Amounts`: القيم الثلاث غير سالبة، و`TotalAmount = AppliedToInvoiceAmount + CustomerCreditAmount` — مطابق للنص الأصلي.
- `CK_ReturnReceiptItem_Quantity`: Quantity وBonusQuantity غير سالبين، وأحدهما موجب، وTotalAmount غير سالب — مطابق للنص الأصلي.

## الفهارس

| الاسم | الأعمدة بالترتيب | النوع |
|---|---|---|
| `IX_InvoiceReturnReceiptItems_InvoiceReturnItemId` | `InvoiceReturnItemId` | Non-unique |
| `IX_InvoiceReturnReceiptItems_ProductId` | `ProductId` | Non-unique |
| `IX_InvoiceReturnReceiptItems_ReceiptId_InvoiceReturnItemId` | `ReceiptId, InvoiceReturnItemId` | Unique |
| `IX_InvoiceReturnReceiptItems_WarehouseId` | `WarehouseId` | Non-unique |
| `IX_InvoiceReturnReceipts_CreditNoteNumber` | `CreditNoteNumber` | Unique |
| `IX_InvoiceReturnReceipts_CustomerId_CreatedAt` | `CustomerId, CreatedAt` | Non-unique |
| `IX_InvoiceReturnReceipts_InvoiceId` | `InvoiceId` | Non-unique |
| `IX_InvoiceReturnReceipts_InvoiceReturnId` | `InvoiceReturnId` | Non-unique |
| `IX_InvoiceReturnReceipts_OperationId` | `OperationId` | Unique |
| `IX_InvoiceReturnReceipts_ReceiptNumber` | `ReceiptNumber` | Unique |
| `IX_InvoiceReturnReceipts_SalesRepId` | `SalesRepId` | Non-unique |

## ضوابط النسخة النهائية

`@ExpectedDatabase = N'db68089'`. تتوقف النسخة عند قاعدة أخرى، أو وجود أحد الجدولين، أو تسجيل migration مسبقًا، أو غياب prerequisite migration أو الجداول المرجعية أو مخالفة تعريف عمود الائتمان. تتوقف كذلك إذا وجدت قيمة ائتمان غير صفرية تحتاج تفسيرًا؛ **لا تُصفّر القيم لتجاوز هذا الشرط**. لم يُثبت من المستخدم وجود Default صفر أو غياب الائتمان غير الصفري؛ السكربت يتحقق منهما عند التنفيذ المصرح به.

الـDDL داخل transaction مع `XACT_ABORT` و`TRY/CATCH` وRollback. لا توجد `UPDATE` أو `DELETE` أو `DROP` أو INSERT لبيانات أعمال. INSERT الوحيد إلى `__EFMigrationsHistory` باسم migration الأصلي وProductVersion `9.0.15` بعد اكتمال DDL؛ ليس إنشاءً لإيصال أو عملية مالية. ALTER الوحيد يضيف Default صفرًا إذا غاب. لا إعادة إرسال لأي Close/Return/Payment ولا تغيير للجلسة 25 أو Pending Operations.

## المرتجعات القديمة والبيانات التجريبية

لا تحتاج إصلاحات schema إلى backfill تاريخي. ستبقى بيانات المرتجعات والفواتير والمخزون الموجودة كما هي، ويبدأ دفتر الإيصالات الجديد فارغًا.

- Pending/Approved تعني طلبًا/اعتمادًا؛ لا تكفي لإثبات استلام فعلي أو تخفيض مديونية، فلا تُحوّل تلقائيًا لإيصال.
- المرتجع القديم المستلم بالمخزن بلا سجل جديد يحتفظ بحجز الكميات التاريخي في `UseFinalReceivedQuantitiesAsync`. لا يُعاد استلامه لإعادة إنشاء الدفتر؛ فقد يؤدي ذلك لتكرار المخزون أو الأثر المالي.
- ملخص المرتجعات الجديد يقرأ الإيصالات الجديدة. المرتجعات القديمة بلا إيصال لن تدخل هذا الإجمالي؛ وقد يعرض القديم صفرًا في إجمالي الإيصالات حتى مع وجود مستند مرتجع. لا يعني ذلك عدم حصول حركة تاريخية.
- **بما أن البيانات تجريبية بحسب المستخدم، يمكن اعتماد بداية اختبار للدورة الجديدة دون ترحيل التاريخ إذا لم تُعتمد التقارير القديمة.** أما إذا أريد اعتماد تقارير الجلسة 25 أو الأرصدة القديمة كأرقام صحيحة، فتلزم مطابقة منفصلة لإثبات الاستلام وحركات المخزون والائتمان السابق قبل أي backfill، بقرار وموافقة مستقلين. لا يوجد backfill أو مسح أو تسوية تاريخية في هذه النسخة.

## التحقق وحدوده

- مطابقة مباشرة مع SQL مولّد من Up() وTargetModel، لكل 13 عبارة CREATE: ناجحة.
- فحص Microsoft ScriptDom / TSql160: صفر أخطاء صياغة.
- فحص ثابت يمنع business DML وDROP، ويقيد ALTER بالـDefault المذكور وINSERT بتاريخ migration فقط: ناجح.
- لم يُنفّذ السكربت على SQL Server، ولم تُختبر صلاحيات db68089 أو بياناتها أو transaction rollback على الخادم. يلزم أخذ نسخة احتياطية وموافقة تنفيذ منفصلة؛ تقرير المطابقة ليس ادعاء بتنفيذ ناجح.

الملف النهائي للموافقة SHA-256:

`9739f9bf9c58c65bce3b1103c5725b5af2c654ae2707a912efecc01f6a349579`

Migration source SHA-256:

`b6406099126a9489f037d588c6c70759ca92ba349e446717f726728e84164066`
