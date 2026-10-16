# وضعیت و ساخت پروژه (فارسی)

## این ZIP چه چیزی دارد؟
- رابط WinForms برای ویندوز.
- سورس اصلی Kotlin به‌عنوان مرجع در `reference/osc-core-android/`.
- DLLهای رسمی Wintun برای x64، x86 و ARM64.
- `WintunNative.cs` برای resolve کردن APIهای DLL.
- `WintunPacketSession.cs` برای ایجاد session و ارسال/دریافت بسته‌های لایهٔ ۳.

## چه چیزی هنوز آماده نیست؟
- هستهٔ SSTP/PPP قابل اجرا در ویندوز.
- اتصال بین هستهٔ SSTP/PPP و Wintun.
- پیکربندی IP، مسیرها و DNS.
- نمودار مختص ترافیک VPN.
- نسخهٔ نصب‌کنندهٔ نهایی.

دکمهٔ Connect در UI فعلی عمداً فقط پیام می‌دهد که موتور آماده نیست. این رفتار را حذف نکنید تا زمانی که اتصال واقعی تکمیل شده باشد.

## ساخت روی Windows
1. .NET 8 SDK را نصب کنید.
2. ZIP را استخراج کنید.
3. PowerShell را در پوشه‌ای که `OpenSSTP.Windows.csproj` قرار دارد باز کنید.
4. اجرا کنید:

```powershell
dotnet build .\OpenSSTP.Windows.csproj -c Release -p:PlatformTarget=x64
```

این دستور فقط UI و کد scaffold را می‌سازد؛ اثبات اتصال VPN نیست. در این محیط ساخت Windows/.NET اجرا نشده است.

## قبل از آزمایش Wintun
آداپتور مجازی تغییر در پیکربندی شبکهٔ سیستم ایجاد می‌کند. فقط روی دستگاه آزمایشی، با دسترسی Administrator و پس از بررسی route/DNS اجرا شود. تا قبل از پیاده‌سازی engine، `WintunPacketSession` را از UI فراخوانی نکنید؛ ترافیک به مقصدی منتقل نخواهد شد.
