using System.Collections.Generic;
using System.Linq;
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
            var gameObjects = Utils.GetAllGameobjectsOnScene();

            for (int i = 0; i < gameObjects.Count; i++)
            {
                // get all monobehaviours within gameobject
                var monoBehaviours = gameObjects[i].GetComponents<MonoBehaviour>();
                
                foreach (MonoBehaviour monoBehaviour in monoBehaviours)
                {
                    // create events
                    var events = Utils.GetEvents(monoBehaviour);

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
                    // get all listeners within monobehaviour
                    //var listenersPerMono = getAllListeners(monoBehaviour);
                    var listenersPerMono = Utils.GetListeners(monoBehaviour, EventInfoWrappers);
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
                    EventInfoWrappers[methodInfoWrapper.EventId].SubscribersBuffer.Add(methodInfoWrapper);
            }
            
            // To properly apply ordering we need to remove all listeners, merge buffer with removed listeneres
            // then apply sorting and finally add listeners back
            // this needs to be done only with those events which buffers are not empty (new listeners available)

            foreach (var key in EventInfoWrappers.Keys)
            {
                if (EventInfoWrappers[key].SubscribersBuffer.Count > 0)
                {
                    foreach (MethodInfoWrapper methodInfoWrapper in EventInfoWrappers[key].Subscribers)
                    {
                        // we remove it from listeners but it still in subscribers list
                        DestroyListener(key, methodInfoWrapper);
                    }
                    
                    // merge buffer to subscribers list
                    EventInfoWrappers[key].Subscribers.AddRange(EventInfoWrappers[key].SubscribersBuffer);
                    
                    // clear buffer
                    EventInfoWrappers[key].SubscribersBuffer = new List<MethodInfoWrapper>();
                    
                    // apply ordering
                    EventInfoWrappers[key].Subscribers =
                        EventInfoWrappers[key].Subscribers.OrderBy(m => m.Order).ToList();
                    
                    // subscribe methods
                    foreach (MethodInfoWrapper methodInfoWrapper in EventInfoWrappers[key].Subscribers)
                    {
                        // we remove it from listeners but it still in subscribers list
                        AddListener(key, methodInfoWrapper);
                    }
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
                EventInfoWrappers[eventId].EventInfo.RemoveEventHandler(
                    EventInfoWrappers[eventId].EventInfo.AddMethod.IsStatic ? null : EventInfoWrappers[eventId].BoundObject,
                    methodInfoWrapper.Delegate);
            }
        }

        void DestroyListener(string eventId, MethodInfoWrapper listener)
        {
            EventInfoWrappers[eventId].EventInfo.RemoveEventHandler(
                EventInfoWrappers[eventId].EventInfo.AddMethod.IsStatic ? null : EventInfoWrappers[eventId].BoundObject,
                listener.Delegate);
        }

        void AddListener(string eventId, MethodInfoWrapper listener)
        {
            EventInfoWrappers[eventId].EventInfo.AddEventHandler(
                EventInfoWrappers[eventId].EventInfo.AddMethod.IsStatic ? null : EventInfoWrappers[eventId].BoundObject,
                listener.Delegate);
        }
    }
}