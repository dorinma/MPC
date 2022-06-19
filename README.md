# MPC <br> User Manual

## Table of Contents
1. [Introduction](#introduction)
2. [Manual](#manual)
      * [Running The Application](#running-the-application)
      * [User Input](#user-input)
3. [Further Process](#further-process)

## Introduction
Our system enables the users to perform computation on their joint data, while keeping each user's data private, this is done by splitting the data in a shared secret form. The output of the computation is saved in the servers for further processes.

The actual data is not exposed at any point of the process, only in a shared secret form as mentioned above (the output of the computation included).

The application is composed of the following components: 
- A randomness client that runs offline.
- Two remote servers that perform the computations.
- 1-N data clients that are in charge of getting the input from the users and send it to the servers.

When the users run the application, they run the data client component.

## Manual
Running the UI application will open the following window:
<p align="center"> <img src="https://user-images.githubusercontent.com/48642477/172847647-89c159e9-42e4-41fa-a7ad-e2f7217b7f25.png" height="350"> </p>

 ### Running The Application
 To run the user application, run the MPC_UI project.
 For the computations to work properly, the servers must be already running.
 
 
 ### User Input
 In order to begin computation, the user must first insert the following data:
 
 #### Servers' details
 IP addresses and ports of both remote servers.
 
 #### Session details
 The user must choose either he wants to start a new session of computation, or join an exising one.
 
 If "New Session" is chosen the user must also insert the number of the participants in the computation process.
 Then, after pressing the "Generate" button, he will receive a session ID- which he will send to the other participants so that they will be able to join the same computation.
 
 Else, if "Join Existing" is chosen, the user only needs to insert the session ID he received from the user who started the session.
 
 #### Input file
 This file includes the data the user wants to perform the computation on. 
 
 Currently, the applicaion supports data of type unsigned int of 32 bits, and the file must be a .csv file in the following format: the first row is a header and the application will ignore it. 
 The next rows will contain the data, with a single number in each row. The numbers must all be different than each other for the algorithm to work correctly.
 
 Example to such file:
 ```yaml
---header---
100
200
300
```

#### Operation to perform
The computation the user wishes to perform on his data. Currently sorting the data is enabled.

#### Debug mode
If the user checks this field, then the application will send the shared secret output back to the user (instead of just saving it in the servers).
 

## Further Process
After the user presses the "Send" button, the servers wait untill the data from all the participants is received. Then, they will perform the specified compoutation and save the output data for further processes (create statistics, train models of machine learning etc.).
