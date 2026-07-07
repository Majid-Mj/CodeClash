using System;
using System.Linq;
using System.Reflection;
using Docker.DotNet;
using Docker.DotNet.Models;

class Program
{
    static void Main()
    {
        Console.WriteLine("--- ContainerExecInspectResponse Properties ---");
        var props = typeof(ContainerExecInspectResponse).GetProperties();
        foreach (var p in props)
        {
            Console.WriteLine($"{p.PropertyType.Name} {p.Name}");
        }
    }
}
