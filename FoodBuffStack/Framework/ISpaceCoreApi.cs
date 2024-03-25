using System;

namespace FoodBuffStack.Framework
{
    public interface ISpaceCoreApi
    {
        public event EventHandler<Action<string, Action>> AdvancedInteractionStarted;
    }
}
