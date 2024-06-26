﻿using YouBoss.Common.Tools.StateMachines;

namespace YouBoss.Content.NPCs.Bosses
{
    public class BossAIState<T>(T identifier) : IState<T> where T : struct
    {
        public T Identifier
        {
            get;
            set;
        } = identifier;

        public int Time;

        public void OnPoppedFromStack()
        {
            Time = 0;
        }
    }
}
