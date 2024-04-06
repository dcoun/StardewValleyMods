using System;
using FoodBuffStack.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using SObject = StardewValley.Object;

namespace FoodBuffStack
{
  class Utils
  {
    public static ModConfig Config;
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

    readonly TYPE type;
    Buff buff;
    public string QualifiedItemId { get; set; }
    int effectStackCount = 0;

    private readonly static object syncLock = new object();

    public BuffWrapper(TYPE type)
    {
      this.type = type;
    }

    public string GetBuffId()
    {
      return this.type == TYPE.Drink ? "drink" : "food";
    }

    public void SetNullBuff()
    {
      this.QualifiedItemId = null;
      this.buff = null;
      this.effectStackCount = 0;
    }

    private void SetAppliedBuff()
    {
      string buffId = this.GetBuffId();
      string prevSource = this.buff != null ? this.buff.source : null;
      Buff next = Game1.player.buffs.AppliedBuffs.ContainsKey(buffId)
        ? Game1.player.buffs.AppliedBuffs[buffId]
        : null;

      if (next != null && next.millisecondsDuration > 0)
      {
        if (prevSource != next.source)
        {
          this.effectStackCount = 0;
        }
        this.buff = next;
      }
      else if (this.buff != null)
      {
        // EndDuration
        this.SetNullBuff();
      }
    }

    private Buff BuildNewBuff(Buff next)
    {
      BuffEffects effects = buff.effects;
      if (effectStackCount < Utils.Config.MaxAttributesStackSize - 1)
      {
        this.effectStackCount++;
        effects.Add(next.effects);
      }

      return new Buff(
        buff.id,
        buff.source,
        buff.displaySource,
        buff.millisecondsDuration + next.millisecondsDuration,
        buff.iconTexture,
        buff.iconSheetIndex,
        effects,
        false,
        buff.displayName,
        buff.description
      );
    }

    private void ApplyBuff()
    {
      if (this.buff == null)
      {
        return;
      }

      Game1.player.applyBuff(this.buff);
    }

    public void ProcessNextBuff(Buff next)
    {
      // TODO: 싱글쓰레드 보장이면 없어도됨
      lock (BuffWrapper.syncLock)
      {
        if (EventStatus == STATUS.DayEnding)
        {
          return;
        }
        else if (EventStatus == STATUS.DayStarted)
        {
          // 이전 buff가 있으면 player에게 적용
          this.ApplyBuff();
        }
        else if (EventStatus == STATUS.OnUpdate && next == null)
        {
          this.SetAppliedBuff();
        }
        else if (next != null)
        {
          // 새로운 buff
          if (this.buff == null || this.buff.source != next.source)
          {
            this.SetAppliedBuff();
          }
          else if (this.buff.source == next.source)
          {
            this.buff = this.BuildNewBuff(next);
            this.ApplyBuff();
          }
        }
      }
    }

    public FoodBuffStackSave GetSaveData()
    {
      FoodBuffStackSave saveData = new FoodBuffStackSave();
      if (this.buff != null && this.QualifiedItemId != null && this.buff.millisecondsDuration > 0)
      {
        saveData.QualifiedItemId = this.QualifiedItemId;
        saveData.Duration = this.buff.millisecondsDuration;
        saveData.IconSheetIndex = this.buff.iconSheetIndex;
        saveData.EffectStackCount = this.effectStackCount;
      }

      return saveData;
    }

    public void LoadFromSaveData(FoodBuffStackSave saveData)
    {
      if (saveData.QualifiedItemId == null || saveData.Duration <= 0)
      {
        return;
      }

      Item item = ItemRegistry.Create(saveData.QualifiedItemId, allowNull: true);
      if (item == null)
      {
        return;
      }

      this.QualifiedItemId = saveData.QualifiedItemId;
      foreach (Buff foodOrDrinkBuff in item.GetFoodOrDrinkBuffs())
      {
        BuffEffects effects = new BuffEffects();
        for (int i = 0; i < saveData.EffectStackCount; i++)
        {
          this.effectStackCount++;
          effects.Add(foodOrDrinkBuff.effects);
        }

        this.buff = new Buff(
          foodOrDrinkBuff.id,
          foodOrDrinkBuff.source,
          foodOrDrinkBuff.displaySource,
          saveData.Duration,
          foodOrDrinkBuff.iconTexture,
          foodOrDrinkBuff.iconSheetIndex,
          effects,
          false,
          foodOrDrinkBuff.displayName,
          foodOrDrinkBuff.description
        );
        this.ApplyBuff();
      }
    }
  }

  public class ModEntry : Mod
  {
    /// <summary>The mod configuration.</summary>
    public ModConfig Config;

    private BuffWrapper DrinkBuff = new(BuffWrapper.TYPE.Drink);
    private BuffWrapper FoodBuff = new(BuffWrapper.TYPE.Food);

    public override void Entry(IModHelper helper)
    {
      this.Config = helper.ReadConfig<ModConfig>();
      Utils.Monitor = this.Monitor;

      helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
      helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
      helper.Events.GameLoop.DayStarted += this.OnDayStarted;
      helper.Events.GameLoop.DayEnding += this.OnDayEnding;

      helper.Events.GameLoop.Saved += this.OnSaved;
      helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
      helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
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
        Utils.Config = this.Config;
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
            DrinkBuff.QualifiedItemId = @object.QualifiedItemId;
            DrinkBuff.ProcessNextBuff(foodOrDrinkBuff);
          }
          else if (foodOrDrinkBuff.id == "food")
          {
            FoodBuff.QualifiedItemId = @object.QualifiedItemId;
            FoodBuff.ProcessNextBuff(foodOrDrinkBuff);
          }
        }
      }
    }

    private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
    {
      if (Context.IsWorldReady)
      {
        BuffWrapper.EventStatus = BuffWrapper.STATUS.OnUpdate;
        DrinkBuff.ProcessNextBuff(null);
        FoodBuff.ProcessNextBuff(null);
      }
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
      BuffWrapper.EventStatus = BuffWrapper.STATUS.DayStarted;
      DrinkBuff.ProcessNextBuff(null);
      FoodBuff.ProcessNextBuff(null);
    }

    private void OnDayEnding(object sender, DayEndingEventArgs e)
    {
      BuffWrapper.EventStatus = BuffWrapper.STATUS.DayEnding;
      DrinkBuff.ProcessNextBuff(null);
      FoodBuff.ProcessNextBuff(null);
    }

    private string GetSaveDataFileName()
    {
      return $"FoodBuffStackLast-{Game1.player.Name}-{Game1.player.farmName}.json";
    }

    private void OnSaved(object sender, SavedEventArgs e)
    {
      FoodBuffStackSaveData saveData = new FoodBuffStackSaveData
      {
        Drink = this.DrinkBuff.GetSaveData(),
        Food = this.FoodBuff.GetSaveData()
      };
      this.Helper.Data.WriteJsonFile(GetSaveDataFileName(), saveData);
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
      FoodBuffStackSaveData data = this.Helper.Data.ReadJsonFile<FoodBuffStackSaveData>(GetSaveDataFileName());
      if (data != null)
      {
        DrinkBuff.LoadFromSaveData(data.Drink);
        FoodBuff.LoadFromSaveData(data.Food);
      }
    }

    private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
      DrinkBuff.SetNullBuff();
      FoodBuff.SetNullBuff();
    }
  }
}
