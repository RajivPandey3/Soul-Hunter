using System.Collections.Generic;
using UnityEngine;

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