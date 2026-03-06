using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.OrderAggregate;

public class SugarLevel : SmartEnum<SugarLevel>
{
  public static readonly SugarLevel Less   = new LessSugar();
  public static readonly SugarLevel Normal = new NormalSugar();
  public static readonly SugarLevel More   = new MoreSugar();

  private SugarLevel(string name, int value) : base(name, value) { }

  private sealed class LessSugar()   : SugarLevel("LESS",   1) { }
  private sealed class NormalSugar() : SugarLevel("NORMAL", 2) { }
  private sealed class MoreSugar()   : SugarLevel("MORE",   3) { }
}
