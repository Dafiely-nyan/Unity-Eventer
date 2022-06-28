using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Eventer
{
    public class EventInfoWrapper
    {
        public EventInfo EventInfo { get; set; }
        public bool DestroyOnLoad { get; set; }
        public string EventId { get; set; }
        public List<MethodInfoWrapper> Subscribers { get; set; }
        public MonoBehaviour BoundObject { get; set; }
    }
}