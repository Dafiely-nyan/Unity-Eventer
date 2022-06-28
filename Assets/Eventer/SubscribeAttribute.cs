using System;

namespace Eventer
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SubscribeAttribute : Attribute
    {
        public string EventId { get; private set; }
        public int Order { get; private set; }
        public bool DestroyOnLoad { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventId">An event's string id</param>
        /// <param name="destroyOnLoad">When a new scene is loaded, should this unsubscribe from event?</param>
        /// <param name="order">If more than 1 method listens to an event, a method with higher value will execute
        /// after methods with lower value</param>
        public SubscribeAttribute(string eventId, int order = 0, bool destroyOnLoad = true)
        {
            EventId = eventId;
            DestroyOnLoad = destroyOnLoad;
            Order = order;
        }
    }
}