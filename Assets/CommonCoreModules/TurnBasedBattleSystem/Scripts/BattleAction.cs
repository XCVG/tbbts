using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{

    public abstract class BattleAction
    {
        protected BattleContext Context;

        public virtual bool IsReorderable { get; }
        public virtual int Priority { get; }

        public virtual void Start(BattleContext context)
        {
            Context = context;
        }

        public virtual void Update()
        {
            //nop
        }

    }

    public class FleeAction : BattleAction
    {

    }

    public abstract class BaseAttackAction : BattleAction
    {
        public string AttackingParticipant { get; set; }
        public string DefendingParticipant { get; set; }
        public int AttackPriority { get; set; }

        public override bool IsReorderable => true;

        public override int Priority => AttackPriority; //I don't like this, we'll see if it lasts
    }

    public class GuardAction : BattleAction
    {
        public string GuardingParticipant { get; set; }

        public override bool IsReorderable => false;
    }

    //TODO probably move this over to another file
    //and, like, y'know, implement it
    public class SimpleAttackAction : BaseAttackAction
    {

    }


}
