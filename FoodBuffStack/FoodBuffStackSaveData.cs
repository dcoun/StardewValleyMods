namespace FoodBuffStack
{
  class FoodBuffStackSaveData
  {
    public FoodBuffStackSave Drink { get; set; }
    public FoodBuffStackSave Food { get; set; }
  }

  // 아 JSON 어레이면 되는데 말이지
  class FoodBuffStackSave
  {
    public string QualifiedItemId { get; set; }
    public int Duration { get; set; }
    public int IconSheetIndex { get; set; }
    public int EffectStackCount { get; set; }
  }
}
