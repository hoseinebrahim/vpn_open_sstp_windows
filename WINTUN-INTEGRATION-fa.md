# فایل‌های Wintun برای Open-SSTP Windows (مرحلهٔ آماده‌سازی)

این بسته، DLL رسمی Wintun نسخهٔ 0.14.1 را برای معماری‌های x64، x86 و ARM64، هدر C و مجوز آن در پروژه قرار می‌دهد. کلاس `WintunNative.cs` می‌تواند DLL مناسب معماری برنامه را بارگذاری و چند export پایه را پیدا کند.

## وضعیت فعلی — مهم

**این تغییر به‌تنهایی VPN را وصل نمی‌کند.** هنوز باید هستهٔ SSTP/PPP پروژه به یک حلقهٔ بستهٔ IP متصل شود:

1. ساخت آداپتور با `WintunCreateAdapter` و مدیریت خطا/دسترسی مدیر سیستم.
2. پیکربندی IP، MTU، مسیرها و DNS (با بازگردانی تنظیمات هنگام قطع).
3. شروع نشست با `WintunStartSession`.
4. انتقال بسته‌های IP خروجی بین Wintun و لایهٔ PPP/SSTP و دریافت بسته‌های برگشتی با APIهای `WintunAllocateSendPacket`، `WintunSendPacket`، `WintunReceivePacket` و `WintunReleaseReceivePacket`.
5. بستن session و adapter و پاک‌سازی مطمئن هنگام قطع یا خطا.
6. اندازه‌گیری سرعت از بایت‌های همان نشست تونل، نه شمارندهٔ کل سیستم.

`WintunNative.cs` فقط لودر/تعریف اولیهٔ API است و هنوز آداپتور یا session ایجاد نمی‌کند. رابط کاربری فعلی عمداً اعلام می‌کند که موتور VPN کامل نیست.

## ساخت در ویندوز

- Visual Studio 2022 یا .NET 8 SDK با workload مربوط به Windows Forms لازم است.
- فایل `OpenSSTP.Windows.csproj` را باز کن و برای تست معماری x64 از `dotnet build -c Release -p:PlatformTarget=x64` استفاده کن.
- DLL در خروجی با مسیر `runtimes/<architecture>/native/wintun.dll` کپی می‌شود.
- قبل از استفادهٔ واقعی، پیاده‌سازی تونل و مسیر‌یابی باید تکمیل و با یک سرور SSTP مجاز آزمایش شود.

## منابع و مجوز

Wintun از بستهٔ رسمی نسخهٔ 0.14.1 گرفته شده است. مجوز در `native/WINTUN-LICENSE.txt` قرار دارد؛ `native/wintun.h` نیز هدر رسمی API است. DLL را از منبع رسمی دریافت‌شده نگه دارید و تغییر ندهید.

## کد جدید در این نسخه

`WintunPacketSession.cs` اکنون یک wrapper کم‌سطح برای ایجاد adapter/session و دریافت/ارسال بسته‌های لایهٔ ۳ دارد. `WintunNative.cs` نیز APIهای لازم برای packet I/O را resolve می‌کند.

این کد **به‌صورت خودکار اجرا نمی‌شود** و هنوز به موتور SSTP/PPP وصل نیست. اجرای constructor آداپتور شبکه ایجاد می‌کند و باید فقط با رضایت کاربر، روی ویندوز آزمایشی و با دسترسی مناسب انجام شود. بستن adapter ایجادشده ممکن است آن را از سیستم حذف کند.

## اتصال واقعی هنوز چه می‌خواهد؟

1. پیاده‌سازی Windows-specific برای جایگزینی `IPTerminal.kt` (نه صرفاً ترجمهٔ خط‌به‌خط آن).
2. تبدیل/انتقال لایهٔ SSTP و PPP به یک هستهٔ قابل اجرا روی .NET؛ سورس فعلی Kotlin هنوز به Android APIs، coroutines، preferences و `SharedBridge` وابسته است.
3. حلقهٔ رفت‌وبرگشت بسته‌ها: Wintun → PPP encapsulation → SSTP/TLS و SSTP/TLS → PPP decapsulation → Wintun.
4. مذاکرهٔ IPCP/IPv6CP، اختصاص آدرس و MTU، route/DNS و پاک‌سازی امن هنگام قطع اتصال.
5. آزمون با سرور سازگار و بررسی نشت ترافیک/قطع ناگهانی.

**محدودیت فعلی:** این wrapper فقط زیرساخت packet I/O است. هنوز یک VPN client کامل یا اتصال آزمایش‌شده نیست.
