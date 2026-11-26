using System.Globalization;
using ShopLedger.Models;
using ShopLedger.ViewModels;

namespace ShopLedger.Services;

/// <summary>Builds the sentences that are read aloud, in English or Urdu. (Phase 6 moves UI text to .resx;
/// these spoken templates stay here because word order differs per language.)</summary>
public static class SpeechTextBuilder
{
    private static bool Ur(string l) => l == "ur";
    private static string M(decimal d) => d.ToString("N0", CultureInfo.InvariantCulture);
    private static string Q(decimal d) => d.ToString("0.##", CultureInfo.InvariantCulture);

    private static CultureInfo Cul(string lang)
    {
        try { return new CultureInfo(Ur(lang) ? "ur-PK" : "en-US"); } catch { return CultureInfo.InvariantCulture; }
    }
    private static string D(DateTime d, string lang) => d.ToString("d MMMM", Cul(lang));

    public static string Empty(string lang) => Ur(lang) ? "پڑھنے کے لیے کچھ نہیں۔" : "Nothing to read.";

    public static string UnitName(string unit, string lang)
    {
        if (!Ur(lang)) return unit;
        return unit switch
        {
            "kg" => "کلو", "gram" => "گرام", "litre" => "لیٹر", "dozen" => "درجن",
            "pack" => "پیکٹ", "box" => "ڈبہ", _ => "عدد"
        };
    }

    public static string TypeLabel(TransactionType t, string lang) => !Ur(lang) ? TransactionRow.Label(t) : t switch
    {
        TransactionType.CreditSale => "ادھار فروخت",
        TransactionType.CashSale => "نقد فروخت",
        TransactionType.Payment => "ادائیگی وصول",
        TransactionType.Adjustment => "دستی اندراج",
        TransactionType.Purchase => "اسٹاک خریداری",
        _ => t.ToString()
    };

    public static string Product(Product p, string lang) => Ur(lang)
        ? $"{p.Name}، {Q(p.Stock)} {UnitName(p.Unit, lang)} موجود، قیمت فروخت {M(p.SalePrice)} روپے" + (p.IsLowStock ? "، اسٹاک کم ہے" : "")
        : $"{p.Name}, {Q(p.Stock)} {p.Unit} in stock, sale price {M(p.SalePrice)} rupees" + (p.IsLowStock ? ", low stock" : "");

    public static string Customer(Customer c, LedgerTransaction? lastPayment, string lang)
    {
        var b = c.CurrentBalance;
        string main;
        if (Ur(lang))
            main = b > 0 ? $"{c.Name}، بقایا {M(b)} روپے" : b < 0 ? $"{c.Name}، پیشگی {M(-b)} روپے" : $"{c.Name}، کوئی بقایا نہیں";
        else
            main = b > 0 ? $"{c.Name}, balance {M(b)} rupees" : b < 0 ? $"{c.Name}, advance {M(-b)} rupees" : $"{c.Name}, no balance";

        if (lastPayment is null) return main;
        return Ur(lang)
            ? $"{main}، آخری ادائیگی {M(lastPayment.PaidAmount)} روپے {D(lastPayment.Date, lang)} کو"
            : $"{main}, last payment {M(lastPayment.PaidAmount)} on {D(lastPayment.Date, lang)}";
    }

    public static string Transaction(TransactionRow row, string lang)
    {
        var label = TypeLabel(row.Transaction.Type, lang);
        var who = string.IsNullOrEmpty(row.CustomerName) ? "" : (Ur(lang) ? $"، {row.CustomerName}" : $", {row.CustomerName}");
        return Ur(lang)
            ? $"{label}{who}، {M(row.Amount)} روپے، {D(row.Transaction.Date, lang)}"
            : $"{label}{who}, {M(row.Amount)} rupees, {D(row.Transaction.Date, lang)}";
    }

