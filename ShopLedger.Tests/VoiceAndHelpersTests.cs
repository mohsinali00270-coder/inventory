using ShopLedger.Services;
using Xunit;

namespace ShopLedger.Tests;

public class FuzzyMatcherTests
{
    private record Item(string Name);
    private static readonly Item[] Items = { new("Ali Khan"), new("Sara Bibi"), new("Bilal Ahmed") };

    [Fact] public void Exact_match() => Assert.Equal("Sara Bibi", FuzzyMatcher.Best(Items, i => i.Name, "sara bibi")!.Name);
    [Fact] public void Prefix_match() => Assert.Equal("Bilal Ahmed", FuzzyMatcher.Best(Items, i => i.Name, "bilal")!.Name);
    [Fact] public void Small_typo_is_tolerated() => Assert.Equal("Sara Bibi", FuzzyMatcher.Best(Items, i => i.Name, "sara bibe")!.Name);
    [Fact] public void Unrelated_name_returns_null() => Assert.Null(FuzzyMatcher.Best(Items, i => i.Name, "zzzzzz"));
    [Fact] public void Empty_query_returns_null() => Assert.Null(FuzzyMatcher.Best(Items, i => i.Name, "  "));
}

public class VoiceCommandParserTests
{
    [Fact]
    public void Credit_word_gives_credit_sale() =>
        Assert.Equal(VoiceIntent.CreditSale, VoiceCommandParser.Parse("sold 2 kg sugar to Ali on credit").Intent);

    [Fact]
    public void Cash_word_gives_cash_sale() =>
        Assert.Equal(VoiceIntent.CashSale, VoiceCommandParser.Parse("cash sale 3 soap").Intent);

    [Fact]
    public void Payment_phrase_extracts_amount()
    {
        var c = VoiceCommandParser.Parse("Ali paid 500 rupees");
        Assert.Equal(VoiceIntent.Payment, c.Intent);
        Assert.Equal(500m, c.Amount);
    }

    [Fact]
    public void Number_words_are_converted()
    {
        var c = VoiceCommandParser.Parse("Ali paid five hundred");
        Assert.Equal(VoiceIntent.Payment, c.Intent);
        Assert.Equal(500m, c.Amount);
    }

    [Fact]
    public void Hint_forces_the_intent() =>
        Assert.Equal(VoiceIntent.AddCustomer, VoiceCommandParser.Parse("Bilal", VoiceIntent.AddCustomer).Intent);

    [Fact]
    public void Gibberish_is_unknown() =>
        Assert.Equal(VoiceIntent.Unknown, VoiceCommandParser.Parse("hello there").Intent);
}

public class DateRangeHelperTests
{
    [Fact]
    public void Today_is_a_single_day()
    {
        var (from, to) = DateRangeHelper.Resolve(DateRangeHelper.Label("Today"), default, default);
        Assert.Equal(DateTime.Today, from);
        Assert.Equal(DateTime.Today, to);
    }

    [Fact]
    public void Last7_spans_seven_days()
    {
        var (from, to) = DateRangeHelper.Resolve(DateRangeHelper.Label("Last7"), default, default);
        Assert.Equal(6, (to!.Value - from!.Value).Days);
    }

    [Fact]
    public void All_is_open_ended()
    {
        var (from, to) = DateRangeHelper.Resolve(DateRangeHelper.Label("All"), default, default);
        Assert.Null(from); Assert.Null(to);
    }
}
