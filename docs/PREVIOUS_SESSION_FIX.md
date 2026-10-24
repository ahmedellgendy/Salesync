# إصلاح ظهور OPEN_SESSION عند بدء يوم جديد

22 أكتوبر 2026. الإصلاح محلي وجاهز للنشر؛ لم يتم نشره أو تعديل أي جلسة/بيانات على الإنتاج.

السبب: منع التداخل في StartSession كان يفحص الجلسات المفتوحة عبر كل التواريخ، بينما GetToday كان يبحث في تاريخ اليوم فقط. لذلك اختفت الجلسة السابقة من التطبيق مع استمرار منع بدء يوم جديد.

الإصلاح: GetToday يعرض الجلسة المفتوحة الحالية للمندوب حتى لو كانت في يوم سابق، قبل العودة لجلسة اليوم المغلقة/حالة عدم البدء. أضيف BusinessDate وHasUnclosedPreviousSession إلى DTO الموبايل كحقول إضافية. HasTodaySession يحتفظ بمعناه: هل توجد جلسة بتاريخ يوم العمل الحالي. الجلسة المغلقة لا تُعرض كمفتوحة حتى لو EndTime غير مكتمل. عند وجود جلسات قديمة متعددة يعرض أحدث جلسة مفتوحة بنفس ترتيب جلسة التحصيل، ثم تظهر السابقة عند إقفالها. لا إعادة تأريخ ولا قفل تلقائي ولا إلغاء لمنع التداخل.

Flutter يعرض تنبيهًا بتاريخ الجلسة السابقة وزر «مراجعة وإقفال الجلسة السابقة»، ويُظهر بياناتها وزر الإقفال حتى إن لم تبدأ جلسة بتاريخ اليوم. التقارير توضح تاريخ الجلسة المعروضة. شروط الإقفال والمخزون والزيارات والتسوية تظل قائمة. الرسالة التجارية أصبحت عربية.

الملفات:

- Salesync.Application/Modules/SalesRep/Services/SalesRepMobileService.cs
- Salesync.Application/Modules/SalesRep/Dtos/Mobile/SalesRepMobileTodayDto.cs
- Salesync.Application/Modules/Sales/Services/SalesRepSessionService.cs
- Salesync.Tests/PreviousSessionTests.cs
- Flutter: lib/features/dashboard/data/models/salesrep_models.dart
- Flutter: lib/features/dashboard/data/repositories/salesrep_repository.dart
- Flutter: lib/features/dashboard/presentation/pages/salesrep_dashboard_page.dart
- Flutter: lib/features/reports/presentation/pages/salesrep_reports_page.dart
- Flutter: test/salesrep_focused_home_test.dart

الاختبارات: dotnet test ناجح (37 اختبارًا)، flutter analyze نظيف، والاختبار الجديد يغطي ظهور الجلسة القديمة ومنع POST لبدء يوم متداخل. Backend Release publish وAPK debug تم بناؤهما بنجاح.

النشر: ارفع ناتج backend-publish الجديد مع الحفاظ على إعدادات اتصال/JWT وترخيص وملفات الموقع الحالية. لا حاجة إلى SQL أو migration جديدة. بعد رفع الـBackend ثبّت APK الجديد كتحديث. افتح الرئيسية ثم مراجعة وإقفال الجلسة السابقة، وعالج أي موانع فعلية من الزيارات/المخزون/التسوية. بعد نجاح الإقفال يظهر بدء يوم العمل الحالي. لا تقفل يومًا حقيقيًا لأغراض التجربة؛ يعتمد المندوب الإقفال الطبيعي بعد مطابقة مستنداته.

مجلد Backend المحلي: D:/My Live Projects/Salesync/salesync_mobile/build/previous-session-fix/backend-publish

APK: D:/My Live Projects/Salesync/salesync_mobile/build/app/outputs/flutter-apk/app-debug.apk

لم يحدث Commit أو Push أو تطبيق SQL أو أي إجراء مالي على الإنتاج.
