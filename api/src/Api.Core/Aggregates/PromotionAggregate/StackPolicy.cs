namespace Api.Core.Aggregates.PromotionAggregate;

public class StackPolicy : SmartEnum<StackPolicy>
{
  /// <summary>Không thể kết hợp với bất kỳ chương trình khuyến mãi nào khác trong cùng order.</summary>
  public static readonly StackPolicy Exclusive = new ExclusivePolicy();

  /// <summary>Có thể kết hợp với các chương trình khuyến mãi STACKABLE khác trong cùng order.</summary>
  public static readonly StackPolicy Stackable = new StackablePolicy();

  private StackPolicy(string name, int value) : base(name, value) { }

  private sealed class ExclusivePolicy() : StackPolicy("EXCLUSIVE", 1) { }
  private sealed class StackablePolicy() : StackPolicy("STACKABLE", 2) { }
}
