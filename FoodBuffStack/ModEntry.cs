using System;
using Microsoft.Xna.Framework;
using FoodBuffStack.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using Newtonsoft;

namespace FoodBuffStack
{
  class Utils
  {
    public static IMonitor Monitor;

    public static void Info(object obj)
    {
      Utils.Monitor.Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), LogLevel.Info);
    }

    public static int[] ArrSum(int[] arr1, int[] arr2)
    {
      int[] result = new int[arr1.Length];
      for (var i = 0; i < arr1.Length; i++)
      {
        result[i] = arr1[i] + arr2[i];
      }

      return result;
    }
  }

  class BuffWrapper
  {
    public enum TYPE
    {
      Drink,
      Food
    };
    public enum STATUS
    {
      DayStarted,
      DayEnding,
      OnUpdate
    };

    public static STATUS EventStatus;

    string flag;
    TYPE type;
    Buff prev;
    int effectStackCount = 1;

    public BuffWrapper(TYPE type)
    {
      this.type = type;
    }

    private bool isSameSource(Buff next)
    {
      return prev != null && next != null && prev.source == next.source;
    }

    private bool isNewItem(Buff next)
    {
      return prev == null || next.which == -1 || !isSameSource(next);
    }

    private Buff buildNewBuff(Buff next)
    {
      bool isSameSource = this.isSameSource(next);

      // extends millisecondsDuration
      int millisecondsDuration = isSameSource
        ? prev.millisecondsDuration + next.millisecondsDuration
        : next.millisecondsDuration;
      int totalMillisecondsDuration = isSameSource
        ? prev.totalMillisecondsDuration + next.totalMillisecondsDuration
        : next.totalMillisecondsDuration;
      int minutesDuration = millisecondsDuration / 1000;
      int which = isSameSource ? prev.which : 1;

      // extends Attributes
      int[] buffAttributes;
      if (!isSameSource)
      {
        flag = "NewItem";
        buffAttributes = next.buffAttributes;
        effectStackCount = 1;
      }
      else if (effectStackCount < ModEntry.Config.MaxAttributesStackSize)
      {
        flag = "NewItem SameSource EffectStack";
        buffAttributes = Utils.ArrSum(
          prev.buffAttributes,
          next.buffAttributes
        );
        effectStackCount++;
      }
      else
      {
        flag = "NewItem SameSource";
        buffAttributes = prev.buffAttributes;
      }

      Buff newBuff = new Buff(
        buffAttributes[0],
        buffAttributes[1],
        buffAttributes[2],
        buffAttributes[3],
        buffAttributes[4],
        buffAttributes[5],
        buffAttributes[6],
        buffAttributes[7],
        buffAttributes[8],
        buffAttributes[9],
        buffAttributes[10],
        buffAttributes[11],
        minutesDuration: minutesDuration,
        source: next.source,
        displaySource: next.displaySource
      );
      newBuff.millisecondsDuration = millisecondsDuration;
      newBuff.totalMillisecondsDuration = totalMillisecondsDuration;
      newBuff.which = which;

      return newBuff;
    }

    public void applyBuff()
    {
      if (prev == null)
      {
        return;
      }

      if (type == TYPE.Drink)
      {
        Game1.buffsDisplay.tryToAddDrinkBuff(prev);
      }
      else
      {
        Game1.buffsDisplay.tryToAddFoodBuff(prev, prev.millisecondsDuration / 1000);
      }
    }

    public void processNextBuff(Buff next)
    {
      if (
        EventStatus == STATUS.DayEnding
        || (prev == null && next == null)
        )
      {
        return;
      }

      if (prev != null && next == null)
      {
        flag = "EndDuration";
        prev = null;
        next = null;
        effectStackCount = 1;
      }
      else if (isNewItem(next))
      {
        // 새로운 음식을 먹은건지 판단이 불가해서 Buff의 which를 바꿔놓음
        prev = buildNewBuff(next);
        applyBuff();
      }

      if (flag != null)
      {
        // Game1.buffsDisplay.syncIcons();
        Utils.Monitor.Log($"{flag} {type}", LogLevel.Info);
        Utils.Info(prev);
        Utils.Info(next);
        Utils.Info(effectStackCount);
        flag = null;
      }
    }
  }

  public class ModEntry : Mod
  {
    public static ModConfig Config;
    private static BuffWrapper DrinkBuff = new BuffWrapper(BuffWrapper.TYPE.Drink);
    private static BuffWrapper FoodBuff = new BuffWrapper(BuffWrapper.TYPE.Food);

    public override void Entry(IModHelper helper)
    {
      Config = this.Helper.ReadConfig<ModConfig>();
      Utils.Monitor = this.Monitor;

      helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
      helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
      helper.Events.GameLoop.DayStarted += this.OnDayStarted;
      helper.Events.GameLoop.DayEnding += this.OnDayEnding;
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
      // get Generic Mod Config Menu's API (if it's installed)
      var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
      if (configMenu is null)
        return;

      // register mod
      configMenu.Register(
          mod: this.ModManifest,
          reset: () => ModEntry.Config = new ModConfig(),
          save: () => this.Helper.WriteConfig(ModEntry.Config)
      );

      // add some config options
      configMenu.AddNumberOption(
          mod: this.ModManifest,
          name: () => "MaxAttributesStackSize",
          tooltip: () => "버프 Attributes 스택 크기",
          getValue: () => ModEntry.Config.MaxAttributesStackSize,
          setValue: value => ModEntry.Config.MaxAttributesStackSize = value
      );
    }

    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
      if (Context.IsWorldReady)
      {
        DrinkBuff.processNextBuff(Game1.buffsDisplay.drink);
        FoodBuff.processNextBuff(Game1.buffsDisplay.food);
      }
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
      BuffWrapper.EventStatus = BuffWrapper.STATUS.DayStarted;
      DrinkBuff.applyBuff();
      FoodBuff.applyBuff();
    }

    private void OnDayEnding(object sender, DayEndingEventArgs e)
    {
      BuffWrapper.EventStatus = BuffWrapper.STATUS.DayEnding;
    }
  }
}
