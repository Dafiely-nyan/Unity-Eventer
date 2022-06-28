using System;

namespace Eventer
{
    [AttributeUsage(AttributeTargets.Event)]
    public class SubscribableAttribute : Attribute
    {
        public string EventId { get; private set; }
        public bool DestroyOnLoad { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventId">Event id can be any valid string. You'd use the same string to subscribe to it in listeners.</param>
        /// <param name="destroyOnLoad">Should event container destroy the event and unsubscribe all its listeners when new scene is loaded
        /// Set this to true when the Gameobject that has this event doesnt call DontDestroyOnLoad()</param>
        public SubscribableAttribute(string eventId, bool destroyOnLoad = false)
        {
            EventId = eventId;
            DestroyOnLoad = destroyOnLoad;
        }
    }
}
