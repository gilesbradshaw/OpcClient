# Example Opc client using OPC Labs client software

You will need.. 

* Opc Labs library - from http://www.opclabs.com/products/quickopc/downloads
* Kepware Opc server - from here https://www.kepware.com/products/kepserverex/

In Kepware open up kepware config.xml and update the runtime project 

**Nb you need the Memory based driver and AllenBradley ethernet installed to load the config**

In kepware OPC UA configuation make sure 127.0.0.1 endpoint uses port 49320 and no security policy (you can set it to need signing but then you must trust "sigopc" in trusted clients..)

## sigopc u

Reads, writes subscribes using OPC-UA

![sigopc u](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/u.PNG "sigopc u")

## sigopc d

Reads, writes subscribes using OPC-DA

![sigopc d](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/d.PNG "sigopc d")

## sigopc ux tags.xml

Reads, writes subscribes a configured set of tags using OPC-UA and Reactive Extensions

![sigopc ux tags.xml](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/ux.PNG "sigopc ux tags.xml")

## sigopc ulog tags.xml log.csv

Logs a configured set of tags using OPC-UA and Reactive Extensions

![sigopc ulog tags.xml log.csv](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/ulog.PNG "sigopc ulog tags.xml log.csv")


## sigopc dx tags.xml

Reads, writes subscribes a configured set of tags using OPC-DA and Reactive Extensions

![sigopc dx](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/dx.PNG "sigopc dx")

## sigopc uxsim

Simulation using OPC-UA (very basic)

![sigopc uxsim](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/uxsim.PNG "sigopc uxsim")




