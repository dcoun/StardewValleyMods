using System;
using Microsoft.Xna.Framework;
using FoodBuffStack.Framework;
using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using SObject = StardewValley.Object;
using System.Linq;
using StardewValley.Buffs;

namespace FoodBuffStack
{
  class Utils
  {
    public static IMonitor Monitor;

    public static void Info(object obj)
    {
      try
      {
        Utils.Monitor.Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), LogLevel.Info);
      }
      catch
      {
        Utils.Monitor.Log(obj.ToString(), LogLevel.Info);
      }
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
    readonly TYPE type;
    Buff buff;
    int effectStackCount = 1;

    public BuffWrapper(TYPE type)
    {
      this.type = type;
    }

    private bool IsSameSource(Buff next)
    {
      return buff != null && next != null && buff.source == next.source;
    }

    private bool IsNewItem(Buff next)
    {
      return buff == null || buff.id != next.id || !IsSameSource(next);
    }

    private Buff BuildNewBuff(Buff next)
    {
      bool isSameSource = this.IsSameSource(next);
      return next;

      // // extends millisecondsDuration
      // int millisecondsDuration = isSameSource
      //   ? prev.millisecondsDuration + next.millisecondsDuration
      //   : next.millisecondsDuration;
      // int totalMillisecondsDuration = isSameSource
      //   ? prev.totalMillisecondsDuration + next.totalMillisecondsDuration
      //   : next.totalMillisecondsDuration;
      // int minutesDuration = millisecondsDuration / 1000;
      // int which = isSameSource ? prev.which : 1;

      // // extends Attributes
      // int[] buffAttributes;
      // if (!isSameSource)
      // {
      //   flag = "NewItem";
      //   buffAttributes = next.buffAttributes;
      //   effectStackCount = 1;
      // }
      // else if (effectStackCount < ModEntry.Config.MaxAttributesStackSize)
      // {
      //   flag = "NewItem SameSource EffectStack";
      //   buffAttributes = Utils.ArrSum(
      //     prev.buffAttributes,
      //     next.buffAttributes
      //   );
      //   effectStackCount++;
      // }
      // else
      // {
      //   flag = "NewItem SameSource";
      //   buffAttributes = prev.buffAttributes;
      // }

      // Buff newBuff = new Buff(
      //   buffAttributes[0],
      //   buffAttributes[1],
      //   buffAttributes[2],
      //   buffAttributes[3],
      //   buffAttributes[4],
      //   buffAttributes[5],
      //   buffAttributes[6],
      //   buffAttributes[7],
      //   buffAttributes[8],
      //   buffAttributes[9],
      //   buffAttributes[10],
      //   buffAttributes[11],
      //   minutesDuration: minutesDuration,
      //   source: next.source,
      //   displaySource: next.displaySource
      // );
      // newBuff.millisecondsDuration = millisecondsDuration;
      // newBuff.totalMillisecondsDuration = totalMillisecondsDuration;
      // newBuff.which = which;

      // return newBuff;
    }

    public void ApplyBuff()
    {
      if (buff == null)
      {
        return;
      }

      if (type == TYPE.Drink)
      {
        // Game1.buffsDisplay.tryToAddDrinkBuff(prev);
      }
      else
      {
        // Game1.buffsDisplay.tryToAddFoodBuff(prev, prev.millisecondsDuration / 1000);
      }
    }

