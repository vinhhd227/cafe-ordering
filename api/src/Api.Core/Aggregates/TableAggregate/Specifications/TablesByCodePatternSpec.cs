namespace Api.Core.Aggregates.TableAggregate.Specifications;

/// <summary>
///   Tìm bàn theo code (partial match, case-insensitive), không lấy bàn đã xóa
/// </summary>
public class TablesByCodePatternSpec : Specification<Table>
{
  public TablesByCodePatternSpec(string code)
  {
    var lower = code.ToLowerInvariant();
    Query.Where(t => t.Code.ToLower().Contains(lower) && !t.IsDeleted);
  }
}
