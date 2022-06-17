# MPC <br> Operation & Maintenance Manual

## Table of Contents
1. [Introduction](#introduction)
2. [System Components](#system-components)
      * [Randomness Client](#randomness-client)
      * [Servers](#servers)
      * [Data Client](#data-client)
3. [Running The System](#running-the-system)
4. [Maintenance](#maintenance)
     * [Error Handeling](#error-handeling)
     * [Extending Functionality](#extending-functionality)

## Introduction
Our system enables the users to perform computation on their joint data, while keeping each user's data private, this is done in a shared-secret form. The output of the computation is saved in the servers for further processes.

The application is composed of following components: 
- A randomness client that runs offline.
- Two remote servers that perform the computations.
- 1-N data clients that is in charge of getting the input from the users and send it to the servers.


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

#### Error Handeling
There is a number of possible errors that may occur while running the system:

##### 1. Crash of a component
Handeling this error depends on the component that had crashed.
###### Data client:
If the data client crashed before his data was sent, the servers will still wait for its data so the user can simply restart the app. In case it crashed after sending the data, the servers will continue with the computation, and the user will not get a confirmation message. In debug mode, the output will still be written to the destinated directory.

If the randomness client crashes or the servers do not have enough correlated randomness, they will remain in their init state- in which they either wait for data from the users or for more randomness. In case all the data has arrived and still they have not recived the needed randomness, they will remain in this state.
/////////the servers send informative message to the users here?

In case one of the servers crashes and the connection between them is broken, the other server will keep trying to connect to it for a few seconds. If this ends unsuccessfully, the operator must restart the fallen server.

##### 2. Computation failure
##### 3. Communication failure

