# Example Opc client using OPC Labs client software

You will need.. 

* Opc Labs library - from http://www.opclabs.com/products/quickopc/downloads
* Kepware Opc server - from here https://www.kepware.com/products/kepserverex/

In Kepware open up kepware config.xml and update the runtime project 

In kepware OPC UA configuation make sure 127.0.0.1 endpoint uses port 49320 and no security policy (you can set it to need signing but then you must trust "AlsSampleOpcClient" in trusted clients..)