    public static List<string> Statement(Statement s, string lang, int maxRows = 15)
    {
        var list = new List<string>();
        list.Add(Ur(lang) ? $"{s.Customer.Name} کا اسٹیٹمنٹ۔ ابتدائی بقایا {M(s.OpeningBalance)} روپے۔"
                          : $"Statement for {s.Customer.Name}. Opening balance {M(s.OpeningBalance)} rupees.");
        foreach (var r in s.Rows.TakeLast(maxRows))
        {
            if (Ur(lang))
            {
                var parts = new List<string> { D(r.Date, lang) };
                if (r.Debit > 0) parts.Add($"ادھار {M(r.Debit)} روپے");
                if (r.Credit > 0) parts.Add($"ادائیگی {M(r.Credit)} روپے");
                parts.Add($"بقایا {M(r.Balance)} روپے");
                list.Add(string.Join("، ", parts));
            }
            else
            {
                var parts = new List<string> { D(r.Date, lang) + ": " + r.Description };
                if (r.Debit > 0) parts.Add($"debit {M(r.Debit)}");
                if (r.Credit > 0) parts.Add($"credit {M(r.Credit)}");
                parts.Add($"balance {M(r.Balance)} rupees");
                list.Add(string.Join(", ", parts));
            }
        }
        if (s.Rows.Count > maxRows)
            list.Insert(1, Ur(lang) ? $"آخری {maxRows} اندراجات پڑھے جا رہے ہیں۔" : $"Reading the last {maxRows} entries.");
        list.Add(Ur(lang) ? $"اختتامی بقایا {M(s.ClosingBalance)} روپے۔" : $"Closing balance {M(s.ClosingBalance)} rupees.");
        return list;
    }

    public static List<string> Dashboard(decimal sales, decimal cash, decimal receivable, int lowStock, string lang) => Ur(lang)
        ? new() { $"آج کی فروخت {M(sales)} روپے۔", $"آج وصول شدہ نقد {M(cash)} روپے۔", $"کل بقایا {M(receivable)} روپے۔",
                  lowStock > 0 ? $"{lowStock} اشیاء کا اسٹاک کم ہے۔" : "تمام اشیاء کا اسٹاک ٹھیک ہے۔" }
        : new() { $"Today's sales {M(sales)} rupees.", $"Cash received today {M(cash)} rupees.",
                  $"Total receivable {M(receivable)} rupees.",
                  lowStock > 0 ? $"{lowStock} items are low in stock." : "All items are well stocked." };

    // ---- voice entry preview + confirmation
    public static string SaleSummary(bool credit, string? customer, string product, decimal qty, string unit, decimal price, decimal total, string lang)
    {
        if (Ur(lang))
            return $"{(credit ? "ادھار فروخت" : "نقد فروخت")}{(customer is null ? "" : $"، {customer}")}: {Q(qty)} {UnitName(unit, lang)} {product}، {M(price)} روپے فی یونٹ، کل {M(total)} روپے";
        return $"{(credit ? "Credit sale" : "Cash sale")}{(customer is null ? "" : $" for {customer}")}: {Q(qty)} {unit} {product} at {M(price)} = {M(total)} rupees";
    }

    public static string PaymentSummary(string customer, decimal amount, string lang) => Ur(lang)
        ? $"{customer} سے {M(amount)} روپے وصول"
        : $"Payment received from {customer}: {M(amount)} rupees";

    public static string NewCustomerSummary(string name, string? phone, decimal? limit, string lang) => Ur(lang)
        ? $"نیا گاہک: {name}" + (phone is null ? "" : $"، فون {phone}") + (limit is null ? "" : $"، حد {M(limit.Value)} روپے")
        : $"New customer: {name}" + (phone is null ? "" : $", phone {phone}") + (limit is null ? "" : $", credit limit {M(limit.Value)}");

    public static string NewProductSummary(string name, decimal price, decimal stock, string unit, string lang) => Ur(lang)
        ? $"نئی چیز: {name}، قیمت {M(price)} روپے، اسٹاک {Q(stock)} {UnitName(unit, lang)}"
        : $"New product: {name}, sale price {M(price)}, stock {Q(stock)} {unit}";

    public static string Saved(string? customer, decimal? balance, string lang)
    {
        if (customer is null || balance is null) return Ur(lang) ? "محفوظ ہو گیا۔" : "Saved.";
        return Ur(lang) ? $"محفوظ ہو گیا۔ {customer} کا بقایا اب {M(balance.Value)} روپے ہے۔"
                        : $"Saved. {customer}'s balance is now {M(balance.Value)} rupees.";
    }
}
