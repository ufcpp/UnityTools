using UnityEditor;
using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class ComponentForwarderWindow : EditorWindow
{
    [MenuItem("Tools/Component Forwarding")]
    public static void ShowWindow()
    {
        var window = CreateInstance<ComponentForwarderWindow>();
        window.titleContent = new GUIContent("Component Behaviour");
        window.Show();
    }

    bool xx;

    void OnGUI()
    {
        if (xx) return;

        try
        {

            Initialize();


            var paths = AssetDatabase.GetAllAssetPaths();

            //Debug.Log("#levels " + Application.levelCount);

            //foreach (var scene in EditorBuildSettings.scenes)
            //{
            //    var a = AssetDatabase.LoadMainAssetAtPath(scene.path);
            //    Debug.Log("scene " + a);


            //}

            foreach (var item in paths.Where(p => p.Contains(".prefab")))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(item);
                Debug.Log("prefab " + go.name + ", " + go.GetType().Name);

                var transforms = go.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    Debug.Log("    obj " + t.name);

                    foreach (var c in t.gameObject.GetComponents<Component>())
                    {
                        var ct = c.GetType();
                        if (_dllComponents.Contains(ct))
                        {
                            Debug.Log("        component " + c.GetType().FullName);
                        }

                    }
                }
            }
        }
        finally
        {
            xx = true;
        }
    }

    IEnumerable<GameObject> Descendants(GameObject go)
    {
        yield return go;

        var count = go.transform.childCount;
        for (int i = 0; i < count; i++)
        {
            var child = go.transform.GetChild(i);

            foreach (var x in Descendants(child.gameObject))
                yield return x;
        }
    }

    List<Assembly> _dllAssemblies = new List<Assembly>();
    HashSet<Type> _dllComponents = new HashSet<Type>();
    List<Assembly> _scriptAssemblies = new List<Assembly>();
    HashSet<Type> _scriptComponents = new HashSet<Type>();

    void Initialize()
    {
        var asms = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var asm in asms)
        {
            var loc = asm.Location.Replace('\\', '/');
            if (loc.Contains("/Editor/")) continue;
            if (loc.Contains("-Editor")) continue;
            if (loc.Contains("/Dlls/"))
            {
                _dllAssemblies.Add(asm);
                foreach (var t in asm.GetTypes().Where(t => typeof(Component).IsAssignableFrom(t))) _dllComponents.Add(t);
            }
            else
            {
                _scriptAssemblies.Add(asm);
                foreach (var t in asm.GetTypes().Where(t => typeof(Component).IsAssignableFrom(t))) _scriptComponents.Add(t);
            }
        }
    }
}
