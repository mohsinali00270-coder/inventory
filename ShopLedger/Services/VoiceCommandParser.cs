using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ShopLedger.Services;

public enum VoiceIntent { Unknown, AddProduct, AddCustomer, CreditSale, CashSale, Payment }

public class VoiceCommand
{
    public VoiceIntent Intent { get; set; }
    public string RawText { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? ProductName { get; set; }
    public string? Category { get; set; }
    public string? Phone { get; set; }
    public string? Unit { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }       // sale price per unit
    public decimal? PurchasePrice { get; set; }
    public decimal? Amount { get; set; }          // payment amount / opening balance
    public decimal? CreditLimit { get; set; }
}

/// <summary>
/// Rule-based parser for English and Urdu speech, e.g.
///  "Add Ali credit sale two kg sugar at 150 each"  -> CreditSale
///  "Ali paid 500 rupees"                           -> Payment
///  "add customer Bilal phone 0300 1234567 limit 5000"
///  "add product rice price 220 stock 30 kg"
/// A hint (the screen the user is on) overrides keyword detection for forms.
/// Urdu number words are supported up to 20 plus tens, hundred, thousand, lakh;
/// compound words like 21-99 are usually returned as digits by the recognizer.
/// </summary>
public static class VoiceCommandParser
{
    private static readonly Dictionary<string, decimal> Numbers = new(StringComparer.Ordinal)
    {
        ["zero"] = 0, ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5, ["six"] = 6, ["seven"] = 7,
        ["eight"] = 8, ["nine"] = 9, ["ten"] = 10, ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14,
        ["fifteen"] = 15, ["sixteen"] = 16, ["seventeen"] = 17, ["eighteen"] = 18, ["nineteen"] = 19, ["twenty"] = 20,
        ["thirty"] = 30, ["forty"] = 40, ["fourty"] = 40, ["fifty"] = 50, ["sixty"] = 60, ["seventy"] = 70, ["eighty"] = 80,
        ["ninety"] = 90, ["half"] = 0.5m,
        ["ایک"] = 1, ["دو"] = 2, ["تین"] = 3, ["چار"] = 4, ["پانچ"] = 5, ["چھ"] = 6, ["سات"] = 7, ["آٹھ"] = 8, ["نو"] = 9,
        ["دس"] = 10, ["گیارہ"] = 11, ["بارہ"] = 12, ["تیرہ"] = 13, ["چودہ"] = 14, ["پندرہ"] = 15, ["سولہ"] = 16, ["سترہ"] = 17,
        ["اٹھارہ"] = 18, ["انیس"] = 19, ["بیس"] = 20, ["تیس"] = 30, ["چالیس"] = 40, ["پچاس"] = 50, ["ساٹھ"] = 60,
        ["ستر"] = 70, ["اسی"] = 80, ["نوے"] = 90, ["ڈیڑھ"] = 1.5m, ["ڈھائی"] = 2.5m, ["آدھا"] = 0.5m, ["آدھی"] = 0.5m, ["آدھ"] = 0.5m
    };

    private const string UnitPattern =
        "kg|kgs|kilo|kilos|kilogram|kilograms|gram|grams|gm|g|litre|litres|liter|liters|ltr|l|dozen|dozens|pcs|pc|piece|pieces|" +
        "pack|packs|packet|packets|box|boxes|کلو|گرام|لیٹر|درجن|عدد|پیکٹ|ڈبہ|ڈبے";

    private static readonly HashSet<string> Lead = new() { "add", "new", "please", "a", "the", "نیا", "نئی", "نیا", "شامل" };

    private static readonly HashSet<string> Stop = new(Lead)
    {
        "sale","sold","sell","credit","cash","on","to","for","from","of","and","each","per","at","rate","price","in","with","is",
        "udhaar","udhar","khata","product","item","customer","client","stock","quantity","qty","opening","category","cost",
        "purchase","buying","selling","phone","mobile","number","contact","limit","balance","owes","due",
        "paid","pay","payment","received","receive","gave","give","deposit","deposited",
        "ادھار","نقد","کو","کے","کی","کا","سے","نے","پر","میں","ہے","فروخت","سیل","کریں","کیجئے","دیں","کھاتہ","کھاتے",
        "بیچا","بیچی","فی","حساب","جمع","ادا","ادائیگی","وصول","دیے","دئیے","دیئے","دی","دیا","طرف","پروڈکٹ","آئٹم","کسٹمر","گاہک",
        "kg","kgs","kilo","kilos","gram","grams","gm","g","litre","litres","liter","liters","ltr","l","dozen","pcs","pc","piece",
        "pieces","pack","packs","packet","packets","box","boxes","کلو","گرام","لیٹر","درجن","عدد","پیکٹ","ڈبہ","ڈبے"
    };

