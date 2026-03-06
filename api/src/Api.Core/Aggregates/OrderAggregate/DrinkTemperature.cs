using Ardalis.SmartEnum;

namespace Api.Core.Aggregates.OrderAggregate;

public class DrinkTemperature : SmartEnum<DrinkTemperature>
{
  public static readonly DrinkTemperature Hot  = new HotTemp();
  public static readonly DrinkTemperature Cold = new ColdTemp();

  private DrinkTemperature(string name, int value) : base(name, value) { }

  private sealed class HotTemp()  : DrinkTemperature("HOT",  1) { }
  private sealed class ColdTemp() : DrinkTemperature("COLD", 2) { }
}
