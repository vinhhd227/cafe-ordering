namespace Api.Core.Aggregates.PromotionAggregate;

public class CodeVisibility : SmartEnum<CodeVisibility>
{
  /// <summary>Mã hiển thị công khai — khách có thể duyệt danh sách để chọn.</summary>
  public static readonly CodeVisibility Public = new PublicVisibility();

  /// <summary>Mã riêng tư — khách phải nhập chính xác (ví dụ: theo dõi fanpage).</summary>
  public static readonly CodeVisibility Private = new PrivateVisibility();

  private CodeVisibility(string name, int value) : base(name, value) { }

  private sealed class PublicVisibility()  : CodeVisibility("PUBLIC",  1) { }
  private sealed class PrivateVisibility() : CodeVisibility("PRIVATE", 2) { }
}