    private static readonly HashSet<string> SaleMarkers = new()
    {
        "credit","cash","sale","sold","sell","udhaar","udhar","khata","on",
        "ادھار","نقد","کو","فروخت","سیل","بیچا","بیچی","کھاتہ","کھاتے"
    };

    // ---------------------------------------------------------------- public
    public static VoiceCommand Parse(string text, VoiceIntent? hint = null)
    {
        var cmd = new VoiceCommand { RawText = text };
        var t = Normalize(text);

        // phone number first, while digits are still intact
        var pm = Regex.Match(t, @"(?:phone|mobile|contact|number|فون|موبائل|نمبر)\s*(?:is|number)?\s*(\+?\d[\d\s\-]{6,}\d)");
        if (pm.Success)
        {
            cmd.Phone = Regex.Replace(pm.Groups[1].Value, @"[\s\-]", "");
            t = t.Remove(pm.Index, pm.Length);
        }
        t = Regex.Replace(t, @"\b(?:rupees|rupee|rs|pkr)\b|روپے|روپیہ|روپیے", " ");
        t = ConvertNumberWords(t);

        var credit = Has(t, "credit", "udhaar", "udhar", "khata", "ادھار", "قرض", "کھاتہ", "کھاتے");
        var cash = Has(t, "cash", "naqd", "نقد");
        var sale = Has(t, "sale", "sold", "sell", "sales", "فروخت", "بیچا", "بیچی", "سیل");
        var pay = Has(t, "paid", "pay", "payment", "received", "receive", "gave", "deposit", "deposited",
                         "جمع", "ادا", "ادائیگی", "وصول", "دیے", "دئیے", "دیئے");

        VoiceIntent intent;
        if (hint is VoiceIntent.AddProduct or VoiceIntent.AddCustomer or VoiceIntent.Payment) intent = hint.Value;
        else if (hint is VoiceIntent.CreditSale or VoiceIntent.CashSale)
            intent = credit ? VoiceIntent.CreditSale : cash ? VoiceIntent.CashSale : hint.Value;
        else if (pay && !sale) intent = VoiceIntent.Payment;
        else if (credit) intent = VoiceIntent.CreditSale;
        else if (sale || cash) intent = VoiceIntent.CashSale;
        else if (Has(t, "product", "item", "پروڈکٹ", "آئٹم", "چیز", "مال")) intent = VoiceIntent.AddProduct;
        else if (Has(t, "customer", "client", "party", "گاہک", "کسٹمر", "پارٹی")) intent = VoiceIntent.AddCustomer;
        else intent = VoiceIntent.Unknown;

        cmd.Intent = intent;
        switch (intent)
        {
            case VoiceIntent.CreditSale:
            case VoiceIntent.CashSale: ParseSale(cmd, t); break;
            case VoiceIntent.Payment: ParsePayment(cmd, t); break;
            case VoiceIntent.AddProduct: ParseProduct(cmd, t); break;
            case VoiceIntent.AddCustomer: ParseCustomer(cmd, t); break;
        }
        return cmd;
    }

