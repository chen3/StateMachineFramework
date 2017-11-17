using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.Logging;
using QiDiTu.StateMachineFramework.Attributes;
using QiDiTu.StateMachineFramework.Collections;
using QiDiTu.StateMachineFramework.Exceptions;

namespace QiDiTu.StateMachineFramework
{
    /// <summary>
    /// String state machine
    /// </summary>
    /// <remarks>Thread Unsafe</remarks>
    public class StringStateMachine
    {
        private static readonly ILog logger = LogManager.GetLogger<StringStateMachine>();

        /// <see cref="AddAction(string, ActionType, Action)"/>
        /// <see cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <see cref="RemoveAction(string, ActionType, Action)"/>
        /// <see cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <seealso cref="ActionAttribute"/>
        public enum ActionType
        {
            EnterState, LeaveState
        }

        public static class Builder
        {
            /// <summary>
            /// Creates a <see cref="StringStateMachine"/> with the arguments
            /// </summary>
            /// <param name="extractAttributesObject">The object is used to extract state machine attributs.</param>
            /// <exception cref="ArgumentNullException"><paramref name="extractAttributesObject"/> is <see langword="null"/></exception>
            /// <returns>A <see cref="StringStateMachine"/> object</returns>
            public static StringStateMachine Create(object extractAttributesObject)
            {
                if (extractAttributesObject == null)
                {
                    throw new ArgumentNullException(nameof(extractAttributesObject));
                }
                return new StringStateMachine(extractAttributesObject);
            }
        }

        /// <summary>
        /// Construction method
        /// </summary>
        /// <param name="extractAttributesObject">The object is used to extract state machine attributs.</param>
        private StringStateMachine(object extractAttributesObject)
        {
            Debug.Assert(extractAttributesObject != null);
            Init(extractAttributesObject);
            CurrentState = InitialState;
        }

        /// <summary>
        /// Construction method
        /// </summary>
        protected StringStateMachine()
        {
            Init(this);
            CurrentState = InitialState;
        }

        private string initialState;
        /// <summary>
        /// The initialization state of the state machine
        /// </summary>
        /// <remarks>Only write once</remarks>
        public string InitialState
        {
            get
            {
                Debug.Assert(!string.IsNullOrEmpty(initialState));
                return initialState;
            }
            private set
            {
                Debug.Assert(initialState == null);
                Debug.Assert(!string.IsNullOrEmpty(value));
                initialState = value;
            }
        }

        /// <summary>
        /// Supported state type
        /// </summary>
        public Type StateFieldType { get; } = typeof(string);

        /// <summary>
        /// The status of the state machine
        /// </summary>
        public ISet<string> States { get; } = new HashSet<string>();

        #region Data storage

        /// <summary>
        /// Storage the Delegate that is called when state change.
        /// For the same key, the value is not repeated.
        /// </summary>
        /// <see cref="ToState"/>
        protected readonly IMultiValuedMap<TranslationData, Delegate> TranslationMap =
            new HashSetValuedHashMap<TranslationData, Delegate>();
        /// <summary>
        /// Storage the listener that is called when state changed.
        /// For the same key, the value is not repeated.
        /// </summary>
        /// <see cref="ToState"/>
        protected readonly IMultiValuedMap<TranslationData, Delegate> TranslateActionMap =
            new HashSetValuedHashMap<TranslationData, Delegate>();
        /// <summary>
        /// Storage the listener that is called when enter state.
        /// For the same key, the value is not repeated.
        /// </summary>
        /// <see cref="ToState"/>
        protected readonly IMultiValuedMap<string, Delegate> EnterActionMap =
            new HashSetValuedHashMap<string, Delegate>();
        /// <summary>
        /// Storage the listener that is called when leaved state.
        /// For the same key, the value is not repeated.
        /// </summary>
        /// <see cref="ToState"/>
        protected readonly IMultiValuedMap<string, Delegate> LeaveActionMap =
            new HashSetValuedHashMap<string, Delegate>();

