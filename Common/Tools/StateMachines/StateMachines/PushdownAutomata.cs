﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace YouBoss.Common.Tools.StateMachines
{
    public class PushdownAutomata<StateWrapper, StateIdentifier> where StateWrapper : IState<StateIdentifier> where StateIdentifier : struct
    {
        /// <summary>
        /// Represents a framework for hijacking a transition's final state selection.
        /// This is useful for allowing states to transition to something customized when its default transition condition has been triggered, without having to duplicate conditions many times.
        /// </summary>
        public class TransitionHijack(Func<StateIdentifier?, StateIdentifier?> selectionHijackFunction, Action<StateIdentifier?> action)
        {
            /// <summary>
            /// What should be done when a hijack is successfully performed.
            /// </summary>
            public Action<StateIdentifier?> HijackAction = action;

            /// <summary>
            /// The selection function. Should return the input argument if nothing is being performed.
            /// </summary>
            public Func<StateIdentifier?, StateIdentifier?> SelectionHijackFunction = selectionHijackFunction;
        }

        /// <summary>
        /// Represents a framework for a state transition's information.
        /// </summary>
        public class TransitionInfo(StateIdentifier? newState, bool rememberPreviousState, Func<bool> transitionCondition, Action transitionCallback = null)
        {
            /// <summary>
            /// The state to transition to.
            /// </summary>
            public StateIdentifier? NewState = newState;

            /// <summary>
            /// Whether the previous state should be retained when the transition happens. If true, this ensures that the previous state is not popped from the stack.
            /// </summary>
            public bool RememberPreviousState = rememberPreviousState;

            /// <summary>
            /// An action that determines any special things that happen after a transition.
            /// </summary>
            public Action TransitionCallback = transitionCallback;

            /// <summary>
            /// Whether the transition is ready to happen.
            /// </summary>
            public Func<bool> TransitionCondition = transitionCondition;
        }

        public PushdownAutomata(StateWrapper initialState)
        {
            StateStack.Push(initialState);
            RegisterState(initialState);
        }

        /// <summary>
        /// A collection of custom states that should be performed when a state is ongoing.
        /// </summary>
        public readonly Dictionary<StateIdentifier, Action> StateBehaviors = [];

        /// <summary>
        /// A list of hijack actions to perform during a state transition.
        /// </summary>
        public List<TransitionHijack> HijackActions = [];

        /// <summary>
        /// A generalized registry of states with individualized data.
        /// </summary>
        public Dictionary<StateIdentifier, StateWrapper> StateRegistry = [];

        /// <summary>
        /// The state stack for the automaton.
        /// </summary>
        public Stack<StateWrapper> StateStack = new();

        protected Dictionary<StateIdentifier, List<TransitionInfo>> transitionTable = [];

        /// <summary>
        /// The current state of the automaton.
        /// </summary>
        public StateWrapper CurrentState => StateStack.Peek();

        public event Action<StateWrapper> OnStatePop;

        public event Action<bool> OnStateTransition;

        public void AddTransitionStateHijack(Func<StateIdentifier?, StateIdentifier?> hijackSelection, Action<StateIdentifier?> hijackAction = null)
        {
            HijackActions.Add(new(hijackSelection, hijackAction));
        }

        public void PerformBehaviors()
        {
            if (StateBehaviors.TryGetValue(CurrentState.Identifier, out Action behavior))
                behavior();
        }

        public void PerformStateTransitionCheck()
        {
            if (!StateStack.Any() || !transitionTable.TryGetValue(CurrentState.Identifier, out List<TransitionInfo> value))
                return;

            List<TransitionInfo> potentialStates = value ?? [];
            List<TransitionInfo> transitionableStates = potentialStates.Where(s => s.TransitionCondition()).ToList();

            if (!transitionableStates.Any())
                return;

            TransitionInfo transition = transitionableStates.First();

            // Pop the previous state if it doesn't need to be remembered.
            if (!transition.RememberPreviousState && StateStack.TryPop(out var oldState))
            {
                OnStatePop?.Invoke(oldState);
                oldState.OnPoppedFromStack();
            }

            // Perform the transition. If there's no state to transition to, simply work down the stack.
            StateIdentifier? newState = transition.NewState;
            var usedHijackAction = HijackActions.FirstOrDefault(h => !h.SelectionHijackFunction(newState).Equals(newState));
            if (usedHijackAction is not null)
            {
                newState = usedHijackAction.SelectionHijackFunction(newState);
                usedHijackAction.HijackAction?.Invoke(newState);
            }
            if (newState is not null)
                StateStack.Push(StateRegistry[newState.Value]);

            // Access the callback, if one is used.
            transition.TransitionCallback?.Invoke();

            OnStateTransition?.Invoke(!transition.RememberPreviousState);

            // Since a transition happened, recursively call Update again.
            // This allows for multiple state transitions to happen in a single frame if necessary.
            PerformStateTransitionCheck();
        }

        public void RegisterState(StateWrapper state) => StateRegistry[state.Identifier] = state;

        public void RegisterStateBehavior(StateIdentifier state, Action behavior)
        {
            StateBehaviors[state] = behavior;
        }

        public void RegisterTransition(StateIdentifier initialState, StateIdentifier? newState, bool rememberPreviousState, Func<bool> transitionCondition, Action transitionCallback = null)
        {
            // Initialize the list of transition states for the initial state if there aren't any yet.
            if (!transitionTable.ContainsKey(initialState))
                transitionTable[initialState] = [];

            // Add to the transition state list.
            transitionTable[initialState].Add(new(newState, rememberPreviousState, transitionCondition, transitionCallback));
        }
    }
}
