using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eventer
{
    public class Eventer : MonoBehaviour
    {
        private readonly Dictionary<string, EventInfoWrapper> EventInfoWrappers = new Dictionary<string, EventInfoWrapper>();

        private void Awake()
        {
            Resolve();
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoad;
        }

        void Resolve()
        {
            // get all gameObjects on scene
            var gameObjects = getAllObjects();

            // init empty container for listeners
            List<MethodInfoWrapper> listeners = new List<MethodInfoWrapper>();

            for (int i = 0; i < gameObjects.Count; i++)
            {
                // get all monobehaviours within gameobject
                var monoBehaviours = gameObjects[i].GetComponents<MonoBehaviour>();
                
                foreach (MonoBehaviour monoBehaviour in monoBehaviours)
                {
                    // create events
                    var events = getListenableEvents(monoBehaviour);

                    foreach (EventInfoWrapper eventInfoWrapper in events)
                    {
                        if (EventInfoWrappers.ContainsKey(eventInfoWrapper.EventId))
                        {
                            // theres already an object that declares the event, skip it
                            continue;
                        }
                        EventInfoWrappers.Add(eventInfoWrapper.EventId, eventInfoWrapper);
                    }
                }
            }
            
            for (int i = 0; i < gameObjects.Count; i++)
            {
                // get all monobehaviours within gameobject
                var monoBehaviours = gameObjects[i].GetComponents<MonoBehaviour>();

                foreach (MonoBehaviour monoBehaviour in monoBehaviours)
                {
                    // get all listeners per within monobehaviour
                    var listenersPerMono = getAllListeners(monoBehaviour);
                    listeners.AddRange(listenersPerMono);
                }
            }

            // Add listeners to events watch list
            foreach (MethodInfoWrapper methodInfoWrapper in listeners)
            {
                EventInfoWrappers[methodInfoWrapper.EventId].Subscribers.Add(methodInfoWrapper);
            }
            
            // Apply ordering
            foreach (string key in EventInfoWrappers.Keys)
            {
                EventInfoWrappers[key].Subscribers =
                    EventInfoWrappers[key].Subscribers.OrderBy(m => m.Order).ToList();
            }
            
            // Finally subscribe
            foreach (string key in EventInfoWrappers.Keys)
            {
                foreach (var methodInfoWrapper in EventInfoWrappers[key].Subscribers)
                {
                    EventInfoWrappers[key].EventInfo.AddEventHandler(EventInfoWrappers[key].BoundObject,
                        methodInfoWrapper.Delegate);
                }
            }
        }

        void OnSceneLoad(Scene s, LoadSceneMode lcm)
        {
            // unscubscribe <destroy on load> listeners
            // destroy <destroy on load> events

            foreach (string key in EventInfoWrappers.Keys)
            {
                if (EventInfoWrappers[key].DestroyOnLoad)
                {
                    foreach (MethodInfoWrapper methodInfoWrapper in EventInfoWrappers[key].Subscribers)
                    {
                        EventInfoWrappers[key].EventInfo.RemoveEventHandler(EventInfoWrappers[key].BoundObject, methodInfoWrapper.Delegate);
                    }

                    EventInfoWrappers.Remove(key);
                }

                else
                {
                    List<MethodInfoWrapper> totalRemove = new List<MethodInfoWrapper>();
                    
                    foreach (MethodInfoWrapper methodInfoWrapper in EventInfoWrappers[key].Subscribers)
                    {
                        if (methodInfoWrapper.DestroyOnLoad)
                        {
                            EventInfoWrappers[key].EventInfo.RemoveEventHandler(EventInfoWrappers[key].BoundObject,
                                methodInfoWrapper.Delegate);
                            totalRemove.Add(methodInfoWrapper);
                        }
                    }
                    
                    totalRemove.ForEach(t => EventInfoWrappers[key].Subscribers.Remove(t));
                }
            }
        }
        
        List<EventInfoWrapper> getListenableEvents(MonoBehaviour g)
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
                    BoundObject = g
                };
                
                eventInfoWrappers.Add(wrapper);
            }

            return eventInfoWrappers;
        }

        List<MethodInfoWrapper> getAllListeners(MonoBehaviour g)
        {
            List<MethodInfoWrapper> methodInfoWrappers = new List<MethodInfoWrapper>();
            
            var methods = g.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo methodInfo in methods)
            {
                var attribute = methodInfo.GetCustomAttribute(typeof(SubscribeAttribute));
                if (attribute == null) continue;

                var subscribeToAttribute = (SubscribeAttribute) attribute;
                
                if (!EventInfoWrappers.ContainsKey(subscribeToAttribute.EventId)) continue;
                
                MethodInfoWrapper wrapper = new MethodInfoWrapper()
                {
                    MethodInfo = methodInfo,
                    DestroyOnLoad = subscribeToAttribute.DestroyOnLoad,
                    Order = subscribeToAttribute.Order,
                    Object = g,
                    Delegate = Delegate.CreateDelegate(EventInfoWrappers[subscribeToAttribute.EventId].EventInfo.EventHandlerType,
                        g, methodInfo),
                    EventId = subscribeToAttribute.EventId,
                };
                
                methodInfoWrappers.Add(wrapper);
            }

            return methodInfoWrappers;
        }

        List<GameObject> getAllObjects()
        {
            return GameObject.FindObjectsOfType<GameObject>().ToList();
        }
    }
}