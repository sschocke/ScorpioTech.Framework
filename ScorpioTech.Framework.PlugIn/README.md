#Simple Plug In Library

##Overview
This is a library I wrote to facilitate the simple addition and management of Plug In functionality in my .NET projects. The basics are fairly simple. There are a bunch of interfaces that define the basic structure required for the plugins, and a base manager class which needs to be extended to manage a specific set of plugins.

##Interfaces
###IPlugInHost
**IPlugInHost** defines the basics that a program needs to implement to host plugins. It only has 2 methods that need to be implemented that deal primarily with error handling for your plugins called _PlugInError_ and _ThrowException_. They allow a plugin to throw an Exception or send an Error Message to your hosting application without needing to know anything about the hosting applications error handling.

###IPlugIn
**IPlugIn&lt;_PlugInHost_&gt;** is a generic interface. The _PlugInHost_ template class defines what interface/class is hosting the plugins. This can be as simple as using **IPlugInHost** directly if you don't need to make any additional host functionlity available to your plugins, or you can define a whole new interface/class inheriting from **IPlugInHost** to make a range of properties, methods and even fields available. Each instance of a plugin will then have a property called _Host_ of the class _PlugInHost_. This is also where you define the methods and properties that will be available to the host application via your plugins.

###PlugInManager
**PlugInManager&lt;_PlugInType, PlugInHost_&gt;** is a generic base class. You can either subclass this if you want to add convenience functions like calling a method on all plugins, or you can simply create an instance for the basic functionality. This class has only one method **Find(_string directory_)**. This method scans the specified directory and finds all **.dll** files that implement **PlugInType**. It then creates an instance of all classes in the **.dll** file that implement **PlugInType**, calls their **.Inititialize()** method, and adds them to the **PlugIns** list of the manager.

##Simple Example
###ISamplePlugIn.cs
```csharp
using ScorpioTech.Framework.PlugIn;

namespace PlugInHost
{
    public interface ISamplePlugIn : IPlugIn<IPlugInHost>
    {
        void SayHello();
    }
}
```
This is the basic plugin definition. In this case, we are creating an interface that specifies that these plugins will be hosted by a **IPlugInHost** host, and that all plugins must implement a method called **SayHello()**

###Program.cs
```csharp
using ScorpioTech.Framework.PlugIn;
using System;

namespace PlugInHost
{
    public class Program : IPlugInHost
    {
        static void Main(string[] args)
        {
            Program instance = new Program();
            PlugInManager<ISamplePlugIn, IPlugInHost> manager = new PlugInManager<ISamplePlugIn, IPlugInHost>(instance);

            Console.Write("Finding plugins in " + Environment.CurrentDirectory + "...");
            manager.Find(Environment.CurrentDirectory);
            Console.WriteLine("Found " + manager.PlugIns.Count + " Sample Plug Ins");

            Console.WriteLine("Asking all plug ins to say hello...");
            foreach(ISamplePlugIn plugin in manager.PlugIns)
            {
                plugin.SayHello();
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public void PlugInError(string error)
        {
            Console.WriteLine("PlugIn Error: " + error);
        }

        public void ThrowException(Exception ex)
        {
            Console.WriteLine("Unhandled PlugIn Exception: " + ex.ToString());
        }
    }
}
```
This is a short console application called **PlugInHost**. It simply finds and loads all plugins in the same directory than itself, and asks all of them to say hello.

###SamplePlugIn1.cs
```csharp
using PlugInHost;
using ScorpioTech.Framework.PlugIn;
using System;

namespace SamplePlugIn1
{
    public class SamplePlugIn1 : ISamplePlugIn
    {
        public void SayHello()
        {
            Console.WriteLine("Sample Plug In 1 saying 'Hello!'");
        }

        public string Name
        {
            get { return "SamplePlugIn1"; }
        }

        public string Author
        {
            get { return "Sebastian Schocke"; }
        }

        public string Description
        {
            get { return "Sample Plug In 1 for ScorpioTech.Framework.PlugIn demo"; }
        }

        public Version Version
        {
            get { return new Version(1,0); }
        }

        public IPlugInHost Host
        {
            get;
            private set;
        }

        public void Initialize(IPlugInHost host)
        {
            this.Host = host;
        }
    }
}
```
This is our sample plug in. This is a separate Class Library project called **SamplePlugIn1**. It references our **PlugInHost** application only because our **ISamplePlugIn** interface is defined in it. It has a couple of pretty standard properties required by IPlugIn, and a method called **Initialize()**. This method is where you would do any initial work your plugin needs to do before being usable. In our case, we are simply storing a reference to the host application.

Also we implemented the **SayHello()** method required by our **ISamplePlugIn** interface. This function can now be called from the host application.

##Conclusion
The above example is very simplistic, but demonstrates the ease of adding plugin functionality to your applications. It can be downloaded [here](http://www.geekhangar.co.za/ScorpioTech/framework/PlugInExample.zip)