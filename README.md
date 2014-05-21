jrmc-vb-status-listener
=======================

A VB application that shows how to poll JRMC status and show the changes.
The main goal is to run some event handler when the play status changes.
The program uses the MCWS API which is REST/XML based.
Using this API is interesting because the listener can listen remotely.

The program needs the Application.config to be customized with the Host and Port of the JRiver Instance, and with the user and password required to access the network serives.
