using System;
using System.Reflection;
using UnityEngine;

namespace Eventer
{
    public class MethodInfoWrapper
    {
        public MethodInfo MethodInfo { get; set; }
        public string EventId { get; set; }
        public bool DestroyOnLoad { get; set; }
        public int Order { get; set; }
        public Delegate Delegate { get; set; }
        public MonoBehaviour Object { get; set; }
    }
}