    public void ProcessNextBuff(Buff next)
    {
      Utils.Info(next);
      if (EventStatus == STATUS.DayEnding || (buff == null && next == null))
      {
        return;
      }

      if (buff != null)
      {
        Utils.Info($"prevBuff {buff.source} {buff.getTimeLeft()}");
        Utils.Info($"nextBuff {next.source} {next.getTimeLeft()}");

        if (buff.source != next.source)
        {
          Buff b = Game1.player.buffs.AppliedBuffs[next.id];
          buff = b;
        }
      }
      else if (buff == null)
      {
        // 새로운 버프
        Buff b = Game1.player.buffs.AppliedBuffs[next.id];
        buff = b;

        // buff = next;
        Utils.Info($"prevBuff {buff.source} {buff.getTimeLeft()}");
        Utils.Info($"nextBuff {next.source} {next.getTimeLeft()}");
      }

      if (buff != null && next == null)
      {
        flag = "EndDuration";
        buff = null;
        next = null;
        effectStackCount = 1;
      }
      else if (IsNewItem(next))
      {
        // 새로운 음식을 먹은건지 판단이 불가해서 Buff의 which를 바꿔놓음
        // prev = BuildNewBuff(next);
        // ApplyBuff();
      }

      if (flag != null)
      {
        // Game1.buffsDisplay.syncIcons();
        Utils.Monitor.Log($"{flag} {type}", LogLevel.Info);
        Utils.Info(buff);
        Utils.Info(next);
        Utils.Info(effectStackCount);
        flag = null;
      }
    }
  }

  public class ModEntry : Mod
  {
    /// <summary>The mod configuration.</summary>
    private ModConfig Config;

    private BuffWrapper DrinkBuff = new(BuffWrapper.TYPE.Drink);
    private BuffWrapper FoodBuff = new(BuffWrapper.TYPE.Drink);

    private static System.Collections.Generic.IDictionary<string, Buff> PrevBuffs;

    public override void Entry(IModHelper helper)
    {
      this.Config = helper.ReadConfig<ModConfig>();
      Utils.Monitor = this.Monitor;

      helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
      helper.Events.GameLoop.DayStarted += this.OnDayStarted;
      helper.Events.GameLoop.DayEnding += this.OnDayEnding;
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
      // get Generic Mod Config Menu's API (if it's installed)
      var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
      if (configMenu is not null)
      {
        // register mod
        configMenu.Register(
            mod: this.ModManifest,
            reset: () => this.Config = new ModConfig(),
            save: () => this.Helper.WriteConfig(this.Config)
        );

        // add some config options
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "MaxAttributesStackSize",
            tooltip: () => "버프 Attributes 스택 크기",
            getValue: () => this.Config.MaxAttributesStackSize,
            setValue: value => this.Config.MaxAttributesStackSize = value
        );
      }

      ISpaceCoreApi spaceCoreApi = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
      if (spaceCoreApi is not null)
      {
        SpaceCore.Events.SpaceEvents.OnItemEaten += OnItemEaten;
      }
      else
      {
        // Skip patcher mod behaviours if we fail to load the objects
        this.Monitor.Log($"Failed to register objects with SpaceCore.{Environment.NewLine}{e}", LogLevel.Error);
      }
    }

    private void OnItemEaten(object sender, EventArgs e)
    {
      if (Game1.player.itemToEat is SObject @object)
      {
        foreach (Buff foodOrDrinkBuff in @object.GetFoodOrDrinkBuffs())
        {
          if (foodOrDrinkBuff.id == "drink")
          {
            DrinkBuff.ProcessNextBuff(foodOrDrinkBuff);
          }
          else if (foodOrDrinkBuff.id == "food")
          {
            FoodBuff.ProcessNextBuff(foodOrDrinkBuff);
          }

          // Buff nextBuff = Game1.player.buffs.AppliedBuffs[foodOrDrinkBuff.id];


          // Utils.Info($"HasBuff: {Game1.player.hasBuff(foodOrDrinkBuff.id)}");
          // Buff prev = Game1.player.buffs.AppliedBuffs[foodOrDrinkBuff.id];

          // Utils.Info($"next id: {foodOrDrinkBuff.id} source: {foodOrDrinkBuff.source}");
          // Utils.Info($"prev id: {prev.id} source: {prev.source}");
          // Utils.Info(Game1.player.buffs.AppliedBuffIds.ToArray());
        }
      }
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
      BuffWrapper.EventStatus = BuffWrapper.STATUS.DayStarted;
      // DrinkBuff.ApplyBuff();
      // FoodBuff.ApplyBuff();
    }

    private void OnDayEnding(object sender, DayEndingEventArgs e)
    {
      BuffWrapper.EventStatus = BuffWrapper.STATUS.DayEnding;
    }
  }
}