    // ---------------------------------------------------------------- per intent
    private static void ParseSale(VoiceCommand cmd, string t)
    {
        var work = t;

        var pm = Regex.Match(work, @"(?:\bat\b|@|\brate\b|\bprice\b|ریٹ|قیمت)\s*(\d+(?:\.\d+)?)");
        if (!pm.Success) pm = Regex.Match(work, @"(\d+(?:\.\d+)?)\s*(?:\beach\b|\bper\b|فی|کے حساب)");
        if (pm.Success) { cmd.UnitPrice = Dec(pm.Groups[1].Value); work = work.Remove(pm.Index, pm.Length); }

        work = TakeQuantity(cmd, work);

        var tokens = work.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        string? customer = null;

        var toIdx = tokens.IndexOf("to");
        if (toIdx >= 0 && toIdx + 1 < tokens.Count)
        {
            var name = tokens.Skip(toIdx + 1).TakeWhile(x => !Stop.Contains(x) && !IsNumber(x)).Take(2).ToList();
            if (name.Count > 0)
            {
                customer = string.Join(' ', name);
                tokens.RemoveRange(toIdx, 1 + name.Count);
            }
        }
        else
        {
            var idx = tokens.FindIndex(x => SaleMarkers.Contains(x));
            if (idx > 0)
            {
                var before = tokens.Take(idx).Where(x => !Lead.Contains(x) && !IsNumber(x)).ToList();
                if (before.Count > 0)
                {
                    customer = string.Join(' ', before.TakeLast(2));
                    tokens.RemoveRange(0, idx);
                }
            }
        }

        cmd.CustomerName = customer is null ? null : ToTitle(customer);
        var rest = tokens.Where(x => !Stop.Contains(x) && !IsNumber(x)).ToList();
        cmd.ProductName = rest.Count > 0 ? ToTitle(string.Join(' ', rest)) : null;
    }

    private static void ParsePayment(VoiceCommand cmd, string t)
    {
        var nm = Regex.Match(t, @"\d+(?:\.\d+)?");
        if (nm.Success) cmd.Amount = Dec(nm.Value);
        var name = t.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !Stop.Contains(x) && !IsNumber(x)).Take(3).ToList();
        cmd.CustomerName = name.Count > 0 ? ToTitle(string.Join(' ', name)) : null;
    }

    private static void ParseProduct(VoiceCommand cmd, string t)
    {
        var work = t;
        work = Extract(work, @"(?:purchase\s+price|buying\s+price|cost\s+price|cost|purchase|خرید)\s*(?:is\s*)?(\d+(?:\.\d+)?)",
                       v => cmd.PurchasePrice = v);
        work = Extract(work, @"(?:sale\s+price|selling\s+price|price|rate|\bat\b|@|قیمت|ریٹ)\s*(?:is\s*)?(\d+(?:\.\d+)?)",
                       v => cmd.UnitPrice = v);

        var sm = Regex.Match(work, $@"(?:stock|quantity|qty|opening|اسٹاک|مقدار)\s*(?:is\s*)?(\d+(?:\.\d+)?)(?:\s*({UnitPattern})(?=\s|$))?");
        if (sm.Success)
        {
            cmd.Quantity = Dec(sm.Groups[1].Value);
            if (sm.Groups[2].Success) cmd.Unit = MapUnit(sm.Groups[2].Value);
            work = work.Remove(sm.Index, sm.Length);
        }
        else work = TakeQuantity(cmd, work);

        var cm = Regex.Match(work, @"(?:category|کیٹگری|قسم)\s+(\S+)");
        if (cm.Success) { cmd.Category = ToTitle(cm.Groups[1].Value); work = work.Remove(cm.Index, cm.Length); }

        var rest = work.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(x => !Stop.Contains(x) && !IsNumber(x)).ToList();
        cmd.ProductName = rest.Count > 0 ? ToTitle(string.Join(' ', rest)) : null;
    }

    private static void ParseCustomer(VoiceCommand cmd, string t)
    {
        var work = t;
        work = Extract(work, @"(?:credit\s+limit|limit|حد|لمٹ)\s*(?:is\s*)?(\d+(?:\.\d+)?)", v => cmd.CreditLimit = v);
        work = Extract(work, @"(?:balance|owes|owe|due|بقایا|باقی)\s*(?:is\s*)?(\d+(?:\.\d+)?)", v => cmd.Amount = v);
        var rest = work.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(x => !Stop.Contains(x) && !IsNumber(x)).Take(3).ToList();
        cmd.CustomerName = rest.Count > 0 ? ToTitle(string.Join(' ', rest)) : null;
    }

