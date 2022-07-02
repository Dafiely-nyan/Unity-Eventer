using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

#pragma warning disable CS0168

namespace Eventer
{
    public static class Utils
    {
        public static List<GameObject> GetAllGameobjectsOnScene()
        {
            return Object.FindObjectsOfType<GameObject>().ToList();
        }

        public static List<MethodInfoWrapper> GetListenersNonDelagated(MonoBehaviour g)
        {
            List<MethodInfoWrapper> methodInfoWrappers = new List<MethodInfoWrapper>();
            
            var methods = g.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo methodInfo in methods)
            {
                var attributes = methodInfo.GetCustomAttributes(typeof(SubscribeAttribute));

                foreach (var attribute in attributes)
                {
                    if (attribute == null) continue;

                    var subscribeToAttribute = (SubscribeAttribute) attribute;

                    MethodInfoWrapper wrapper = new MethodInfoWrapper()
                    {
                        MethodInfo = methodInfo,
                        DestroyOnLoad = subscribeToAttribute.DestroyOnLoad,
                        Order = subscribeToAttribute.Order,
                        Object = g,
                        Delegate = null,
                        EventId = subscribeToAttribute.EventId,
                    };
                
                    methodInfoWrappers.Add(wrapper);
                }
            }

            return methodInfoWrappers;
        }
        
        public static List<MethodInfoWrapper> GetListeners(MonoBehaviour g, Dictionary<string, EventInfoWrapper> eventInfoWrappers)
        {
            List<MethodInfoWrapper> methodInfoWrappers = new List<MethodInfoWrapper>();
            
            var methods = g.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo methodInfo in methods)
            {
                var attributes = methodInfo.GetCustomAttributes(typeof(SubscribeAttribute));

                foreach (var attribute in attributes)
                {
                    if (attribute == null) continue;

                    var subscribeToAttribute = (SubscribeAttribute) attribute;
                    
                    if (!eventInfoWrappers.ContainsKey(subscribeToAttribute.EventId)) continue;

                    MethodInfoWrapper wrapper = new MethodInfoWrapper()
                    {
                        MethodInfo = methodInfo,
                        DestroyOnLoad = subscribeToAttribute.DestroyOnLoad,
                        Order = subscribeToAttribute.Order,
                        Object = g,
                        Delegate = Delegate.CreateDelegate(eventInfoWrappers[subscribeToAttribute.EventId].EventInfo.EventHandlerType,
                            g, methodInfo),
                        EventId = subscribeToAttribute.EventId,
                    };
                
                    methodInfoWrappers.Add(wrapper);
                }
            }

            return methodInfoWrappers;
        }

        public static List<EventInfoWrapper> GetEvents(MonoBehaviour g)
        {
            List<EventInfoWrapper> eventInfoWrappers = new List<EventInfoWrapper>();
            
            var events = g.GetType().GetEvents(
                BindingFlags.Instance | BindingFlags.Public);

            foreach (EventInfo eventInfo in events)
            {
                var attribute = eventInfo.GetCustomAttribute(typeof(SubscribableAttribute));
                if (attribute == null) continue;

                var subscribableAttribute = (SubscribableAttribute) attribute;
                
                EventInfoWrapper wrapper = new EventInfoWrapper()
                {
                    EventInfo = eventInfo,
                    DestroyOnLoad = subscribableAttribute.DestroyOnLoad,
                    EventId = subscribableAttribute.EventId,
                    Subscribers = new List<MethodInfoWrapper>(),
                    SubscribersBuffer = new List<MethodInfoWrapper>(),
                    BoundObject = g
                };
                
                eventInfoWrappers.Add(wrapper);
            }

            return eventInfoWrappers;
        }

        public static bool VerifiyDelegateMatch(EventInfoWrapper eventInfoWrapper, MethodInfoWrapper methodInfoWrapper)
        {
            try
            {
                Delegate d = Delegate.CreateDelegate(eventInfoWrapper.EventInfo.EventHandlerType, methodInfoWrapper.Object,
                    methodInfoWrapper.MethodInfo);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Delegate type mismatch! Make sure a method <{methodInfoWrapper.MethodInfo.Name}> declared in" +
                               $" {methodInfoWrapper.Object} matches signature of event <{eventInfoWrapper.EventInfo.Name}> " +
                               $"declared in {eventInfoWrapper.BoundObject}");

                return false;
            }
        }
    }
}