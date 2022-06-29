using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eventer
{
    [DefaultExecutionOrder(-1000)]
    public class Eventer : MonoBehaviour
    {
        private readonly Dictionary<string, EventInfoWrapper> EventInfoWrappers = new Dictionary<string, EventInfoWrapper>();
        private static bool _instanced;

        private void Awake()
        {
            Resolve();
            InitializeSingleInstance();
        }

        void InitializeSingleInstance()
        {
            if (_instanced) Destroy(gameObject);
            else
            {
                _instanced = true;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoad;
        }

        void Resolve()
        {
            // get all gameObjects on scene
            var gameObjects = getAllObjects();

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
            
            // init empty container for listeners
            List<MethodInfoWrapper> listeners = new List<MethodInfoWrapper>();
            
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
                // before add make sure there are no copy of this listener
                // it is the same method and the same object if their delegates match
                var sameEntry = EventInfoWrappers[methodInfoWrapper.EventId].Subscribers.Find(m =>
                    m.Delegate == methodInfoWrapper.Delegate);
                
                if (sameEntry == null)
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
            
            if (lcm == LoadSceneMode.Single)
                DestroyEventsAndListenersOnLoad();
            
            Resolve();
        }

        void DestroyEventsAndListenersOnLoad()
        {
            List<string> totalDeletionEvents = new List<string>();

            foreach (string key in EventInfoWrappers.Keys)
            {
                if (EventInfoWrappers[key].DestroyOnLoad)
                {
                    DestroyEvent(key);
                    totalDeletionEvents.Add(key);
                }

                else
                {
                    List<MethodInfoWrapper> totalRemove = new List<MethodInfoWrapper>();
                    
                    foreach (MethodInfoWrapper methodInfoWrapper in EventInfoWrappers[key].Subscribers)
                    {
                        if (methodInfoWrapper.DestroyOnLoad)
                        {
                            DestroyListener(key, methodInfoWrapper);
                            totalRemove.Add(methodInfoWrapper);
                        }
                    }
                    
                    totalRemove.ForEach(t => EventInfoWrappers[key].Subscribers.Remove(t));
                }
            }
            
            totalDeletionEvents.ForEach(key => EventInfoWrappers.Remove(key));
        }

        void DestroyEvent(string eventId)
        {
            foreach (MethodInfoWrapper methodInfoWrapper in EventInfoWrappers[eventId].Subscribers)
            {
                EventInfoWrappers[eventId].EventInfo.RemoveEventHandler(EventInfoWrappers[eventId].BoundObject, methodInfoWrapper.Delegate);
            }
        }

        void DestroyListener(string eventId, MethodInfoWrapper listener)
        {
            EventInfoWrappers[eventId].EventInfo.RemoveEventHandler(EventInfoWrappers[eventId].BoundObject,
                listener.Delegate);
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
                var attributes = methodInfo.GetCustomAttributes(typeof(SubscribeAttribute));

                foreach (var attribute in attributes)
                {
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
            }

            return methodInfoWrappers;
        }

        List<GameObject> getAllObjects()
        {
            return GameObject.FindObjectsOfType<GameObject>().ToList();
        }
    }
}