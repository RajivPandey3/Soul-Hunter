using System.Collections.Generic;
using UnityEngine;


    /// <summary>
    /// Learning Comment:
    /// Yeh script SimpleServiceContainer.cs ke core logic ko handle karti hai.
    /// </summary>
public class SimpleServiceContainer
{
    private Dictionary<string, object> services = new();


    public void Add(string name, object service)
    {
        services[name] = service;
    }


    public object Get(string name)
    {
        return services[name];
    }
}