        #endregion

        #region Init
        
        /// <summary>
        /// Get the attributs from the <paramref name="target"/> and initialize the state machine
        /// </summary>
        /// <param name="target">The object is used to extract state machine attributs.</param>
        /// <exception cref="StateMachineInitException">Initialization state machine failed</exception>
        private void Init(object target)
        {
            Debug.Assert(target != null);
            // 获取所有成员
            InitState(target);

            if (InitialState == null)
            {
                throw new StateMachineInitException("Not find initial state");
            }

            //获取所有方法
            InitTranslation(target);

            InitAction(target);
        }

        /// <summary>
        ///  Get the <see cref="StateAttribute"/> from the <paramref name="target"/> and initialize the state machine
        /// </summary>
        /// <param name="target">The object is used to extract <see cref="StateAttribute"/>.</param>
        /// <exception cref="StateMachineInitException">Initialization state machine failed</exception>
        private void InitState(object target)
        {
            Debug.Assert(target != null);
            FieldInfo[] fieldInfos = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static
                                                        | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                StateAttribute attribute = fieldInfo.GetCustomAttribute<StateAttribute>();
                if (attribute == null)
                {
                    continue;
                }
                if (fieldInfo.FieldType != StateFieldType)
                {
                    throw new StateMachineInitException($"Attributed field type must is {StateFieldType}. field name: {fieldInfo.Name}");
                }
                var state = (string)fieldInfo.GetValue(this);
                if (string.IsNullOrEmpty(state))
                {
                    throw new StateMachineInitException($"Unsupport state value is null or empty. field name: {fieldInfo.Name}");
                }
                if (attribute.IsInitState)
                {
                    if (InitialState != null)
                    {
                        throw new StateMachineInitException($"Duplicate init state. field name: {fieldInfo.Name}, state: {state}");
                    }
                    InitialState = state;
                }
                if (!States.Add(state))
                {
                    logger.Warn($"Find same state. field name: {fieldInfo.Name}, state: {state}");
                }
            }
        }

