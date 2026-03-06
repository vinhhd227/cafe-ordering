using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.OrderAggregate;

public class IceLevel : SmartEnum<IceLevel>
{
  public static readonly IceLevel Less   = new LessIce();
  public static readonly IceLevel Normal = new NormalIce();
  public static readonly IceLevel More   = new MoreIce();

  private IceLevel(string name, int value) : base(name, value) { }

  private sealed class LessIce()   : IceLevel("LESS",   1) { }
  private sealed class NormalIce() : IceLevel("NORMAL", 2) { }
  private sealed class MoreIce()   : IceLevel("MORE",   3) { }
}
