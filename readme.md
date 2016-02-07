# Example Opc client using OPC Labs client software

You will need.. 

* Opc Labs library - from http://www.opclabs.com/products/quickopc/downloads
* Kepware Opc server - from here https://www.kepware.com/products/kepserverex/

In Kepware open up kepware config.xml and update the runtime project 

**Nb you need the Memory based driver and AllenBradley ethernet installed to load the config**

In kepware OPC UA configuation make sure 127.0.0.1 endpoint uses port 49320 and no security policy (you can set it to need signing but then you must trust "AlsSampleOpcClient" in trusted clients..)

## AlsSampleOpcClient u

Reads, writes subscribes using OPC-UA

![AlsSampleOpcClient u](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/u.PNG "AlsSampleOpcClient u")

## AlsSampleOpcClient d

Reads, writes subscribes using OPC-DA

![AlsSampleOpcClient d](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/d.PNG "AlsSampleOpcClient d")

## AlsSampleOpcClient ux tags.xml

Reads, writes subscribes a configured set of tags using OPC-UA and Reactive Extensions

![AlsSampleOpcClient ux tags.xml](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/ux.PNG "AlsSampleOpcClient ux tags.xml")

## AlsSampleOpcClient ulog tags.xml log.csv

Logs a configured set of tags using OPC-UA and Reactive Extensions

![AlsSampleOpcClient ulog tags.xml log.csv](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/ulog.PNG "AlsSampleOpcClient ulog tags.xml log.csv")


## AlsSampleOpcClient dx tags.xml

Reads, writes subscribes a configured set of tags using OPC-DA and Reactive Extensions

![AlsSampleOpcClient dx](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/dx.PNG "AlsSampleOpcClient dx")

## AlsSampleOpcClient uxsim

Simulation using OPC-UA (very basic)

![AlsSampleOpcClient uxsim](https://raw.githubusercontent.com/gilesbradshaw/OpcClient/master/uxsim.PNG "AlsSampleOpcClient uxsim")