        /// <summary>
        ///  Get the <see cref="TranslationAttribute"/> from the <paramref name="target"/> and initialize the state machine
        /// </summary>
        /// <param name="target">The object is used to extract <see cref="TranslationAttribute"/>.</param>
        /// <exception cref="StateMachineInitException">Initialization state machine failed</exception>
        private void InitTranslation(object target)
        {
            Debug.Assert(target != null);
            MethodInfo[] methodInfos = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static
                                                        | BindingFlags.Public | BindingFlags.NonPublic);
            Type translateAttributeType = typeof(TranslationAttribute);
            Type translateType = typeof(TranslationData);
            Type voidType = typeof(void);
            foreach (MethodInfo methodInfo in methodInfos)
            {
                if (!Attribute.IsDefined(methodInfo, translateAttributeType))
                {
                    continue;
                }
                if (methodInfo.ReturnType != voidType)
                {
                    throw new StateMachineInitException($"Attributed method return type must be {voidType.Name}. Mthod: {methodInfo}");
                }
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                if (parameterInfos.Length > 1 ||
                    (parameterInfos.Length == 1 && parameterInfos[0].ParameterType != translateType))
                {
                    throw new StateMachineInitException($"Attributed method parameter must be {voidType.Name} or {nameof(TranslationData)}. Mthod: {methodInfo}");
                }

                Lazy<Delegate> delegateLazy = new Lazy<Delegate>(() => GetTranslateMethodDelegate(methodInfo, target));
                foreach (TranslationAttribute attribute in methodInfo.GetCustomAttributes<TranslationAttribute>())
                {
                    if (attribute.From == null)
                    {
                        throw new StateMachineInitException($"{nameof(attribute.From)} state must not null. Mthod: {methodInfo}");
                    }
                    if (attribute.From != string.Empty && !States.Contains(attribute.From))
                    {
                        throw new StateMachineInitException($"Unknow {nameof(attribute.From)} state: {attribute.From}");
                    }
                    if (attribute.To == null)
                    {
                        throw new StateMachineInitException($"{nameof(attribute.To)} state must not null. Mthod: {methodInfo}");
                    }
                    if (attribute.To != string.Empty && !States.Contains(attribute.To))
                    {
                        throw new StateMachineInitException($"Unknow {nameof(attribute.To)} state: {attribute.To}");
                    }
                    if (!TranslationMap.Put(new TranslationData(attribute.From, attribute.To), delegateLazy.Value))
                    {
                        logger.Warn($"Repeated add method. Method: {methodInfo}");
                    }
                }
            }
        }

        /// <summary>
        ///  Get the <see cref="ActionAttribute"/> from the <paramref name="target"/> and initialize the state machine
        /// </summary>
        /// <param name="target">The object is used to extract <see cref="ActionAttribute"/>.</param>
        /// <exception cref="StateMachineInitException">Initialization state machine failed</exception>
        private void InitAction(object target)
        {
            Debug.Assert(target != null);
            MethodInfo[] methodInfos = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static
                                                        | BindingFlags.Public | BindingFlags.NonPublic);
            
            Type actionAttributeType = typeof(ActionAttribute);
            Type translateType = typeof(TranslationData);
            Type voidType = typeof(void);
            foreach (MethodInfo methodInfo in methodInfos)
            {
                if (!Attribute.IsDefined(methodInfo, actionAttributeType))
                {
                    continue;
                }
                if (methodInfo.ReturnType != voidType)
                {
                    throw new StateMachineInitException($"Attributed method return type must be {voidType.Name}. Mthod: {methodInfo}");
                }
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                if (parameterInfos.Length > 1 ||
                    (parameterInfos.Length == 1 && parameterInfos[0].ParameterType != translateType))
                {
                    throw new StateMachineInitException($"Attributed method parameter must be {voidType.Name} or {nameof(TranslationData)}. Mthod: {methodInfo}");
                }

                var delegateLazy = new Lazy<Delegate>(() => GetTranslateMethodDelegate(methodInfo, target));
                foreach (ActionAttribute attribute in methodInfo.GetCustomAttributes<ActionAttribute>())
                {
                    if (attribute.WorkType == ActionAttribute.ActionWorkType.SignalStateEnterOrLeave)
                    {
                        if (attribute.State == null)
                        {
                            throw new StateMachineInitException($"Action state must not null.Mthod: {methodInfo}");
                        }
                        if (attribute.State != string.Empty && !States.Contains(attribute.State))
                        {
                            throw new StateMachineInitException($"Unknow {nameof(attribute.State)}: {attribute.State}");
                        }

                        if (attribute.Type == ActionType.EnterState)
                        {
                            if (!EnterActionMap.Put(attribute.State, delegateLazy.Value))
                            {
                                logger.Warn($"Repeated add method. Method: {methodInfo}");
                            }
                        }
                        else
                        {
                            if (!LeaveActionMap.Put(attribute.State, delegateLazy.Value))
                            {
                                logger.Warn($"Repeated add method. Method: {methodInfo}");
                            }
                        }
                    }
                    else
                    {
                        if (attribute.Data.From == null)
                        {
                            throw new StateMachineInitException($"{nameof(attribute.Data.From)} state must not null. Mthod: {methodInfo}");
                        }
                        if (attribute.Data.From != string.Empty && !States.Contains(attribute.Data.From))
                        {
                            throw new StateMachineInitException($"Unknow {nameof(attribute.Data.From)} state: {attribute.Data.From}");
                        }
                        if (attribute.Data.To == null)
                        {
                            throw new StateMachineInitException($"{nameof(attribute.Data.To)} state must not null. Mthod: {methodInfo}");
                        }
                        if (attribute.Data.To != string.Empty && !States.Contains(attribute.Data.To))
                        {
                            throw new StateMachineInitException($"Unknow {nameof(attribute.Data.To)} state: {attribute.Data.To}");
                        }

                        if(!TranslateActionMap.Put(new TranslationData(attribute.Data.From, attribute.Data.To), delegateLazy.Value))
                        {
                            logger.Warn($"Repeated add method. Method: {methodInfo}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert MethodInfo to Delegate
        /// </summary>
        /// <param name="methodInfo">Need be convert</param>
        /// <param name="target">Method's object</param>
        /// <returns>Converted delegate</returns>
        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        private static Delegate GetTranslateMethodDelegate(MethodInfo methodInfo, object target)
        {
            Debug.Assert(methodInfo != null);
            Debug.Assert(target != null);
            Type methodDelegateType = methodInfo.GetParameters().Length == 0 ? typeof(Action) : typeof(Action<TranslationData>);
            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(methodDelegateType, methodInfo);
            }
            return Delegate.CreateDelegate(methodDelegateType, target, methodInfo.Name);
        }

        #endregion

        #region Switch state

        /// <summary>
        /// Try switch state from current state to target state
        /// </summary>
        /// <param name="nextState">target state</param>
        /// <returns>Success is true, otherwise is false <see cref="SwitchStateException"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="nextState"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="nextState"/> is empty</exception>
        [SuppressMessage("ReSharper", "ArgumentsStyleStringLiteral")]
        [SuppressMessage("ReSharper", "ArgumentsStyleNamedExpression")]
        [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue")]
        public bool ToState(string nextState)
        {
            if (nextState == null)
            {
                throw new ArgumentNullException(nameof(nextState));
            }
            if (nextState.Length == 0)
            {
                throw new ArgumentException("Can't be empty", nameof(nextState));
            }
            if (!States.Contains(nextState))
            {
                SwitchStateException = new StateNotFoundException(nextState);
                return false;
            }
            var data = new TranslationData(CurrentState, nextState);
            try
            {
                // Get from current state to next state
                // ReSharper disable once SuggestVarOrType_Elsewhere
                ICollection<Delegate> currentStateToNextState =
                    TranslationMap.ValuesClone(new TranslationData(from: CurrentState, to: nextState));
                if (!(currentStateToNextState?.Count > 0))
                {
                    SwitchStateException = new SwitchStateException(CurrentState, nextState,
                        $"you need add add least hook from {CurrentState} to {nextState}, example: {nameof(AddHook)}(new {nameof(TranslationData)}({CurrentState}, {nextState}), {nameof(StringStateMachine)}.{nameof(Allow)})");
                    return false;
                }
                // Get from any to any
                // ReSharper disable once SuggestVarOrType_Elsewhere
                ICollection<Delegate> anyToAnyDelegates =
                    TranslationMap.ValuesClone(new TranslationData(from: "", to: ""));
                // Get from current state to any
                // ReSharper disable once SuggestVarOrType_Elsewhere
                ICollection<Delegate> currentStateToAnyDelegates =
                    TranslationMap.ValuesClone(new TranslationData(from: CurrentState, to: ""));
                // Get from any to next state
                // ReSharper disable once SuggestVarOrType_Elsewhere
                ICollection<Delegate> anyToNextStateDelegates =
                    TranslationMap.ValuesClone(new TranslationData(from: "", to: nextState));

                InvokeTranslateDelegates(anyToAnyDelegates, data);
                InvokeTranslateDelegates(currentStateToAnyDelegates, data);
                InvokeTranslateDelegates(anyToNextStateDelegates, data);
                InvokeTranslateDelegates(currentStateToNextState, data);
            }
            catch (Exception exception)
            {
                SwitchStateException = exception;
                return false;
            }

            string lastState = CurrentState;
            CurrentState = nextState;
            StateHistory.Add(lastState);

            InvokeActionDelegates(LeaveActionMap.ValuesClone(lastState), data);
            InvokeActionDelegates(TranslateActionMap.ValuesClone(data), data);
            InvokeActionDelegates(EnterActionMap.ValuesClone(nextState), data);
            
            return true;
        }

        /// <summary>
        /// Invoke all translation dalagate
        /// </summary>
        /// <param name="delegates">Invoke each delegate</param>
        /// <param name="data">Current state change data</param>
        private static void InvokeTranslateDelegates(ICollection<Delegate> delegates, TranslationData data)
        {
            try
            {
                if (delegates == null)
                {
                    return;
                }
                foreach (Delegate d in delegates)
                {
                    bool hasParameter = d.Method.GetParameters().Length != 0;
                    if (hasParameter)
                    {
                        ((Action<TranslationData>)d).Invoke(data);
                    }
                    else
                    {
                        ((Action)d).Invoke();
                    }
                }
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        /// <summary>
        /// Invoke all action dalagate
        /// </summary>
        /// <param name="delegates">Invoke each delegate</param>
        /// <param name="data">Current state change data</param>
        /// <remarks>Unthrow any exception</remarks>
        private void InvokeActionDelegates(ICollection<Delegate> delegates, TranslationData data)
        {
            if (delegates == null)
            {
                return;
            }
            foreach (Delegate d in delegates)
            {
                bool hasParameter = d.Method.GetParameters().Length != 0;
                try
                {
                    if (hasParameter)
                    {
                        ((Action<TranslationData>)d).Invoke(data);
                    }
                    else
                    {
                        ((Action)d).Invoke();
                    }
                }
                catch (Exception exception)
                {
                    if (TranslateActionHandle == null)
                    {
                        logger.Error(exception);
                    }
                    else
                    {
                        try
                        {

                            TranslateActionHandle.Invoke(exception);
                        }
                        catch (Exception exception1)
                        {
                            logger.Error(exception1);
                        }
                    }
                    
                }
            }
        }

        #endregion

        #region Add/Remove Translation Hook

        /// <summary>
        /// Add translation hook
        /// </summary>
        /// <param name="data">
        ///     <paramref name="action"/> will be invoke if state switching
        ///     from <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">Action that need to be invoke.</param>
        /// <returns>Add success is true, otherwise is false.</returns>
        /// <seealso cref="AddHook(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveHook(TranslationData, Action)"/>
        /// <seealso cref="RemoveHook(TranslationData, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langwork="null"/>.</exception>
        public bool AddHook(TranslationData data, Action action)
        {
            return AddHook(data, action as Delegate);
        }

        /// <summary>
        /// Add translation hook
        /// </summary>
        /// <param name="data">
        ///     <paramref name="action"/> will be invoke if state switching
        ///     from <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">Action that need to be invoke.</param>
        /// <returns>Add success is true, otherwise is false.</returns>
        /// <seealso cref="AddHook(TranslationData, Action)"/>
        /// <seealso cref="RemoveHook(TranslationData, Action)"/>
        /// <seealso cref="RemoveHook(TranslationData, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langwork="null"/>.</exception>
        public bool AddHook(TranslationData data, Action<TranslationData> action)
        {
            return AddHook(data, action as Delegate);
        }

        /// <summary>
        /// Add translation hook implement
        /// </summary>
        /// <param name="data">
        ///     <paramref name="action"/> will be invoke if state switching
        ///     from <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">Delegate that need to be invoke.</param>
        /// <returns>Add success is true, otherwise is false.</returns>
        /// <see cref="AddHook(TranslationData, Action)"/>
        /// <see cref="AddHook(TranslationData, Action{TranslationData})"/>
        /// <see cref="RemoveHook(TranslationData, Action)"/>
        /// <see cref="RemoveHook(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveHook(TranslationData, Delegate)"/>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langwork="null"/>.</exception>
        private bool AddHook(TranslationData data, Delegate action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            return TranslationMap.Put(data, action);
        }

        /// <summary>
        /// Remove translation hook
        /// </summary>
        /// <param name="data">
        ///     Remove <paramref name="action"/> from 
        ///     <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">The action you want to remove.</param>
        /// <returns>Remove success is true, otherwise is false.</returns>
        /// <seealso cref="AddHook(TranslationData, Action)"/>
        /// <seealso cref="AddHook(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveHook(TranslationData, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langwork="null"/>.</exception>
        public bool RemoveHook(TranslationData data, Action action)
        {
            return RemoveHook(data, action as Delegate);
        }

        /// <summary>
        /// Remove translation hook
        /// </summary>
        /// <param name="data">
        ///     Remove <paramref name="action"/> from 
        ///     <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">The action you want to remove.</param>
        /// <returns>Remove success is true, otherwise is false.</returns>
        /// <seealso cref="AddHook(TranslationData, Action)"/>
        /// <seealso cref="AddHook(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveHook(TranslationData, Action)"/>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langwork="null"/>.</exception>
        public bool RemoveHook(TranslationData data, Action<TranslationData> action)
        {
            return RemoveHook(data, action as Delegate);
        }

        /// <summary>
        /// Remove translation hook implement
        /// </summary>
        /// <param name="data">
        ///     Remove <paramref name="action"/> from 
        ///     <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">The action you want to remove.</param>
        /// <returns>Remove success is true, otherwise is false.</returns>
        /// <see cref="AddHook(TranslationData, Action)"/>
        /// <see cref="AddHook(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddHook(TranslationData, Delegate)"/>
        /// <see cref="RemoveHook(TranslationData, Action)"/>
        /// <see cref="RemoveHook(TranslationData, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langwork="null"/>.</exception>
        private bool RemoveHook(TranslationData data, Delegate action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            return TranslationMap.RemoveMapping(data, action);
        }
        #endregion

        #region Add/Remove Action

        /// <summary>
        /// Add action that will be invoke after state changed
        /// </summary>
        /// <param name="data">
        ///     <paramref name="action"/> will be invoke after state changed
        ///     from <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">Action that need to be invoke.</param>
        /// <returns>Add success is true, otherwise is false.</returns>
        /// <seealso cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Action)"/>
        /// <seealso cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(string, ActionType, Action)"/>
        /// <seealso cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action)"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        public bool AddAction(TranslationData data, Action action)
        {
            return AddAction(data, action as Delegate);
        }

        /// <summary>
        /// Add action that will be invoke after state changed
        /// </summary>
        /// <param name="data">
        ///     <paramref name="action"/> will be invoke after state changed
        ///     from <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">Action that need to be invoke.</param>
        /// <returns>Add success is true, otherwise is false.</returns>
        /// <seealso cref="AddAction(TranslationData, Action)"/>
        /// <seealso cref="RemoveAction(TranslationData, Action)"/>
        /// <seealso cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(string, ActionType, Action)"/>
        /// <seealso cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action)"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        public bool AddAction(TranslationData data, Action<TranslationData> action)
        {
            return AddAction(data, action as Delegate);
        }

        /// <summary>
        /// Add action implement
        /// </summary>
        /// <param name="data">
        ///     <paramref name="action"/> will be invoke after state changed
        ///     from <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">Delegate that need to be invoke.</param>
        /// <returns>Add success is true, otherwise is false.</returns>
        /// <see cref="AddAction(TranslationData, Action)"/>
        /// <see cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <see cref="RemoveAction(TranslationData, Action)"/>
        /// <see cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Delegate)"/>
        /// <see cref="AddAction(string, ActionType, Action)"/>
        /// <see cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealse cref="AddAction(string, ActionType, Delegate)"/>
        /// <see cref="RemoveAction(string, ActionType, Action)"/>
        /// <see cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <seealse cref="RemoveAction(string, ActionType, Delegate)"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        private bool AddAction(TranslationData data, Delegate action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            return TranslateActionMap.Put(data, action);
        }

        /// <summary>
        /// Remove action
        /// </summary>
        /// <param name="data">
        ///     Remove <paramref name="action"/> from 
        ///     <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">The action you want to remove.</param>
        /// <returns>Remove success is true, otherwise is false.</returns>
        /// <seealso cref="AddAction(TranslationData, Action)"/>
        /// <seealso cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(string, ActionType, Action)"/>
        /// <seealso cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action)"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        public bool RemoveAction(TranslationData data, Action action)
        {
            return RemoveAction(data, action as Delegate);
        }

        /// <summary>
        /// Remove action
        /// </summary>
        /// <param name="data">
        ///     Remove <paramref name="action"/> from 
        ///     <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">The action you want to remove.</param>
        /// <returns>Remove success is true, otherwise is false.</returns>
        /// <seealso cref="AddAction(TranslationData, Action)"/>
        /// <seealso cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Action)"/>
        /// <seealso cref="AddAction(string, ActionType, Action)"/>
        /// <seealso cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action)"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        public bool RemoveAction(TranslationData data, Action<TranslationData> action)
        {
            return RemoveAction(data, action as Delegate);
        }

        /// <summary>
        /// Remove action implement
        /// </summary>
        /// <param name="data">
        ///     Remove <paramref name="action"/> from 
        ///     <code>data.From</code> to <code>data.To</code>
        /// </param>
        /// <param name="action">The action you want to remove.</param>
        /// <returns>Remove success is true, otherwise is false.</returns>
        /// <see cref="AddAction(TranslationData, Action)"/>
        /// <see cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(TranslationData, Delegate)"/>
        /// <see cref="RemoveAction(TranslationData, Action)"/>
        /// <see cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <see cref="AddAction(string, ActionType, Action)"/>
        /// <see cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealse cref="AddAction(string, ActionType, Delegate)"/>
        /// <see cref="RemoveAction(string, ActionType, Action)"/>
        /// <see cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <seealse cref="RemoveAction(string, ActionType, Delegate)"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        private bool RemoveAction(TranslationData data, Delegate action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            return TranslateActionMap.RemoveMapping(data, action);
        }

        /// <summary>
        /// Add action that will be invoke after state changed
        /// </summary>
        /// <param name="state">Need listen state</param>
        /// <param name="type">Listen enter or leave</param>
        /// <param name="action">Need invoke action</param>
        /// <returns>Add success is true, otherwise is false.</returns>
        /// <seealso cref="AddAction(TranslationData, Action)"/>
        /// <seealso cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Action)"/>
        /// <seealso cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action)"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="state"/> or <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="state"/> is empty</exception>
        public bool AddAction(string state, ActionType type, Action action)
        {
            return AddAction(state, type, action as Delegate);
        }

        /// <summary>
        /// Add action that will be invoke after state changed
        /// </summary>
        /// <param name="state">Need listen state</param>
        /// <param name="type">Listen enter or leave</param>
        /// <param name="action">Need invoke action</param>
        /// <returns>Add success is true, otherwise is false.</returns>
        /// <seealso cref="AddAction(TranslationData, Action)"/>
        /// <seealso cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Action)"/>
        /// <seealso cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(string, ActionType, Action)"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action)"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="state"/> or <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="state"/> is empty</exception>
        public bool AddAction(string state, ActionType type, Action<TranslationData> action)
        {
            return AddAction(state, type, action as Delegate);
        }

        /// <summary>
        /// Add action implement
        /// </summary>
        /// <param name="state">Need listen state</param>
        /// <param name="type">Listen enter or leave</param>
        /// <param name="action">Need invoke action</param>
        /// <returns>Add success is true, otherwise is false.</returns>
        /// <see cref="AddAction(TranslationData, Action)"/>
        /// <see cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(TranslationData, Delegate)"/>
        /// <see cref="RemoveAction(TranslationData, Action)"/>
        /// <see cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Delegate)"/>
        /// <see cref="AddAction(string, ActionType, Action)"/>
        /// <see cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <see cref="RemoveAction(string, ActionType, Action)"/>
        /// <see cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <seealse cref="RemoveAction(string, ActionType, Delegate)"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="state"/> or <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="state"/> is empty</exception>
        private bool AddAction(string state, ActionType type, Delegate action)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            return type == ActionType.EnterState ? 
                        EnterActionMap.Put(state, action) :
                        LeaveActionMap.Put(state, action);
        }

        /// <summary>
        /// Remove action
        /// </summary>
        /// <param name="state">Action listening state</param>
        /// <param name="type">Action listening type</param>
        /// <param name="action">Need remove action</param>
        /// <returns>Remove success is true, otherwise is false.</returns>
        /// <seealso cref="AddAction(TranslationData, Action)"/>
        /// <seealso cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Action)"/>
        /// <seealso cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(string, ActionType, Action)"/>
        /// <seealso cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="state"/> or <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="state"/> is empty</exception>
        public bool RemoveAction(string state, ActionType type, Action action)
        {
            return RemoveAction(state, type, action as Delegate);
        }

        /// <summary>
        /// Remove action
        /// </summary>
        /// <param name="state">Action listening state</param>
        /// <param name="type">Action listening type</param>
        /// <param name="action">Need remove action</param>
        /// <returns>Remove success is true, otherwise is false.</returns>
        /// <seealso cref="AddAction(TranslationData, Action)"/>
        /// <seealso cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Action)"/>
        /// <seealso cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(string, ActionType, Action)"/>
        /// <seealso cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(string, ActionType, Action)"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="state"/> or <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="state"/> is empty</exception>
        public bool RemoveAction(string state, ActionType type, Action<TranslationData> action)
        {
            return RemoveAction(state, type, action as Delegate);
        }

        /// <summary>
        /// Remove action implement
        /// </summary>
        /// <param name="state">Action listening state</param>
        /// <param name="type">Action listening type</param>
        /// <param name="action">Need remove action</param>
        /// <returns>Remove success is true, otherwise is false.</returns>
        /// <see cref="AddAction(TranslationData, Action)"/>
        /// <see cref="AddAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="AddAction(TranslationData, Delegate)"/>
        /// <see cref="RemoveAction(TranslationData, Action)"/>
        /// <see cref="RemoveAction(TranslationData, Action{TranslationData})"/>
        /// <seealso cref="RemoveAction(TranslationData, Delegate)"/>
        /// <see cref="AddAction(string, ActionType, Action)"/>
        /// <see cref="AddAction(string, ActionType, Action{TranslationData})"/>
        /// <seealse cref="AddAction(string, ActionType, Delegate)"/>
        /// <see cref="RemoveAction(string, ActionType, Action)"/>
        /// <see cref="RemoveAction(string, ActionType, Action{TranslationData})"/>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="state"/> or <paramref name="action"/> is <see langwork="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="state"/> is empty</exception>
        private bool RemoveAction(string state, ActionType type, Delegate action)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            if (state.Length == 0)
            {
                throw new ArgumentException("Can't be empty", nameof(state));
            }
            return type == ActionType.EnterState ?
                EnterActionMap.RemoveMapping(state, action) :
                LeaveActionMap.RemoveMapping(state, action);
        }

        #endregion

        /// <summary>
        /// If translation hook throw exception, <see cref="ToState"/> will return <see langwork="false"/>,
        /// the exception store here
        /// </summary>
        /// <seealso cref="ToState"/>
        public Exception SwitchStateException
        {
            get;
            protected set;
        }

        /// <summary>
        /// Current state of state machine
        /// </summary>
        public string CurrentState
        {
            get;
            private set;
        }

        /// <summary>
        /// Handle action exception
        /// </summary>
        protected Action<Exception> TranslateActionHandle
        {
            get;
            set;
        }

        /// <summary>
        /// State machine state switch history
        /// </summary>
        protected IList<string> StateHistory { get; } = new List<string>();

        public static readonly Action Allow = () => { };
    }

    
}
