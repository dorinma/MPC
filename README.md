# MPC <br> User Manual

## Table of Contents
1. [Introduction](#introduction)
2. [Manual](#manual)
      * [User Input](#user-input)

## Introduction
Our system enables the users to perform computation on their joint data, while keeping each user's data private.

It is composed of four components: 
- A randomness client that runs offline.
- Two remote servers that perform the computations.
- A data client that is in charge of getting the input from the users and send it to the servers.

When the users run the application, they run the data client component.

## Manual
Running the app will open the following screen:
<p align="center"> <img src="https://user-images.githubusercontent.com/48642477/172847647-89c159e9-42e4-41fa-a7ad-e2f7217b7f25.png" height="350"> </p>

 ### User Input
 In order to begin computation, the user must first insert the following data:
 
 #### Servers' details
 IP addresses and ports of both remote servers.
 
 #### Session details
 The user must choose either he wants to start a new session of computation, or join an exising one.
 
 If "New Session" is chosen the user must also insert the number of the participants in the computation proccess.
 Then, after pressing the "Generate" button, he will receive a session ID- which he will send to the other participants so that they will be able to join the same computation.
 
 Else, if "Join Existing" is chosen, the user only needs to insert the session ID he received from the user who started the session.
 
 #### Input File
 This file includes the data the user wants to perform the computation on. 
 
 Currently, the applicaion supports data of type UInt32, and the file must be a .csv file in the following format: the first row is a header and the application will ignore it. 
 The next rows will contain the data, with a single number in each row. The numbers must all be different than each other for the algorithm to work correctly.
 
 Example to such file:
 ```yaml
---header---
100
200
300
```
 
