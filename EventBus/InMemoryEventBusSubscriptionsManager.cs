using EventBus.Abstractions;
using EventBus.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus
{
    public partial class InMemoryEventBusSubscriptionsManager:IEventBusSubscriptionsManager
    {
        private readonly Dictionary<String, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _eventTypes;
        public event EventHandler<string> OnEventRemoved;
        public bool IsEmpty =>!_handlers.Keys.Any();
        public InMemoryEventBusSubscriptionsManager()
        {
            _handlers = new Dictionary<String, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
        }

        public void AddDynamicSubscription<TH>(string eventName) where TH : IDynamicIntegrationEventHandler 
        {
            DoAddSubscription(typeof(TH), eventName, true);
        }
        public void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T> 
        {

            var eventName = GetEventKey<T>();
            DoAddSubscription(typeof(TH), eventName, false);
            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }
        }
        public void RemoveDynamicSubscription<TH>(string eventName) where TH : IDynamicIntegrationEventHandler 
        {
            var handlerToRemove = FindDynamicSubscriptionToRemove<TH>(eventName);
            DoRemoveHanlder(eventName, handlerToRemove);
        }
        public void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T> 
        {
            var handlerToRemove = FindSubscriptionToRemove<T, TH>();
            DoRemoveHanlder(GetEventKey<T>(), handlerToRemove);
        }
        public bool HasSubscriptionForEvent<T>() where T : IntegrationEvent => HasSubscriptionForEvent(GetEventKey<T>());
        public bool HasSubscriptionForEvent(string eventName) =>_handlers.ContainsKey(eventName);
        public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(e=>e.Name==eventName);
        public void Clear()
        {
             _handlers.Clear();
        }
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent => GetHandlersForEvent(GetEventKey<T>());
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];
        public string GetEventKey<T>()
        {
            return typeof(T).Name;
        }

        #region Private
        private void DoAddSubscription(Type handlerType, string eventName,bool isDynamic)
        {
            if (!HasSubscriptionForEvent(eventName))
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }
            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }
            if (isDynamic)
            {
                _handlers[eventName].Add(SubscriptionInfo.Dynamic(handlerType));
            }
            else
            {
                _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
            }
        }

        private void DoRemoveHanlder(string eventName,SubscriptionInfo subsToRemove)
        {
            if(subsToRemove != null)
            {
                _handlers[eventName].Remove(subsToRemove);
                if (!_handlers[eventName].Any())
                {
                    _handlers.Remove(eventName);
                    var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                    if (eventType != null)
                    {
                        _eventTypes.Remove(eventType);
                    }
                    RaiseOnEventRemoved(eventName);
                }
            }
        }
        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventName);
        }

        private SubscriptionInfo FindDynamicSubscriptionToRemove<TH>(String eventName) where TH:IDynamicIntegrationEventHandler
        {
            return DoFindSubscriptionToRemove(eventName,typeof(TH));
        }

        private SubscriptionInfo DoFindSubscriptionToRemove(string eventName,Type handlerType)
        {
            if (!HasSubscriptionForEvent(eventName))
            {
                return null;
            }
            return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
        }
        private SubscriptionInfo FindSubscriptionToRemove<T,TH>() where T : IntegrationEvent
             where TH : IIntegrationEventHandler<T>
        {
            var enventName = GetEventKey<T>();
            return DoFindSubscriptionToRemove(enventName,typeof(TH));
        }
        #endregion

    }
}
