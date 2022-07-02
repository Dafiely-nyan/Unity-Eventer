using System;

namespace Eventer
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SubscribeAttribute : Attribute
    {
        /// <summary>
        /// Id of the event to listen
        /// </summary>
        public string EventId { get; }
        /// <summary>
        /// Order of execution within an event, lower - earlier. Default is 0
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// Unsubscribe from event when a new scene is loaded. Default is true
        /// </summary>
        public bool DestroyOnLoad { get; set; }
        
        public SubscribeAttribute(string eventId)
        {
            EventId = eventId;
            DestroyOnLoad = true;
            Order = 0;
        }
    }
}