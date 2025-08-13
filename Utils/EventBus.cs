using System;
using System.Collections.Generic;

namespace DrJaw.Utils
{
    public static class EventBus
    {
        private static readonly Dictionary<string, List<Action>> _subscribers = new();

        public static void Subscribe(string eventName, Action handler)
        {
            if (!_subscribers.ContainsKey(eventName))
                _subscribers[eventName] = new();

            _subscribers[eventName].Add(handler);
        }

        public static void Publish(string eventName)
        {
            if (_subscribers.TryGetValue(eventName, out var handlers))
            {
                foreach (var handler in handlers)
                    handler.Invoke();
            }
        }

        public static void Unsubscribe(string eventName, Action handler)
        {
            if (_subscribers.TryGetValue(eventName, out var handlers))
                handlers.Remove(handler);
        }
    }
}
