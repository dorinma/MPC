# MPC <br> Operation & Maintenance Manual

## Table of Contents
1. [Introduction](#introduction)
2. [System Components](#system-components)
      * [Randomness Client](#randomness-client)
      * [Servers](#servers)
      * [Data Client](#data-client)
3. [Running The System](#running-the-system)
4. [Maintenance](#maintenance)
     * [Extending Functionality](#extending-functionality)
     * [Error Handeling](#error-handeling)

## Introduction
Our system enables the users to perform computation on their joint data, while keeping each user's data private, this is done by splitting the data in a shared secret form. The output of the computation is saved in the servers for further processes.

The application is composed of the following components: 
- A randomness client that runs offline.
- Two remote servers that perform the computations.
- 1-N data clients that are in charge of getting the input from the users and send it to the servers.


## System Components

#### Randomness Client
Generates corrolated randomness for future input. Run the randomness client after both servers are on and connected, so it will be able to send them the masks and keys it generated.

#### Servers
These are the main components of the system, which are in charge of performing the computation and storing the output for further processes. 

There are two instances of the servers- A and B, and their ID is given as a program argument. For the system to run properly, the operator must first run server B because it awaits for server A to connect to it. 

#### Data Client
This is the component that runs when the users run the application. The operator doesn't need to run this component, unless he also takes part in the computation (sends his own data to the servers).

## Running The System
1. Run server B
2. Run server A
3. Run randomness client
4. Wait for inputs (data clients can now connect)

## Maintenance

#### Extending Functionality
The software enables users to sort data. We used different design patterns to make this part easily extendable, so that more computations will be allowed.

In order to add operations (such as merging lists, finding the i-th element), follow these steps:

1. In MPCTypes.cs add the new operation in the enum OPERATION & in the class Operation.
2. In the MPCServer project create a class that implements the Computer abstract class, which is responsible for the logic of the computation (make sure to implement the Compute method).
3. In the InitComputer method found in ManagerServer.cs, add a case matching to the new operation which will create the appropriate Computer.
4. In the MPCRandomnessClient project create a class that implements the Circuit abstract class. This class will be in charge of generating the correlated randomness for this specific computation.

#### Error Handeling
##### Data client:
If case the data client crashes before his data was sent, the servers will still wait for its data so the user can simply restart the app. 

If it crashed after sending the data, the servers will continue with the computation, and the user will not get a confirmation message. The output will still be written to the destinated directory.

In any other case, the application should be restarted.

##### Randomness client:
If the randomness client crashes or the servers do not have enough correlated randomness, they will remain in their init state- in which they either wait for data from the users or for more randomness. In case all the data has arrived and still they have not received the needed randomness, the servers will remain in this state and will send informative message to the users.

In the case the randomness has failed to arrive to a server, it will not be in correlation with the other server. Therefore, the randomness client will resend the randomness it generated to both servers.
TODO: new randomness? old? send to both or to one?
************************************************************************

##### Server:
Errors in the communication or computation may prevent the servers from proceeding with the computation. In such case, the server will catch the error and restart itself by clearing the computation data, closing the open sockets and returning to its init state. 

Once a server restarts, it closes the socket in which it communicated with the other server, so the other server will receive a communication error and also restart itself. At this point both servers are synchronized can communicate and work together properly.
