using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eventer.Editor
{
    public class EventsSceneInfoWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private bool _expnadedAll = true;
        
        private static List<EventInfoWrapper> _events = new List<EventInfoWrapper>();
        private static List<MethodInfoWrapper> _listeners = new List<MethodInfoWrapper>();
        private static Dictionary<string, EventInfoWrapper> _eventsContainer = new Dictionary<string, EventInfoWrapper>();
        private static bool[] _expandedList;
        private static MethodInfoWrapper _selectedMethodInfoWrapper;
        private static GUIStyle _customButtonStyle;
        private static GUIStyle _customFoldoutStyle;
        
        [MenuItem("Window/General/Eventer #&e")]
        private static void ShowWindow()
        {
            var window = GetWindow<EventsSceneInfoWindow>();
            window.titleContent = new GUIContent("Events on scene");
            window.Show();
        }

        static void InitializeVariables()
        {
            _events = new List<EventInfoWrapper>();
            _listeners = new List<MethodInfoWrapper>();
            _eventsContainer = new Dictionary<string, EventInfoWrapper>();
            _selectedMethodInfoWrapper = null;

            var objects = Utils.GetAllGameobjectsOnScene();

            foreach (GameObject gameObject in objects)
            {
                var monoBehaviours = gameObject.GetComponents<MonoBehaviour>();

                foreach (MonoBehaviour monoBehaviour in monoBehaviours)
                {
                    _events.AddRange(Utils.GetEvents(monoBehaviour));
                    _listeners.AddRange(Utils.GetListenersNonDelagated(monoBehaviour));
                }
            }

            foreach (EventInfoWrapper eventInfoWrapper in _events)
            {
                if (_eventsContainer.ContainsKey(eventInfoWrapper.EventId)) continue;
                _eventsContainer.Add(eventInfoWrapper.EventId, eventInfoWrapper);
            }

            foreach (MethodInfoWrapper methodInfoWrapper in _listeners)
            {
                if (_eventsContainer.ContainsKey(methodInfoWrapper.EventId))
                    _eventsContainer[methodInfoWrapper.EventId].Subscribers.Add(methodInfoWrapper);

                else
                {
                    if (!_eventsContainer.ContainsKey("<Unknown event>"))
                        _eventsContainer.Add("<Unknown event>", new EventInfoWrapper() {Subscribers = new List<MethodInfoWrapper>()});
                    
                    _eventsContainer["<Unknown event>"].Subscribers.Add(methodInfoWrapper);
                }
            }

            _expandedList = new bool[_eventsContainer.Count];
            for (int i = 0; i < _expandedList.Length; i++)
            {
                _expandedList[i] = true;
            }
            
            foreach (string key in _eventsContainer.Keys)
            {
                _eventsContainer[key].Subscribers =
                    _eventsContainer[key].Subscribers.OrderBy(m => m.Order).ToList();
            }
        }

        private void OnGUI()
        {
            _customButtonStyle = GetCustomButtonStyle();
            _customFoldoutStyle = GetCustomFoldoutStyle();
            
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(_expnadedAll ? "Shrink all" : "Expand all"))
                ExpandOrShrinkInfo();
            
            if (GUILayout.Button("Verify"))
                VerifyDelegates();
            
            EditorGUILayout.EndHorizontal();

            var spacing = GetRectWithHeight(5);
            EditorGUI.DrawRect(spacing, Color.clear);
            
            _scrollPos =
                EditorGUILayout.BeginScrollView(_scrollPos);

            int k = 0;
            foreach (string key in _eventsContainer.Keys)
            {
                bool previousState = _expandedList[k];

                string destroyOnLoadEvent = _eventsContainer[key].DestroyOnLoad ? "<color=#F15952>[DestroyOnLoad]</color>" : String.Empty;
                string staticEvent = _eventsContainer[key].EventInfo.AddMethod.IsStatic ? "<color=#9D52F1>[Static]</color>" : String.Empty;
                
                _expandedList[k] =
                    EditorGUILayout.BeginFoldoutHeaderGroup(_expandedList[k], 
                        $"<color=#F1A952>{_eventsContainer[key].BoundObject}</color>.<color=#52F1A9>{_eventsContainer[key].EventInfo.Name}</color>" +
                        $" {staticEvent}" +
                        $" {destroyOnLoadEvent}", _customFoldoutStyle);
                
                if (_expandedList[k] != previousState && _expandedList[k])
                    SetSelectedObject(_eventsContainer[key].BoundObject);
                
                if (_expandedList[k])
                {
                    foreach (MethodInfoWrapper methodInfoWrapper in _eventsContainer[key].Subscribers)
                    {
                        var rect = EditorGUILayout.BeginHorizontal();

                        var prevColor = GUI.color;
                        GUI.color = Color.gray;
                        
                        if (rect.Contains(Event.current.mousePosition) || _selectedMethodInfoWrapper == methodInfoWrapper)
                        {
                            GUI.color = Color.grey * 1.1f;
                        }
                        
                        EditorGUI.DrawRect(rect, GUI.color);

                        GUI.color = prevColor;

                        string destroyOnLoadListener =
                            methodInfoWrapper.DestroyOnLoad ? "<color=#F15952>[DestroyOnLoad]</color>" : String.Empty;
                        string staticMethod = methodInfoWrapper.MethodInfo.IsStatic ? "<color=#9D52F1>[Static]</color>" : String.Empty;
                        
                        if (GUILayout.Button($"<color=#F1A952>{methodInfoWrapper.Object}</color>.<color=#9BF152>{methodInfoWrapper.MethodInfo.Name}</color>" +
                                             $" {staticMethod} {destroyOnLoadListener}",
                            _customButtonStyle, GUILayout.MaxHeight(17)))
                        {
                            SetSelectedObject(methodInfoWrapper.Object);
                            _selectedMethodInfoWrapper = methodInfoWrapper;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }

                k++;
                
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUILayout.EndScrollView();
        }

        void VerifyDelegates()
        {
            int totalChecked = 0;
            int failed = 0;
            int ignored = 0;
            
            foreach (var key in _eventsContainer.Keys)
            {
                if (key == "<Unknown event>")
                {
                    ignored += _eventsContainer[key].Subscribers.Count;
                    continue;
                }
                
                foreach (MethodInfoWrapper methodInfoWrapper in _eventsContainer[key].Subscribers)
                {
                    totalChecked++;
                    if (!Utils.VerifiyDelegateMatch(_eventsContainer[key], methodInfoWrapper))
                        failed++;
                }
            }
            
            Debug.Log($"Verified {totalChecked} listeners {failed} failed {ignored} ignored (because no event found)");
        }

        static GUIStyle GetCustomButtonStyle()
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
                {margin = {left = 0, right = 0}, padding = {left = 25}, richText = true};

            return style;
        }

        static GUIStyle GetCustomFoldoutStyle()
        {
            GUIStyle style = new GUIStyle(EditorStyles.foldoutHeader)
                {richText = true};

            return style;
        }

        Rect GetRectWithHeight(int height)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height);

            rect.height = height;

            return rect;
        }

        void ExpandOrShrinkInfo()
        {
            if (!_expnadedAll)
            {
                _expnadedAll = true;
                for (int i = 0; i < _expandedList.Length; i++)
                {
                    _expandedList[i] = true;
                }
            }
            else
            {
                _expnadedAll = false;
                for (int i = 0; i < _expandedList.Length; i++)
                {
                    _expandedList[i] = false;
                }
            }
        }

        void SetSelectedObject(Object o)
        {
            EditorGUIUtility.PingObject(o);
            Selection.activeObject = o;
        }
        
        void FreeResources()
        {
            _events = null;
            _listeners = null;
            _eventsContainer = null;
            _expandedList = null;
            _selectedMethodInfoWrapper = null;
        }

        private void OnDisable()
        {
            FreeResources();
        }

        private void OnInspectorUpdate()
        {
            this.Repaint();
        }

        // should be called when something is changed in scripts
        // also seems like editor's styles singleton will give null ref so call it in ongui instead
        private void OnEnable()
        {
            InitializeVariables();
        }
    }
}