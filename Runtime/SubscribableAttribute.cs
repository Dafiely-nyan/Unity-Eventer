using System;

namespace Eventer
{
    [AttributeUsage(AttributeTargets.Event)]
    public sealed class SubscribableAttribute : Attribute
    {
        /// <summary>
        /// Any string value that will be used by listeners to subscribe to this event
        /// </summary>
        public string EventId { get; }
        /// <summary>
        /// Destroy an event and all its subscribers when a new scene is loaded. Default is true
        /// </summary>
        public bool DestroyOnLoad { get; set; }

        
        public SubscribableAttribute(string eventId)
        {
            EventId = eventId;
            DestroyOnLoad = true;
        }
    }
}
