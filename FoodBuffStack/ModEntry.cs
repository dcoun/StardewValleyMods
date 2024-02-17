using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using Newtonsoft;

namespace FoodBuffStack
{
  public class ModEntry : Mod
  {
    private static ModConfig Config;
    private static Buff PrevFood;
    private static Buff PrevDrink;

    public override void Entry(IModHelper helper)
    {
      Config = this.Helper.ReadConfig<ModConfig>();
      helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }

    private bool isSameSource(Buff newBuff, Buff oldBuff)
    {
      return newBuff.source == oldBuff.source;
    }

    private bool isNewSource(Buff newBuff, Buff oldBuff)
    {
      boolean isSameSource = isSameSource(newBuff, oldBuff);
      if (!isSameSource)
      {
        return true;
      }
      if (newBuff.millisecondsDuration > oldBuff.millisecondsDuration)
      {
        return true;
      }

      return false;
    }

    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
      // Config.MaxEffectStackSize
      Buff newFood = Game1.buffsDisplay.food;
      Buff newDrink = Game1.buffsDisplay.drink;
      if (newFood != null)
      {
        if (PrevFood == null || !isNewSource(PrevFood, newFood))
        {
          PrevFood = newFood;
          newFood = null;
        }
      }
      if (newDrink != null)
      {
        if (PrevDrink == null)
        {
          PrevDrink = newDrink;
          newDrink = null;
        }
        else if (isNewSource(PrevDrink, PrevDrink))
        {

        }
      }
    }

    private void Log(object obj)
    {
      this.Monitor.Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), LogLevel.Info);
    }
  }
}
