# ScorpioTech Framework Libraries

This is an open source collection of libraries I have built for use in my .NET applications.
Feel free to use them, or to modify them. If you make a contribution and think it's great, feel free to send
me a pull request and I'll merge it in

Starting from the simplest:

## 1. ScorpioTech.Framework.LogServer
This simple library creates a logging target to send anything you want to log to. It then creates a listening
TCP port(default 446, but can be passed in as a parameter). Simply use your favourite telnet client to connect
to the port, and you can watch a live log of what your application is doing. Great for debugging things like
Windows Services.
_See the documentation in the folder for more details if you are interested in using it._

## 2. ScorpioTech.Framework.PlugIn
A collection of classes and interfaces to simplify having a plugin framework in your application. Simply
inherit from the IPlugInHost interface to create a host class, IPlugIn to define an interface for your
plugins, and implement a concrete instance of the PlugInManager abstract class to get automatic detection
and enumeration of all plugins present in the specified folder.
_See the documentation in the folder for more details if you are interested in using it._

## 3. ScorpioTech.Framework.netNUTClient
A library for connecting to upsd servers from the [NUT project](www.networkupstools.org). Can use this to
query or monitor UPS that you have connected to a Linux server.
_See the documentation in the folder for more details if you are interested in using it._

## 4. ScorpioTech.Framework.DBSchema
This is a project I have been working on a long time. The tl;dr version is it's a library where you define 
your database structure in an XML file, and this library will automatically ensure that your actual database
matches it. If you need to add a column, simply define it in the XML file, and when you run the app, this
library will compare the actual database with what the XML says, and make any structural changes required.
This is NOT a ORM library, or something like Entity Framework. It only deals in the structure of the database,
not any data.
_See the documentation in the folder for more details if you are interested in using it._