    // ---------------------------------------------------------------- helpers
    private static string TakeQuantity(VoiceCommand cmd, string work)
    {
        var qm = Regex.Match(work, $@"(\d+(?:\.\d+)?)\s*({UnitPattern})(?=\s|$)");
        if (qm.Success)
        {
            cmd.Quantity = Dec(qm.Groups[1].Value);
            cmd.Unit = MapUnit(qm.Groups[2].Value);
            return work.Remove(qm.Index, qm.Length);
        }
        var nm = Regex.Match(work, @"\d+(?:\.\d+)?");
        if (nm.Success) { cmd.Quantity = Dec(nm.Value); return work.Remove(nm.Index, nm.Length); }
        return work;
    }

    private static string Extract(string work, string pattern, Action<decimal> set)
    {
        var m = Regex.Match(work, pattern);
        if (!m.Success) return work;
        set(Dec(m.Groups[1].Value));
        return work.Remove(m.Index, m.Length);
    }

    private static string MapUnit(string u) => u switch
    {
        "kg" or "kgs" or "kilo" or "kilos" or "kilogram" or "kilograms" or "کلو" => "kg",
        "gram" or "grams" or "gm" or "g" or "گرام" => "gram",
        "litre" or "litres" or "liter" or "liters" or "ltr" or "l" or "لیٹر" => "litre",
        "dozen" or "dozens" or "درجن" => "dozen",
        "pack" or "packs" or "packet" or "packets" or "پیکٹ" => "pack",
        "box" or "boxes" or "ڈبہ" or "ڈبے" => "box",
        _ => "pcs"
    };

    private static bool Has(string t, params string[] words) =>
        words.Any(w => w.Any(c => c > 127) ? t.Contains(w) : Regex.IsMatch(t, $@"\b{Regex.Escape(w)}\b"));

    private static decimal Dec(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);
    private static bool IsNumber(string s) => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    private static string ToTitle(string s) =>
        s.All(c => c < 128) ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s) : s;

    private static string Normalize(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s.ToLowerInvariant())
        {
            if (ch >= '۰' && ch <= '۹') sb.Append((char)('0' + (ch - '۰')));       // Persian/Urdu digits
            else if (ch >= '٠' && ch <= '٩') sb.Append((char)('0' + (ch - '٠')));   // Arabic-Indic digits
            else sb.Append(ch);
        }
        var t = sb.ToString();
        t = Regex.Replace(t, @"(?<=\d),(?=\d{3}\b)", "");          // 2,500 -> 2500
        t = Regex.Replace(t, @"[!?؟،۔;:,""'()]", " ");
        t = Regex.Replace(t, @"(?<!\d)\.|\.(?!\d)", " ");           // keep decimal points only
        return Regex.Replace(t, @"\s+", " ").Trim();
    }

    /// <summary>"one hundred and fifty" -> "150", "ڈیڑھ سو" -> "150", "2 thousand" -> "2000".</summary>
    private static string ConvertNumberWords(string text)
    {
        var output = new List<string>();
        decimal total = 0, current = 0;
        bool inNum = false, lastWasDigit = false;

        void Flush()
        {
            if (inNum) { output.Add((total + current).ToString(CultureInfo.InvariantCulture)); total = 0; current = 0; inNum = false; }
            lastWasDigit = false;
        }

        foreach (var tok in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Numbers.TryGetValue(tok, out var v)) { current += v; inNum = true; lastWasDigit = false; }
            else if (tok is "hundred" or "سو") { current = (current == 0 ? 1 : current) * 100; inNum = true; lastWasDigit = false; }
            else if (tok is "thousand" or "ہزار") { total += (current == 0 ? 1 : current) * 1000; current = 0; inNum = true; lastWasDigit = false; }
            else if (tok is "lakh" or "lac" or "لاکھ") { total += (current == 0 ? 1 : current) * 100000; current = 0; inNum = true; lastWasDigit = false; }
            else if (inNum && !lastWasDigit && tok is "and" or "a") { /* "one hundred and fifty", "one and a half" */ }
            else if (decimal.TryParse(tok, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            {
                if (lastWasDigit) Flush();
                current += d; inNum = true; lastWasDigit = true;
            }
            else { Flush(); output.Add(tok); }
        }
        Flush();
        return string.Join(' ', output);
    }
}
