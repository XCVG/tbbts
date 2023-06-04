using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{

    public abstract class BattleAction
    {
        protected BattleContext Context;

        public virtual void Start(BattleContext context)
        {
            Context = context;
        }

        public abstract void Update();

    }


}
